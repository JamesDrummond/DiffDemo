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
        }
        else
        {
            var builder = new InlineDiffBuilder(differ);
            inlineDiff = builder.BuildDiffModel(OldText ?? string.Empty, NewText ?? string.Empty);
        }
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

