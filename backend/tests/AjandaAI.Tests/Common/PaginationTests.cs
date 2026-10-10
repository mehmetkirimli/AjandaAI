// PagedResult hesaplamalarının ve PageRequest sınır çekme davranışının birim testleridir.

using AjandaAI.Application.Activities.Dtos;
using AjandaAI.Application.Activities.Validators;
using AjandaAI.Application.Common;

namespace AjandaAI.Tests.Common;

public class PaginationTests
{
    private static PagedResult<int> Page(int totalCount, int page, int pageSize) =>
        new(Array.Empty<int>(), totalCount, page, pageSize);

    [Theory]
    [InlineData(0, 20, 0)]
    [InlineData(1, 20, 1)]
    [InlineData(20, 20, 1)]
    [InlineData(21, 20, 2)]
    [InlineData(25, 20, 2)]
    [InlineData(100, 10, 10)]
    public void TotalPages_IsCeilingOfTotalCountOverPageSize(int totalCount, int pageSize, int expected)
    {
        Assert.Equal(expected, Page(totalCount, 1, pageSize).TotalPages);
    }

    [Fact]
    public void FirstPage_HasNoPrevious_HasNext()
    {
        var result = Page(25, 1, 20);

        Assert.False(result.HasPrevious);
        Assert.True(result.HasNext);
    }

    [Fact]
    public void LastPage_HasPrevious_HasNoNext()
    {
        var result = Page(25, 2, 20);

        Assert.True(result.HasPrevious);
        Assert.False(result.HasNext);
    }

    [Fact]
    public void EmptyResult_HasNeitherPreviousNorNext()
    {
        var result = Page(0, 1, 20);

        Assert.False(result.HasPrevious);
        Assert.False(result.HasNext);
    }

    [Fact]
    public void Map_PreservesPagingInfo()
    {
        var mapped = new PagedResult<int>(new[] { 1, 2 }, 12, 3, 5).Map(x => x.ToString());

        Assert.Equal(new[] { "1", "2" }, mapped.Items);
        Assert.Equal((12, 3, 5, 3), (mapped.TotalCount, mapped.Page, mapped.PageSize, mapped.TotalPages));
    }

    [Fact]
    public void PageRequest_Defaults()
    {
        var request = new PageRequest();

        Assert.Equal(1, request.Page);
        Assert.Equal(20, request.PageSize);
        Assert.Equal(0, request.Skip);
    }

    [Theory]
    [InlineData(0, 500, 1, 100)]
    [InlineData(-5, 0, 1, 1)]
    [InlineData(3, 50, 3, 50)]
    public void PageRequest_ClampsInvalidValues(int page, int pageSize, int expectedPage, int expectedPageSize)
    {
        var request = new PageRequest { Page = page, PageSize = pageSize };

        Assert.Equal(expectedPage, request.Page);
        Assert.Equal(expectedPageSize, request.PageSize);
    }

    [Fact]
    public void ActivityFilterValidator_FromAfterTo_Fails()
    {
        var now = DateTimeOffset.UtcNow;
        var result = new ActivityFilterDtoValidator().Validate(new ActivityFilterDto { From = now, To = now.AddDays(-1) });

        Assert.Contains(result.Errors, e => e.ErrorMessage == "Başlangıç tarihi bitişten sonra olamaz.");
    }
}
