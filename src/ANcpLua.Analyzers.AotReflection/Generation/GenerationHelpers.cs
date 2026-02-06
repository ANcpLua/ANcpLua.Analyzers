namespace ANcpLua.Analyzers.AotReflection;

internal static partial class GenerationHelpers {
    public const string BindingFlagsAll =
        "global::System.Reflection.BindingFlags.Public | " +
        "global::System.Reflection.BindingFlags.NonPublic | " +
        "global::System.Reflection.BindingFlags.Instance | " +
        "global::System.Reflection.BindingFlags.Static";

    public static string StringLiteral(string value)
        => SymbolDisplay.FormatLiteral(value, true);

    public static string GetTypeOf(string fullyQualifiedType)
        => $"typeof({fullyQualifiedType})";
}
