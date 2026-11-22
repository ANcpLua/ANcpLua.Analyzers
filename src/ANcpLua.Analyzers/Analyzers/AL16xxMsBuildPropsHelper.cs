using Microsoft.CodeAnalysis.Text;
using System.Xml;
using System.Xml.Linq;

namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     Shared MSBuild props-file helpers for the AL16xx version-management analyzers.
/// </summary>
internal static partial class Al16xxMsBuildPropsHelper {
    /// <summary>
    ///     Checks whether the props file sets <c>ManagePackageVersionsCentrally</c> to <c>true</c>
    ///     (CPM native mode). Unreadable or malformed files count as <c>false</c>.
    /// </summary>
    internal static bool IsCpmNativeMode(AdditionalText propsFile, CancellationToken cancellationToken) {
        if (propsFile.GetText(cancellationToken) is not { } sourceText) {
            return false;
        }

        try {
            var doc = XDocument.Parse(sourceText.ToString());

            return doc.Descendants()
                .Any(static e =>
                    e.Name.LocalName == "ManagePackageVersionsCentrally" &&
                    string.Equals(e.Value.Trim(), "true", StringComparison.OrdinalIgnoreCase));
        } catch (Exception) {
            return false;
        }
    }

    /// <summary>
    ///     Creates a location that covers the attribute's line and points at the attribute's start.
    ///     Returns <see cref="Location.None" /> when the document was parsed without line info.
    /// </summary>
    internal static Location CreateAttributeLocation(AdditionalText propsFile, SourceText sourceText, XAttribute attribute) {
        IXmlLineInfo lineInfo = attribute;
        if (lineInfo.HasLineInfo()) {
            var linePosition = new LinePosition(lineInfo.LineNumber - 1, lineInfo.LinePosition - 1);
            var textSpan = sourceText.Lines[lineInfo.LineNumber - 1].Span;
            return Location.Create(propsFile.Path, textSpan,
                new LinePositionSpan(linePosition, linePosition));
        }

        return Location.None;
    }
}
