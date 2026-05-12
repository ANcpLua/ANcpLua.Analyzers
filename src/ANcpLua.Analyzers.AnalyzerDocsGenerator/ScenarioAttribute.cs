namespace ANcpLua.Analyzers.AnalyzerDocsGenerator;

/// <summary>
///     Marks a method in a Docs class as a documentation scenario.
///     The DocsGenerator walks every method tagged with this attribute and emits one
///     <c>### scenario</c> section per method into the generated markdown.
///     Companion methods named <c>{Scenario}_Failure</c> capture the analyzer's diagnostic
///     message (by running the analyzer on the failing input and throwing) so the generated
///     <c>#### Failure messages</c> block reflects the real, current diagnostic text.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed partial class ScenarioAttribute : Attribute;
