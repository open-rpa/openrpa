using Microsoft.CodeAnalysis;
using OpenRPA.Interfaces;
using System;
using System.Collections.Immutable;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;

namespace OpenRPA.CodeEditor
{
    public class TaggedTextListConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var list = (ImmutableArray<TaggedText>)value;
            if (list.IsDefault)
            {
                return "...";
            }
            return CreateTextBlock(list);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        private static TextBlock CreateTextBlock(ImmutableArray<TaggedText> text)
        {
            var textBlock = new TextBlock() { MaxWidth = 600, TextWrapping = TextWrapping.Wrap };
            try
            {
                foreach (var part in text)
                {
                    var res = CreateRun(part);
                    if (res != null) textBlock.Inlines.Add(res);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
            return textBlock;
        }

        private static Run CreateRun(TaggedText text)
        {
            try
            {
                var run = new Run(text.ToString());
                var classificationTypeName = ClassificationTags.GetClassificationTypeName(text.Tag);
                run.Foreground = new SolidColorBrush(CodeHighlightColors.GetColor(classificationTypeName));
                return run;
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                return null;
            }
        }
    }
}
