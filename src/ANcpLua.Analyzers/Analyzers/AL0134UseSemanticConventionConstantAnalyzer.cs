namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0134: Detects hardcoded OpenTelemetry semantic convention attribute names used as
///     string literals in telemetry calls, and suggests the typed constant from the
///     OpenTelemetry.SemanticConventions package.
/// </summary>
/// <remarks>
///     <para>
///         The catalog of "string literal → typed constant" mappings is built at compilation
///         start by resolving known anchor types in the consumer's compilation (via
///         <see cref="Compilation.GetTypeByMetadataName"/>) and enumerating their public
///         <c>const string</c> members. If the consumer does not reference
///         <c>OpenTelemetry.SemanticConventions</c>, the analyzer is a no-op.
///     </para>
///     <para>
///         The diagnostic fires only inside a telemetry call site (e.g.
///         <c>activity.SetTag("http.request.method", ...)</c>, <c>TagList.Add</c>, dictionary
///         initializer on a <c>KeyValuePair&lt;string, object&gt;[]</c>). Generated files,
///         test methods, nameof arguments, and const declarations are skipped.
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0134UseSemanticConventionConstantAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL0134.</summary>
    public const string DiagnosticId = "AL0134";

    private static readonly DiagnosticDescriptor Rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.OpenTelemetry,
        DiagnosticSeverity.Warning);

    /// <summary>Property key carrying the suggested typed constant reference for the code fix.</summary>
    public const string ConstantPropertyKey = "Constant";

    /// <summary>
    ///     Known anchor types whose public <c>const string</c> fields represent OTel
    ///     semantic convention attribute name constants. Each anchor is resolved per
    ///     compilation; missing anchors are silently skipped.
    /// </summary>
    private static readonly string[] CatalogAnchors = [
        // Legacy aggregator type (shipped since v1.0.0)
        "OpenTelemetry.SemanticConventions.SemanticConventions",
        "OpenTelemetry.Trace.TraceSemanticConventions",
        "OpenTelemetry.Resource.ResourceSemanticConventions",

        // Grouped domain attribute classes (v1.11+)
        "OpenTelemetry.SemanticConventions.Attributes.ClientAttributes",
        "OpenTelemetry.SemanticConventions.Attributes.CloudAttributes",
        "OpenTelemetry.SemanticConventions.Attributes.CodeAttributes",
        "OpenTelemetry.SemanticConventions.Attributes.ContainerAttributes",
        "OpenTelemetry.SemanticConventions.Attributes.DbAttributes",
        "OpenTelemetry.SemanticConventions.Attributes.DeploymentAttributes",
        "OpenTelemetry.SemanticConventions.Attributes.ErrorAttributes",
        "OpenTelemetry.SemanticConventions.Attributes.ExceptionAttributes",
        "OpenTelemetry.SemanticConventions.Attributes.FaasAttributes",
        "OpenTelemetry.SemanticConventions.Attributes.FeatureFlagAttributes",
        "OpenTelemetry.SemanticConventions.Attributes.GenAiAttributes",
        "OpenTelemetry.SemanticConventions.Attributes.HostAttributes",
        "OpenTelemetry.SemanticConventions.Attributes.HttpAttributes",
        "OpenTelemetry.SemanticConventions.Attributes.K8sAttributes",
        "OpenTelemetry.SemanticConventions.Attributes.MessagingAttributes",
        "OpenTelemetry.SemanticConventions.Attributes.NetworkAttributes",
        "OpenTelemetry.SemanticConventions.Attributes.OsAttributes",
        "OpenTelemetry.SemanticConventions.Attributes.OtelAttributes",
        "OpenTelemetry.SemanticConventions.Attributes.ProcessAttributes",
        "OpenTelemetry.SemanticConventions.Attributes.RpcAttributes",
        "OpenTelemetry.SemanticConventions.Attributes.ServerAttributes",
        "OpenTelemetry.SemanticConventions.Attributes.ServiceAttributes",
        "OpenTelemetry.SemanticConventions.Attributes.TelemetryAttributes",
        "OpenTelemetry.SemanticConventions.Attributes.ThreadAttributes",
        "OpenTelemetry.SemanticConventions.Attributes.UrlAttributes",
        "OpenTelemetry.SemanticConventions.Attributes.UserAgentAttributes",
        "OpenTelemetry.SemanticConventions.Attributes.UserAttributes"
    ];

    private static readonly HashSet<string> TelemetryMethodNames = new(StringComparer.Ordinal) {
        "SetTag", "AddTag", "SetAttribute", "AddAttribute", "SetCustomProperty"
    };

    private static readonly HashSet<string> TestAttributeNames = new(StringComparer.Ordinal) {
        "Fact", "FactAttribute",
        "Theory", "TheoryAttribute",
        "Test", "TestAttribute",
        "TestMethod", "TestMethodAttribute",
        "TestClass", "TestClassAttribute",
        "InlineData", "InlineDataAttribute"
    };

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    /// <summary>Registers a compilation-start action that resolves the per-compilation catalog.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterCompilationStartAction(OnCompilationStart);

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        var catalog = BuildCatalog(context.Compilation);
        if (catalog.Count is 0) {
            return;
        }

        context.RegisterSyntaxNodeAction(
            ctx => Analyze(ctx, catalog),
            SyntaxKind.StringLiteralExpression);
    }

    private static ImmutableDictionary<string, string> BuildCatalog(Compilation compilation) {
        var builder = ImmutableDictionary.CreateBuilder<string, string>(StringComparer.Ordinal);

        foreach (var anchor in CatalogAnchors) {
            if (compilation.GetTypeByMetadataName(anchor) is not { } type) {
                continue;
            }

            AddConstantsFrom(type, builder);
        }

        return builder.ToImmutable();
    }

    private static void AddConstantsFrom(INamedTypeSymbol type, ImmutableDictionary<string, string>.Builder builder) {
        foreach (var member in type.GetMembers()) {
            if (member is not IFieldSymbol {
                    IsConst: true,
                    DeclaredAccessibility: Accessibility.Public,
                    ConstantValue: string value
                } field
                || string.IsNullOrEmpty(value)
                || builder.ContainsKey(value)) {
                continue;
            }

            builder[value] = $"{type.Name}.{field.Name}";
        }
    }

    private static void Analyze(SyntaxNodeAnalysisContext context, ImmutableDictionary<string, string> catalog) {
        var literal = (LiteralExpressionSyntax)context.Node;
        var value = literal.Token.ValueText;

        if (string.IsNullOrEmpty(value) || !catalog.TryGetValue(value, out var qualified)) {
            return;
        }

        if (IsInGeneratedFile(literal.SyntaxTree)
            || IsInsideConstantDeclaration(literal)
            || IsInsideNameof(literal)
            || IsInTestContext(literal)
            || !IsInTelemetryContext(literal)) {
            return;
        }

        var properties = ImmutableDictionary<string, string?>.Empty.Add(ConstantPropertyKey, qualified);
        context.ReportDiagnostic(Diagnostic.Create(Rule, literal.GetLocation(), properties, qualified, value));
    }

    private static bool IsInGeneratedFile(SyntaxTree tree) {
        var path = tree.FilePath;
        if (path.EndsWithOrdinal(".g.cs")
            || path.EndsWithOrdinal(".g.i.cs")
            || path.EndsWithOrdinal(".Designer.cs")
            || path.EndsWithOrdinal(".generated.cs")) {
            return true;
        }

        if (tree.GetRoot() is not CompilationUnitSyntax unit) {
            return false;
        }

        var header = unit.GetLeadingTrivia().ToFullString();
        return header.ContainsOrdinal("<auto-generated") || header.ContainsOrdinal("<autogenerated");
    }

    private static bool IsInTestContext(SyntaxNode node) {
        for (var current = node.Parent; current is not null; current = current.Parent) {
            if (current is MemberDeclarationSyntax member && HasTestAttribute(member)) {
                return true;
            }
        }

        return false;
    }

    private static bool HasTestAttribute(MemberDeclarationSyntax member) {
        foreach (var list in member.AttributeLists) {
            foreach (var attr in list.Attributes) {
                if (GetAttributeName(attr) is { } name && TestAttributeNames.Contains(name)) {
                    return true;
                }
            }
        }

        return false;
    }

    private static string? GetAttributeName(AttributeSyntax attr) => attr.Name switch {
        IdentifierNameSyntax id => id.Identifier.Text,
        QualifiedNameSyntax q => q.Right.Identifier.Text,
        _ => null
    };

    private static bool IsInTelemetryContext(SyntaxNode node) {
        for (var current = node.Parent; current is not null; current = current.Parent) {
            switch (current) {
                case InvocationExpressionSyntax invocation when IsTelemetryMethod(invocation):
                case ElementAccessExpressionSyntax element when IsLikelyTelemetryIdentifier(element.Expression):
                case InitializerExpressionSyntax init when IsTelemetryInitializer(init):
                case AssignmentExpressionSyntax { Parent: InitializerExpressionSyntax inner } when IsTelemetryInitializer(inner):
                    return true;
            }
        }

        return false;
    }

    private static bool IsTelemetryMethod(InvocationExpressionSyntax invocation) {
        if (invocation.Expression switch {
                MemberAccessExpressionSyntax m => m.Name.Identifier.Text,
                IdentifierNameSyntax id => id.Identifier.Text,
                _ => null
            } is not { } name) {
            return false;
        }

        if (TelemetryMethodNames.Contains(name)) {
            return true;
        }

        return name.EqualsOrdinal("Add")
            && invocation.Expression is MemberAccessExpressionSyntax member
            && IsLikelyTelemetryIdentifier(member.Expression);
    }

    private static bool IsLikelyTelemetryIdentifier(ExpressionSyntax expr) {
        var name = expr switch {
            IdentifierNameSyntax id => id.Identifier.Text,
            MemberAccessExpressionSyntax m => m.Name.Identifier.Text,
            _ => null
        };

        return name is not null
            && (name.ContainsIgnoreCase("TAG")
                || name.ContainsIgnoreCase("ATTRIBUTE")
                || name.ContainsIgnoreCase("ATTR"));
    }

    private static bool IsTelemetryInitializer(InitializerExpressionSyntax init) {
        if (init.Parent is not ObjectCreationExpressionSyntax creation) {
            return false;
        }

        var typeName = creation.Type.ToString();
        return typeName.ContainsOrdinal("Tag")
            || typeName.ContainsOrdinal("Attribute")
            || typeName.ContainsOrdinal("KeyValuePair");
    }

    private static bool IsInsideConstantDeclaration(SyntaxNode node) {
        for (var current = node.Parent; current is not null; current = current.Parent) {
            switch (current) {
                case FieldDeclarationSyntax field when field.Modifiers.Any(SyntaxKind.ConstKeyword):
                case LocalDeclarationStatementSyntax local when local.Modifiers.Any(SyntaxKind.ConstKeyword):
                    return true;
            }
        }

        return false;
    }

    private static bool IsInsideNameof(SyntaxNode node) {
        for (var current = node.Parent; current is not null; current = current.Parent) {
            if (current is InvocationExpressionSyntax { Expression: IdentifierNameSyntax id }
                && id.Identifier.Text.EqualsOrdinal("nameof")) {
                return true;
            }
        }

        return false;
    }
}
