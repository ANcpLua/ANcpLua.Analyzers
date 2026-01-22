using ANcpLua.Analyzers.Analyzers;
using Basic.Reference.Assemblies;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL0020-AL0024: Form binding analyzers for ASP.NET Core.
/// </summary>
public sealed class Al0020ToAl0024AnalyzerTests {
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
        var test = new CSharpAnalyzerTest<Al0020ToAl0024FormBindingAnalyzer, DefaultVerifier> {
            TestCode = (Stubs + "\n" + source).ReplaceLineEndings(), ReferenceAssemblies = new ReferenceAssemblies("net10.0"), MarkupOptions = MarkupOptions.UseFirstDescriptor
        };
        test.TestState.AdditionalReferences.AddRange(Net100.References.All);
        return test.RunAsync();
    }

    #region AL0022: Mixed IFormCollection with DTO

    [Fact]
    public Task AL0022_ShouldReportMixedFormCollectionAndDto() =>
        VerifyAsync($$"""
                      public class Dto { public string Name { get; set; } }
                      public class C { void M({{Ff}} {{Ifc}} {|AL0021:{|AL0022:form|}|}, {{Ff}} Dto dto) { } }
                      """);

    #endregion

    #region AL0020: IFormCollection requires explicit [FromForm]

    [Theory]
    [InlineData($"{Ifc} {{|AL0020:form|}}", "without attribute")]
    public Task AL0020_ShouldReport(string param, string _) =>
        VerifyAsync($"public class C {{ void M({param}) {{ }} }}");

    [Theory]
    [InlineData($"{Ff} {Ifc} form", "with FromForm")]
    [InlineData($"{Iff} file", "IFormFile without attribute is OK")]
    public Task AL0020_ShouldNotReport(string param, string _) =>
        VerifyAsync($"public class C {{ void M({param}) {{ }} }}");

    #endregion

    #region AL0021: Multiple structured form sources

    [Fact]
    public Task AL0021_ShouldReportMultipleFromFormDtos() =>
        VerifyAsync($$"""
                      public class Dto1 { public string Name { get; set; } }
                      public class Dto2 { public string Value { get; set; } }
                      public class C { void M({{Ff}} Dto1 {|AL0021:dto1|}, {{Ff}} Dto2 dto2) { } }
                      """);

    [Fact]
    public Task AL0021_ShouldNotReportSingleFromFormDto() =>
        VerifyAsync($$"""
                      public class Dto { public string Name { get; set; } }
                      public class C { void M({{Ff}} Dto dto) { } }
                      """);

    #endregion

    #region AL0023: Unsupported form type

    [Theory]
    [InlineData("public interface IMyData { string Name { get; } }", "IMyData")]
    [InlineData("public abstract class AbstractData { public abstract string Name { get; } }", "AbstractData")]
    public Task AL0023_ShouldReport(string typeDecl, string typeName) =>
        VerifyAsync($$"""
                      {{typeDecl}}
                      public class C { void M({{Ff}} {{typeName}} {|AL0023:data|}) { } }
                      """);

    [Theory]
    [InlineData($"{Ff} string name, {Ff} int count", "primitives")]
    public Task AL0023_ShouldNotReportPrimitives(string param, string _) =>
        VerifyAsync($"public class C {{ void M({param}) {{ }} }}");

    [Fact]
    public Task AL0023_ShouldNotReportDtoWithParameterlessConstructor() =>
        VerifyAsync($$"""
                      public class Dto { public Dto() { } public string Name { get; set; } }
                      public class C { void M({{Ff}} Dto dto) { } }
                      """);

    #endregion

    #region AL0024: Form and body conflict

    [Fact]
    public Task AL0024_ShouldReportFormAndBodyConflict() =>
        VerifyAsync($$"""
                      public class FormDto { public string Name { get; set; } }
                      public class BodyDto { public string Data { get; set; } }
                      public class C { void M({{Ff}} FormDto form, {{Fb}} BodyDto {|AL0024:body|}) { } }
                      """);

    [Theory]
    [InlineData($"{Ff} FormDto form", "only FromForm")]
    [InlineData($"{Fb} BodyDto body", "only FromBody")]
    public Task AL0024_ShouldNotReport(string param, string _) =>
        VerifyAsync($$"""
                      public class FormDto { public string Name { get; set; } }
                      public class BodyDto { public string Data { get; set; } }
                      public class C { void M({{param}}) { } }
                      """);

    #endregion
}
