using DocumentArchive.API.Controllers;
using DocumentArchive.Core.DTOs.Shared;
using DocumentArchive.Core.Models;
using DocumentArchive.Infrastructure.Data;
using DocumentArchive.Tests.ServicesTests;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DocumentArchive.Tests.ControllersTests;

public class AuditControllerTests : TestBase
{
    private readonly AuditController _controller;

    public AuditControllerTests()
    {
        _controller = new AuditController(Context);
    }

    protected override void SeedData()
    {
        var logs = new List<SecurityAuditLog>();
        for (int i = 0; i < 5; i++)
        {
            logs.Add(new SecurityAuditLog
            {
                Id = Guid.NewGuid(),
                EventType = SecurityEventType.Login,
                UserId = Guid.NewGuid(),
                Timestamp = DateTime.UtcNow.AddDays(-i),
                Success = true
            });
        }
        Context.SecurityAuditLogs.AddRange(logs);
        Context.SaveChanges();
    }

    [Fact]
    public async Task GetAuditLogs_ShouldReturnPagedResult_WithDefaultParams()
    {
        // Act
        var result = await _controller.GetAuditLogs();

        // Assert
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);

        var paged = okResult.Value as PagedResult<SecurityAuditLog>;
        paged.Should().NotBeNull();
        paged!.PageNumber.Should().Be(1);
        paged.PageSize.Should().Be(20);
        paged.TotalCount.Should().Be(5);
    }

    [Fact]
    public async Task GetAuditLogs_WithFilters_ShouldFilter()
    {
        // Arrange
        var userId = Context.SecurityAuditLogs.First().UserId!.Value;
        var fromDate = DateTime.UtcNow.AddDays(-2);

        // Act
        var result = await _controller.GetAuditLogs(
            page: 1,
            pageSize: 10,
            eventType: SecurityEventType.Login,
            userId: userId,
            fromDate: fromDate,
            toDate: null);

        // Assert
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        var paged = okResult!.Value as PagedResult<SecurityAuditLog>;
        paged.Should().NotBeNull();
        paged!.Items.All(l => l.EventType == SecurityEventType.Login).Should().BeTrue();
        paged.Items.All(l => l.UserId == userId).Should().BeTrue();
        paged.Items.All(l => l.Timestamp >= fromDate).Should().BeTrue();
    }

    [Fact]
    public async Task GetUserLogs_ShouldReturnLogsForUser()
    {
        // Arrange
        var userId = Context.SecurityAuditLogs.First().UserId!.Value;

        // Act
        var result = await _controller.GetUserLogs(userId);

        // Assert
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
        var logs = okResult.Value as List<SecurityAuditLog>;
        logs.Should().NotBeNull();
        logs!.All(l => l.UserId == userId).Should().BeTrue();
    }
}