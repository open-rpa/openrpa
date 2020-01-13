using ICSharpCode.AvalonEdit.Highlighting;
using Microsoft.CodeAnalysis.Classification;
using System.Collections.Generic;
using System.Windows.Media;

namespace OpenRPA.CodeEditor
{
    internal static class CodeHighlightColors
    {
        private static readonly CachedHighlightingColor defaultHighlightingColor = new CachedHighlightingColor(Colors.Black);
        private static readonly CachedHighlightingColor typeHighlightingColor = new CachedHighlightingColor(Colors.Teal);
        private static readonly CachedHighlightingColor commentHighlightingColor = new CachedHighlightingColor(Colors.Green);
        private static readonly CachedHighlightingColor xmlCommentHighlightingColor = new CachedHighlightingColor(Colors.Gray);
        private static readonly CachedHighlightingColor keywordHighlightingColor = new CachedHighlightingColor(Colors.Blue);
        private static readonly CachedHighlightingColor preprocessorKeywordHighlightingColor = new CachedHighlightingColor(Colors.Gray);
        private static readonly CachedHighlightingColor stringHighlightingColor = new CachedHighlightingColor(Colors.Maroon);

        private static readonly Dictionary<string, CachedHighlightingColor> highlightingColorsMap = new Dictionary<string, CachedHighlightingColor>
        {
            [ClassificationTypeNames.ClassName] = typeHighlightingColor,
            [ClassificationTypeNames.StructName] = typeHighlightingColor,
            [ClassificationTypeNames.InterfaceName] = typeHighlightingColor,
            [ClassificationTypeNames.DelegateName] = typeHighlightingColor,
            [ClassificationTypeNames.EnumName] = typeHighlightingColor,
            [ClassificationTypeNames.ModuleName] = typeHighlightingColor,
            [ClassificationTypeNames.TypeParameterName] = typeHighlightingColor,
            [ClassificationTypeNames.Comment] = commentHighlightingColor,
            [ClassificationTypeNames.XmlDocCommentAttributeName] = xmlCommentHighlightingColor,
            [ClassificationTypeNames.XmlDocCommentAttributeQuotes] = xmlCommentHighlightingColor,
            [ClassificationTypeNames.XmlDocCommentAttributeValue] = xmlCommentHighlightingColor,
            [ClassificationTypeNames.XmlDocCommentCDataSection] = xmlCommentHighlightingColor,
            [ClassificationTypeNames.XmlDocCommentComment] = xmlCommentHighlightingColor,
            [ClassificationTypeNames.XmlDocCommentDelimiter] = xmlCommentHighlightingColor,
            [ClassificationTypeNames.XmlDocCommentEntityReference] = xmlCommentHighlightingColor,
            [ClassificationTypeNames.XmlDocCommentName] = xmlCommentHighlightingColor,
            [ClassificationTypeNames.XmlDocCommentProcessingInstruction] = xmlCommentHighlightingColor,
            [ClassificationTypeNames.XmlDocCommentText] = commentHighlightingColor,
            [ClassificationTypeNames.Keyword] = keywordHighlightingColor,
            [ClassificationTypeNames.PreprocessorKeyword] = preprocessorKeywordHighlightingColor,
            [ClassificationTypeNames.StringLiteral] = stringHighlightingColor,
            [ClassificationTypeNames.VerbatimStringLiteral] = stringHighlightingColor
        };

        public static HighlightingColor DefaultHighlightingColor => defaultHighlightingColor;

        public static Color GetColor(string classificationTypeName)
        {
            return GetHighlightingColorCore(classificationTypeName).Color;
        }

        public static HighlightingColor GetHighlightingColor(string classificationTypeName)
        {
            return GetHighlightingColorCore(classificationTypeName);
        }

        private static CachedHighlightingColor GetHighlightingColorCore(string classificationTypeName)
        {
            highlightingColorsMap.TryGetValue(classificationTypeName, out var color);
            return color ?? defaultHighlightingColor;
        }


        private sealed class CachedHighlightingColor : HighlightingColor
        {
            public CachedHighlightingColor(Color color)
            {
                Color = color;
                Foreground = new SimpleHighlightingBrush(color);
                Freeze();
            }

            public Color Color { get; }
        }
    }
}
