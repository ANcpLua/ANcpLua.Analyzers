using ANcpLua.Analyzers.CodeFixes.Refactorings;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Integration tests for AR0002: Make Static Lambda Refactoring.
///     These tests verify the refactoring actually applies changes correctly.
/// </summary>
public sealed partial class Ar0002RefactoringTests : IDisposable {
    private static readonly MetadataReference[] References = [
        MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(Console).Assembly.Location)
    ];

    private readonly AdhocWorkspace _workspace = new();

    public void Dispose() => _workspace.Dispose();

    [Fact]
    public async Task MakeStaticSingle_ShouldAddStaticKeyword() {
        const string Source = """
                              using System;
                              public class C {
                                  Func<int, int> f = x => x * 2;
                              }
                              """;

        const string Expected = """
                                using System;
                                public class C {
                                    Func<int, int> f = static x => x * 2;
                                }
                                """;

        var (document, span) = CreateDocumentWithSpan(Source, "x => x * 2");
        var actions = await GetRefactoringsAsync(document, span);

        actions.Should().Contain(static a => a.Title == "Make lambda static");

        var makeStaticAction = actions.First(static a => a.Title == "Make lambda static");
        var changedDocument = await ApplyCodeActionAsync(document, makeStaticAction);
        var newText = await changedDocument.GetTextAsync(TestContext.Current.CancellationToken);

        newText.ToString().Should().Be(Expected);
    }

    [Fact]
    public async Task MakeStaticInFile_ShouldFixAllLambdasInDocument() {
        const string Source = """
                              using System;
                              using System.Linq;
                              public class C {
                                  void M() {
                                      var list = new[] { 1, 2, 3 };
                                      list.Where(x => x > 0).Select(y => y * 2);
                                  }
                              }
                              """;

        const string Expected = """
                                using System;
                                using System.Linq;
                                public class C {
                                    void M() {
                                        var list = new[] { 1, 2, 3 };
                                        list.Where(static x => x > 0).Select(static y => y * 2);
                                    }
                                }
                                """;

        var (document, span) = CreateDocumentWithSpan(Source, "x => x > 0");
        var actions = await GetRefactoringsAsync(document, span);

        actions.Should().Contain(static a => a.Title == "Make all lambdas static in file");

        var makeStaticInFileAction = actions.First(static a => a.Title == "Make all lambdas static in file");
        var changedDocument = await ApplyCodeActionAsync(document, makeStaticInFileAction);
        var newText = await changedDocument.GetTextAsync(TestContext.Current.CancellationToken);

        newText.ToString().Should().Be(Expected);
    }

    [Fact]
    public async Task MakeStaticInSolution_ShouldFixAllLambdasAcrossDocuments() {
        const string Source1 = """
                               using System;
                               public class C1 {
                                   Func<int, int> f = x => x * 2;
                               }
                               """;

        const string Source2 = """
                               using System;
                               public class C2 {
                                   Func<int, int> g = y => y + 1;
                               }
                               """;

        const string Expected1 = """
                                 using System;
                                 public class C1 {
                                     Func<int, int> f = static x => x * 2;
                                 }
                                 """;

        const string Expected2 = """
                                 using System;
                                 public class C2 {
                                     Func<int, int> g = static y => y + 1;
                                 }
                                 """;

        var solution = CreateSolution(("File1.cs", Source1), ("File2.cs", Source2));
        var document1 = solution.Projects.First().Documents.First(static d => d.Name == "File1.cs");
        var text1 = await document1.GetTextAsync(TestContext.Current.CancellationToken);
        var span = GetSpan(text1, "x => x * 2");

        var actions = await GetRefactoringsAsync(document1, span);

        actions.Should().Contain(static a => a.Title == "Make all lambdas static in solution");

        var makeStaticInSolutionAction = actions.First(static a => a.Title == "Make all lambdas static in solution");
        var changedSolution = await ApplySolutionCodeActionAsync(makeStaticInSolutionAction);

        var changedDoc1 = changedSolution.Projects.First().Documents.First(static d => d.Name == "File1.cs");
        var changedDoc2 = changedSolution.Projects.First().Documents.First(static d => d.Name == "File2.cs");

        var changedText1 = await changedDoc1.GetTextAsync(TestContext.Current.CancellationToken);
        var changedText2 = await changedDoc2.GetTextAsync(TestContext.Current.CancellationToken);

        changedText1.ToString().Should().Be(Expected1);
        changedText2.ToString().Should().Be(Expected2);
    }

    [Fact]
    public async Task MakeStaticInProject_ShouldOnlyFixLambdasInCurrentProject() {
        const string Source1 = """
                               using System;
                               public class C1 {
                                   Func<int, int> f = x => x * 2;
                               }
                               """;

        const string Source2 = """
                               using System;
                               public class C2 {
                                   Func<int, int> g = y => y + 1;
                               }
                               """;

        const string Expected1 = """
                                 using System;
                                 public class C1 {
                                     Func<int, int> f = static x => x * 2;
                                 }
                                 """;

        // Project2 should remain unchanged when applying project-scoped refactoring on Project1
        const string ExpectedUnchanged2 = """
                                          using System;
                                          public class C2 {
                                              Func<int, int> g = y => y + 1;
                                          }
                                          """;

        var solution = CreateMultiProjectSolution(
            ("Project1", "File1.cs", Source1),
            ("Project2", "File2.cs", Source2));

        var document1 = solution.Projects.First(static p => p.Name == "Project1").Documents.First();
        var text1 = await document1.GetTextAsync(TestContext.Current.CancellationToken);
        var span = GetSpan(text1, "x => x * 2");

        var actions = await GetRefactoringsAsync(document1, span);

        actions.Should().Contain(static a => a.Title == "Make all lambdas static in project");

        var makeStaticInProjectAction = actions.First(static a => a.Title == "Make all lambdas static in project");
        var changedSolution = await ApplySolutionCodeActionAsync(makeStaticInProjectAction);

        var changedDoc1 = changedSolution.Projects.First(static p => p.Name == "Project1").Documents.First();
        var changedDoc2 = changedSolution.Projects.First(static p => p.Name == "Project2").Documents.First();

        var changedText1 = await changedDoc1.GetTextAsync(TestContext.Current.CancellationToken);
        var changedText2 = await changedDoc2.GetTextAsync(TestContext.Current.CancellationToken);

        changedText1.ToString().Should().Be(Expected1);
        changedText2.ToString().Should().Be(ExpectedUnchanged2,
            "Project2 should not be affected by project-scoped refactoring on Project1");
    }

    [Fact]
    public async Task MakeStaticInSolution_ShouldFixAllLambdasAcrossMultipleProjects() {
        const string Source1 = """
                               using System;
                               public class C1 {
                                   Func<int, int> f = x => x * 2;
                               }
                               """;

        const string Source2 = """
                               using System;
                               public class C2 {
                                   Func<int, int> g = y => y + 1;
                               }
                               """;

        const string Source3 = """
                               using System;
                               public class C3 {
                                   Func<int, int> h = z => z - 1;
                               }
                               """;

        const string Expected1 = """
                                 using System;
                                 public class C1 {
                                     Func<int, int> f = static x => x * 2;
                                 }
                                 """;

        const string Expected2 = """
                                 using System;
                                 public class C2 {
                                     Func<int, int> g = static y => y + 1;
                                 }
                                 """;

        const string Expected3 = """
                                 using System;
                                 public class C3 {
                                     Func<int, int> h = static z => z - 1;
                                 }
                                 """;

        var solution = CreateMultiProjectSolution(
            ("Project1", "File1.cs", Source1),
            ("Project2", "File2.cs", Source2),
            ("Project3", "File3.cs", Source3));

        var document1 = solution.Projects.First(static p => p.Name == "Project1").Documents.First();
        var text1 = await document1.GetTextAsync(TestContext.Current.CancellationToken);
        var span = GetSpan(text1, "x => x * 2");

        var actions = await GetRefactoringsAsync(document1, span);

        actions.Should().Contain(static a => a.Title == "Make all lambdas static in solution");

        var makeStaticInSolutionAction = actions.First(static a => a.Title == "Make all lambdas static in solution");
        var changedSolution = await ApplySolutionCodeActionAsync(makeStaticInSolutionAction);

        var changedDoc1 = changedSolution.Projects.First(static p => p.Name == "Project1").Documents.First();
        var changedDoc2 = changedSolution.Projects.First(static p => p.Name == "Project2").Documents.First();
        var changedDoc3 = changedSolution.Projects.First(static p => p.Name == "Project3").Documents.First();

        var changedText1 = await changedDoc1.GetTextAsync(TestContext.Current.CancellationToken);
        var changedText2 = await changedDoc2.GetTextAsync(TestContext.Current.CancellationToken);
        var changedText3 = await changedDoc3.GetTextAsync(TestContext.Current.CancellationToken);

        changedText1.ToString().Should().Be(Expected1);
        changedText2.ToString().Should().Be(Expected2);
        changedText3.ToString().Should().Be(Expected3);
    }

    [Fact]
    public async Task ShouldNotOfferRefactoring_WhenLambdaCapturesVariable() {
        const string Source = """
                              using System;
                              public class C {
                                  void M() {
                                      int captured = 5;
                                      Func<int, int> f = x => x + captured;
                                  }
                              }
                              """;

        var (document, span) = CreateDocumentWithSpan(Source, "x => x + captured");
        var actions = await GetRefactoringsAsync(document, span);

        actions.Should().BeEmpty();
    }

    [Fact]
    public async Task ShouldNotOfferRefactoring_WhenLambdaIsAlreadyStatic() {
        const string Source = """
                              using System;
                              public class C {
                                  Func<int, int> f = static x => x * 2;
                              }
                              """;

        var (document, span) = CreateDocumentWithSpan(Source, "static x => x * 2");
        var actions = await GetRefactoringsAsync(document, span);

        actions.Should().BeEmpty();
    }

    private (Document document, TextSpan span) CreateDocumentWithSpan(string source, string textToFind) {
        var project = _workspace.AddProject("TestProject", LanguageNames.CSharp)
            .WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .WithMetadataReferences(References);

        var document = _workspace.AddDocument(project.Id, "Test.cs", SourceText.From(source));
        var span = GetSpan(SourceText.From(source), textToFind);

        return (document, span);
    }

    private Solution CreateSolution(params (string name, string source)[] documents) {
        var project = _workspace.AddProject("TestProject", LanguageNames.CSharp)
            .WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .WithMetadataReferences(References);

        var solution = project.Solution;
        foreach (var (name, source) in documents) {
            var docId = DocumentId.CreateNewId(project.Id, name);
            solution = solution.AddDocument(docId, name, SourceText.From(source));
        }

        return solution;
    }

    private Solution CreateMultiProjectSolution(params (string projectName, string fileName, string source)[] items) {
        var solution = _workspace.CurrentSolution;

        foreach (var (projectName, fileName, source) in items) {
            var projectId = ProjectId.CreateNewId(projectName);
            solution = solution.AddProject(projectId, projectName, projectName, LanguageNames.CSharp)
                .WithProjectCompilationOptions(projectId,
                    new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                .WithProjectMetadataReferences(projectId, References);

            var docId = DocumentId.CreateNewId(projectId, fileName);
            solution = solution.AddDocument(docId, fileName, SourceText.From(source));
        }

        return solution;
    }

    private static TextSpan GetSpan(SourceText text, string textToFind) {
        var start = text.ToString().IndexOf(textToFind, StringComparison.Ordinal);
        return new TextSpan(start, textToFind.Length);
    }

    private static async Task<ImmutableArray<CodeAction>> GetRefactoringsAsync(Document document, TextSpan span) {
        var provider = new Ar0002MakeStaticLambdaRefactoring();
        var actions = new List<CodeAction>();

        var context = new CodeRefactoringContext(
            document,
            span,
            actions.Add,
            TestContext.Current.CancellationToken);

        await provider.ComputeRefactoringsAsync(context);

        return [.. actions];
    }

    private static async Task<Document> ApplyCodeActionAsync(Document document, CodeAction action) {
        var operations = await action.GetOperationsAsync(TestContext.Current.CancellationToken);
        var applyChanges = operations.OfType<ApplyChangesOperation>().First();
        return applyChanges.ChangedSolution.GetDocument(document.Id)!;
    }

    private static async Task<Solution> ApplySolutionCodeActionAsync(CodeAction action) {
        var operations = await action.GetOperationsAsync(TestContext.Current.CancellationToken);
        var applyChanges = operations.OfType<ApplyChangesOperation>().First();
        return applyChanges.ChangedSolution;
    }
}
