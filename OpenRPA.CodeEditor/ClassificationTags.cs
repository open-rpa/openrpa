using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Classification;
using System;

namespace OpenRPA.CodeEditor
{
    public static class ClassificationTags
    {
        public static string GetClassificationTypeName(string textTag)
        {
            switch (textTag)
            {
                case TextTags.Keyword:
                    return ClassificationTypeNames.Keyword;

                case TextTags.Class:
                    return ClassificationTypeNames.ClassName;

                case TextTags.Delegate:
                    return ClassificationTypeNames.DelegateName;

                case TextTags.Enum:
                    return ClassificationTypeNames.EnumName;

                case TextTags.Interface:
                    return ClassificationTypeNames.InterfaceName;

                case TextTags.Module:
                    return ClassificationTypeNames.ModuleName;

                case TextTags.Struct:
                    return ClassificationTypeNames.StructName;

                case TextTags.TypeParameter:
                    return ClassificationTypeNames.TypeParameterName;

                case TextTags.Alias:
                case TextTags.Assembly:
                case TextTags.Field:
                case TextTags.ErrorType:
                case TextTags.Event:
                case TextTags.Label:
                case TextTags.Local:
                case TextTags.Method:
                case TextTags.Namespace:
                case TextTags.Parameter:
                case TextTags.Property:
                case TextTags.RangeVariable:
                    return ClassificationTypeNames.Identifier;

                case TextTags.NumericLiteral:
                    return ClassificationTypeNames.NumericLiteral;

                case TextTags.StringLiteral:
                    return ClassificationTypeNames.StringLiteral;

                case TextTags.Space:
                case TextTags.LineBreak:
                    return ClassificationTypeNames.WhiteSpace;

                case TextTags.Operator:
                    return ClassificationTypeNames.Operator;

                case TextTags.Punctuation:
                    return ClassificationTypeNames.Punctuation;

                case TextTags.AnonymousTypeIndicator:
                case TextTags.Text:
                    return ClassificationTypeNames.Text;

                default:
                    throw new NotSupportedException(textTag);
            }
        }
    }
}
