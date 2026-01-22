using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Analyzers.CodeFixes.CodeFixes;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL0012: Detects usage of deprecated OpenTelemetry semantic convention attributes.
/// </summary>
public sealed class Al0012AnalyzerTests : AnalyzerTest<Al0012DeprecatedAttributeAnalyzer> {
    [Theory]
    [InlineData("""
                using System.Collections.Generic;

                public class C {
                    void M() {
                        var attributes = new Dictionary<string, object>();
                        attributes[[|"http.method"|]] = "GET";
                    }
                }
                """)]
    [InlineData("""
                using System.Collections.Generic;

                public class C {
                    void M() {
                        var tags = new Dictionary<string, object>();
                        tags[[|"http.status_code"|]] = 200;
                    }
                }
                """)]
    [InlineData("""
                using System.Collections.Generic;

                public class C {
                    void M() {
                        var attrs = new Dictionary<string, object>();
                        attrs[[|"db.statement"|]] = "SELECT * FROM users";
                    }
                }
                """)]
    [InlineData("""
                using System.Collections.Generic;

                public class C {
                    void M() {
                        var attr = new Dictionary<string, object>();
                        attr[[|"net.host.name"|]] = "localhost";
                    }
                }
                """)]
    public Task ShouldReportDeprecatedAttributesInDictionary(string source) => VerifyAsync(source);

    [Theory]
    [InlineData("""
                public class Tag {
                    public string Key { get; set; }
                    public object Value { get; set; }
                }

                public class C {
                    void M() {
                        var tag = new Tag { Key = [|"http.url"|], Value = "https://example.com" };
                    }
                }
                """)]
    [InlineData("""
                public class C {
                    void SetAttribute(string key, object value) { }
                    void M() {
                        SetAttribute([|"code.filepath"|], "/path/to/file.cs");
                    }
                }
                """)]
    [InlineData("""
                public class C {
                    void AddTag(string key, object value) { }
                    void M() {
                        AddTag([|"faas.execution"|], "123");
                    }
                }
                """)]
    public Task ShouldReportDeprecatedAttributesInTelemetryContext(string source) => VerifyAsync(source);

    [Theory]
    [InlineData("""
                using System.Collections.Generic;

                public class C {
                    void M() {
                        var attributes = new Dictionary<string, object>();
                        attributes["http.request.method"] = "GET";
                    }
                }
                """)]
    [InlineData("""
                using System.Collections.Generic;

                public class C {
                    void M() {
                        var tags = new Dictionary<string, object>();
                        tags["http.response.status_code"] = 200;
                    }
                }
                """)]
    [InlineData("""
                using System.Collections.Generic;

                public class C {
                    void M() {
                        var attrs = new Dictionary<string, object>();
                        attrs["db.query.text"] = "SELECT * FROM users";
                    }
                }
                """)]
    public Task ShouldNotReportModernAttributes(string source) => VerifyAsync(source);

    [Theory]
    [InlineData("""
                public class C {
                    void M() {
                        var method = "http.method";
                    }
                }
                """)]
    [InlineData("""
                public class C {
                    void Log(string message) { }
                    void M() {
                        Log("http.method is deprecated");
                    }
                }
                """)]
    public Task ShouldNotReportOutsideTelemetryContext(string source) => VerifyAsync(source);
}

/// <summary>
///     Code fix tests for AL0012: Replaces deprecated attributes with modern equivalents.
/// </summary>
public sealed class
    Al0012CodeFixTests : CodeFixTest<Al0012DeprecatedAttributeAnalyzer, Al0012DeprecatedAttributeCodeFixProvider> {
    [Fact]
    public Task ShouldReplaceHttpMethod() => VerifyAsync(
        """
        using System.Collections.Generic;

        public class C {
            void M() {
                var attributes = new Dictionary<string, object>();
                attributes[[|"http.method"|]] = "GET";
            }
        }
        """,
        """
        using System.Collections.Generic;

        public class C {
            void M() {
                var attributes = new Dictionary<string, object>();
                attributes["http.request.method"] = "GET";
            }
        }
        """);

    [Fact]
    public Task ShouldReplaceHttpStatusCode() => VerifyAsync(
        """
        using System.Collections.Generic;

        public class C {
            void M() {
                var tags = new Dictionary<string, object>();
                tags[[|"http.status_code"|]] = 200;
            }
        }
        """,
        """
        using System.Collections.Generic;

        public class C {
            void M() {
                var tags = new Dictionary<string, object>();
                tags["http.response.status_code"] = 200;
            }
        }
        """);

    [Fact]
    public Task ShouldReplaceDbStatement() => VerifyAsync(
        """
        using System.Collections.Generic;

        public class C {
            void M() {
                var attrs = new Dictionary<string, object>();
                attrs[[|"db.statement"|]] = "SELECT 1";
            }
        }
        """,
        """
        using System.Collections.Generic;

        public class C {
            void M() {
                var attrs = new Dictionary<string, object>();
                attrs["db.query.text"] = "SELECT 1";
            }
        }
        """);

    [Fact]
    public Task ShouldReplaceNetHostName() => VerifyAsync(
        """
        using System.Collections.Generic;

        public class C {
            void M() {
                var attr = new Dictionary<string, object>();
                attr[[|"net.host.name"|]] = "localhost";
            }
        }
        """,
        """
        using System.Collections.Generic;

        public class C {
            void M() {
                var attr = new Dictionary<string, object>();
                attr["server.address"] = "localhost";
            }
        }
        """);

    [Fact]
    public Task ShouldReplaceCodeFilepath() => VerifyAsync(
        """
        public class C {
            void SetAttribute(string key, object value) { }
            void M() {
                SetAttribute([|"code.filepath"|], "/path/file.cs");
            }
        }
        """,
        """
        public class C {
            void SetAttribute(string key, object value) { }
            void M() {
                SetAttribute("code.file.path", "/path/file.cs");
            }
        }
        """);
}
