using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using Microsoft.AspNetCore.Components;

namespace DiffDemo.Components;

public partial class DiffViewer : ComponentBase
{
    [Parameter]
    public string OldText { get; set; } = string.Empty;

    [Parameter]
    public string NewText { get; set; } = string.Empty;

    [Parameter]
    public DiffViewMode ViewMode { get; set; } = DiffViewMode.SideBySide;

    [Parameter]
    public bool ShowLineNumbers { get; set; } = true;

    public enum DiffViewMode
    {
        SideBySide,
        Inline
    }

    private DiffViewMode viewMode => ViewMode;
    private SideBySideDiffModel? sideBySideDiff;
    private DiffPaneModel? inlineDiff;
    private readonly Differ differ = new Differ();

    protected override void OnParametersSet()
    {
        BuildDiff();
    }

    private void BuildDiff()
    {
        if (string.IsNullOrEmpty(OldText) && string.IsNullOrEmpty(NewText))
        {
            sideBySideDiff = null;
            inlineDiff = null;
            return;
        }

        if (viewMode == DiffViewMode.SideBySide)
        {
            var builder = new SideBySideDiffBuilder(differ);
            sideBySideDiff = builder.BuildDiffModel(OldText ?? string.Empty, NewText ?? string.Empty);
            
            // Build word-level diffs for modified lines
            if (sideBySideDiff != null)
            {
                BuildWordLevelDiffs(sideBySideDiff.OldText.Lines, sideBySideDiff.NewText.Lines);
            }
        }
        else
        {
            var builder = new InlineDiffBuilder(differ);
            inlineDiff = builder.BuildDiffModel(OldText ?? string.Empty, NewText ?? string.Empty);
            
            // Build word-level diffs for modified lines
            if (inlineDiff != null)
            {
                BuildWordLevelDiffsForInline(inlineDiff.Lines);
            }
        }
    }

    private void BuildWordLevelDiffs(List<DiffPiece> oldLines, List<DiffPiece> newLines)
    {
        for (int i = 0; i < Math.Min(oldLines.Count, newLines.Count); i++)
        {
            var oldLine = oldLines[i];
            var newLine = newLines[i];
            
            // Only build word-level diffs for modified lines
            if (oldLine.Type == ChangeType.Modified || newLine.Type == ChangeType.Modified)
            {
                if (oldLine.SubPieces == null || oldLine.SubPieces.Count == 0)
                {
                    oldLine.SubPieces = BuildWordLevelDiff(oldLine.Text ?? string.Empty, newLine.Text ?? string.Empty, true);
                }
                if (newLine.SubPieces == null || newLine.SubPieces.Count == 0)
                {
                    newLine.SubPieces = BuildWordLevelDiff(oldLine.Text ?? string.Empty, newLine.Text ?? string.Empty, false);
                }
            }
        }
    }

    private void BuildWordLevelDiffsForInline(List<DiffPiece> lines)
    {
        foreach (var line in lines)
        {
            if (line.Type == ChangeType.Modified && (line.SubPieces == null || line.SubPieces.Count == 0))
            {
                // For inline view, we need to compare with the corresponding line
                // This is a simplified version - you might need to adjust based on your needs
                var oldText = line.Text ?? string.Empty;
                var newText = line.Text ?? string.Empty;
                line.SubPieces = BuildWordLevelDiff(oldText, newText, false);
            }
        }
    }

    private List<DiffPiece> BuildWordLevelDiff(string oldText, string newText, bool isOld)
    {
        var pieces = new List<DiffPiece>();
        
        if (string.IsNullOrEmpty(oldText) && string.IsNullOrEmpty(newText))
        {
            return pieces;
        }
        
        // Split text into words (preserving spaces)
        var oldParts = SplitIntoWordsAndSpaces(oldText);
        var newParts = SplitIntoWordsAndSpaces(newText);
        
        // Use DiffPlex to create word-level diff by treating each word as a line
        var oldTextForDiff = string.Join(Environment.NewLine, oldParts);
        var newTextForDiff = string.Join(Environment.NewLine, newParts);
        
        var diffResult = differ.CreateLineDiffs(oldTextForDiff, newTextForDiff, false);
        
        if (diffResult != null && diffResult.DiffBlocks != null && diffResult.DiffBlocks.Count > 0)
        {
            int currentIndex = 0;
            var sourceParts = isOld ? oldParts : newParts;
            
            foreach (var block in diffResult.DiffBlocks)
            {
                int blockStart = isOld ? block.DeleteStartA : block.InsertStartB;
                int blockLength = isOld ? block.DeleteCountA : block.InsertCountB;
                
                // Add unchanged parts before the diff block
                while (currentIndex < blockStart && currentIndex < sourceParts.Count)
                {
                    pieces.Add(new DiffPiece(sourceParts[currentIndex], ChangeType.Unchanged));
                    currentIndex++;
                }
                
                // Add the changed parts
                for (int i = 0; i < blockLength && (blockStart + i) < sourceParts.Count; i++)
                {
                    var changeType = isOld ? ChangeType.Deleted : ChangeType.Inserted;
                    pieces.Add(new DiffPiece(sourceParts[blockStart + i], changeType));
                    currentIndex++;
                }
            }
            
            // Add remaining unchanged parts
            while (currentIndex < sourceParts.Count)
            {
                pieces.Add(new DiffPiece(sourceParts[currentIndex], ChangeType.Unchanged));
                currentIndex++;
            }
        }
        else
        {
            // No differences found, return original text as unchanged
            var parts = isOld ? oldParts : newParts;
            foreach (var part in parts)
            {
                pieces.Add(new DiffPiece(part, ChangeType.Unchanged));
            }
        }
        
        return pieces;
    }

    private List<string> SplitIntoWordsAndSpaces(string text)
    {
        var parts = new List<string>();
        if (string.IsNullOrEmpty(text))
            return parts;
            
        var currentPart = new System.Text.StringBuilder();
        bool? inWord = null; // null = start, true = in word, false = in space
        
        foreach (char c in text)
        {
            bool isWordChar = !char.IsWhiteSpace(c);
            
            if (inWord.HasValue && isWordChar != inWord.Value)
            {
                // Transition: word to space or space to word
                parts.Add(currentPart.ToString());
                currentPart.Clear();
            }
            
            currentPart.Append(c);
            inWord = isWordChar;
        }
        
        if (currentPart.Length > 0)
        {
            parts.Add(currentPart.ToString());
        }
        
        return parts;
    }

    private string GetLineClass(ChangeType changeType)
    {
        // Keep the line background color - we'll add character-level highlighting on top
        return changeType switch
        {
            ChangeType.Inserted => "diff-inserted",
            ChangeType.Modified => "diff-modified",
            ChangeType.Deleted => "diff-deleted",
            ChangeType.Unchanged => "diff-unchanged",
            _ => string.Empty
        };
    }

    private MarkupString FormatLineWithSubPieces(DiffPiece piece)
    {
        if (piece == null || string.IsNullOrEmpty(piece.Text))
        {
            return new MarkupString(" ");
        }

        // If the piece has sub-pieces (word/character-level changes), render those
        if (piece.SubPieces != null && piece.SubPieces.Count > 0)
        {
            var html = new System.Text.StringBuilder();
            foreach (var subPiece in piece.SubPieces)
            {
                var text = EscapeHtml(subPiece.Text);
                text = text.Replace(" ", "\u00A0").Replace("\t", "    ");
                
                var className = subPiece.Type switch
                {
                    ChangeType.Inserted => "diff-char-inserted",
                    ChangeType.Deleted => "diff-char-deleted",
                    ChangeType.Modified => "diff-char-modified",
                    _ => ""
                };

                if (!string.IsNullOrEmpty(className))
                {
                    html.Append($"<span class=\"{className}\">{text}</span>");
                }
                else
                {
                    html.Append(text);
                }
            }
            return new MarkupString(html.ToString());
        }

        // No sub-pieces, just render the text normally
        var plainText = EscapeHtml(piece.Text);
        plainText = plainText.Replace(" ", "\u00A0").Replace("\t", "    ");
        return new MarkupString(plainText);
    }

    private string FormatLine(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return " ";
        }
        return text.Replace(" ", "\u00A0").Replace("\t", "    ");
    }

    private string EscapeHtml(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }
        return text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&#39;");
    }
}

