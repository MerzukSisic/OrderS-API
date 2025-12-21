using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using OrdersAPI.API.Controllers;
using OrdersAPI.Application.DTOs;
using OrdersAPI.Application.Interfaces;
using OrdersAPI.Domain.Entities;
using OrdersAPI.Tests.Helpers;
using Xunit;

namespace OrdersAPI.Tests.Controllers;

public class TablesControllerTests : TestBase
{
    private readonly Mock<ITableService> _tableServiceMock;
    private readonly Mock<ILogger<TablesController>> _loggerMock;
    private readonly TablesController _controller;

    public TablesControllerTests()
    {
        _tableServiceMock = new Mock<ITableService>();
        _loggerMock = new Mock<ILogger<TablesController>>();
        _controller = new TablesController(_tableServiceMock.Object, _loggerMock.Object);
        _controller.ControllerContext = CreateControllerContext(Guid.NewGuid());
    }

    [Fact]
    public async Task GetTables_ReturnsOkWithTables()
    {
        // Arrange
        var tables = new List<TableDto>
        {
            new() { Id = Guid.NewGuid(), TableNumber = "1", Status = "Available" },
            new() { Id = Guid.NewGuid(), TableNumber = "2", Status = "Occupied" }
        };

        _tableServiceMock.Setup(x => x.GetAllTablesAsync())
            .ReturnsAsync(tables);

        // Act
        var result = await _controller.GetTables();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedTables = okResult.Value.Should().BeAssignableTo<IEnumerable<TableDto>>().Subject;
        returnedTables.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetTable_ExistingId_ReturnsOkWithTable()
    {
        // Arrange
        var tableId = Guid.NewGuid();
        var table = new TableDto { Id = tableId, TableNumber = "5", Capacity = 4 };

        _tableServiceMock.Setup(x => x.GetTableByIdAsync(tableId))
            .ReturnsAsync(table);

        // Act
        var result = await _controller.GetTable(tableId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedTable = okResult.Value.Should().BeOfType<TableDto>().Subject;
        returnedTable.TableNumber.Should().Be("5");
    }

    [Fact]
    public async Task CreateTable_ValidData_ReturnsCreatedAtAction()
    {
        // Arrange
        var createDto = new CreateTableDto
        {
            TableNumber = "10",
            Capacity = 6,
            Location = "Terrace"
        };

        var createdTable = new TableDto
        {
            Id = Guid.NewGuid(),
            TableNumber = "10",
            Capacity = 6
        };

        _tableServiceMock.Setup(x => x.CreateTableAsync(createDto))
            .ReturnsAsync(createdTable);

        // Act
        var result = await _controller.CreateTable(createDto);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var returnedTable = createdResult.Value.Should().BeOfType<TableDto>().Subject;
        returnedTable.TableNumber.Should().Be("10");
    }

    [Fact]
    public async Task UpdateTableStatus_ValidStatus_ReturnsNoContent()
    {
        // Arrange
        var tableId = Guid.NewGuid();
        var status = "Occupied";

        _tableServiceMock.Setup(x => x.UpdateTableStatusAsync(tableId, TableStatus.Occupied))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.UpdateTableStatus(tableId, status);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }
}
