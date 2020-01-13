using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Tags;
using OpenRPA.Interfaces;

namespace OpenRPA.CodeEditor
{
    public class CodeCompletionData : ICompletionData
    {
        private readonly Lazy<object> description;
        private readonly Func<Task<ImmutableArray<TaggedText>>> getDescriptionFunc;
        private readonly ImmutableArray<string> tags;
        private readonly Lazy<ImageSource> image;
        public CodeCompletionData(string text, Func<Task<ImmutableArray<TaggedText>>> getDescriptionFunc, ImmutableArray<string> tags)
        {
            try
            {
                Text = text;
                description = new Lazy<object>(CreateDescription);
                this.getDescriptionFunc = getDescriptionFunc;
                this.tags = tags;
                image = new Lazy<ImageSource>(GetImage);
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                throw;
            }
        }
        public double Priority => 0;
        public string Text { get; }
        public object Description => description.Value;
        public object Content => Text;
        public ImageSource Image => image.Value;
        public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
        {
            textArea.Document.Replace(completionSegment, Text);
        }
        private object CreateDescription()
        {
            return new CodeCompletionDescription(getDescriptionFunc());
        }
        private ImageSource GetImage()
        {
            var tag = tags.FirstOrDefault();
            if (tag == null) { return null; }
            switch (tag)
            {
                case WellKnownTags.Class:
                    return GetImage("ClassImageSource");
                case WellKnownTags.Constant:
                    return GetImage("ConstantImageSource");
                case WellKnownTags.Delegate:
                    return GetImage("DelegateImageSource");
                case WellKnownTags.Enum:
                    return GetImage("EnumImageSource");
                case WellKnownTags.EnumMember:
                    return GetImage("EnumItemImageSource");
                case WellKnownTags.Event:
                    return GetImage("EventImageSource");
                case WellKnownTags.ExtensionMethod:
                    return GetImage("ExtensionMethodImageSource");
                case WellKnownTags.Field:
                    return GetImage("FieldImageSource");
                case WellKnownTags.Interface:
                    return GetImage("InterfaceImageSource");
                case WellKnownTags.Keyword:
                    return GetImage("KeywordImageSource");
                case WellKnownTags.Method:
                    return GetImage("MethodImageSource");
                case WellKnownTags.Module:
                    return GetImage("ModuleImageSource");
                case WellKnownTags.Namespace:
                    return GetImage("NamespaceImageSource");
                case WellKnownTags.Property:
                    return GetImage("PropertyImageSource");
                case WellKnownTags.Structure:
                    return GetImage("StructureImageSource");
            }
            return null;
        }
        private static ImageSource GetImage(string resourceKey)
        {
            return (ImageSource)Application.Current.Resources[resourceKey];
        }
    }
}
