namespace ANcpLua.Analyzers.AotReflection;

internal static partial class AccessibilityExtensions {
    public static string ToAccessibilityString(this Accessibility accessibility)
        => accessibility switch {
            Accessibility.Public => "public",
            Accessibility.Private => "private",
            Accessibility.Internal => "internal",
            Accessibility.Protected => "protected",
            Accessibility.ProtectedAndInternal => "protected internal",
            Accessibility.ProtectedOrInternal => "protected internal",
            _ => "private"
        };
}
