using System.Collections.Concurrent;

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
    /// <summary>The diagnostic identifier for AL0053.</summary>
    private const string DiagnosticId = "AL0053";

    private const string AotUnsafeAttributeName = "AotUnsafe";
    private const string RequiresDynamicCodeAttributeName = "RequiresDynamicCode";

    private static readonly DiagnosticDescriptor Rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.AotTesting,
        DiagnosticSeverities.Suggestion);

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
        var methodUnsafePatterns = new ConcurrentDictionary<IMethodSymbol, bool>(SymbolEqualityComparer.Default);

        context.RegisterSymbolAction(ctx => {
            var method = (IMethodSymbol)ctx.Symbol;
            if (method.HasAttributeByShortName(AotUnsafeAttributeName)) {
                methodUnsafePatterns.TryAdd(method, false);
            }
        }, SymbolKind.Method);

        context.RegisterOperationAction(ctx => {
            if (!TryGetTrackedMethod(ctx, methodUnsafePatterns, out var method)) {
                return;
            }

            var targetMethod = ((IInvocationOperation)ctx.Operation).TargetMethod;

            if (targetMethod.HasAttributeByShortName(RequiresDynamicCodeAttributeName) ||
                IsAotUnsafe(targetMethod) ||
                IsKnownReflectionApi(targetMethod)) {
                methodUnsafePatterns[method] = true;
            } else {
                methodUnsafePatterns.TryAdd(method, false);
            }
        }, OperationKind.Invocation);

        context.RegisterOperationAction(ctx => {
            if (!TryGetTrackedMethod(ctx, methodUnsafePatterns, out var method)) {
                return;
            }

            if (((IObjectCreationOperation)ctx.Operation).Type is INamedTypeSymbol { ContainingNamespace: { } ns }
                && ns.ToDisplayString() is "System.Reflection.Emit") {
                methodUnsafePatterns[method] = true;
            }
        }, OperationKind.ObjectCreation);

        context.RegisterOperationAction(MarkUnsafe, OperationKind.DynamicInvocation);
        context.RegisterOperationAction(MarkUnsafe, OperationKind.DynamicMemberReference);

        context.RegisterCompilationEndAction(ctx => {
            foreach (var kvp in methodUnsafePatterns) {
                if (kvp.Value || !kvp.Key.HasAttributeByShortName(AotUnsafeAttributeName)) {
                    continue;
                }

                var attributeLocation = GetAotUnsafeAttributeLocation(kvp.Key);
                ctx.ReportDiagnostic(Diagnostic.Create(
                    Rule,
                    attributeLocation ?? kvp.Key.Locations.FirstOrDefault(),
                    kvp.Key.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
            }
        });
        return;

        // Dynamic invocations and member references are always AOT-unsafe
        void MarkUnsafe(OperationAnalysisContext ctx) {
            if (TryGetTrackedMethod(ctx, methodUnsafePatterns, out var method)) {
                methodUnsafePatterns[method] = true;
            }
        }
    }

    private static bool TryGetTrackedMethod(
        OperationAnalysisContext ctx,
        ConcurrentDictionary<IMethodSymbol, bool> patterns,
        out IMethodSymbol method) {
        method = null!;

        if (ctx.ContainingSymbol is not IMethodSymbol m) {
            return false;
        }

        if (!m.HasAttributeByShortName(AotUnsafeAttributeName)) {
            return false;
        }

        if (patterns.TryGetValue(m, out var hasUnsafe) && hasUnsafe) {
            return false;
        }

        method = m;
        return true;
    }

    private static bool IsAotUnsafe(IMethodSymbol method) {
        if (method.HasAttributeByShortName(AotUnsafeAttributeName)) {
            return true;
        }

        for (var type = method.ContainingType; type is not null; type = type.ContainingType) {
            if (type.HasAttributeByShortName(AotUnsafeAttributeName)) {
                return true;
            }
        }

        return false;
    }

    private static bool IsKnownReflectionApi(IMethodSymbol method) {
        if (!UnsafeReflectionMethods.Contains(method.Name)) {
            return false;
        }

        if (method.ContainingType is not { } containingType) {
            return false;
        }

        if (UnsafeReflectionTypes.Contains(containingType.ToDisplayString())) {
            return true;
        }

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
