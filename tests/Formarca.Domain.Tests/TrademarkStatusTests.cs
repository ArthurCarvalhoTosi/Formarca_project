using Formarca.Domain.Processes;

namespace Formarca.Domain.Tests;

public class TrademarkStatusTests
{
    [Fact]
    public void Catalog_Keeps_The_Inpi_Sequence()
    {
        var catalog = TrademarkStatusCatalog.All;

        Assert.Equal(23, catalog.Count);
        Assert.Equal(catalog.Count, catalog.Select(item => item.Code).Distinct().Count());
        Assert.Equal("deposito-efetuado", catalog[0].Code);
        Assert.Equal("registro-extinto", catalog[^1].Code);
        Assert.All(
            catalog.Zip(catalog.Skip(1)),
            pair => Assert.True(pair.First.SortOrder < pair.Second.SortOrder));
    }

    [Fact]
    public void NormalizeCode_Strips_Accents_And_Separators()
    {
        Assert.Equal("exigencia-formal", TrademarkStatus.NormalizeCode("  Exigência formal "));
    }

    [Fact]
    public void AssignTrademarkStatus_Records_The_Current_Status()
    {
        var process = Process.CreateGenerated(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var statusId = Guid.NewGuid();

        process.AssignTrademarkStatus(statusId);

        Assert.Equal(statusId, process.TrademarkStatusId);
        Assert.NotNull(process.TrademarkStatusChangedAtUtc);
        Assert.Throws<InvalidOperationException>(() => process.AssignTrademarkStatus(statusId));
    }

    [Fact]
    public void Update_Rejects_An_Empty_Title()
    {
        var status = TrademarkStatus.Create(
            "deposito-efetuado",
            "Depósito efetuado",
            "Aguarda triagem.",
            10,
            TrademarkStatusPhase.Filing,
            TrademarkStatusTone.Progress);

        Assert.Throws<ArgumentException>(() => status.Update(
            " ",
            "Aguarda triagem.",
            10,
            TrademarkStatusPhase.Filing,
            TrademarkStatusTone.Progress,
            true));
    }
}
