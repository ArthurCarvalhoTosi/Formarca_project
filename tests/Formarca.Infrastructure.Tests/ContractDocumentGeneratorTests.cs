using Formarca.Application.Common.Interfaces;
using Formarca.Domain.Contracts;
using Formarca.Infrastructure.Documents;

namespace Formarca.Infrastructure.Tests;

public class ContractDocumentGeneratorTests
{
    [Fact]
    public void GeneratePdf_Renders_Markdown_And_Signature_Blocks()
    {
        var generator = new ContractDocumentGenerator();
        var pdf = generator.GeneratePdf(Model(clientSignature: null));

        Assert.True(pdf.Length > 500);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(pdf, 0, 4));
    }

    [Fact]
    public void GeneratePdf_Accepts_Client_Signature_Png()
    {
        var generator = new ContractDocumentGenerator();
        var png = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+ip1sAAAAASUVORK5CYII=");
        var unsigned = generator.GeneratePdf(Model(null));
        var signed = generator.GeneratePdf(Model(png));

        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(signed, 0, 4));
        Assert.NotEqual(unsigned.Length, signed.Length);
    }

    [Fact]
    public void Template_Provider_Returns_Prestacao_Servicos()
    {
        var provider = new EmbeddedContractTemplateProvider();
        var template = provider.GetTemplate(ContractTemplateKeys.PrestacaoServicos);

        Assert.Contains("{{contratanteNome}}", template);
        Assert.Contains("{{cnpj}}", template);
        Assert.Contains("{{periodo}}", template);
        Assert.Contains("FOR MARCA LTDA", template);
        Assert.Contains("62.500.545/0001-14", template);
        Assert.Contains("CLÁUSULA 1ª", template);
    }

    [Fact]
    public void Template_Provider_Unknown_Key_Throws()
    {
        var provider = new EmbeddedContractTemplateProvider();
        Assert.Throws<InvalidOperationException>(() => provider.GetTemplate("does-not-exist"));
    }

    private static ContractDocumentModel Model(byte[]? clientSignature) =>
        new(
            "FM-2026-000001",
            "11222333000181",
            "39053344705",
            "Acme",
            "9",
            1500m,
            DateTime.UtcNow,
            DateTime.UtcNow,
            clientSignature is null ? null : DateTime.UtcNow,
            "Issued",
            "Acme Ltd",
            "Av Paulista",
            "100",
            "São Paulo",
            "SP",
            "01310100",
            ContractTemplateKeys.DefaultMinimumTerm,
            """
            # CONTRATO DE PRESTAÇÃO DE SERVIÇOS

            **CONTRATANTE:** Acme Ltd

            ## CLÁUSULA 1ª – DO OBJETO

            - Registro da marca Acme em 1 classe(s) no valor de R$ 1.500,00.
            """,
            clientSignature,
            StampFormarcaSignature: true);
}
