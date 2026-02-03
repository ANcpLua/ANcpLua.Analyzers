using System.Collections.Concurrent;
using ANcpLua.Analyzers.Core;

namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0053: [AotUnsafe] attribute applied to code that doesn't use AOT-incompatible patterns.
/// </summary>
/// <remarks>
///     <para>
///         This analyzer detects when [AotUnsafe] is applied to code that doesn't actually
///         use any AOT-incompatible patterns. This helps prevent over-annotation where developers
///         mark code as unsafe "just to be safe" when it's actually AOT-compatible.
///     </para>
///     <para>
///         The analyzer checks for:
///         <list type="bullet">
///             <item>Calls to methods with [RequiresDynamicCode]</item>
///             <item>Calls to other [AotUnsafe] methods</item>
///             <item>Direct use of reflection APIs (Type.GetMethod, PropertyInfo.GetValue, etc.)</item>
///             <item>Expression.Compile() usage</item>
///             <item>Reflection.Emit namespace usage</item>
///         </list>
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0053UnnecessaryAotUnsafeAnalyzer : AlAnalyzer {
    private const string AotUnsafeAttributeName = "AotUnsafe";
    private const string RequiresDynamicCodeAttributeName = "RequiresDynamicCode";

    private static readonly LocalizableResourceString Title = new(
        nameof(Resources.AL0053AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableResourceString MessageFormat = new(
        nameof(Resources.AL0053AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableResourceString Description = new(
        nameof(Resources.AL0053AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.UnnecessaryAotUnsafe,
        Title, MessageFormat, DiagnosticCategories.AotTesting,
        DiagnosticSeverities.Suggestion, true, Description,
        HelpLinkBase,
        WellKnownDiagnosticTags.CompilationEnd);

    /// <summary>
    /// Reflection APIs that indicate AOT-incompatible code.
    /// </summary>
    private static readonly HashSet<string> UnsafeReflectionMethods = new(StringComparer.Ordinal) {
        "GetMethod",
        "GetMethods",
        "GetProperty",
        "GetProperties",
        "GetField",
        "GetFields",
        "GetConstructor",
        "GetConstructors",
        "GetMember",
        "GetMembers",
        "GetValue",
        "SetValue",
        "Invoke",
        "CreateInstance",
        "MakeGenericType",
        "MakeGenericMethod",
        "Compile", // Expression.Compile
        "CompileToMethod",
        "CreateDelegate",
    };

    /// <summary>
    /// Types that contain reflection APIs.
    /// </summary>
    private static readonly HashSet<string> UnsafeReflectionTypes = new(StringComparer.Ordinal) {
        "System.Type",
        "System.Reflection.MethodInfo",
        "System.Reflection.MethodBase",
        "System.Reflection.PropertyInfo",
        "System.Reflection.FieldInfo",
        "System.Reflection.ConstructorInfo",
        "System.Reflection.MemberInfo",
        "System.Activator",
        "System.Linq.Expressions.Expression",
        "System.Linq.Expressions.LambdaExpression",
        "System.Delegate",
    };

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    /// <summary>Registers syntax or operation actions for analysis.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterCompilationStartAction(OnCompilationStart);

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        // Track methods marked with [AotUnsafe] and whether they have unsafe patterns
        // Use ConcurrentDictionary because analyzer callbacks run concurrently
        var methodUnsafePatterns = new ConcurrentDictionary<IMethodSymbol, bool>(SymbolEqualityComparer.Default);

        // Register all methods with [AotUnsafe] attribute first (to catch methods with no operations)
        context.RegisterSymbolAction(ctx => {
            var method = (IMethodSymbol)ctx.Symbol;
            if (method.HasAttributeByShortName(AotUnsafeAttributeName)) {
                methodUnsafePatterns.TryAdd(method, false); // Assume safe until proven otherwise
            }
        }, SymbolKind.Method);

        // Check all invocations for unsafe patterns
        context.RegisterOperationAction(ctx => {
            if (ctx.ContainingSymbol is not IMethodSymbol method) {
                return;
            }

            // Only track methods marked with [AotUnsafe]
            if (!method.HasAttributeByShortName(AotUnsafeAttributeName)) {
                return;
            }

            // If we already found unsafe patterns, skip
            if (methodUnsafePatterns.TryGetValue(method, out var hasUnsafe) && hasUnsafe) {
                return;
            }

            var invocation = (IInvocationOperation)ctx.Operation;
            var targetMethod = invocation.TargetMethod;

            // Check if target has [RequiresDynamicCode]
            if (targetMethod.HasAttributeByShortName(RequiresDynamicCodeAttributeName)) {
                methodUnsafePatterns[method] = true;
                return;
            }

            // Check if target is [AotUnsafe]
            if (IsAotUnsafe(targetMethod)) {
                methodUnsafePatterns[method] = true;
                return;
            }

            // Check if target is a known reflection API
            if (IsKnownReflectionApi(targetMethod)) {
                methodUnsafePatterns[method] = true;
                return;
            }

            // Mark as not having unsafe patterns (yet)
            methodUnsafePatterns.TryAdd(method, false);
        }, OperationKind.Invocation);

        // Check object creation for Reflection.Emit types
        context.RegisterOperationAction(ctx => {
            if (ctx.ContainingSymbol is not IMethodSymbol method) {
                return;
            }

            if (!method.HasAttributeByShortName(AotUnsafeAttributeName)) {
                return;
            }

            if (methodUnsafePatterns.TryGetValue(method, out var hasUnsafe) && hasUnsafe) {
                return;
            }

            var creation = (IObjectCreationOperation)ctx.Operation;
            if (creation.Type is INamedTypeSymbol createdType) {
                var ns = createdType.ContainingNamespace?.ToDisplayString();
                if (ns is "System.Reflection.Emit") {
                    methodUnsafePatterns[method] = true;
                }
            }
        }, OperationKind.ObjectCreation);

        // Check for dynamic type usage
        context.RegisterOperationAction(ctx => {
            if (ctx.ContainingSymbol is not IMethodSymbol method) {
                return;
            }

            if (!method.HasAttributeByShortName(AotUnsafeAttributeName)) {
                return;
            }

            if (methodUnsafePatterns.TryGetValue(method, out var hasUnsafe) && hasUnsafe) {
                return;
            }

            var dynamicInvocation = (IDynamicInvocationOperation)ctx.Operation;
            // Any dynamic invocation is unsafe
            methodUnsafePatterns[method] = true;
        }, OperationKind.DynamicInvocation);

        context.RegisterOperationAction(ctx => {
            if (ctx.ContainingSymbol is not IMethodSymbol method) {
                return;
            }

            if (!method.HasAttributeByShortName(AotUnsafeAttributeName)) {
                return;
            }

            if (methodUnsafePatterns.TryGetValue(method, out var hasUnsafe) && hasUnsafe) {
                return;
            }

            // Any dynamic member reference is unsafe
            methodUnsafePatterns[method] = true;
        }, OperationKind.DynamicMemberReference);

        // Report methods that have [AotUnsafe] but no unsafe patterns
        context.RegisterCompilationEndAction(ctx => {
            foreach (var kvp in methodUnsafePatterns) {
                if (kvp.Value) {
                    continue;
                }

                var method = kvp.Key;

                // Double-check: method still has the attribute
                if (!method.HasAttributeByShortName(AotUnsafeAttributeName)) {
                    continue;
                }

                var attributeLocation = GetAotUnsafeAttributeLocation(method);
                var methodName = method.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

                ctx.ReportDiagnostic(Diagnostic.Create(
                    Rule,
                    attributeLocation ?? method.Locations.FirstOrDefault(),
                    methodName));
            }
        });
    }

    private static bool IsAotUnsafe(IMethodSymbol method) {
        // Check method itself
        if (method.HasAttributeByShortName(AotUnsafeAttributeName)) {
            return true;
        }

        // Check containing type hierarchy
        for (var containingType = method.ContainingType; containingType is not null; containingType = containingType.ContainingType) {
            if (containingType.HasAttributeByShortName(AotUnsafeAttributeName)) {
                return true;
            }
        }

        return false;
    }

    private static bool IsKnownReflectionApi(IMethodSymbol method) {
        var methodName = method.Name;

        // Quick check: is this a known unsafe method name?
        if (!UnsafeReflectionMethods.Contains(methodName)) {
            return false;
        }

        // Verify it's from a reflection type
        if (method.ContainingType is not { } containingType) {
            return false;
        }

        var typeName = containingType.ToDisplayString();

        // Direct match
        if (UnsafeReflectionTypes.Contains(typeName)) {
            return true;
        }

        // Check if it inherits from a reflection type
        for (var baseType = containingType.BaseType; baseType is not null; baseType = baseType.BaseType) {
            if (UnsafeReflectionTypes.Contains(baseType.ToDisplayString())) {
                return true;
            }
        }

        return false;
    }

    private static Location? GetAotUnsafeAttributeLocation(IMethodSymbol method) {
        foreach (var attr in method.GetAttributes()) {
            if (attr.AttributeClass?.Name is "AotUnsafeAttribute" or "AotUnsafe") {
                return attr.ApplicationSyntaxReference?.GetSyntax().GetLocation();
            }
        }
        return null;
    }
}
