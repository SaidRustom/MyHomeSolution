using FluentAssertions;
using MyHomeSolution.Application.Common.Models;

namespace MyHomeSolution.Application.Tests.Common.Models;

public sealed class PaginatedListTests
{
    [Fact]
    public void Constructor_ShouldSetProperties()
    {
        var items = new[] { "a", "b", "c" };

        var list = new PaginatedList<string>(items, count: 10, pageNumber: 2, pageSize: 3);

        list.Items.Should().BeEquivalentTo(items);
        list.TotalCount.Should().Be(10);
        list.PageNumber.Should().Be(2);
        list.TotalPages.Should().Be(4); // ceil(10 / 3)
    }

    [Fact]
    public void HasPreviousPage_ShouldBeFalse_WhenOnFirstPage()
    {
        var list = new PaginatedList<int>([1, 2], count: 5, pageNumber: 1, pageSize: 2);

        list.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public void HasPreviousPage_ShouldBeTrue_WhenNotOnFirstPage()
    {
        var list = new PaginatedList<int>([3, 4], count: 5, pageNumber: 2, pageSize: 2);

        list.HasPreviousPage.Should().BeTrue();
    }

    [Fact]
    public void HasNextPage_ShouldBeTrue_WhenNotOnLastPage()
    {
        var list = new PaginatedList<int>([1, 2], count: 5, pageNumber: 1, pageSize: 2);

        list.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public void HasNextPage_ShouldBeFalse_WhenOnLastPage()
    {
        var list = new PaginatedList<int>([5], count: 5, pageNumber: 3, pageSize: 2);

        list.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public void TotalPages_ShouldBeOne_WhenCountFitsInSinglePage()
    {
        var list = new PaginatedList<int>([1, 2, 3], count: 3, pageNumber: 1, pageSize: 10);

        list.TotalPages.Should().Be(1);
    }

    [Fact]
    public void TotalPages_ShouldRoundUp_WhenCountDoesNotDivideEvenly()
    {
        var list = new PaginatedList<int>([1], count: 7, pageNumber: 1, pageSize: 3);

        list.TotalPages.Should().Be(3); // ceil(7 / 3) = 3
    }

    [Fact]
    public void TotalPages_ShouldBeZero_WhenCountIsZero()
    {
        var list = new PaginatedList<string>([], count: 0, pageNumber: 1, pageSize: 10);

        list.TotalPages.Should().Be(0);
        list.HasPreviousPage.Should().BeFalse();
        list.HasNextPage.Should().BeFalse();
    }
}
