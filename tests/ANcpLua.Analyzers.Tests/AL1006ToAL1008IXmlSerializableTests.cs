using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Analyzers.CodeFixes.CodeFixes;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL1006: GetSchema should be explicitly implemented.
/// </summary>
public sealed partial class Al1006AnalyzerTests : AnalyzerTest<Al1006ToAl1008IXmlSerializableAnalyzer> {
    [Theory]
    [InlineData("""
                using System.Xml;
                using System.Xml.Schema;
                using System.Xml.Serialization;

                public class C : IXmlSerializable {
                    public XmlSchema? {|AL1006:GetSchema|}() => null;
                    public void ReadXml(XmlReader reader) { }
                    public void WriteXml(XmlWriter writer) { }
                }
                """)]
    [InlineData("""
                using System.Xml;
                using System.Xml.Schema;
                using System.Xml.Serialization;

                public class C : IXmlSerializable {
                    public XmlSchema? {|AL1006:GetSchema|}() { return null; }
                    public void ReadXml(XmlReader reader) { }
                    public void WriteXml(XmlWriter writer) { }
                }
                """)]
    public Task ShouldReportWhenGetSchemaNotExplicit(string source) => VerifyAsync(source);

    [Theory]
    [InlineData("""
                using System.Xml;
                using System.Xml.Schema;
                using System.Xml.Serialization;

                public class C : IXmlSerializable {
                    XmlSchema? IXmlSerializable.GetSchema() => null;
                    public void ReadXml(XmlReader reader) { }
                    public void WriteXml(XmlWriter writer) { }
                }
                """)]
    [InlineData("""
                using System.Xml;
                using System.Xml.Schema;
                using System.Xml.Serialization;

                public class C : IXmlSerializable {
                    XmlSchema? IXmlSerializable.GetSchema() => null;

                    public XmlSchema? GetSchema(string format) => new XmlSchema();

                    public void ReadXml(XmlReader reader) { }
                    public void WriteXml(XmlWriter writer) { }
                }
                """)]
    public Task ShouldNotReportWhenExplicitlyImplemented(string source) => VerifyAsync(source);
}

/// <summary>
///     Tests for AL1007: GetSchema must return null and not be abstract.
/// </summary>
public sealed partial class Al1007AnalyzerTests : AnalyzerTest<Al1006ToAl1008IXmlSerializableAnalyzer> {
    [Theory]
    [InlineData("""
                using System.Xml;
                using System.Xml.Schema;
                using System.Xml.Serialization;

                public class C : IXmlSerializable {
                    XmlSchema? IXmlSerializable.GetSchema() {|AL1007:=> new XmlSchema()|};
                    public void ReadXml(XmlReader reader) { }
                    public void WriteXml(XmlWriter writer) { }
                }
                """)]
    [InlineData("""
                using System.Xml;
                using System.Xml.Schema;
                using System.Xml.Serialization;

                public class C : IXmlSerializable {
                    XmlSchema? IXmlSerializable.GetSchema() {|AL1007:{ return new XmlSchema(); }|}
                    public void ReadXml(XmlReader reader) { }
                    public void WriteXml(XmlWriter writer) { }
                }
                """)]
    public Task ShouldReportWhenReturnsNonNull(string source) => VerifyAsync(source);

    [Theory]
    [InlineData("""
                using System.Xml;
                using System.Xml.Schema;
                using System.Xml.Serialization;

                public abstract class C : IXmlSerializable {
                    {|AL1007:public abstract XmlSchema? {|AL1006:GetSchema|}();|}
                    public void ReadXml(XmlReader reader) { }
                    public void WriteXml(XmlWriter writer) { }
                }
                """)]
    public Task ShouldReportWhenAbstract(string source) => VerifyAsync(source);

    [Theory]
    [InlineData("""
                using System.Xml;
                using System.Xml.Schema;
                using System.Xml.Serialization;

                public class C : IXmlSerializable {
                    XmlSchema? IXmlSerializable.GetSchema() => null;
                    public XmlSchema? GetSchema(string format) => new XmlSchema();
                    public void ReadXml(XmlReader reader) { }
                    public void WriteXml(XmlWriter writer) { }
                }
                """)]
    [InlineData("""
                using System.Xml;
                using System.Xml.Schema;
                using System.Xml.Serialization;

                public class C : IXmlSerializable {
                    XmlSchema? IXmlSerializable.GetSchema() { return null; }
                    public void ReadXml(XmlReader reader) { }
                    public void WriteXml(XmlWriter writer) { }
                }
                """)]
    public Task ShouldNotReportWhenReturnsNull(string source) => VerifyAsync(source);
}

/// <summary>
///     Tests for AL1008: Don't call GetSchema.
/// </summary>
public sealed partial class Al1008AnalyzerTests : AnalyzerTest<Al1006ToAl1008IXmlSerializableAnalyzer> {
    [Theory]
    [InlineData("""
                using System.Xml;
                using System.Xml.Schema;
                using System.Xml.Serialization;

                public class C : IXmlSerializable {
                    XmlSchema? IXmlSerializable.GetSchema() => null;
                    public void ReadXml(XmlReader reader) { }
                    public void WriteXml(XmlWriter writer) { }
                }

                public class C2 : IXmlSerializable {
                    XmlSchema? IXmlSerializable.GetSchema() => null;

                    public XmlSchema? GetSchema(string format) => null;
                    public void ReadXml(XmlReader reader) { }
                    public void WriteXml(XmlWriter writer) { }
                }

                public class D {
                    void M(IXmlSerializable x) {
                        _ = {|AL1008:x.GetSchema()|};
                    }

                    void M2(C2 x) {
                        _ = x.GetSchema("format");
                    }
                }
                """)]
    public Task ShouldReportWhenCallingGetSchema(string source) => VerifyAsync(source);
}

/// <summary>
///     Code fix tests for AL1007: Makes GetSchema return null.
/// </summary>
public sealed partial class
    Al1007CodeFixTests : CodeFixTest<Al1006ToAl1008IXmlSerializableAnalyzer, Al1007IXmlSerializableCodeFixProvider> {
    [Fact]
    public Task ShouldReplaceNonNullReturnWithNull() => VerifyAsync(
        """
        using System.Xml;
        using System.Xml.Schema;
        using System.Xml.Serialization;

        public class C : IXmlSerializable {
            XmlSchema? IXmlSerializable.GetSchema() {|AL1007:=> new XmlSchema()|};
            public void ReadXml(XmlReader reader) { }
            public void WriteXml(XmlWriter writer) { }
        }
        """,
        """
        using System.Xml;
        using System.Xml.Schema;
        using System.Xml.Serialization;

        public class C : IXmlSerializable {
            XmlSchema? IXmlSerializable.GetSchema() => null;
            public void ReadXml(XmlReader reader) { }
            public void WriteXml(XmlWriter writer) { }
        }
        """);

    [Fact]
    public Task ShouldReplaceBlockBodyWithNullArrow() => VerifyAsync(
        """
        using System.Xml;
        using System.Xml.Schema;
        using System.Xml.Serialization;

        public class C : IXmlSerializable {
            XmlSchema? IXmlSerializable.GetSchema() {|AL1007:{ return new XmlSchema(); }|}
            public void ReadXml(XmlReader reader) { }
            public void WriteXml(XmlWriter writer) { }
        }
        """,
        """
        using System.Xml;
        using System.Xml.Schema;
        using System.Xml.Serialization;

        public class C : IXmlSerializable {
            XmlSchema? IXmlSerializable.GetSchema() => null;
            public void ReadXml(XmlReader reader) { }
            public void WriteXml(XmlWriter writer) { }
        }
        """);
}
