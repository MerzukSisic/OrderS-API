using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using OrdersAPI.API.Controllers;
using OrdersAPI.Application.DTOs;
using OrdersAPI.Application.Interfaces;
using OrdersAPI.Domain.Entities;
using System.Security.Claims;
using OrdersAPI.Domain.Enums;
using Xunit;

namespace OrdersAPI.Tests.Controllers;

public class TablesControllerTests
{
    private readonly Mock<ITableService> _tableServiceMock;
    private readonly TablesController _controller;
    private readonly Guid _testTableId;

    public TablesControllerTests()
    {
        _tableServiceMock = new Mock<ITableService>();
        _controller = new TablesController(_tableServiceMock.Object);
        _testTableId = Guid.NewGuid();

        SetupAuthenticatedUser("Admin");
    }

    #region GetTables Tests

    [Fact]
    public async Task GetTables_ReturnsAllTables()
    {
        // Arrange
        var expectedTables = new List<TableDto>
        {
            new TableDto
            {
                Id = Guid.NewGuid(),
                TableNumber = "1",
                Capacity = 4,
                Status = "Available",
                Location = "Main Hall",
                CurrentOrderId = null,
                CurrentOrderTotal = null,
                ActiveOrderCount = 0
            },
            new TableDto
            {
                Id = Guid.NewGuid(),
                TableNumber = "2",
                Capacity = 6,
                Status = "Occupied",
                Location = "Terrace",
                CurrentOrderId = Guid.NewGuid(),
                CurrentOrderTotal = 125.50m,
                ActiveOrderCount = 1
            },
            new TableDto
            {
                Id = Guid.NewGuid(),
                TableNumber = "3",
                Capacity = 2,
                Status = "Reserved",
                Location = "Window Side",
                CurrentOrderId = null,
                CurrentOrderTotal = null,
                ActiveOrderCount = 0
            }
        };

        _tableServiceMock
            .Setup(x => x.GetAllTablesAsync())
            .ReturnsAsync(expectedTables);

        // Act
        var result = await _controller.GetTables();

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var tables = okResult!.Value as IEnumerable<TableDto>;

        tables.Should().NotBeNull();
        tables.Should().HaveCount(3);
        tables!.First().TableNumber.Should().Be("1");
        tables.ElementAt(1).Status.Should().Be("Occupied");
        tables.Last().Capacity.Should().Be(2);

        _tableServiceMock.Verify(x => x.GetAllTablesAsync(), Times.Once);
    }

    [Fact]
    public async Task GetTables_NoTables_ReturnsEmptyList()
    {
        // Arrange
        _tableServiceMock
            .Setup(x => x.GetAllTablesAsync())
            .ReturnsAsync(new List<TableDto>());

        // Act
        var result = await _controller.GetTables();

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var tables = okResult!.Value as IEnumerable<TableDto>;

        tables.Should().NotBeNull();
        tables.Should().BeEmpty();

        _tableServiceMock.Verify(x => x.GetAllTablesAsync(), Times.Once);
    }

    #endregion

    #region GetTable Tests

    [Fact]
    public async Task GetTable_ExistingId_ReturnsTable()
    {
        // Arrange
        var expectedTable = new TableDto
        {
            Id = _testTableId,
            TableNumber = "5",
            Capacity = 4,
            Status = "Available",
            Location = "Main Hall",
            CurrentOrderId = null,
            CurrentOrderTotal = null,
            ActiveOrderCount = 0
        };

        _tableServiceMock
            .Setup(x => x.GetTableByIdAsync(_testTableId))
            .ReturnsAsync(expectedTable);

        // Act
        var result = await _controller.GetTable(_testTableId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var table = okResult!.Value as TableDto;

        table.Should().NotBeNull();
        table!.Id.Should().Be(_testTableId);
        table.TableNumber.Should().Be("5");
        table.Capacity.Should().Be(4);
        table.Status.Should().Be("Available");

        _tableServiceMock.Verify(x => x.GetTableByIdAsync(_testTableId), Times.Once);
    }

    [Fact]
    public async Task GetTable_OccupiedTable_ReturnsTableWithOrderInfo()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var expectedTable = new TableDto
        {
            Id = _testTableId,
            TableNumber = "8",
            Capacity = 6,
            Status = "Occupied",
            Location = "VIP Section",
            CurrentOrderId = orderId,
            CurrentOrderTotal = 250.00m,
            ActiveOrderCount = 2
        };

        _tableServiceMock
            .Setup(x => x.GetTableByIdAsync(_testTableId))
            .ReturnsAsync(expectedTable);

        // Act
        var result = await _controller.GetTable(_testTableId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var table = okResult!.Value as TableDto;

        table.Should().NotBeNull();
        table!.Status.Should().Be("Occupied");
        table.CurrentOrderId.Should().Be(orderId);
        table.CurrentOrderTotal.Should().Be(250.00m);
        table.ActiveOrderCount.Should().Be(2);

        _tableServiceMock.Verify(x => x.GetTableByIdAsync(_testTableId), Times.Once);
    }

    [Fact]
    public async Task GetTable_NonExistingId_ThrowsKeyNotFoundException()
    {
        // Arrange
        _tableServiceMock
            .Setup(x => x.GetTableByIdAsync(It.IsAny<Guid>()))
            .ThrowsAsync(new KeyNotFoundException($"Table with ID {_testTableId} not found"));

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _controller.GetTable(_testTableId));

        _tableServiceMock.Verify(x => x.GetTableByIdAsync(_testTableId), Times.Once);
    }

    #endregion

    #region CreateTable Tests

    [Fact]
    public async Task CreateTable_ValidData_ReturnsCreatedTable()
    {
        // Arrange
        var createDto = new CreateTableDto
        {
            TableNumber = "10",
            Capacity = 4,
            Location = "Garden"
        };

        var expectedTable = new TableDto
        {
            Id = _testTableId,
            TableNumber = "10",
            Capacity = 4,
            Status = "Available",
            Location = "Garden",
            CurrentOrderId = null,
            CurrentOrderTotal = null,
            ActiveOrderCount = 0
        };

        _tableServiceMock
            .Setup(x => x.CreateTableAsync(It.IsAny<CreateTableDto>()))
            .ReturnsAsync(expectedTable);

        // Act
        var result = await _controller.CreateTable(createDto);

        // Assert
        result.Result.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = result.Result as CreatedAtActionResult;
        var table = createdResult!.Value as TableDto;

        table.Should().NotBeNull();
        table!.Id.Should().Be(_testTableId);
        table.TableNumber.Should().Be("10");
        table.Capacity.Should().Be(4);
        table.Status.Should().Be("Available");

        createdResult.ActionName.Should().Be(nameof(_controller.GetTable));
        createdResult.RouteValues!["id"].Should().Be(_testTableId);

        _tableServiceMock.Verify(x => x.CreateTableAsync(It.IsAny<CreateTableDto>()), Times.Once);
    }

    [Fact]
    public async Task CreateTable_DuplicateTableNumber_ThrowsInvalidOperationException()
    {
        // Arrange
        var createDto = new CreateTableDto
        {
            TableNumber = "5",
            Capacity = 4
        };

        _tableServiceMock
            .Setup(x => x.CreateTableAsync(It.IsAny<CreateTableDto>()))
            .ThrowsAsync(new InvalidOperationException("Table number already exists"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _controller.CreateTable(createDto));

        _tableServiceMock.Verify(x => x.CreateTableAsync(It.IsAny<CreateTableDto>()), Times.Once);
    }

    #endregion

    #region UpdateTable Tests

    [Fact]
    public async Task UpdateTable_ValidData_ReturnsNoContent()
    {
        // Arrange
        var updateDto = new UpdateTableDto
        {
            TableNumber = "5A",
            Capacity = 6,
            Location = "Terrace"
        };

        _tableServiceMock
            .Setup(x => x.UpdateTableAsync(_testTableId, It.IsAny<UpdateTableDto>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.UpdateTable(_testTableId, updateDto);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        _tableServiceMock.Verify(x => x.UpdateTableAsync(_testTableId, It.IsAny<UpdateTableDto>()), Times.Once);
    }

    [Fact]
    public async Task UpdateTable_PartialUpdate_ReturnsNoContent()
    {
        // Arrange
        var updateDto = new UpdateTableDto
        {
            Capacity = 8,
            Location = null
        };

        _tableServiceMock
            .Setup(x => x.UpdateTableAsync(_testTableId, It.IsAny<UpdateTableDto>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.UpdateTable(_testTableId, updateDto);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        _tableServiceMock.Verify(x => x.UpdateTableAsync(_testTableId, It.IsAny<UpdateTableDto>()), Times.Once);
    }

    [Fact]
    public async Task UpdateTable_NonExistingId_ThrowsKeyNotFoundException()
    {
        // Arrange
        var updateDto = new UpdateTableDto { TableNumber = "99" };

        _tableServiceMock
            .Setup(x => x.UpdateTableAsync(_testTableId, It.IsAny<UpdateTableDto>()))
            .ThrowsAsync(new KeyNotFoundException("Table not found"));

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _controller.UpdateTable(_testTableId, updateDto));

        _tableServiceMock.Verify(x => x.UpdateTableAsync(_testTableId, It.IsAny<UpdateTableDto>()), Times.Once);
    }

    #endregion

    #region UpdateTableStatus Tests

    [Fact]
    public async Task UpdateTableStatus_ToOccupied_ReturnsNoContent()
    {
        // Arrange
        var status = "Occupied";

        _tableServiceMock
            .Setup(x => x.UpdateTableStatusAsync(_testTableId, TableStatus.Occupied))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.UpdateTableStatus(_testTableId, status);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        _tableServiceMock.Verify(x => x.UpdateTableStatusAsync(_testTableId, TableStatus.Occupied), Times.Once);
    }

    [Fact]
    public async Task UpdateTableStatus_ToAvailable_ReturnsNoContent()
    {
        // Arrange
        var status = "Available";

        _tableServiceMock
            .Setup(x => x.UpdateTableStatusAsync(_testTableId, TableStatus.Available))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.UpdateTableStatus(_testTableId, status);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        _tableServiceMock.Verify(x => x.UpdateTableStatusAsync(_testTableId, TableStatus.Available), Times.Once);
    }

    [Fact]
    public async Task UpdateTableStatus_ToReserved_ReturnsNoContent()
    {
        // Arrange
        var status = "Reserved";

        _tableServiceMock
            .Setup(x => x.UpdateTableStatusAsync(_testTableId, TableStatus.Reserved))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.UpdateTableStatus(_testTableId, status);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        _tableServiceMock.Verify(x => x.UpdateTableStatusAsync(_testTableId, TableStatus.Reserved), Times.Once);
    }

    [Theory]
    [InlineData("Available")]
    [InlineData("Occupied")]
    [InlineData("Reserved")]
    public async Task UpdateTableStatus_AllValidStatuses_ReturnsNoContent(string status)
    {
        // Arrange
        var enumStatus = Enum.Parse<TableStatus>(status);

        _tableServiceMock
            .Setup(x => x.UpdateTableStatusAsync(_testTableId, enumStatus))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.UpdateTableStatus(_testTableId, status);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        _tableServiceMock.Verify(x => x.UpdateTableStatusAsync(_testTableId, enumStatus), Times.Once);
    }

    [Fact]
    public async Task UpdateTableStatus_NonExistingTable_ThrowsKeyNotFoundException()
    {
        // Arrange
        var status = "Available";

        _tableServiceMock
            .Setup(x => x.UpdateTableStatusAsync(_testTableId, TableStatus.Available))
            .ThrowsAsync(new KeyNotFoundException("Table not found"));

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _controller.UpdateTableStatus(_testTableId, status));

        _tableServiceMock.Verify(x => x.UpdateTableStatusAsync(_testTableId, TableStatus.Available), Times.Once);
    }

    #endregion

    #region DeleteTable Tests

    [Fact]
    public async Task DeleteTable_ExistingId_ReturnsNoContent()
    {
        // Arrange
        _tableServiceMock
            .Setup(x => x.DeleteTableAsync(_testTableId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.DeleteTable(_testTableId);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        _tableServiceMock.Verify(x => x.DeleteTableAsync(_testTableId), Times.Once);
    }

    [Fact]
    public async Task DeleteTable_NonExistingId_ThrowsKeyNotFoundException()
    {
        // Arrange
        _tableServiceMock
            .Setup(x => x.DeleteTableAsync(_testTableId))
            .ThrowsAsync(new KeyNotFoundException("Table not found"));

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _controller.DeleteTable(_testTableId));

        _tableServiceMock.Verify(x => x.DeleteTableAsync(_testTableId), Times.Once);
    }

    [Fact]
    public async Task DeleteTable_OccupiedTable_ThrowsInvalidOperationException()
    {
        // Arrange
        _tableServiceMock
            .Setup(x => x.DeleteTableAsync(_testTableId))
            .ThrowsAsync(new InvalidOperationException("Cannot delete occupied table"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _controller.DeleteTable(_testTableId));

        _tableServiceMock.Verify(x => x.DeleteTableAsync(_testTableId), Times.Once);
    }

    #endregion

    #region Helper Methods

    private void SetupAuthenticatedUser(string role)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Email, $"{role.ToLower()}@example.com"),
            new Claim(ClaimTypes.Role, role)
        };

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = claimsPrincipal
            }
        };
    }

    #endregion
}
