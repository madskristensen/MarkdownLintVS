using System.Linq;
using MarkdownLintVS.Linting;
using Microsoft.VisualStudio.Text;

namespace MarkdownLintVS.CodeFixes.Actions
{
    /// <summary>
    /// Fix action to add missing column separators to an incomplete table delimiter row (MD056).
    /// For example, fixes "|---|--|" to "|---|---|---|" when the header has 3 columns.
    /// </summary>
    [FixForRule("MD056", RequiresFactory = true)]
    public class FixTableDelimiterAction(ITextSnapshot snapshot, Span span, int expectedColumns)
        : MarkdownFixAction(snapshot, span)
    {
        public override string DisplayText => "Fix table delimiter row";

        /// <summary>
        /// Creates a fix action if the violation is on a delimiter row with too few columns.
        /// Returns null for data row violations (those can't be safely auto-fixed).
        /// </summary>
        public static MarkdownFixAction Create(ITextSnapshot snapshot, Span span, LintViolation violation)
        {
            (int Expected, int Actual)? counts = ViolationMessageParser.ExtractExpectedAndActualCount(violation.Message);
            if (counts == null)
                return null;

            // Only fix delimiter rows (actual < expected and the line looks like a delimiter)
            ITextSnapshotLine line = snapshot.GetLineFromPosition(span.Start);
            var lineText = line.GetText().Trim();
            var isDelimiter = lineText.Length > 0 &&
                lineText.All(c => c == '|' || c == '-' || c == ':' || char.IsWhiteSpace(c));

            if (!isDelimiter || counts.Value.Actual >= counts.Value.Expected)
                return null;

            return new FixTableDelimiterAction(snapshot, span, counts.Value.Expected);
        }

        public override void ApplyFix(ITextEdit edit)
        {
            ITextSnapshotLine line = Snapshot.GetLineFromPosition(Span.Start);
            edit.Replace(line.Start, line.Length, GetFixedText());
        }

        protected override string GetFixedText()
        {
            ITextSnapshotLine line = Snapshot.GetLineFromPosition(Span.Start);
            var text = line.GetText();

            // Parse existing delimiter segments to preserve their style (e.g., :---:, ---:)
            var trimmed = text.Trim();
            var hasLeadingPipe = trimmed.StartsWith("|");
            var hasTrailingPipe = trimmed.EndsWith("|");

            var inner = trimmed;
            if (hasLeadingPipe)
                inner = inner.Substring(1);
            if (hasTrailingPipe)
                inner = inner.Substring(0, inner.Length - 1);

            var existingSegments = inner.Split('|');
            var segments = new string[expectedColumns];

            for (var i = 0; i < expectedColumns; i++)
            {
                if (i < existingSegments.Length && existingSegments[i].Contains("-"))
                {
                    // Keep existing segment style (preserves alignment markers)
                    segments[i] = existingSegments[i];
                }
                else
                {
                    // Add a new default segment matching the spacing style of the first segment
                    segments[i] = existingSegments.Length > 0 && existingSegments[0].Contains(" ")
                        ? " --- "
                        : "---";
                }
            }

            var result = string.Join("|", segments);
            if (hasLeadingPipe)
                result = "|" + result;
            if (hasTrailingPipe)
                result += "|";

            return result;
        }
    }
}
