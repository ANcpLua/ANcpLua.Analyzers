
namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0086: Detects OpenTelemetry attributes set with incorrect types.
/// </summary>
/// <remarks>
///     <para>
///         OpenTelemetry semantic conventions specify expected types for attributes.
///         Using incorrect types (e.g., passing a string where an integer is expected)
///         can cause issues with telemetry backends, dashboards, and aggregation logic.
///     </para>
///     <para>
///         Examples of common type mismatches:
///         <list type="bullet">
///             <item>gen_ai.usage.input_tokens: should be int, not string</item>
///             <item>http.response.status_code: should be int, not string</item>
///             <item>db.operation.batch_size: should be int, not string</item>
///         </list>
///     </para>
///     <para>
///         The analyzer identifies SetTag/SetAttribute calls where the attribute name
///         matches a known semantic convention and the value type does not match
///         the expected type per the specification.
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0086IncorrectAttributeTypeAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL0086.</summary>
    public const string DiagnosticId = "AL0086";

    private static readonly DiagnosticDescriptor Rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.OpenTelemetry,
        DiagnosticSeverity.Warning);

    /// <summary>
    ///     Maps attribute names to their expected types per OTel semantic conventions.
    /// </summary>
    private static readonly Dictionary<string, ExpectedType> AttributeTypeMap = new(StringComparer.Ordinal) {
        // GenAI token counts (must be integers)
        ["gen_ai.usage.input_tokens"] = ExpectedType.WholeNumber,
        ["gen_ai.usage.output_tokens"] = ExpectedType.WholeNumber,
        ["gen_ai.usage.total_tokens"] = ExpectedType.WholeNumber,
        ["gen_ai.request.max_tokens"] = ExpectedType.WholeNumber,
        ["gen_ai.response.finish_reasons"] = ExpectedType.CharacterSequenceArray,

        // GenAI numeric attributes
        ["gen_ai.request.temperature"] = ExpectedType.FloatingPoint,
        ["gen_ai.request.top_p"] = ExpectedType.FloatingPoint,
        ["gen_ai.request.top_k"] = ExpectedType.WholeNumber,
        ["gen_ai.request.frequency_penalty"] = ExpectedType.FloatingPoint,
        ["gen_ai.request.presence_penalty"] = ExpectedType.FloatingPoint,

        // HTTP attributes (must be integers)
        ["http.response.status_code"] = ExpectedType.WholeNumber,
        ["http.request.body.size"] = ExpectedType.WholeNumber,
        ["http.response.body.size"] = ExpectedType.WholeNumber,
        ["http.request.resend_count"] = ExpectedType.WholeNumber,

        // Database attributes
        ["db.operation.batch_size"] = ExpectedType.WholeNumber,

        // Network attributes
        ["network.peer.port"] = ExpectedType.WholeNumber,
        ["server.port"] = ExpectedType.WholeNumber,
        ["client.port"] = ExpectedType.WholeNumber,
        ["url.port"] = ExpectedType.WholeNumber,

        // Thread/process attributes
        ["thread.id"] = ExpectedType.WholeNumber,
        ["process.pid"] = ExpectedType.WholeNumber,
        ["process.parent_pid"] = ExpectedType.WholeNumber,

        // Error count
        ["error.count"] = ExpectedType.WholeNumber
    };

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    /// <summary>Registers operation actions to analyze invocation expressions.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);

    private static void AnalyzeInvocation(OperationAnalysisContext context) {
        var invocation = (IInvocationOperation)context.Operation;
        var method = invocation.TargetMethod;

        // Check for SetTag, SetAttribute, AddTag, or similar methods
        if (!IsAttributeSetterMethod(method)) {
            return;
        }

        // Get the attribute name (first argument, must be string constant)
        if (invocation.Arguments.Length < 2) {
            return;
        }

        var nameArg = invocation.Arguments[0];
        if (!nameArg.Value.TryGetConstantValue(out string? attributeName) || attributeName is null) {
            return;
        }

        // Check if this attribute has a known expected type
        if (!AttributeTypeMap.TryGetValue(attributeName, out var expectedType)) {
            return;
        }

        // Get the value argument
        var valueArg = invocation.Arguments[1];

        if (valueArg.Value.Type is not { } valueType) {
            return;
        }

        // Check if the actual type matches the expected type
        if (IsTypeMatch(valueType, expectedType)) {
            return;
        }

        // Report diagnostic
        var expectedTypeName = GetTypeName(expectedType);
        var actualTypeName = valueType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

        context.ReportDiagnostic(Rule, valueArg.Syntax.GetLocation(), attributeName, expectedTypeName, actualTypeName);
    }

    private static bool IsAttributeSetterMethod(IMethodSymbol method) {
        var methodName = method.Name;

        // Common Activity/Span attribute setter methods
        return methodName.EqualsOrdinal("SetTag") ||
               methodName.EqualsOrdinal("SetAttribute") ||
               methodName.EqualsOrdinal("AddTag") ||
               methodName.EqualsOrdinal("SetCustomProperty") ||
               methodName.EqualsOrdinal("Add") && IsTagContextMethod(method);
    }

    private static bool IsTagContextMethod(IMethodSymbol method) {
        // Check if this is a dictionary-style Add where the containing type suggests tags
        var containingType = method.ContainingType?.Name;
        return containingType is not null &&
               (containingType.ContainsOrdinal("Tag") ||
                containingType.ContainsOrdinal("Attribute"));
    }

    private static bool IsTypeMatch(ITypeSymbol actualType, ExpectedType expectedType) {
        return expectedType switch {
            ExpectedType.WholeNumber => IsIntegerType(actualType),
            ExpectedType.WholeNumber64 => IsLongType(actualType),
            ExpectedType.FloatingPoint => IsNumericType(actualType),
            ExpectedType.CharacterSequence => actualType.SpecialType == SpecialType.System_String,
            ExpectedType.TrueOrFalse => actualType.SpecialType == SpecialType.System_Boolean,
            ExpectedType.CharacterSequenceArray => IsStringArrayType(actualType),
            _ => true // Unknown expected type, don't flag
        };
    }

    private static bool IsIntegerType(ITypeSymbol type) =>
        type.SpecialType is
            SpecialType.System_Int32 or
            SpecialType.System_Int64 or
            SpecialType.System_Int16 or
            SpecialType.System_Byte or
            SpecialType.System_UInt32 or
            SpecialType.System_UInt64 or
            SpecialType.System_UInt16 or
            SpecialType.System_SByte;

    private static bool IsLongType(ITypeSymbol type) =>
        type.SpecialType is SpecialType.System_Int64 or SpecialType.System_UInt64 ||
        IsIntegerType(type); // Allow int promotion to long

    private static bool IsNumericType(ITypeSymbol type) =>
        IsIntegerType(type) ||
        type.SpecialType is
            SpecialType.System_Single or
            SpecialType.System_Double or
            SpecialType.System_Decimal;

    private static bool IsStringArrayType(ITypeSymbol type) {
        if (type is IArrayTypeSymbol arrayType) {
            return arrayType.ElementType.SpecialType == SpecialType.System_String;
        }

        // Also accept List<string>, IEnumerable<string>, etc.
        if (type is INamedTypeSymbol namedType && namedType.IsGenericType) {
            var typeArgs = namedType.TypeArguments;
            if (typeArgs.Length == 1 && typeArgs[0].SpecialType == SpecialType.System_String) {
                var typeName = namedType.Name;
                return typeName.ContainsOrdinal("List") ||
                       typeName.ContainsOrdinal("Enumerable") ||
                       typeName.ContainsOrdinal("Collection") ||
                       typeName.ContainsOrdinal("Array");
            }
        }

        return false;
    }

    private static string GetTypeName(ExpectedType expectedType) =>
        expectedType switch {
            ExpectedType.WholeNumber => "int",
            ExpectedType.WholeNumber64 => "long",
            ExpectedType.FloatingPoint => "double",
            ExpectedType.CharacterSequence => "string",
            ExpectedType.TrueOrFalse => "bool",
            ExpectedType.CharacterSequenceArray => "string[]",
            _ => "unknown"
        };

    private enum ExpectedType {
        WholeNumber,
        WholeNumber64,
        FloatingPoint,
        CharacterSequence,
        TrueOrFalse,
        CharacterSequenceArray
    }
}
