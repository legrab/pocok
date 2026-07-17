// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Collections.Frozen;
using System.Xml;
using System.Xml.Linq;

namespace Pocok.Localization;

internal static class ResxLocalizationParser
{
    internal static FrozenDictionary<string, string> Parse(string path)
    {
        try
        {
            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                IgnoreComments = false,
                IgnoreWhitespace = false
            };

            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            using var reader = XmlReader.Create(stream, settings);
            var document = XDocument.Load(reader, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);
            XElement root = document.Root ?? throw new FormatException("The RESX document has no root element.");
            if (!root.Name.LocalName.Equals("root", StringComparison.Ordinal))
            {
                throw new FormatException("The RESX document root element must be named 'root'.");
            }

            var entries = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (XElement data in root.Elements().Where(element => element.Name.LocalName == "data"))
            {
                XAttribute? nameAttribute = data.Attribute("name");
                if (nameAttribute is null || string.IsNullOrEmpty(nameAttribute.Value))
                {
                    throw new FormatException("A RESX data element is missing a non-empty name attribute.");
                }

                if (data.Attributes().Any(attribute =>
                    attribute.Name.LocalName is "type" or "mimetype"))
                {
                    throw new FormatException($"RESX resource '{nameAttribute.Value}' must be a plain string entry.");
                }

                XElement[] children = data.Elements().ToArray();
                XElement[] values = children.Where(element => element.Name.LocalName == "value").ToArray();
                var hasUnsupportedChildren = children.Any(element => element.Name.LocalName is not "value" and not "comment");
                if (values.Length != 1 || values[0].Elements().Any() || hasUnsupportedChildren)
                {
                    throw new FormatException($"RESX resource '{nameAttribute.Value}' must contain exactly one plain-text value element and an optional comment.");
                }

                if (!entries.TryAdd(nameAttribute.Value, values[0].Value))
                {
                    throw new FormatException($"Duplicate RESX resource name '{nameAttribute.Value}'.");
                }
            }

            return entries.ToFrozenDictionary(StringComparer.Ordinal);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or XmlException or FormatException)
        {
            throw new FormatException($"Failed to parse RESX localization file '{path}'. {exception.Message}", exception);
        }
    }
}
