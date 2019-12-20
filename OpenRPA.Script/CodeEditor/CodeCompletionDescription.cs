using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace OpenRPA.Script.CodeEditor
{
    public class CodeCompletionDescription : NotifyChange //: Model
    {
        private ImmutableArray<TaggedText> summary;
        public CodeCompletionDescription(Task<ImmutableArray<TaggedText>> lazySummary)
        {
            UpdateSummary(lazySummary);
        }
        public ImmutableArray<TaggedText> Summary
        {
            get => summary;
            private set {
                summary = value;
                NotifyPropertyChanged("Summary");
                //SetProperty(ref summary, value)
            }
        }
        private async void UpdateSummary(Task<ImmutableArray<TaggedText>> lazySummary)
        {
            Summary = await lazySummary;
        }
    }
}
