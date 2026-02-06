namespace ANcpLua.Analyzers.AotReflection;

internal static partial class LiteralFormatter {
    public static string? FormatConstant(object? value, ITypeSymbol? type) {
        if (value is null) {
            return "null";
        }

        if (type is INamedTypeSymbol { TypeKind: TypeKind.Enum }) {
            var underlying = Convert.ToInt64(value, CultureInfo.InvariantCulture);
            return $"({type.GetFullyQualifiedName()}){underlying}";
        }

        return value switch {
            string s => $"\"{EscapeString(s)}\"",
            char c => $"'{EscapeChar(c)}'",
            bool b => b ? "true" : "false",
            float f => $"{f.ToString(CultureInfo.InvariantCulture)}f",
            double d => $"{d.ToString(CultureInfo.InvariantCulture)}d",
            decimal m => $"{m.ToString(CultureInfo.InvariantCulture)}m",
            long l => $"{l.ToString(CultureInfo.InvariantCulture)}L",
            ulong ul => $"{ul.ToString(CultureInfo.InvariantCulture)}uL",
            uint ui => $"{ui.ToString(CultureInfo.InvariantCulture)}u",
            ushort us => us.ToString(CultureInfo.InvariantCulture),
            byte b => b.ToString(CultureInfo.InvariantCulture),
            sbyte sb => sb.ToString(CultureInfo.InvariantCulture),
            short sh => sh.ToString(CultureInfo.InvariantCulture),
            int i => i.ToString(CultureInfo.InvariantCulture),
            _ => null
        };
    }

    public static string? GetDefaultValueLiteral(IParameterSymbol parameter, CancellationToken cancellationToken) {
        var syntax = parameter.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax(cancellationToken) as ParameterSyntax;
        var fromSyntax = syntax?.Default?.Value.ToString().Trim();
        if (!string.IsNullOrWhiteSpace(fromSyntax)) {
            return fromSyntax;
        }

        return FormatConstant(parameter.ExplicitDefaultValue, parameter.Type);
    }

    private static string EscapeString(string value)
        => value.Replace("\\", "\\\\").Replace("\"", "\\\"");

    private static string EscapeChar(char value)
        => value switch {
            '\\' => "\\\\",
            '\'' => "\\'",
            '\n' => "\\n",
            '\r' => "\\r",
            '\t' => "\\t",
            _ => value.ToString()
        };
}
