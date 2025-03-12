using Dapr.Client;
using Moq;
using Worker.Models;
using Worker.Services;
using Xunit;

namespace Tests.Worker;

public class WorkerServiceTests
{
    private readonly Mock<DaprClient> _mockDaprClient;
    private readonly WorkService _workerService;

    public WorkerServiceTests()
    {
        _mockDaprClient = new Mock<DaprClient>();
        _workerService = new WorkService(_mockDaprClient.Object);
    }
    
    [Fact]
    public async Task ProcessWorkAsync_Should_UpdateWorkStatus_AndPublishEvent()
    {
        // Arrange
        var workTodo = new WorkTodo
        {
            Id = Guid.NewGuid(),
            ProcessId = Guid.NewGuid(),
            Name = "Test Work",
            Duration = 50,
            Status = "Started",
            startAt = DateTime.Now
        };
        
        var metadata = new Dictionary<string, string>
        {
            { "cloudevent.traceparent", "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01" }
        };
        
        WorkTodo? capturedWork = null;
        
        _mockDaprClient
            .Setup(c => c.PublishEventAsync<WorkTodo>(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<WorkTodo>(),
                It.IsAny<Dictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, string, WorkTodo, Dictionary<string, string>, CancellationToken>(
                (_, _, work, _, _) => capturedWork = work)
            .Returns(Task.CompletedTask);
        
        // Act
        await _workerService.ProcessWorkAsync(workTodo, metadata);
        
        // Assert
        Assert.NotNull(capturedWork);
        Assert.Equal(workTodo.Id, capturedWork!.Id);
        Assert.Equal(workTodo.ProcessId, capturedWork.ProcessId);
        Assert.Equal("Completed", capturedWork.Status);
        
        _mockDaprClient.Verify(
            c => c.PublishEventAsync<WorkTodo>(
                "kafka-pubsub",
                "workCompleted",
                It.IsAny<WorkTodo>(),
                metadata,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
    
    [Fact]
    public async Task ProcessWorkAsync_ShouldSimulateWorkDuration()
    {
        // Arrange
        var workTodo = new WorkTodo
        {
            Id = Guid.NewGuid(),
            ProcessId = Guid.NewGuid(),
            Name = "Fast Work",
            Duration = 10, // Very short duration for test
            Status = "Started",
            startAt = DateTime.Now
        };
        
        var metadata = new Dictionary<string, string>();
        
        _mockDaprClient
            .Setup(c => c.PublishEventAsync<WorkTodo>(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<WorkTodo>(),
                It.IsAny<Dictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        
        var startTime = DateTime.Now;
        
        // Act
        await _workerService.ProcessWorkAsync(workTodo, metadata);
        
        var endTime = DateTime.Now;
        var elapsedMs = (endTime - startTime).TotalMilliseconds;
        
        // Assert
        // Should take at least the duration time (or close to it) to complete
        // Using 5ms as buffer since very short sleeps might not be exact
        Assert.True(elapsedMs >= 5, $"Work processing was too fast: {elapsedMs}ms");
        
        _mockDaprClient.Verify(
            c => c.PublishEventAsync<WorkTodo>(
                "kafka-pubsub",
                "workCompleted",
                It.Is<WorkTodo>(w => w.Status == "Completed"),
                It.IsAny<Dictionary<string, string>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
    
    [Fact]
    public async Task ProcessWorkAsync_Should_LogWorkProcessing()
    {
        // Arrange
        var processId = Guid.NewGuid();
        var workId = Guid.NewGuid();
        
        var workTodo = new WorkTodo
        {
            Id = workId,
            ProcessId = processId,
            Name = "Test Work for Logging",
            Duration = 10,
            Status = "Started",
            startAt = DateTime.Now
        };
        
        var metadata = new Dictionary<string, string>();
        
        // We can't directly test logging output, but we can verify the work object is processed correctly
        _mockDaprClient
            .Setup(c => c.PublishEventAsync<WorkTodo>(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<WorkTodo>(),
                It.IsAny<Dictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        
        // Act
        await _workerService.ProcessWorkAsync(workTodo, metadata);
        
        // Assert
        _mockDaprClient.Verify(
            c => c.PublishEventAsync<WorkTodo>(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.Is<WorkTodo>(w => 
                    w.Id == workId && 
                    w.ProcessId == processId && 
                    w.Status == "Completed"),
                It.IsAny<Dictionary<string, string>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
    
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Started")]
    [InlineData("In Progress")]
    public async Task ProcessWorkAsync_Should_UpdateStatusToCompleted_RegardlessOfInitialStatus(string? initialStatus)
    {
        // Arrange
        var workTodo = new WorkTodo
        {
            Id = Guid.NewGuid(),
            ProcessId = Guid.NewGuid(),
            Name = "Status Test Work",
            Duration = 5,
            Status = initialStatus,
            startAt = DateTime.Now
        };
        
        var metadata = new Dictionary<string, string>();
        
        WorkTodo? capturedWork = null;
        
        _mockDaprClient
            .Setup(c => c.PublishEventAsync<WorkTodo>(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<WorkTodo>(),
                It.IsAny<Dictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, string, WorkTodo, Dictionary<string, string>, CancellationToken>(
                (_, _, work, _, _) => capturedWork = work)
            .Returns(Task.CompletedTask);
        
        // Act
        await _workerService.ProcessWorkAsync(workTodo, metadata);
        
        // Assert
        Assert.NotNull(capturedWork);
        Assert.Equal("Completed", capturedWork!.Status);
    }
}
