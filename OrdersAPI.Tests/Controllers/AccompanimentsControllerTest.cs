using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using OrdersAPI.API.Controllers;
using OrdersAPI.Application.DTOs;
using OrdersAPI.Application.Interfaces;
using Xunit;

namespace OrdersAPI.Tests.Controllers;

public class AccompanimentsControllerTests
{
    private readonly Mock<IAccompanimentService> _accompanimentServiceMock;
    private readonly AccompanimentsController _controller;
    private readonly Guid _testGroupId;
    private readonly Guid _testAccompanimentId;
    private readonly Guid _testProductId;

    public AccompanimentsControllerTests()
    {
        _accompanimentServiceMock = new Mock<IAccompanimentService>();
        _controller = new AccompanimentsController(_accompanimentServiceMock.Object);
        _testGroupId = Guid.NewGuid();
        _testAccompanimentId = Guid.NewGuid();
        _testProductId = Guid.NewGuid();
    }

    #region GetByProduct Tests

    [Fact]
    public async Task GetByProduct_ExistingProductId_ReturnsAccompanimentGroups()
    {
        // Arrange
        var expectedGroups = new List<AccompanimentGroupDto>
        {
            new AccompanimentGroupDto
            {
                Id = _testGroupId,
                Name = "Sauce",
                ProductId = _testProductId,
                SelectionType = "Single",
                MinSelections = 1,
                MaxSelections = 1,
                IsRequired = true,
                DisplayOrder = 1,
                Accompaniments = new List<AccompanimentDto>
                {
                    new AccompanimentDto 
                    { 
                        Id = Guid.NewGuid(), 
                        Name = "Ketchup", 
                        ExtraCharge = 0,
                        AccompanimentGroupId = _testGroupId,
                        IsAvailable = true,
                        DisplayOrder = 1
                    },
                    new AccompanimentDto 
                    { 
                        Id = Guid.NewGuid(), 
                        Name = "Mayo", 
                        ExtraCharge = 0.50m,
                        AccompanimentGroupId = _testGroupId,
                        IsAvailable = true,
                        DisplayOrder = 2
                    }
                }
            }
        };

        _accompanimentServiceMock
            .Setup(x => x.GetByProductIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(expectedGroups);

        // Act
        var result = await _controller.GetByProduct(_testProductId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var groups = okResult!.Value as List<AccompanimentGroupDto>;

        groups.Should().NotBeNull();
        groups.Should().HaveCount(1);
        groups![0].Name.Should().Be("Sauce");
        groups[0].Accompaniments.Should().HaveCount(2);

        _accompanimentServiceMock.Verify(x => x.GetByProductIdAsync(_testProductId), Times.Once);
    }

    [Fact]
    public async Task GetByProduct_ProductWithNoAccompaniments_ReturnsEmptyList()
    {
        // Arrange
        _accompanimentServiceMock
            .Setup(x => x.GetByProductIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new List<AccompanimentGroupDto>());

        // Act
        var result = await _controller.GetByProduct(_testProductId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var groups = okResult!.Value as List<AccompanimentGroupDto>;

        groups.Should().NotBeNull();
        groups.Should().BeEmpty();

        _accompanimentServiceMock.Verify(x => x.GetByProductIdAsync(_testProductId), Times.Once);
    }

    #endregion

    #region CreateGroup Tests

    [Fact]
    public async Task CreateGroup_ValidData_ReturnsCreatedGroup()
    {
        // Arrange
        var createDto = new CreateAccompanimentGroupDto
        {
            Name = "Sauce",
            ProductId = _testProductId,
            SelectionType = "Single",
            MinSelections = 1,
            MaxSelections = 1,
            IsRequired = true,
            DisplayOrder = 1
        };

        var expectedGroup = new AccompanimentGroupDto
        {
            Id = _testGroupId,
            Name = "Sauce",
            ProductId = _testProductId,
            SelectionType = "Single",
            MinSelections = 1,
            MaxSelections = 1,
            IsRequired = true,
            DisplayOrder = 1,
            Accompaniments = new List<AccompanimentDto>()
        };

        _accompanimentServiceMock
            .Setup(x => x.CreateGroupAsync(It.IsAny<CreateAccompanimentGroupDto>()))
            .ReturnsAsync(expectedGroup);

        // Act
        var result = await _controller.CreateGroup(createDto);

        // Assert
        result.Result.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = result.Result as CreatedAtActionResult;
        var group = createdResult!.Value as AccompanimentGroupDto;

        group.Should().NotBeNull();
        group!.Name.Should().Be("Sauce");
        group.IsRequired.Should().BeTrue();

        _accompanimentServiceMock.Verify(x => x.CreateGroupAsync(It.IsAny<CreateAccompanimentGroupDto>()), Times.Once);
    }

    #endregion

    #region GetGroup Tests

    [Fact]
    public async Task GetGroup_ExistingId_ReturnsGroup()
    {
        // Arrange
        var expectedGroup = new AccompanimentGroupDto
        {
            Id = _testGroupId,
            Name = "Sauce",
            ProductId = _testProductId,
            SelectionType = "Single",
            IsRequired = true,
            DisplayOrder = 1,
            Accompaniments = new List<AccompanimentDto>()
        };

        _accompanimentServiceMock
            .Setup(x => x.GetGroupByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(expectedGroup);

        // Act
        var result = await _controller.GetGroup(_testGroupId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var group = okResult!.Value as AccompanimentGroupDto;

        group.Should().NotBeNull();
        group!.Id.Should().Be(_testGroupId);
        group.Name.Should().Be("Sauce");

        _accompanimentServiceMock.Verify(x => x.GetGroupByIdAsync(_testGroupId), Times.Once);
    }

    [Fact]
    public async Task GetGroup_NonExistingId_ReturnsNotFound()
    {
        // Arrange
        _accompanimentServiceMock
            .Setup(x => x.GetGroupByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((AccompanimentGroupDto?)null);

        // Act
        var result = await _controller.GetGroup(_testGroupId);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result.Result as NotFoundObjectResult;
        notFoundResult!.Value.Should().BeEquivalentTo(new 
        { 
            message = $"Accompaniment group with ID {_testGroupId} not found" 
        });

        _accompanimentServiceMock.Verify(x => x.GetGroupByIdAsync(_testGroupId), Times.Once);
    }

    #endregion

    #region UpdateGroup Tests

    [Fact]
    public async Task UpdateGroup_ValidData_ReturnsNoContent()
    {
        // Arrange
        var updateDto = new UpdateAccompanimentGroupDto
        {
            Name = "Updated Sauce",
            SelectionType = "Multiple",
            MaxSelections = 2,
            IsRequired = true,
            DisplayOrder = 1
        };

        _accompanimentServiceMock
            .Setup(x => x.UpdateGroupAsync(It.IsAny<Guid>(), It.IsAny<UpdateAccompanimentGroupDto>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.UpdateGroup(_testGroupId, updateDto);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        _accompanimentServiceMock.Verify(x => x.UpdateGroupAsync(_testGroupId, updateDto), Times.Once);
    }

    [Fact]
    public async Task UpdateGroup_NonExistingId_ThrowsKeyNotFoundException()
    {
        // Arrange
        var updateDto = new UpdateAccompanimentGroupDto
        {
            Name = "Updated name",
            SelectionType = "Single",
            IsRequired = false,
            DisplayOrder = 1
        };

        _accompanimentServiceMock
            .Setup(x => x.UpdateGroupAsync(It.IsAny<Guid>(), It.IsAny<UpdateAccompanimentGroupDto>()))
            .ThrowsAsync(new KeyNotFoundException($"Accompaniment group with ID {_testGroupId} not found"));

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => 
            _controller.UpdateGroup(_testGroupId, updateDto));

        _accompanimentServiceMock.Verify(x => x.UpdateGroupAsync(_testGroupId, updateDto), Times.Once);
    }

    #endregion

    #region DeleteGroup Tests

    [Fact]
    public async Task DeleteGroup_ExistingId_ReturnsNoContent()
    {
        // Arrange
        _accompanimentServiceMock
            .Setup(x => x.DeleteGroupAsync(It.IsAny<Guid>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.DeleteGroup(_testGroupId);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        _accompanimentServiceMock.Verify(x => x.DeleteGroupAsync(_testGroupId), Times.Once);
    }

    [Fact]
    public async Task DeleteGroup_NonExistingId_ThrowsKeyNotFoundException()
    {
        // Arrange
        _accompanimentServiceMock
            .Setup(x => x.DeleteGroupAsync(It.IsAny<Guid>()))
            .ThrowsAsync(new KeyNotFoundException($"Accompaniment group with ID {_testGroupId} not found"));

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => 
            _controller.DeleteGroup(_testGroupId));

        _accompanimentServiceMock.Verify(x => x.DeleteGroupAsync(_testGroupId), Times.Once);
    }

    #endregion

    #region AddAccompaniment Tests

    [Fact]
    public async Task AddAccompaniment_ValidData_ReturnsAccompaniment()
    {
        // Arrange
        var createDto = new CreateAccompanimentDto
        {
            Name = "Extra Cheese",
            ExtraCharge = 1.50m,
            DisplayOrder = 1,
            IsAvailable = true
        };

        var expectedAccompaniment = new AccompanimentDto
        {
            Id = _testAccompanimentId,
            Name = "Extra Cheese",
            ExtraCharge = 1.50m,
            IsAvailable = true,
            DisplayOrder = 1,
            AccompanimentGroupId = _testGroupId
        };

        _accompanimentServiceMock
            .Setup(x => x.AddAccompanimentAsync(It.IsAny<Guid>(), It.IsAny<CreateAccompanimentDto>()))
            .ReturnsAsync(expectedAccompaniment);

        // Act
        var result = await _controller.AddAccompaniment(_testGroupId, createDto);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var accompaniment = okResult!.Value as AccompanimentDto;

        accompaniment.Should().NotBeNull();
        accompaniment!.Name.Should().Be("Extra Cheese");
        accompaniment.ExtraCharge.Should().Be(1.50m);
        accompaniment.AccompanimentGroupId.Should().Be(_testGroupId);

        _accompanimentServiceMock.Verify(x => x.AddAccompanimentAsync(_testGroupId, createDto), Times.Once);
    }

    #endregion

    #region GetAccompaniment Tests

    [Fact]
    public async Task GetAccompaniment_ExistingId_ReturnsAccompaniment()
    {
        // Arrange
        var expectedAccompaniment = new AccompanimentDto
        {
            Id = _testAccompanimentId,
            Name = "Ketchup",
            ExtraCharge = 0,
            IsAvailable = true,
            DisplayOrder = 1,
            AccompanimentGroupId = _testGroupId
        };

        _accompanimentServiceMock
            .Setup(x => x.GetAccompanimentByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(expectedAccompaniment);

        // Act
        var result = await _controller.GetAccompaniment(_testAccompanimentId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var accompaniment = okResult!.Value as AccompanimentDto;

        accompaniment.Should().NotBeNull();
        accompaniment!.Id.Should().Be(_testAccompanimentId);
        accompaniment.Name.Should().Be("Ketchup");

        _accompanimentServiceMock.Verify(x => x.GetAccompanimentByIdAsync(_testAccompanimentId), Times.Once);
    }

    [Fact]
    public async Task GetAccompaniment_NonExistingId_ReturnsNotFound()
    {
        // Arrange
        _accompanimentServiceMock
            .Setup(x => x.GetAccompanimentByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((AccompanimentDto?)null);

        // Act
        var result = await _controller.GetAccompaniment(_testAccompanimentId);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result.Result as NotFoundObjectResult;
        notFoundResult!.Value.Should().BeEquivalentTo(new 
        { 
            message = $"Accompaniment with ID {_testAccompanimentId} not found" 
        });

        _accompanimentServiceMock.Verify(x => x.GetAccompanimentByIdAsync(_testAccompanimentId), Times.Once);
    }

    #endregion

    #region UpdateAccompaniment Tests

    [Fact]
    public async Task UpdateAccompaniment_ValidData_ReturnsNoContent()
    {
        // Arrange
        var updateDto = new UpdateAccompanimentDto
        {
            Name = "Updated Ketchup",
            ExtraCharge = 0.25m,
            DisplayOrder = 2,
            IsAvailable = true
        };

        _accompanimentServiceMock
            .Setup(x => x.UpdateAccompanimentAsync(It.IsAny<Guid>(), It.IsAny<UpdateAccompanimentDto>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.UpdateAccompaniment(_testAccompanimentId, updateDto);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        _accompanimentServiceMock.Verify(x => x.UpdateAccompanimentAsync(_testAccompanimentId, updateDto), Times.Once);
    }

    #endregion

    #region DeleteAccompaniment Tests

    [Fact]
    public async Task DeleteAccompaniment_ExistingId_ReturnsNoContent()
    {
        // Arrange
        _accompanimentServiceMock
            .Setup(x => x.DeleteAccompanimentAsync(It.IsAny<Guid>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.DeleteAccompaniment(_testAccompanimentId);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        _accompanimentServiceMock.Verify(x => x.DeleteAccompanimentAsync(_testAccompanimentId), Times.Once);
    }

    #endregion

    #region ToggleAvailability Tests

    [Fact]
    public async Task ToggleAvailability_Available_ReturnsNewStatus()
    {
        // Arrange
        var newStatus = false;

        _accompanimentServiceMock
            .Setup(x => x.ToggleAvailabilityAsync(It.IsAny<Guid>()))
            .ReturnsAsync(newStatus);

        // Act
        var result = await _controller.ToggleAvailability(_testAccompanimentId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(new 
        { 
            id = _testAccompanimentId, 
            isAvailable = newStatus 
        });

        _accompanimentServiceMock.Verify(x => x.ToggleAvailabilityAsync(_testAccompanimentId), Times.Once);
    }

    [Fact]
    public async Task ToggleAvailability_Unavailable_ReturnsNewStatus()
    {
        // Arrange
        var newStatus = true;

        _accompanimentServiceMock
            .Setup(x => x.ToggleAvailabilityAsync(It.IsAny<Guid>()))
            .ReturnsAsync(newStatus);

        // Act
        var result = await _controller.ToggleAvailability(_testAccompanimentId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(new 
        { 
            id = _testAccompanimentId, 
            isAvailable = newStatus 
        });

        _accompanimentServiceMock.Verify(x => x.ToggleAvailabilityAsync(_testAccompanimentId), Times.Once);
    }

    #endregion

    #region ValidateSelection Tests

    [Fact]
    public async Task ValidateSelection_ValidSelection_ReturnsValidResult()
    {
        // Arrange
        var request = new ValidateSelectionRequest
        {
            ProductId = _testProductId,
            SelectedAccompanimentIds = new List<Guid> 
            { 
                Guid.NewGuid(), 
                Guid.NewGuid() 
            }
        };

        // ✅ Use the correct ValidationResult from OrdersAPI.Application.Interfaces
        var validationResult = new OrdersAPI.Application.Interfaces.ValidationResult
        {
            IsValid = true,
            Errors = new List<string>()
        };

        _accompanimentServiceMock
            .Setup(x => x.ValidateSelectionAsync(It.IsAny<Guid>(), It.IsAny<List<Guid>>()))
            .ReturnsAsync(validationResult);

        // Act
        var result = await _controller.ValidateSelection(request);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var validation = okResult!.Value as OrdersAPI.Application.Interfaces.ValidationResult;

        validation.Should().NotBeNull();
        validation!.IsValid.Should().BeTrue();
        validation.Errors.Should().BeEmpty();

        _accompanimentServiceMock.Verify(x => x.ValidateSelectionAsync(
            request.ProductId, 
            request.SelectedAccompanimentIds), Times.Once);
    }

    [Fact]
    public async Task ValidateSelection_InvalidSelection_ReturnsInvalidResult()
    {
        // Arrange
        var request = new ValidateSelectionRequest
        {
            ProductId = _testProductId,
            SelectedAccompanimentIds = new List<Guid>()
        };

        var validationResult = new OrdersAPI.Application.Interfaces.ValidationResult
        {
            IsValid = false,
            Errors = new List<string> 
            { 
                "Sauce selection is required",
                "You must select at least 1 sauce"
            }
        };

        _accompanimentServiceMock
            .Setup(x => x.ValidateSelectionAsync(It.IsAny<Guid>(), It.IsAny<List<Guid>>()))
            .ReturnsAsync(validationResult);

        // Act
        var result = await _controller.ValidateSelection(request);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var validation = okResult!.Value as OrdersAPI.Application.Interfaces.ValidationResult;

        validation.Should().NotBeNull();
        validation!.IsValid.Should().BeFalse();
        validation.Errors.Should().HaveCount(2);

        _accompanimentServiceMock.Verify(x => x.ValidateSelectionAsync(
            request.ProductId, 
            request.SelectedAccompanimentIds), Times.Once);
    }

    #endregion

    #region CalculateCharges Tests

    [Fact]
    public async Task CalculateCharges_WithCharges_ReturnsTotalAmount()
    {
        // Arrange
        var accompanimentIds = new List<Guid> 
        { 
            Guid.NewGuid(), 
            Guid.NewGuid(), 
            Guid.NewGuid() 
        };
        var expectedTotal = 3.50m;

        _accompanimentServiceMock
            .Setup(x => x.CalculateTotalExtraChargesAsync(It.IsAny<List<Guid>>()))
            .ReturnsAsync(expectedTotal);

        // Act
        var result = await _controller.CalculateCharges(accompanimentIds);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(new { totalExtraCharge = expectedTotal });

        _accompanimentServiceMock.Verify(x => x.CalculateTotalExtraChargesAsync(accompanimentIds), Times.Once);
    }

    [Fact]
    public async Task CalculateCharges_NoCharges_ReturnsZero()
    {
        // Arrange
        var accompanimentIds = new List<Guid> { Guid.NewGuid() };
        var expectedTotal = 0m;

        _accompanimentServiceMock
            .Setup(x => x.CalculateTotalExtraChargesAsync(It.IsAny<List<Guid>>()))
            .ReturnsAsync(expectedTotal);

        // Act
        var result = await _controller.CalculateCharges(accompanimentIds);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(new { totalExtraCharge = 0m });

        _accompanimentServiceMock.Verify(x => x.CalculateTotalExtraChargesAsync(accompanimentIds), Times.Once);
    }

    [Fact]
    public async Task CalculateCharges_EmptyList_ReturnsZero()
    {
        // Arrange
        var accompanimentIds = new List<Guid>();
        var expectedTotal = 0m;

        _accompanimentServiceMock
            .Setup(x => x.CalculateTotalExtraChargesAsync(It.IsAny<List<Guid>>()))
            .ReturnsAsync(expectedTotal);

        // Act
        var result = await _controller.CalculateCharges(accompanimentIds);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(new { totalExtraCharge = 0m });

        _accompanimentServiceMock.Verify(x => x.CalculateTotalExtraChargesAsync(accompanimentIds), Times.Once);
    }

    #endregion
}
