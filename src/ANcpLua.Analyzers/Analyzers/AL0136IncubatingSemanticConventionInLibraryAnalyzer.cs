namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0136: Flags usage of incubating OpenTelemetry semantic-convention members from
///     within instrumentation-library projects, where drift across minor versions would
///     cascade to downstream consumers.
/// </summary>
/// <remarks>
///     <para>
///         The <c>OpenTelemetry.SemanticConventions.Incubating</c> namespace carries
///         members that may change name or value between minor package releases. Baking
///         a direct reference into a library forces every consumer onto that exact
///         version. The recommended pattern is to copy the constant into the library's
///         own file so the public surface area stays stable.
///     </para>
///     <para>
///         Skip conditions: executable projects (they own their own dependency tree),
///         test projects (no transitive consumers), and compilations without any
///         incubating symbol.
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0136IncubatingSemanticConventionInLibraryAnalyzer : AlAnalyzer {
    private const string DiagnosticId = "AL0136";

    private static readonly DiagnosticDescriptor s_rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.OpenTelemetry,
        DiagnosticSeverity.Warning);

    private const string IncubatingNamespaceFragment = ".Incubating";

    private static readonly string[] s_incubatingRootNamespaces = [
        "OpenTelemetry.SemanticConventions",
        "OpenTelemetry.SemConv"
    ];

    private static readonly string[] s_testAssemblyAttributes = [
        "Xunit.TestFrameworkAttribute",
        "Xunit.Sdk.TestFrameworkAttribute",
        "Microsoft.VisualStudio.TestTools.UnitTesting.TestClassAttribute"
    ];

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_rule];

    /// <summary>Registers a compilation-start action that gates analysis on project shape.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterCompilationStartAction(OnCompilationStart);

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        if (!IsLibraryProject(context.Compilation)) {
            return;
        }

        context.RegisterOperationAction(AnalyzeMemberReference,
            OperationKind.FieldReference,
            OperationKind.PropertyReference,
            OperationKind.MethodReference,
            OperationKind.Invocation);
    }

    private static void AnalyzeMemberReference(OperationAnalysisContext context) {
        var containingType = context.Operation switch {
            IFieldReferenceOperation field => field.Field.ContainingType,
            IPropertyReferenceOperation property => property.Property.ContainingType,
            IMethodReferenceOperation methodRef => methodRef.Method.ContainingType,
            IInvocationOperation invocation => invocation.TargetMethod.ContainingType,
            _ => null
        };

        if (containingType is null || !IsIncubatingSemConvType(containingType)) {
            return;
        }

        if (IsInsideLocalCopy(context.Operation.Syntax)) {
            return;
        }

        var memberName = context.Operation switch {
            IFieldReferenceOperation field => field.Field.Name,
            IPropertyReferenceOperation property => property.Property.Name,
            IMethodReferenceOperation methodRef => methodRef.Method.Name,
            IInvocationOperation invocation => invocation.TargetMethod.Name,
            _ => "?"
        };

        context.ReportDiagnostic(
            s_rule,
            context.Operation.Syntax.GetLocation(),
            $"{containingType.ToDisplayString()}.{memberName}");
    }

    private static bool IsLibraryProject(Compilation compilation) {
        if (compilation.Options.OutputKind is OutputKind.ConsoleApplication or OutputKind.WindowsApplication) {
            return false;
        }

        if (IsTestAssembly(compilation)) {
            return false;
        }

        return true;
    }

    private static bool IsTestAssembly(Compilation compilation) {
        var name = compilation.AssemblyName;
        if (name is not null
            && (name.EndsWithOrdinal(".Tests") || name.EndsWithOrdinal(".Test") || name.ContainsOrdinal(".Tests."))) {
            return true;
        }

        foreach (var attributeTypeName in s_testAssemblyAttributes) {
            if (compilation.GetTypeByMetadataName(attributeTypeName) is not null) {
                return true;
            }
        }

        return false;
    }

    private static bool IsIncubatingSemConvType(INamedTypeSymbol type) {
        if (type.ContainingNamespace?.ToDisplayString() is not { } ns) {
            return false;
        }

        if (!ns.ContainsOrdinal(IncubatingNamespaceFragment)) {
            return false;
        }

        foreach (var root in s_incubatingRootNamespaces) {
            if (ns.StartsWithOrdinal(root)) {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    ///     Suppresses the diagnostic when the usage sits inside a file that declares a
    ///     local <c>const string</c> copy of the convention — the recommended mitigation.
    /// </summary>
    private static bool IsInsideLocalCopy(SyntaxNode node) {
        for (var current = node.Parent; current is not null; current = current.Parent) {
            if (current is not FieldDeclarationSyntax field) {
                continue;
            }

            if (field.Modifiers.Any(SyntaxKind.ConstKeyword)) {
                return true;
            }
        }

        return false;
    }
}
