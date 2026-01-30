using ANcpLua.Analyzers.CodeFixes.Refactorings;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AR0002: Make Static Lambda Refactoring.
/// </summary>
public sealed partial class Ar0002RefactoringTests
    : SolutionRefactoringTest<Ar0002MakeStaticLambdaRefactoring>
{
    private const string SingleLambdaSource = """
        using System;
        public class C {
            Func<int, int> f = x => x * 2;
        }
        """;

    private const string SingleLambdaExpected = """
        using System;
        public class C {
            Func<int, int> f = static x => x * 2;
        }
        """;

    private const string MultipleLambdasSource = """
        using System;
        using System.Linq;
        public class C {
            void M() {
                var list = new[] { 1, 2, 3 };
                list.Where(x => x > 0).Select(y => y * 2);
            }
        }
        """;

    private const string MultipleLambdasExpected = """
        using System;
        using System.Linq;
        public class C {
            void M() {
                var list = new[] { 1, 2, 3 };
                list.Where(static x => x > 0).Select(static y => y * 2);
            }
        }
        """;

    private const string CapturingLambdaSource = """
        using System;
        public class C {
            void M() {
                int captured = 5;
                Func<int, int> f = x => x + captured;
            }
        }
        """;

    private const string AlreadyStaticSource = """
        using System;
        public class C {
            Func<int, int> f = static x => x * 2;
        }
        """;

    [Fact]
    public Task MakeStaticSingle_ShouldAddStaticKeyword() =>
        VerifyMultiDocumentAsync(
            documents: [("Test.cs", SingleLambdaSource)],
            expected: [("Test.cs", SingleLambdaExpected)],
            triggerFile: "Test.cs",
            triggerText: "x => x * 2",
            refactoringTitle: "Make lambda static");

    [Fact]
    public Task MakeStaticInFile_ShouldFixAllLambdasInDocument() =>
        VerifyMultiDocumentAsync(
            documents: [("Test.cs", MultipleLambdasSource)],
            expected: [("Test.cs", MultipleLambdasExpected)],
            triggerFile: "Test.cs",
            triggerText: "x => x > 0",
            refactoringTitle: "Make all lambdas static in file");

    [Fact]
    public Task MakeStaticInSolution_ShouldFixAllLambdasAcrossDocuments() =>
        VerifyMultiDocumentAsync(
            documents: [
                ("File1.cs", SingleLambdaSource),
                ("File2.cs", """
                    using System;
                    public class C2 {
                        Func<int, int> g = y => y + 1;
                    }
                    """)
            ],
            expected: [
                ("File1.cs", SingleLambdaExpected),
                ("File2.cs", """
                    using System;
                    public class C2 {
                        Func<int, int> g = static y => y + 1;
                    }
                    """)
            ],
            triggerFile: "File1.cs",
            triggerText: "x => x * 2",
            refactoringTitle: "Make all lambdas static in solution");

    [Fact]
    public Task MakeStaticInProject_ShouldOnlyFixLambdasInCurrentProject() =>
        VerifyMultiProjectAsync(
            projects: [
                ("Project1", [("File1.cs", SingleLambdaSource)]),
                ("Project2", [("File2.cs", """
                    using System;
                    public class C2 {
                        Func<int, int> g = y => y + 1;
                    }
                    """)])
            ],
            expected: [
                ("Project1", [("File1.cs", SingleLambdaExpected)]),
                ("Project2", [("File2.cs", """
                    using System;
                    public class C2 {
                        Func<int, int> g = y => y + 1;
                    }
                    """)])  // Unchanged - different project
            ],
            triggerProject: "Project1",
            triggerFile: "File1.cs",
            triggerText: "x => x * 2",
            refactoringTitle: "Make all lambdas static in project");

    [Fact]
    public Task MakeStaticInSolution_ShouldFixAllLambdasAcrossMultipleProjects() =>
        VerifyMultiProjectAsync(
            projects: [
                ("Project1", [("File1.cs", SingleLambdaSource)]),
                ("Project2", [("File2.cs", """
                    using System;
                    public class C2 {
                        Func<int, int> g = y => y + 1;
                    }
                    """)]),
                ("Project3", [("File3.cs", """
                    using System;
                    public class C3 {
                        Func<int, int> h = z => z - 1;
                    }
                    """)])
            ],
            expected: [
                ("Project1", [("File1.cs", SingleLambdaExpected)]),
                ("Project2", [("File2.cs", """
                    using System;
                    public class C2 {
                        Func<int, int> g = static y => y + 1;
                    }
                    """)]),
                ("Project3", [("File3.cs", """
                    using System;
                    public class C3 {
                        Func<int, int> h = static z => z - 1;
                    }
                    """)])
            ],
            triggerProject: "Project1",
            triggerFile: "File1.cs",
            triggerText: "x => x * 2",
            refactoringTitle: "Make all lambdas static in solution");

    [Fact]
    public Task ShouldNotOfferRefactoring_WhenLambdaCapturesVariable() =>
        VerifyNoRefactoringAsync(
            documents: [("Test.cs", CapturingLambdaSource)],
            triggerFile: "Test.cs",
            triggerText: "x => x + captured");

    [Fact]
    public Task ShouldNotOfferRefactoring_WhenLambdaIsAlreadyStatic() =>
        VerifyNoRefactoringAsync(
            documents: [("Test.cs", AlreadyStaticSource)],
            triggerFile: "Test.cs",
            triggerText: "static x => x * 2");
}
