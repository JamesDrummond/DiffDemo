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

    private enum DiffViewMode
    {
        SideBySide,
        Inline
    }

    private DiffViewMode viewMode = DiffViewMode.SideBySide;
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

    private void SetViewMode(DiffViewMode mode)
    {
        viewMode = mode;
        BuildDiff();
    }

    private string GetLineClass(ChangeType changeType)
    {
        return changeType switch
        {
            ChangeType.Inserted => "diff-inserted",
            ChangeType.Modified => "diff-modified",
            ChangeType.Deleted => "diff-deleted",
            ChangeType.Unchanged => "diff-unchanged",
            _ => string.Empty
        };
    }

    private string FormatLine(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return " ";
        }
        return text.Replace(" ", "\u00A0").Replace("\t", "    ");
    }
}

