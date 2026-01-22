using ANcpLua.Analyzers.Core;

namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0020-AL0024: Form binding analyzers for ASP.NET Core Minimal APIs.
///     Validates [FromForm], [FromBody], IFormCollection, and IFormFile usage patterns.
/// </summary>
/// <remarks>
///     <para>
///         ASP.NET Core Minimal APIs have specific constraints on form binding that differ
///         from MVC controllers. Violations of these constraints cause runtime exceptions
///         that are difficult to debug. This analyzer catches common mistakes at compile time.
///     </para>
///     <para>
///         AL0020: <c>IFormCollection</c> parameters require explicit <c>[FromForm]</c> attribute.
///         AL0021: Multiple structured form sources (DTOs or form collections with <c>[FromForm]</c>)
///         conflict with each other. AL0022: Mixing <c>IFormCollection</c> and DTO binding is
///         unsupported. AL0023: Abstract types, interfaces, and types without suitable constructors
///         cannot be form-bound. AL0024: <c>[FromForm]</c> and <c>[FromBody]</c> on the same
///         method conflict.
///     </para>
///     <para>
///         The analyzer does not flag primitive parameters with <c>[FromForm]</c> or
///         <c>IFormFile</c>/<c>IFormFileCollection</c> parameters, as these have special
///         handling in the framework.
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0020ToAl0024FormBindingAnalyzer : AlAnalyzer {
    private static readonly DiagnosticDescriptor RuleAl0020 = CreateRule(
        DiagnosticIds.FormCollectionRequiresExplicitAttribute, "AL0020");

    private static readonly DiagnosticDescriptor RuleAl0021 = CreateRule(
        DiagnosticIds.MultipleStructuredFormSources, "AL0021");

    private static readonly DiagnosticDescriptor RuleAl0022 = CreateRule(
        DiagnosticIds.MixedFormCollectionAndDto, "AL0022");

    private static readonly DiagnosticDescriptor RuleAl0023 = CreateRule(
        DiagnosticIds.UnsupportedFormType, "AL0023");

    private static readonly DiagnosticDescriptor RuleAl0024 = CreateRule(
        DiagnosticIds.FormAndBodyConflict, "AL0024");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [RuleAl0020, RuleAl0021, RuleAl0022, RuleAl0023, RuleAl0024];

    private static DiagnosticDescriptor CreateRule(string id, string ruleNumber) => new(
        id,
        new LocalizableResourceString($"{ruleNumber}AnalyzerTitle", Resources.ResourceManager, typeof(Resources)),
        new LocalizableResourceString($"{ruleNumber}AnalyzerMessageFormat", Resources.ResourceManager,
            typeof(Resources)),
        DiagnosticCategories.AspNetCore,
        DiagnosticSeverity.Error,
        true,
        new LocalizableResourceString($"{ruleNumber}AnalyzerDescription", Resources.ResourceManager, typeof(Resources)),
        HelpLinkBase);

    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterCompilationStartAction(OnCompilationStart);

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        var cache = WellKnownTypeCache.Create(context.Compilation);
        context.RegisterSymbolAction(ctx => AnalyzeMethod(ctx, cache), SymbolKind.Method);
    }

    private static void AnalyzeMethod(SymbolAnalysisContext context, WellKnownTypeCache cache) {
        if (context.Symbol is not IMethodSymbol { Parameters.IsEmpty: false } method) {
            return;
        }

        var hasFromBody = false;
        var hasFromForm = false;
        var fromFormDtoCount = 0;
        var hasFromFormCollection = false;
        var hasFromFormDto = false;

        IParameterSymbol? fromBodyParam = null;
        IParameterSymbol? firstFromFormParam = null;

        foreach (var param in method.Parameters) {
            var hasFromFormAttr = cache.HasAttribute(param, WellKnownType.FromFormAttribute);
            var hasFromBodyAttr = cache.HasAttribute(param, WellKnownType.FromBodyAttribute);
            var isFormCollection = cache.IsType(param.Type, WellKnownType.IFormCollection);
            var isFormFile = cache.IsType(param.Type, WellKnownType.IFormFile);
            var isFormFileCollection = cache.IsType(param.Type, WellKnownType.IFormFileCollection);

            if (hasFromBodyAttr) {
                hasFromBody = true;
                fromBodyParam = param;
            }

            if (isFormCollection && !hasFromFormAttr) {
                context.ReportDiagnostic(Diagnostic.Create(
                    RuleAl0020,
                    param.Locations.FirstOrDefault() ?? Location.None,
                    param.Name,
                    method.Name));
            }

            if (hasFromFormAttr) {
                hasFromForm = true;
                firstFromFormParam ??= param;

                if (isFormCollection) {
                    hasFromFormCollection = true;
                    fromFormDtoCount++;
                } else if (!IsPrimitive(param.Type) && !isFormFile && !isFormFileCollection) {
                    hasFromFormDto = true;
                    fromFormDtoCount++;

                    var reason = GetUnsupportedFormTypeReason(param.Type);
                    if (reason is not null) {
                        context.ReportDiagnostic(Diagnostic.Create(
                            RuleAl0023,
                            param.Locations.FirstOrDefault() ?? Location.None,
                            param.Name,
                            method.Name,
                            reason));
                    }
                }
            }
        }

        if (hasFromBody && hasFromForm) {
            context.ReportDiagnostic(Diagnostic.Create(
                RuleAl0024,
                fromBodyParam?.Locations.FirstOrDefault() ?? method.Locations.FirstOrDefault() ?? Location.None,
                method.Name));
        }

        if (fromFormDtoCount > 1) {
            context.ReportDiagnostic(Diagnostic.Create(
                RuleAl0021,
                firstFromFormParam?.Locations.FirstOrDefault() ?? method.Locations.FirstOrDefault() ?? Location.None,
                method.Name));
        }

        if (hasFromFormCollection && hasFromFormDto) {
            context.ReportDiagnostic(Diagnostic.Create(
                RuleAl0022,
                firstFromFormParam?.Locations.FirstOrDefault() ?? method.Locations.FirstOrDefault() ?? Location.None,
                method.Name));
        }
    }

    private static bool IsPrimitive(ITypeSymbol type) {
        if (type is INamedTypeSymbol {
                IsGenericType: true, ConstructedFrom.SpecialType: SpecialType.System_Nullable_T
            } namedType) {
            type = namedType.TypeArguments[0];
        }

        return type.SpecialType is
                   SpecialType.System_Boolean or SpecialType.System_Byte or SpecialType.System_SByte or
                   SpecialType.System_Int16 or SpecialType.System_UInt16 or SpecialType.System_Int32 or
                   SpecialType.System_UInt32 or SpecialType.System_Int64 or SpecialType.System_UInt64 or
                   SpecialType.System_Single or SpecialType.System_Double or SpecialType.System_Decimal or
                   SpecialType.System_Char or SpecialType.System_String or SpecialType.System_DateTime
               || type.ToDisplayString() is "System.Guid" or "System.TimeSpan" or "System.DateTimeOffset"
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
            .Where(static c => c.DeclaredAccessibility == Accessibility.Public)
            .ToList();

        if (publicConstructors.Count is 0) {
            return "no public constructor available";
        }

        var hasValidConstructor = publicConstructors.Exists(static ctor =>
            ctor.Parameters.IsEmpty || ctor.Parameters.All(static p => IsPrimitive(p.Type)));

        return hasValidConstructor ? null : "no suitable constructor (needs parameterless or all-primitive parameters)";
    }
}
