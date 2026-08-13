using Formarca.Application.Common.Models;

namespace Formarca.Application.Tests;

public class PagingTests
{
    [Fact]
    public void Normalize_Applies_Defaults_And_Caps()
    {
        var (page, pageSize, skip) = Paging.Normalize(0, 0);
        Assert.Equal(1, page);
        Assert.Equal(Paging.DefaultPageSize, pageSize);
        Assert.Equal(0, skip);

        var capped = Paging.Normalize(3, 500);
        Assert.Equal(3, capped.Page);
        Assert.Equal(Paging.MaxPageSize, capped.PageSize);
        Assert.Equal(2 * Paging.MaxPageSize, capped.Skip);
    }
}
