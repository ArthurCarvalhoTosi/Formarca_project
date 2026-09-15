using Formarca.Application.Common;
using Formarca.Application.Common.Models;
using Formarca.Domain.Contracts;

namespace Formarca.Application.Tests;

public class QueryFilterValuesTests
{
    [Fact]
    public void ParseEnums_Splits_Csv_And_Repeated_Values()
    {
        var parsed = QueryFilterValues.ParseEnums<ContractStatus>(["Issued,Signed", "Cancelled"]);
        Assert.Equal(
            [ContractStatus.Issued, ContractStatus.Signed, ContractStatus.Cancelled],
            parsed);
    }

    [Fact]
    public void ParseEnums_Ignores_Unknown_Values()
    {
        var parsed = QueryFilterValues.ParseEnums<ProcessFunnelStage>(["Brand", "Nope", "Process"]);
        Assert.Equal([ProcessFunnelStage.Brand, ProcessFunnelStage.Process], parsed);
    }

    [Fact]
    public void MergeGuids_Combines_Single_And_Csv()
    {
        var first = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var second = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var merged = QueryFilterValues.MergeGuids(first, [$"{second}"]);
        Assert.Equal([first, second], merged);
    }
}
