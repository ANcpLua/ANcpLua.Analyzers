using ANcpLua.Analyzers.Core;

namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0020-AL0024: Form binding analyzers for ASP.NET Core Minimal APIs.
///     Validates [FromForm], [FromBody], IFormCollection, and IFormFile usage patterns.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AL0020ToAL0024FormBindingAnalyzer : ALAnalyzer {
    private const string FromFormAttribute = "Microsoft.AspNetCore.Mvc.FromFormAttribute";
    private const string FromBodyAttribute = "Microsoft.AspNetCore.Mvc.FromBodyAttribute";
    private const string IFormCollection = "Microsoft.AspNetCore.Http.IFormCollection";
    private const string IFormFile = "Microsoft.AspNetCore.Http.IFormFile";
    private const string IFormFileCollection = "Microsoft.AspNetCore.Http.IFormFileCollection";

    private static readonly DiagnosticDescriptor RuleAL0020 = CreateRule(
        DiagnosticIds.FormCollectionRequiresExplicitAttribute, "AL0020");

    private static readonly DiagnosticDescriptor RuleAL0021 = CreateRule(
        DiagnosticIds.MultipleStructuredFormSources, "AL0021");

    private static readonly DiagnosticDescriptor RuleAL0022 = CreateRule(
        DiagnosticIds.MixedFormCollectionAndDto, "AL0022");

    private static readonly DiagnosticDescriptor RuleAL0023 = CreateRule(
        DiagnosticIds.UnsupportedFormType, "AL0023");

    private static readonly DiagnosticDescriptor RuleAL0024 = CreateRule(
        DiagnosticIds.FormAndBodyConflict, "AL0024");

    private static DiagnosticDescriptor CreateRule(string id, string ruleNumber) => new(
        id,
        new LocalizableResourceString($"{ruleNumber}AnalyzerTitle", Resources.ResourceManager, typeof(Resources)),
        new LocalizableResourceString($"{ruleNumber}AnalyzerMessageFormat", Resources.ResourceManager, typeof(Resources)),
        DiagnosticCategories.AspNetCore,
        DiagnosticSeverity.Error,
        true,
        new LocalizableResourceString($"{ruleNumber}AnalyzerDescription", Resources.ResourceManager, typeof(Resources)),
        HelpLinkBase + $"rules/{ruleNumber}.md");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [RuleAL0020, RuleAL0021, RuleAL0022, RuleAL0023, RuleAL0024];

    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterSymbolAction(AnalyzeMethod, SymbolKind.Method);

    private static void AnalyzeMethod(SymbolAnalysisContext context) {
        if (context.Symbol is not IMethodSymbol method) {
            return;
        }

        // Skip methods without parameters
        if (method.Parameters.IsEmpty) {
            return;
        }

        // Collect parameter classifications
        var hasFromBody = false;
        var hasFromForm = false;
        var fromFormDtoCount = 0;
        var hasFromFormCollection = false;
        var hasFromFormDto = false;

        IParameterSymbol? fromBodyParam = null;
        IParameterSymbol? firstFromFormParam = null;

        foreach (var param in method.Parameters) {
            var typeFqn = GetFullTypeName(param.Type);
            var hasFromFormAttr = HasAttribute(param, FromFormAttribute);
            var hasFromBodyAttr = HasAttribute(param, FromBodyAttribute);
            var isFormCollection = typeFqn == IFormCollection;
            var isFormFile = typeFqn == IFormFile;
            var isFormFileCollection = typeFqn == IFormFileCollection;

            // AL0024: Check for [FromBody]
            if (hasFromBodyAttr) {
                hasFromBody = true;
                fromBodyParam = param;
            }

            // AL0020: IFormCollection without [FromForm]
            if (isFormCollection && !hasFromFormAttr) {
                context.ReportDiagnostic(Diagnostic.Create(
                    RuleAL0020,
                    param.Locations.FirstOrDefault() ?? Location.None,
                    param.Name,
                    method.Name));
            }

            // Track [FromForm] parameters
            if (hasFromFormAttr) {
                hasFromForm = true;
                firstFromFormParam ??= param;

                if (isFormCollection) {
                    hasFromFormCollection = true;
                    fromFormDtoCount++;
                } else if (!IsPrimitive(param.Type) && !isFormFile && !isFormFileCollection) {
                    // It's a DTO (non-primitive, non-file)
                    hasFromFormDto = true;
                    fromFormDtoCount++;

                    // AL0023: Check if DTO is form-bindable
                    var reason = GetUnsupportedFormTypeReason(param.Type);
                    if (reason is not null) {
                        context.ReportDiagnostic(Diagnostic.Create(
                            RuleAL0023,
                            param.Locations.FirstOrDefault() ?? Location.None,
                            param.Name,
                            method.Name,
                            reason));
                    }
                }
            }
        }

        // AL0024: Form and Body conflict
        if (hasFromBody && hasFromForm) {
            context.ReportDiagnostic(Diagnostic.Create(
                RuleAL0024,
                fromBodyParam?.Locations.FirstOrDefault() ?? method.Locations.FirstOrDefault() ?? Location.None,
                method.Name));
        }

        // AL0021: Multiple structured form sources
        if (fromFormDtoCount > 1) {
            context.ReportDiagnostic(Diagnostic.Create(
                RuleAL0021,
                firstFromFormParam?.Locations.FirstOrDefault() ?? method.Locations.FirstOrDefault() ?? Location.None,
                method.Name));
        }

        // AL0022: Mixed IFormCollection with DTO
        if (hasFromFormCollection && hasFromFormDto) {
            context.ReportDiagnostic(Diagnostic.Create(
                RuleAL0022,
                firstFromFormParam?.Locations.FirstOrDefault() ?? method.Locations.FirstOrDefault() ?? Location.None,
                method.Name));
        }
    }

    private static bool HasAttribute(IParameterSymbol param, string attributeFullName) =>
        param.GetAttributes().Any(attr =>
            attr.AttributeClass is not null && GetFullTypeName(attr.AttributeClass) == attributeFullName);

    private static string GetFullTypeName(ITypeSymbol type) =>
        type.ContainingNamespace is null || type.ContainingNamespace.IsGlobalNamespace
            ? type.Name
            : $"{type.ContainingNamespace.ToDisplayString()}.{type.Name}";

    private static bool IsPrimitive(ITypeSymbol type) {
        // Handle nullable types
        if (type is INamedTypeSymbol { IsGenericType: true } namedType &&
            namedType.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T) {
            type = namedType.TypeArguments[0];
        }

        return type.SpecialType is
            SpecialType.System_Boolean or SpecialType.System_Byte or SpecialType.System_SByte or
            SpecialType.System_Int16 or SpecialType.System_UInt16 or SpecialType.System_Int32 or
            SpecialType.System_UInt32 or SpecialType.System_Int64 or SpecialType.System_UInt64 or
            SpecialType.System_Single or SpecialType.System_Double or SpecialType.System_Decimal or
            SpecialType.System_Char or SpecialType.System_String or SpecialType.System_DateTime
            || GetFullTypeName(type) is "System.Guid" or "System.TimeSpan" or "System.DateTimeOffset"
                or "System.DateOnly" or "System.TimeOnly" or "System.Uri";
    }

    private static string? GetUnsupportedFormTypeReason(ITypeSymbol type) {
        if (type is not INamedTypeSymbol namedType) {
            return "type is not a named type";
        }

        if (namedType.TypeKind == TypeKind.Interface) {
            return "interfaces cannot be form-bound";
        }

        if (namedType.IsAbstract) {
            return "abstract types cannot be form-bound";
        }

        var publicConstructors = namedType.InstanceConstructors
            .Where(c => c.DeclaredAccessibility == Accessibility.Public)
            .ToList();

        if (publicConstructors.Count == 0) {
            return "no public constructor available";
        }

        var hasValidConstructor = publicConstructors.Exists(ctor =>
            ctor.Parameters.IsEmpty || ctor.Parameters.All(p => IsPrimitive(p.Type)));

        return hasValidConstructor ? null : "no suitable constructor (needs parameterless or all-primitive parameters)";
    }
}
