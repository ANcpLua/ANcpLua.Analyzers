using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Analyzers.CodeFixes.CodeFixes;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL0007: GetSchema should be explicitly implemented.
/// </summary>
public sealed class Al0007AnalyzerTests : AnalyzerTest<Al0007ToAl0009IXmlSerializableAnalyzer> {
    [Theory]
    [InlineData("""
                using System.Xml;
                using System.Xml.Schema;
                using System.Xml.Serialization;

                public class C : IXmlSerializable {
                    public XmlSchema? {|AL0007:GetSchema|}() => null;
                    public void ReadXml(XmlReader reader) { }
                    public void WriteXml(XmlWriter writer) { }
                }
                """)]
    [InlineData("""
                using System.Xml;
                using System.Xml.Schema;
                using System.Xml.Serialization;

                public class C : IXmlSerializable {
                    public XmlSchema? {|AL0007:GetSchema|}() { return null; }
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
    public Task ShouldNotReportWhenExplicitlyImplemented(string source) => VerifyAsync(source);
}

/// <summary>
///     Tests for AL0008: GetSchema must return null and not be abstract.
/// </summary>
public sealed class Al0008AnalyzerTests : AnalyzerTest<Al0007ToAl0009IXmlSerializableAnalyzer> {
    [Theory]
    [InlineData("""
                using System.Xml;
                using System.Xml.Schema;
                using System.Xml.Serialization;

                public class C : IXmlSerializable {
                    XmlSchema? IXmlSerializable.GetSchema() {|AL0008:=> new XmlSchema()|};
                    public void ReadXml(XmlReader reader) { }
                    public void WriteXml(XmlWriter writer) { }
                }
                """)]
    [InlineData("""
                using System.Xml;
                using System.Xml.Schema;
                using System.Xml.Serialization;

                public class C : IXmlSerializable {
                    XmlSchema? IXmlSerializable.GetSchema() {|AL0008:{ return new XmlSchema(); }|}
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
                    {|AL0008:public abstract XmlSchema? {|AL0007:GetSchema|}();|}
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
///     Tests for AL0009: Don't call GetSchema.
/// </summary>
public sealed class Al0009AnalyzerTests : AnalyzerTest<Al0007ToAl0009IXmlSerializableAnalyzer> {
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

                public class D {
                    void M(IXmlSerializable x) {
                        _ = {|AL0009:x.GetSchema()|};
                    }
                }
                """)]
    public Task ShouldReportWhenCallingGetSchema(string source) => VerifyAsync(source);
}

/// <summary>
///     Code fix tests for AL0008: Makes GetSchema return null.
/// </summary>
public sealed class
    Al0008CodeFixTests : CodeFixTest<Al0007ToAl0009IXmlSerializableAnalyzer, Al0008IXmlSerializableCodeFixProvider> {
    [Fact]
    public Task ShouldReplaceNonNullReturnWithNull() => VerifyAsync(
        """
        using System.Xml;
        using System.Xml.Schema;
        using System.Xml.Serialization;

        public class C : IXmlSerializable {
            XmlSchema? IXmlSerializable.GetSchema() {|AL0008:=> new XmlSchema()|};
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
            XmlSchema? IXmlSerializable.GetSchema() {|AL0008:{ return new XmlSchema(); }|}
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
