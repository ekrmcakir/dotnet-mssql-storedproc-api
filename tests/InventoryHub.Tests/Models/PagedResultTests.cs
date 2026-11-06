using FluentAssertions;
using InventoryHub.Core.Models;
using Xunit;

namespace InventoryHub.Tests.Models;

public class PagedResultTests
{
    [Theory]
    [InlineData(25, 10, 3)]
    [InlineData(10, 10, 1)]
    [InlineData(0, 10, 0)]
    [InlineData(31, 5, 7)]
    public void TotalPages_CalculatesCorrectly(int totalRecords, int pageSize, int expectedPages)
    {
        // Act
        var result = new PagedResult<string>(Array.Empty<string>(), totalRecords, pageNumber: 1, pageSize);

        // Assert
        result.TotalPages.Should().Be(expectedPages);
    }

    [Fact]
    public void HasNextAndPreviousPage_EvaluatesCorrectly()
    {
        // Page 2 of 3 pages
        var result = new PagedResult<int>(new[] { 1, 2, 3 }, totalRecords: 30, pageNumber: 2, pageSize: 10);

        result.HasPreviousPage.Should().BeTrue();
        result.HasNextPage.Should().BeTrue();
    }
}

