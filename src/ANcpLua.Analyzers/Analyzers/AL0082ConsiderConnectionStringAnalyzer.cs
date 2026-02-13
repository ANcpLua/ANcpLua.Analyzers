using ANcpLua.Analyzers.Core;
using ANcpLua.Roslyn.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using System.Collections.Immutable;

namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0082: Consider using configuration for connection strings.
///     Reports hardcoded connection strings passed to database/cache client constructors.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0082ConsiderConnectionStringAnalyzer : AlAnalyzer {
    /// <summary>Connection string prefixes that indicate a hardcoded connection string (case-insensitive).</summary>
    private static readonly string[] ConnectionStringPrefixes = [
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
    private static readonly string[] ConnectionClientTypes = [
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

    private static readonly DiagnosticDescriptor Rule = CreateRule(
        DiagnosticIds.ConsiderConnectionString,
        DiagnosticCategories.Configuration,
        DiagnosticSeverities.HiddenByDefault);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    /// <summary>Registers operation action to analyze object creation and invocation operations.</summary>
    protected override void RegisterActions(AnalysisContext context) {
        context.RegisterOperationAction(AnalyzeObjectCreation, OperationKind.ObjectCreation);
        context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
    }

    private static void AnalyzeObjectCreation(OperationAnalysisContext context) {
        if (context.Operation is not IObjectCreationOperation creation) {
            return;
        }

        if (creation.Type is not INamedTypeSymbol type) {
            return;
        }

        // Check if creating a known connection client type
        if (!IsConnectionClientType(type)) {
            return;
        }

        // Check constructor arguments for hardcoded connection strings
        foreach (var argument in creation.Arguments) {
            if (IsHardcodedConnectionString(argument.Value)) {
                context.ReportDiagnostic(Rule, argument.Syntax.GetLocation());
                return;
            }
        }
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context) {
        if (context.Operation is not IInvocationOperation invocation) {
            return;
        }

        var method = invocation.TargetMethod;

        // Check for factory methods that accept connection strings
        // e.g., ConnectionMultiplexer.Connect(connectionString)
        // e.g., MongoClient.Create(connectionString)
        if (!IsConnectionFactoryMethod(method)) {
            return;
        }

        // Check method arguments for hardcoded connection strings
        foreach (var argument in invocation.Arguments) {
            if (IsHardcodedConnectionString(argument.Value)) {
                context.ReportDiagnostic(Rule, argument.Syntax.GetLocation());
                return;
            }
        }
    }

    private static bool IsConnectionClientType(INamedTypeSymbol type) {
        var typeName = type.Name;
        foreach (var clientType in ConnectionClientTypes) {
            if (typeName.EqualsOrdinal(clientType)) {
                return true;
            }
            // Also check if it ends with the client type (e.g., Npgsql.NpgsqlConnection)
            if (clientType.ContainsOrdinal(".") && type.ToDisplayString().EndsWithOrdinal(clientType)) {
                return true;
            }
        }
        return false;
    }

    private static bool IsConnectionFactoryMethod(IMethodSymbol method) {
        // Check for common factory methods
        var methodName = method.Name;

        // ConnectionMultiplexer.Connect, ConnectionMultiplexer.ConnectAsync
        if ((methodName.EqualsOrdinal("Connect") || methodName.EqualsOrdinal("ConnectAsync")) &&
            method.ContainingType?.Name.EqualsOrdinal("ConnectionMultiplexer") == true) {
            return true;
        }

        // NpgsqlDataSource.Create
        if (methodName.EqualsOrdinal("Create") &&
            method.ContainingType?.Name.EqualsOrdinal("NpgsqlDataSource") == true) {
            return true;
        }

        // Check for Open/OpenAsync on connection types with string parameter
        if ((methodName.EqualsOrdinal("Open") || methodName.EqualsOrdinal("OpenAsync")) &&
            method.ContainingType is { } containingType &&
            IsConnectionClientType(containingType)) {
            return true;
        }

        return false;
    }

    private static bool IsHardcodedConnectionString(IOperation? operation) {
        if (operation is null) {
            return false;
        }

        // Unwrap conversions
        var unwrapped = operation.UnwrapAllConversions();

        // Check for literal string
        if (unwrapped is ILiteralOperation literal &&
            literal.ConstantValue.HasValue &&
            literal.ConstantValue.Value is string stringValue) {
            return LooksLikeConnectionString(stringValue);
        }

        // Check for interpolated string with connection string-like content
        if (unwrapped is IInterpolatedStringOperation interpolated) {
            // Build the constant parts to check for connection string patterns
            var constantParts = new System.Text.StringBuilder();
            foreach (var part in interpolated.Parts) {
                if (part is IInterpolatedStringTextOperation textPart &&
                    textPart.Text is ILiteralOperation textLiteral &&
                    textLiteral.ConstantValue.Value is string text) {
                    constantParts.Append(text);
                }
            }
            return LooksLikeConnectionString(constantParts.ToString());
        }

        // Check for string concatenation with connection string-like content
        if (unwrapped is IBinaryOperation binary &&
            binary.OperatorKind == BinaryOperatorKind.Add) {
            // Check if either operand is a connection string prefix
            return IsHardcodedConnectionString(binary.LeftOperand) ||
                   IsHardcodedConnectionString(binary.RightOperand);
        }

        return false;
    }

    private static bool LooksLikeConnectionString(string value) {
        if (string.IsNullOrWhiteSpace(value)) {
            return false;
        }

        foreach (var prefix in ConnectionStringPrefixes) {
            if (value.StartsWithIgnoreCase(prefix)) {
                return true;
            }
        }

        return false;
    }
}
