using Formarca.Application.Common.Interfaces;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Formarca.Infrastructure.Documents;

internal static class MarkdownPdfRenderer
{
    public static void Render(ColumnDescriptor column, string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return;
        }

        var document = Markdig.Markdown.Parse(markdown);
        foreach (var block in document)
        {
            RenderBlock(column, block, 0);
        }
    }

    private static void RenderBlock(ColumnDescriptor column, Block block, int indent)
    {
        switch (block)
        {
            case HeadingBlock heading:
                column.Item()
                    .PaddingLeft(indent)
                    .PaddingTop(heading.Level <= 1 ? 0 : 10)
                    .Text(text =>
                    {
                        ApplyHeading(text, heading.Level);
                        RenderInline(text, heading.Inline);
                    });
                break;
            case ParagraphBlock paragraph:
                column.Item()
                    .PaddingLeft(indent)
                    .PaddingTop(4)
                    .Text(text =>
                    {
                        text.DefaultTextStyle(style => style.FontSize(10));
                        RenderInline(text, paragraph.Inline);
                    });
                break;
            case ListBlock list:
                var index = 1;
                foreach (var item in list)
                {
                    if (item is not ListItemBlock listItem)
                    {
                        continue;
                    }

                    var prefix = list.IsOrdered ? $"{index}. " : "• ";
                    index++;
                    column.Item().PaddingLeft(indent + 8).PaddingTop(2).Row(row =>
                    {
                        row.ConstantItem(18).Text(prefix).FontSize(10);
                        row.RelativeItem().Column(inner =>
                        {
                            inner.Spacing(2);
                            foreach (var child in listItem)
                            {
                                RenderBlock(inner, child, 0);
                            }
                        });
                    });
                }

                break;
            case ThematicBreakBlock:
                column.Item().PaddingVertical(8).LineHorizontal(0.5f);
                break;
        }
    }

    private static void ApplyHeading(TextDescriptor text, int level)
    {
        var size = level switch
        {
            1 => 16f,
            2 => 12f,
            _ => 11f
        };
        text.DefaultTextStyle(style => style.FontSize(size).SemiBold());
    }

    private static void RenderInline(TextDescriptor text, ContainerInline? inline)
    {
        if (inline is null)
        {
            return;
        }

        foreach (var child in inline)
        {
            RenderInlineNode(text, child, bold: false, italic: false);
        }
    }

    private static void RenderInlineNode(TextDescriptor text, Inline inline, bool bold, bool italic)
    {
        switch (inline)
        {
            case LiteralInline literal:
                ApplySpan(text.Span(literal.Content.ToString()), bold, italic);
                break;
            case CodeInline code:
                ApplySpan(text.Span(code.Content), bold, italic).FontFamily("Courier New");
                break;
            case LineBreakInline:
                text.Span("\n");
                break;
            case EmphasisInline emphasis:
                var nextBold = bold || emphasis.DelimiterCount >= 2;
                var nextItalic = italic || emphasis.DelimiterCount == 1;
                foreach (var child in emphasis)
                {
                    RenderInlineNode(text, child, nextBold, nextItalic);
                }

                break;
            case LinkInline link:
                foreach (var child in link)
                {
                    RenderInlineNode(text, child, bold, italic);
                }

                break;
            case ContainerInline container:
                foreach (var child in container)
                {
                    RenderInlineNode(text, child, bold, italic);
                }

                break;
            default:
                if (!string.IsNullOrWhiteSpace(inline.ToString()))
                {
                    ApplySpan(text.Span(inline.ToString()), bold, italic);
                }

                break;
        }
    }

    private static TextSpanDescriptor ApplySpan(TextSpanDescriptor span, bool bold, bool italic)
    {
        if (bold)
        {
            span = span.SemiBold();
        }

        if (italic)
        {
            span = span.Italic();
        }

        return span;
    }
}
