using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using OrdersAPI.API.Controllers;
using OrdersAPI.Application.DTOs;
using OrdersAPI.Application.Interfaces;
using System.Security.Claims;
using Xunit;

namespace OrdersAPI.Tests.Controllers;

public class NotificationsControllerTests
{
    private readonly Mock<INotificationService> _notificationServiceMock;
    private readonly NotificationsController _controller;
    private readonly Guid _testUserId;
    private readonly Guid _testNotificationId;

    public NotificationsControllerTests()
    {
        _notificationServiceMock = new Mock<INotificationService>();
        _controller = new NotificationsController(_notificationServiceMock.Object);
        _testUserId = Guid.NewGuid();
        _testNotificationId = Guid.NewGuid();

        SetupAuthenticatedUser(_testUserId);
    }

    #region GetNotifications Tests

    [Fact]
    public async Task GetNotifications_AllNotifications_ReturnsAllUserNotifications()
    {
        // Arrange
        var expectedNotifications = new List<NotificationDto>
        {
            new NotificationDto
            {
                Id = Guid.NewGuid(),
                UserId = _testUserId,
                Title = "Order Ready",
                Message = "Your order #1234 is ready",
                Type = "Order",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            },
            new NotificationDto
            {
                Id = Guid.NewGuid(),
                UserId = _testUserId,
                Title = "New Promotion",
                Message = "Check out our new deals",
                Type = "Promotion",
                IsRead = true,
                CreatedAt = DateTime.UtcNow.AddHours(-2)
            }
        };

        _notificationServiceMock
            .Setup(x => x.GetUserNotificationsAsync(_testUserId, false))
            .ReturnsAsync(expectedNotifications);

        // Act
        var result = await _controller.GetNotifications(false);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var notifications = okResult!.Value as IEnumerable<NotificationDto>;

        notifications.Should().NotBeNull();
        notifications.Should().HaveCount(2);
        notifications!.First().Title.Should().Be("Order Ready");
        notifications.Last().IsRead.Should().BeTrue();

        _notificationServiceMock.Verify(x => x.GetUserNotificationsAsync(_testUserId, false), Times.Once);
    }

    [Fact]
    public async Task GetNotifications_UnreadOnly_ReturnsOnlyUnreadNotifications()
    {
        // Arrange
        var expectedNotifications = new List<NotificationDto>
        {
            new NotificationDto
            {
                Id = Guid.NewGuid(),
                UserId = _testUserId,
                Title = "Order Ready",
                Message = "Your order #1234 is ready",
                Type = "Order",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            }
        };

        _notificationServiceMock
            .Setup(x => x.GetUserNotificationsAsync(_testUserId, true))
            .ReturnsAsync(expectedNotifications);

        // Act
        var result = await _controller.GetNotifications(true);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var notifications = okResult!.Value as IEnumerable<NotificationDto>;

        notifications.Should().NotBeNull();
        notifications.Should().HaveCount(1);
        notifications!.All(n => !n.IsRead).Should().BeTrue();

        _notificationServiceMock.Verify(x => x.GetUserNotificationsAsync(_testUserId, true), Times.Once);
    }

    [Fact]
    public async Task GetNotifications_NoNotifications_ReturnsEmptyList()
    {
        // Arrange
        _notificationServiceMock
            .Setup(x => x.GetUserNotificationsAsync(_testUserId, false))
            .ReturnsAsync(new List<NotificationDto>());

        // Act
        var result = await _controller.GetNotifications(false);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var notifications = okResult!.Value as IEnumerable<NotificationDto>;

        notifications.Should().NotBeNull();
        notifications.Should().BeEmpty();

        _notificationServiceMock.Verify(x => x.GetUserNotificationsAsync(_testUserId, false), Times.Once);
    }

    #endregion

    #region GetUnreadCount Tests

    [Fact]
    public async Task GetUnreadCount_ReturnsUnreadCount()
    {
        // Arrange
        var expectedCount = 5;

        _notificationServiceMock
            .Setup(x => x.GetUnreadCountAsync(_testUserId))
            .ReturnsAsync(expectedCount);

        // Act
        var result = await _controller.GetUnreadCount();

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(new { count = expectedCount });

        _notificationServiceMock.Verify(x => x.GetUnreadCountAsync(_testUserId), Times.Once);
    }

    [Fact]
    public async Task GetUnreadCount_NoUnreadNotifications_ReturnsZero()
    {
        // Arrange
        _notificationServiceMock
            .Setup(x => x.GetUnreadCountAsync(_testUserId))
            .ReturnsAsync(0);

        // Act
        var result = await _controller.GetUnreadCount();

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(new { count = 0 });

        _notificationServiceMock.Verify(x => x.GetUnreadCountAsync(_testUserId), Times.Once);
    }

    #endregion

    #region CreateNotification Tests

    [Fact]
    public async Task CreateNotification_ValidData_ReturnsCreatedNotification()
    {
        // Arrange
        var createDto = new CreateNotificationDto
        {
            UserId = Guid.NewGuid(),
            Title = "System Alert",
            Message = "Scheduled maintenance tonight",
            Type = "System"
        };

        var expectedNotification = new NotificationDto
        {
            Id = _testNotificationId,
            UserId = createDto.UserId,
            Title = "System Alert",
            Message = "Scheduled maintenance tonight",
            Type = "System",
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        _notificationServiceMock
            .Setup(x => x.CreateNotificationAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .ReturnsAsync(expectedNotification);

        // Act
        var result = await _controller.CreateNotification(createDto);

        // Assert
        result.Result.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = result.Result as CreatedAtActionResult;
        var notification = createdResult!.Value as NotificationDto;

        notification.Should().NotBeNull();
        notification!.Title.Should().Be("System Alert");
        notification.Message.Should().Be("Scheduled maintenance tonight");
        notification.Type.Should().Be("System");
        notification.IsRead.Should().BeFalse();

        createdResult.ActionName.Should().Be(nameof(_controller.GetNotifications));
        createdResult.RouteValues!["id"].Should().Be(_testNotificationId);

        _notificationServiceMock.Verify(x => x.CreateNotificationAsync(
            createDto.UserId,
            createDto.Title,
            createDto.Message,
            createDto.Type), Times.Once);
    }

    [Fact]
    public async Task CreateNotification_InvalidUserId_ThrowsKeyNotFoundException()
    {
        // Arrange
        var createDto = new CreateNotificationDto
        {
            UserId = Guid.NewGuid(),
            Title = "Test",
            Message = "Test message",
            Type = "Info"
        };

        _notificationServiceMock
            .Setup(x => x.CreateNotificationAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .ThrowsAsync(new KeyNotFoundException("User not found"));

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _controller.CreateNotification(createDto));

        _notificationServiceMock.Verify(x => x.CreateNotificationAsync(
            createDto.UserId,
            createDto.Title,
            createDto.Message,
            createDto.Type), Times.Once);
    }

    #endregion

    #region MarkAsRead Tests

    [Fact]
    public async Task MarkAsRead_ExistingNotification_ReturnsNoContent()
    {
        // Arrange
        _notificationServiceMock
            .Setup(x => x.MarkAsReadAsync(It.IsAny<Guid>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.MarkAsRead(_testNotificationId);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        _notificationServiceMock.Verify(x => x.MarkAsReadAsync(_testNotificationId), Times.Once);
    }

    [Fact]
    public async Task MarkAsRead_NonExistingNotification_ThrowsKeyNotFoundException()
    {
        // Arrange
        _notificationServiceMock
            .Setup(x => x.MarkAsReadAsync(It.IsAny<Guid>()))
            .ThrowsAsync(new KeyNotFoundException($"Notification with ID {_testNotificationId} not found"));

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _controller.MarkAsRead(_testNotificationId));

        _notificationServiceMock.Verify(x => x.MarkAsReadAsync(_testNotificationId), Times.Once);
    }

    #endregion

    #region MarkAllAsRead Tests

    [Fact]
    public async Task MarkAllAsRead_MarksAllUserNotifications_ReturnsNoContent()
    {
        // Arrange
        _notificationServiceMock
            .Setup(x => x.MarkAllAsReadAsync(_testUserId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.MarkAllAsRead();

        // Assert
        result.Should().BeOfType<NoContentResult>();

        _notificationServiceMock.Verify(x => x.MarkAllAsReadAsync(_testUserId), Times.Once);
    }

    #endregion

    #region DeleteNotification Tests

    [Fact]
    public async Task DeleteNotification_ExistingNotification_ReturnsNoContent()
    {
        // Arrange
        _notificationServiceMock
            .Setup(x => x.DeleteNotificationAsync(It.IsAny<Guid>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.DeleteNotification(_testNotificationId);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        _notificationServiceMock.Verify(x => x.DeleteNotificationAsync(_testNotificationId), Times.Once);
    }

    [Fact]
    public async Task DeleteNotification_NonExistingNotification_ThrowsKeyNotFoundException()
    {
        // Arrange
        _notificationServiceMock
            .Setup(x => x.DeleteNotificationAsync(It.IsAny<Guid>()))
            .ThrowsAsync(new KeyNotFoundException($"Notification with ID {_testNotificationId} not found"));

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _controller.DeleteNotification(_testNotificationId));

        _notificationServiceMock.Verify(x => x.DeleteNotificationAsync(_testNotificationId), Times.Once);
    }

    #endregion

    #region DeleteReadNotifications Tests

    [Fact]
    public async Task DeleteReadNotifications_DeletesAllReadNotifications_ReturnsNoContent()
    {
        // Arrange
        _notificationServiceMock
            .Setup(x => x.DeleteReadNotificationsAsync(_testUserId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.DeleteReadNotifications();

        // Assert
        result.Should().BeOfType<NoContentResult>();

        _notificationServiceMock.Verify(x => x.DeleteReadNotificationsAsync(_testUserId), Times.Once);
    }

    #endregion

    #region Helper Methods

    private void SetupAuthenticatedUser(Guid userId)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, "test@example.com"),
            new Claim(ClaimTypes.Role, "Admin")
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
