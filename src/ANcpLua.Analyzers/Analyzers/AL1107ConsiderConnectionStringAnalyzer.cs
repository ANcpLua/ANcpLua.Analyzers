
namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL1107: Consider using configuration for connection strings.
///     Reports hardcoded connection strings passed to database/cache client constructors.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al1107ConsiderConnectionStringAnalyzer : AlAnalyzer {
    /// <summary>Connection string prefixes that indicate a hardcoded connection string (case-insensitive).</summary>
    private static readonly string[] s_connectionStringPrefixes = [
        "Server=",
        "Host=",
        "Data Source=",
        "mongodb://",
        "mongodb+srv://",
        "redis://",
        "rediss://",
        "amqp://",
        "amqps://",
        "postgres://",
        "postgresql://",
        "mysql://",
        "sqlserver://",
        "mssql://"
    ];

    /// <summary>Type names that typically accept connection strings in constructors.</summary>
    private static readonly string[] s_connectionClientTypes = [
        "NpgsqlConnection",
        "NpgsqlDataSource",
        "NpgsqlDataSourceBuilder",
        "SqlConnection",
        "MySqlConnection",
        "MongoClient",
        "ConnectionMultiplexer",
        "IConnectionMultiplexer",
        "RabbitMQ.Client.ConnectionFactory",
        "ConnectionFactory",
        "DbConnection",
        "SqliteConnection",
        "OracleConnection"
    ];

    /// <summary>The diagnostic identifier for AL1107.</summary>
    private const string DiagnosticId = "AL1107";

    private static readonly DiagnosticDescriptor s_rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.Configuration,
        DiagnosticSeverities.HiddenByDefault);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_rule];

    /// <summary>Registers operation action to analyze object creation and invocation operations.</summary>
    protected override void RegisterActions(AnalysisContext context) {
        context.RegisterOperationAction(AnalyzeObjectCreation, OperationKind.ObjectCreation);
        context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
    }

    private static void AnalyzeObjectCreation(OperationAnalysisContext context) {
        if (context.Operation is not IObjectCreationOperation { Type: INamedTypeSymbol type } creation
            || !IsConnectionClientType(type)) {
            return;
        }

        foreach (var argument in creation.Arguments) {
            if (IsHardcodedConnectionString(argument.Value)) {
                context.ReportDiagnostic(s_rule, argument.Syntax.GetLocation());
                return;
            }
        }
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context) {
        if (context.Operation is not IInvocationOperation invocation
            || !IsConnectionFactoryMethod(invocation.TargetMethod)) {
            return;
        }

        foreach (var argument in invocation.Arguments) {
            if (IsHardcodedConnectionString(argument.Value)) {
                context.ReportDiagnostic(s_rule, argument.Syntax.GetLocation());
                return;
            }
        }
    }

    private static bool IsConnectionClientType(INamedTypeSymbol type) {
        var typeName = type.Name;
        foreach (var clientType in s_connectionClientTypes) {
            if (typeName.EqualsOrdinal(clientType)) {
                return true;
            }

            // Fully-qualified match (e.g., Npgsql.NpgsqlConnection)
            if (clientType.ContainsOrdinal(".") && type.ToDisplayString().EndsWithOrdinal(clientType)) {
                return true;
            }
        }
        return false;
    }

    private static bool IsConnectionFactoryMethod(IMethodSymbol method) =>
        method switch {
            { Name: "Connect" or "ConnectAsync", ContainingType.Name: "ConnectionMultiplexer" } => true,
            { Name: "Create", ContainingType.Name: "NpgsqlDataSource" } => true,
            { Name: "Open" or "OpenAsync", ContainingType: INamedTypeSymbol ct } => IsConnectionClientType(ct),
            _ => false
        };

    private static bool IsHardcodedConnectionString(IOperation? operation) {
        if (operation is null) {
            return false;
        }

        var unwrapped = operation.UnwrapAllConversions();

        switch (unwrapped) {
            case ILiteralOperation { ConstantValue: { HasValue: true, Value: string stringValue } }:
                return LooksLikeConnectionString(stringValue);

            case IInterpolatedStringOperation interpolated: {
                var constantParts = new System.Text.StringBuilder();
                foreach (var part in interpolated.Parts) {
                    if (part is IInterpolatedStringTextOperation { Text: ILiteralOperation { ConstantValue.Value: string text } }) {
                        constantParts.Append(text);
                    }
                }
                return LooksLikeConnectionString(constantParts.ToString());
            }

            case IBinaryOperation { OperatorKind: BinaryOperatorKind.Add } binary:
                return IsHardcodedConnectionString(binary.LeftOperand)
                    || IsHardcodedConnectionString(binary.RightOperand);

            default:
                return false;
        }
    }

    private static bool LooksLikeConnectionString(string value) {
        if (string.IsNullOrWhiteSpace(value)) {
            return false;
        }

        foreach (var prefix in s_connectionStringPrefixes) {
            if (value.StartsWithIgnoreCase(prefix)) {
                return true;
            }
        }

        return false;
    }
}
