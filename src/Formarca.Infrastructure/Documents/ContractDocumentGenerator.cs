using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Formarca.Application.Common.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using OpenXmlDocument = DocumentFormat.OpenXml.Wordprocessing.Document;
using OpenXmlParagraph = DocumentFormat.OpenXml.Wordprocessing.Paragraph;
using OpenXmlText = DocumentFormat.OpenXml.Wordprocessing.Text;

namespace Formarca.Infrastructure.Documents;

public sealed class ContractDocumentGenerator : IContractDocumentGenerator
{
    public ContractDocumentGenerator()
    {
        QuestPDF.Settings.License = LicenseType.Community;
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
                page.Header().Text("Formarca — Contract").FontSize(18).SemiBold();
                page.Content().Column(column =>
                {
                    column.Spacing(8);
                    foreach (var line in Lines(model))
                    {
                        column.Item().Text(line);
                    }
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
            body.AppendChild(CreateParagraph("Formarca — Contract", bold: true));
            foreach (var line in Lines(model))
            {
                body.AppendChild(CreateParagraph(line));
            }

            main.Document = new OpenXmlDocument(body);
            main.Document.Save();
        }

        return stream.ToArray();
    }

    private static IEnumerable<string> Lines(ContractDocumentModel model)
    {
        yield return $"Code: {model.Code}";
        yield return $"Status: {model.Status}";
        yield return $"Brand: {model.BrandName}";
        yield return $"Classes: {model.Classes}";
        yield return $"Company document: {model.CompanyDocument}";
        yield return $"Representative document: {model.RepresentativeDocument}";
        yield return $"Amount: {model.Amount:N2}";
        yield return $"Generated (UTC): {model.GeneratedAtUtc:yyyy-MM-dd HH:mm}";
        yield return $"Formarca signed (UTC): {model.FormarcaSignedAt:yyyy-MM-dd HH:mm}";
        yield return $"Client signed (UTC): {(model.ClientSignedAt is null ? "-" : model.ClientSignedAt.Value.ToString("yyyy-MM-dd HH:mm"))}";
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
