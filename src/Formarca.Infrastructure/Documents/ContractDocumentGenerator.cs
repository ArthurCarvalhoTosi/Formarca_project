using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Formarca.Application.Common.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using OpenXmlDocument = DocumentFormat.OpenXml.Wordprocessing.Document;
using OpenXmlParagraph = DocumentFormat.OpenXml.Wordprocessing.Paragraph;
using OpenXmlText = DocumentFormat.OpenXml.Wordprocessing.Text;

namespace Formarca.Infrastructure.Documents;

public sealed class ContractDocumentGenerator : IContractDocumentGenerator
{
    private const string LogoResourceName = "Formarca.Infrastructure.Documents.Assets.formarca-logo.jpg";

    private readonly byte[]? _logoBytes;
    private readonly byte[]? _formarcaSignatureBytes;

    public ContractDocumentGenerator()
        : this(LoadEmbedded(LogoResourceName), null)
    {
    }

    public ContractDocumentGenerator(IOptions<DocumentOptions> options, IHostEnvironment environment)
        : this(LoadEmbedded(LogoResourceName), LoadOptionalFile(options.Value.FormarcaSignaturePath, environment.ContentRootPath))
    {
    }

    public ContractDocumentGenerator(byte[]? logoBytes, byte[]? formarcaSignatureBytes)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        _logoBytes = logoBytes is { Length: > 0 } ? logoBytes : LoadEmbedded(LogoResourceName);
        _formarcaSignatureBytes = formarcaSignatureBytes is { Length: > 0 } ? formarcaSignatureBytes : null;
    }

    public byte[] GeneratePdf(ContractDocumentModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        return QuestPDF.Fluent.Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(40);
                page.Size(PageSizes.A4);
                page.DefaultTextStyle(style => style.FontSize(10).FontColor(Colors.Grey.Darken4));

                page.Header().Element(header => ComposeHeader(header, model));
                page.Content().Column(column =>
                {
                    column.Spacing(6);
                    MarkdownPdfRenderer.Render(column, model.BodyMarkdown);
                    column.Item().PageBreak();
                    column.Item().Element(block => ComposeSignatures(block, model));
                });
                page.Footer().AlignCenter().DefaultTextStyle(style => style.FontSize(8).FontColor(Colors.Grey.Medium)).Text(text =>
                {
                    text.Span("Página ");
                    text.CurrentPageNumber();
                    text.Span(" de ");
                    text.TotalPages();
                });
            });
        }).GeneratePdf();
    }

    public byte[] GenerateWord(ContractDocumentModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        using var stream = new MemoryStream();
        using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var main = document.AddMainDocumentPart();
            var body = new Body();
            body.AppendChild(CreateParagraph("Formarca — Contrato", bold: true));
            foreach (var line in model.BodyMarkdown.Split('\n'))
            {
                body.AppendChild(CreateParagraph(line));
            }

            main.Document = new OpenXmlDocument(body);
            main.Document.Save();
        }

        return stream.ToArray();
    }

    private void ComposeHeader(IContainer container, ContractDocumentModel model)
    {
        container.Row(row =>
        {
            if (_logoBytes is { Length: > 0 })
            {
                row.ConstantItem(150).Height(42).Image(_logoBytes).FitArea();
            }
            else
            {
                row.RelativeItem().Text("formarca").FontSize(18).SemiBold();
            }

            row.RelativeItem().AlignRight().AlignMiddle().Column(column =>
            {
                column.Item().Text(model.Code).FontSize(10).SemiBold();
                column.Item().Text(model.Status).FontSize(8).FontColor(Colors.Grey.Darken1);
            });
        });
    }

    private void ComposeSignatures(IContainer container, ContractDocumentModel model)
    {
        container.Row(row =>
        {
            row.RelativeItem().PaddingRight(16).Element(left =>
                SignatureColumn(
                    left,
                    "FOR MARCA LTDA",
                    model.StampFormarcaSignature ? _formarcaSignatureBytes : null,
                    model.StampFormarcaSignature
                        ? $"Assinado eletronicamente em {model.FormarcaSignedAt:dd/MM/yyyy HH:mm} UTC"
                        : null,
                    emptyLabel: null));

            row.RelativeItem().PaddingLeft(16).Element(right =>
                SignatureColumn(
                    right,
                    string.IsNullOrWhiteSpace(model.CompanyName) ? "Contratante" : model.CompanyName,
                    model.ClientSignaturePng,
                    model.ClientSignedAt is null
                        ? null
                        : $"Assinado em {model.ClientSignedAt.Value:dd/MM/yyyy HH:mm} UTC",
                    emptyLabel: null));
        });
    }

    private static void SignatureColumn(
        IContainer container,
        string label,
        byte[]? image,
        string? caption,
        string? emptyLabel)
    {
        container.Column(column =>
        {
            column.Spacing(6);
            column.Item()
                .Border(1)
                .BorderColor(Colors.Grey.Lighten1)
                .Height(72)
                .Padding(6)
                .Element(box =>
                {
                    if (image is { Length: > 0 })
                    {
                        box.Image(image).FitArea();
                        return;
                    }

                    if (!string.IsNullOrWhiteSpace(emptyLabel))
                    {
                        box.AlignCenter().AlignMiddle().Text(emptyLabel).FontSize(9).FontColor(Colors.Grey.Medium);
                        return;
                    }

                    box.AlignBottom().LineHorizontal(0.5f).LineColor(Colors.Grey.Medium);
                });
            column.Item().AlignCenter().Text(label).FontSize(9).SemiBold();
            if (!string.IsNullOrWhiteSpace(caption))
            {
                column.Item().AlignCenter().Text(caption).FontSize(8).FontColor(Colors.Grey.Darken1);
            }
        });
    }

    private static byte[]? LoadEmbedded(string resourceName)
    {
        using var stream = typeof(ContractDocumentGenerator).Assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
        {
            return null;
        }

        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }

    private static byte[]? LoadOptionalFile(string? path, string contentRoot)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        var candidates = Path.IsPathRooted(path)
            ? [path]
            : new[]
            {
                Path.Combine(contentRoot, path),
                Path.Combine(AppContext.BaseDirectory, path)
            };

        foreach (var candidate in candidates)
        {
            if (File.Exists(candidate))
            {
                return File.ReadAllBytes(candidate);
            }
        }

        return null;
    }

    private static OpenXmlParagraph CreateParagraph(string text, bool bold = false)
    {
        var runProperties = new RunProperties();
        if (bold)
        {
            runProperties.AppendChild(new Bold());
        }

        return new OpenXmlParagraph(
            new Run(runProperties, new OpenXmlText(text) { Space = SpaceProcessingModeValues.Preserve }));
    }
}
