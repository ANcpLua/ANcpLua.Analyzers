using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL0088: Detects potential PII or credential data in span attributes.
/// </summary>
public sealed partial class Al0088SensitiveDataInAttributeTests : AnalyzerTest<Al0088SensitiveDataInAttributeAnalyzer> {
    [Theory]
    [InlineData("""
                using System.Collections.Generic;

                public class C {
                    void M() {
                        var attributes = new Dictionary<string, object>();
                        attributes[[|"password"|]] = "secret123";
                    }
                }
                """)]
    [InlineData("""
                using System.Collections.Generic;

                public class C {
                    void M() {
                        var tags = new Dictionary<string, object>();
                        tags[[|"user.password"|]] = "secret";
                    }
                }
                """)]
    [InlineData("""
                using System.Collections.Generic;

                public class C {
                    void M() {
                        var attrs = new Dictionary<string, object>();
                        attrs[[|"api_key"|]] = "sk-12345";
                    }
                }
                """)]
    [InlineData("""
                using System.Collections.Generic;

                public class C {
                    void M() {
                        var attr = new Dictionary<string, object>();
                        attr[[|"access_token"|]] = "eyJhbGc...";
                    }
                }
                """)]
    [InlineData("""
                using System.Collections.Generic;

                public class C {
                    void M() {
                        var attributes = new Dictionary<string, object>();
                        attributes[[|"secret_key"|]] = "abc123";
                    }
                }
                """)]
    public Task ShouldReportSensitiveAttributeNamesInDictionary(string source) => VerifyAsync(source);

    [Theory]
    [InlineData("""
                using System.Collections.Generic;

                public class C {
                    void M() {
                        var attributes = new Dictionary<string, object>();
                        attributes[[|"ssn"|]] = "123-45-6789";
                    }
                }
                """)]
    [InlineData("""
                using System.Collections.Generic;

                public class C {
                    void M() {
                        var tags = new Dictionary<string, object>();
                        tags[[|"credit_card"|]] = "4111111111111111";
                    }
                }
                """)]
    [InlineData("""
                using System.Collections.Generic;

                public class C {
                    void M() {
                        var attrs = new Dictionary<string, object>();
                        attrs[[|"social_security"|]] = "123456789";
                    }
                }
                """)]
    public Task ShouldReportPiiAttributeNames(string source) => VerifyAsync(source);

    [Theory]
    [InlineData("""
                public class C {
                    void SetTag(string key, object value) { }
                    void M() {
                        SetTag([|"auth_token"|], "bearer xyz");
                    }
                }
                """)]
    [InlineData("""
                public class C {
                    void AddAttribute(string key, object value) { }
                    void M() {
                        AddAttribute([|"bearer"|], "token123");
                    }
                }
                """)]
    [InlineData("""
                public class C {
                    void SetAttribute(string key, object value) { }
                    void M() {
                        SetAttribute([|"private_key"|], "-----BEGIN RSA PRIVATE KEY-----");
                    }
                }
                """)]
    public Task ShouldReportSensitiveAttributesInTelemetryMethods(string source) => VerifyAsync(source);

    [Theory]
    [InlineData("""
                using System.Collections.Generic;

                public class C {
                    void M() {
                        var attributes = new Dictionary<string, object>();
                        attributes["http.method"] = "GET";
                    }
                }
                """)]
    [InlineData("""
                using System.Collections.Generic;

                public class C {
                    void M() {
                        var tags = new Dictionary<string, object>();
                        tags["user.name"] = "john";
                    }
                }
                """)]
    [InlineData("""
                using System.Collections.Generic;

                public class C {
                    void M() {
                        var attrs = new Dictionary<string, object>();
                        attrs["db.operation"] = "SELECT";
                    }
                }
                """)]
    public Task ShouldNotReportNonSensitiveAttributes(string source) => VerifyAsync(source);

    [Theory]
    [InlineData("""
                public class C {
                    void M() {
                        var password = "secret123";
                    }
                }
                """)]
    [InlineData("""
                public class C {
                    void Log(string message) { }
                    void M() {
                        Log("user entered password");
                    }
                }
                """)]
    public Task ShouldNotReportOutsideTelemetryContext(string source) => VerifyAsync(source);

    [Theory]
    [InlineData("""
                using System.Collections.Generic;

                public class C {
                    void M() {
                        var span = new Dictionary<string, object>();
                        span[[|"connection_string"|]] = "Server=localhost;Password=secret";
                    }
                }
                """)]
    [InlineData("""
                using System.Collections.Generic;

                public class C {
                    void M() {
                        var activity = new Dictionary<string, object>();
                        activity[[|"credentials"|]] = "user:pass";
                    }
                }
                """)]
    public Task ShouldReportInSpanAndActivityContexts(string source) => VerifyAsync(source);

    [Theory]
    [InlineData("""
                using System.Collections.Generic;

                public class C {
                    void M() {
                        var attributes = new Dictionary<string, object>();
                        attributes[[|"user_authorization"|]] = "Basic xyz";
                    }
                }
                """)]
    [InlineData("""
                using System.Collections.Generic;

                public class C {
                    void M() {
                        var tags = new Dictionary<string, object>();
                        tags[[|"cvv"|]] = "123";
                    }
                }
                """)]
    [InlineData("""
                using System.Collections.Generic;

                public class C {
                    void M() {
                        var attrs = new Dictionary<string, object>();
                        attrs[[|"pin"|]] = "1234";
                    }
                }
                """)]
    public Task ShouldReportVariousSensitivePatterns(string source) => VerifyAsync(source);
}
