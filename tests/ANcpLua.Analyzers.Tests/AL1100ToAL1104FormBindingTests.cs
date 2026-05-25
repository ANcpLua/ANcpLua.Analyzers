using ANcpLua.Analyzers.Analyzers;
using Basic.Reference.Assemblies;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL1100-AL1104: Form binding analyzers for ASP.NET Core.
/// </summary>
public sealed partial class Al1100ToAl1104FormBindingTests {
    private const string Stubs = """
                                 namespace Microsoft.AspNetCore.Mvc {
                                     [System.AttributeUsage(System.AttributeTargets.Parameter)]
                                     public class FromFormAttribute : System.Attribute { }
                                     [System.AttributeUsage(System.AttributeTargets.Parameter)]
                                     public class FromBodyAttribute : System.Attribute { }
                                 }
                                 namespace Microsoft.AspNetCore.Http {
                                     public interface IFormCollection { }
                                     public interface IFormFile { }
                                     public interface IFormFileCollection { }
                                 }
                                 """;

    private const string Ff = "[Microsoft.AspNetCore.Mvc.FromForm]";
    private const string Fb = "[Microsoft.AspNetCore.Mvc.FromBody]";
    private const string Ifc = "Microsoft.AspNetCore.Http.IFormCollection";
    private const string Iff = "Microsoft.AspNetCore.Http.IFormFile";

    private static Task VerifyAsync(string source) {
        var test = new CSharpAnalyzerTest<Al1100ToAl1104FormBindingAnalyzer, DefaultVerifier> {
            TestCode = (Stubs + "\n" + source).ReplaceLineEndings(),
            ReferenceAssemblies = new ReferenceAssemblies("net10.0"),
            MarkupOptions = MarkupOptions.UseFirstDescriptor
        };
        test.TestState.AdditionalReferences.AddRange(Net100.References.All);
        return test.RunAsync();
    }

    #region AL1102: Mixed IFormCollection with DTO

    [Fact]
    public Task AL1102_ShouldReportMixedFormCollectionAndDto() =>
        VerifyAsync($$"""
                      public class Dto { public string Name { get; set; } }
                      public class C { void M({{Ff}} {{Ifc}} {|AL1101:{|AL1102:form|}|}, {{Ff}} Dto dto) { } }
                      """);

    #endregion

    #region AL1100: IFormCollection requires explicit [FromForm]

    [Theory]
    [InlineData($"{Ifc} {{|AL1100:form|}}", "without attribute")]
    public Task AL1100_ShouldReport(string param, string _) =>
        VerifyAsync($"public class C {{ void M({param}) {{ }} }}");

    [Theory]
    [InlineData($"{Ff} {Ifc} form", "with FromForm")]
    [InlineData($"{Iff} file", "IFormFile without attribute is OK")]
    public Task AL1100_ShouldNotReport(string param, string _) =>
        VerifyAsync($"public class C {{ void M({param}) {{ }} }}");

    #endregion

    #region AL1101: Multiple structured form sources

    [Fact]
    public Task AL1101_ShouldReportMultipleFromFormDtos() =>
        VerifyAsync($$"""
                      public class Dto1 { public string Name { get; set; } }
                      public class Dto2 { public string Value { get; set; } }
                      public class C { void M({{Ff}} Dto1 {|AL1101:dto1|}, {{Ff}} Dto2 dto2) { } }
                      """);

    [Fact]
    public Task AL1101_ShouldNotReportSingleFromFormDto() =>
        VerifyAsync($$"""
                      public class Dto { public string Name { get; set; } }
                      public class C { void M({{Ff}} Dto dto) { } }
                      """);

    #endregion

    #region AL1103: Unsupported form type

    [Theory]
    [InlineData("public interface IMyData { string Name { get; } }", "IMyData")]
    [InlineData("public abstract class AbstractData { public abstract string Name { get; } }", "AbstractData")]
    public Task AL1103_ShouldReport(string typeDecl, string typeName) =>
        VerifyAsync($$"""
                      {{typeDecl}}
                      public class C { void M({{Ff}} {{typeName}} {|AL1103:data|}) { } }
                      """);

    [Theory]
    [InlineData($"{Ff} string name, {Ff} int count", "primitives")]
    public Task AL1103_ShouldNotReportPrimitives(string param, string _) =>
        VerifyAsync($"public class C {{ void M({param}) {{ }} }}");

    [Fact]
    public Task AL1103_ShouldNotReportDtoWithParameterlessConstructor() =>
        VerifyAsync($$"""
                      public class Dto { public Dto() { } public string Name { get; set; } }
                      public class C { void M({{Ff}} Dto dto) { } }
                      """);

    #endregion

    #region AL1104: Form and body conflict

    [Fact]
    public Task AL1104_ShouldReportFormAndBodyConflict() =>
        VerifyAsync($$"""
                      public class FormDto { public string Name { get; set; } }
                      public class BodyDto { public string Data { get; set; } }
                      public class C { void M({{Ff}} FormDto form, {{Fb}} BodyDto {|AL1104:body|}) { } }
                      """);

    [Theory]
    [InlineData($"{Ff} FormDto form", "only FromForm")]
    [InlineData($"{Fb} BodyDto body", "only FromBody")]
    public Task AL1104_ShouldNotReport(string param, string _) =>
        VerifyAsync($$"""
                      public class FormDto { public string Name { get; set; } }
                      public class BodyDto { public string Data { get; set; } }
                      public class C { void M({{param}}) { } }
                      """);

    #endregion
}
