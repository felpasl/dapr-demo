using Dapr.Client;
using Microsoft.Extensions.Logging;
using Moq;
using OrderItemProcessing.Models;
using OrderItemProcessing.Services;
using Xunit;

namespace Tests.OrderItemProcessing;

public class WorkerServiceTests
{
    private readonly Mock<DaprClient> _mockDaprClient;
    private readonly OrderItemService _workerService;

    public WorkerServiceTests()
    {
        _mockDaprClient = new Mock<DaprClient>();
        _workerService = new OrderItemService(_mockDaprClient.Object, Mock.Of<ILogger<OrderItemService>>());
    }

    [Fact]
    public async Task ProcessWorkAsync_Should_UpdateWorkStatus_AndPublishEvent()
    {
        // Arrange
        var workTodo = new OrderItem
        {
            Id = Guid.NewGuid(),
            ProcessId = Guid.NewGuid(),
            Name = "Test Work",
            Duration = 50,
            Status = "Started",
            startAt = DateTime.Now,
        };

        var metadata = new Dictionary<string, string>
        {
            { "cloudevent.traceparent", "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01" },
        };

        OrderItem? capturedWork = null;

        _mockDaprClient
            .Setup(c =>
                c.PublishEventAsync<OrderItem>(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<OrderItem>(),
                    It.IsAny<Dictionary<string, string>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .Callback<string, string, OrderItem, Dictionary<string, string>, CancellationToken>(
                (_, _, work, _, _) => capturedWork = work
            )
            .Returns(Task.CompletedTask);

        // Act
        await _workerService.ProcessWorkAsync(workTodo, metadata);

        // Assert
        Assert.NotNull(capturedWork);
        Assert.Equal(workTodo.Id, capturedWork!.Id);
        Assert.Equal(workTodo.ProcessId, capturedWork.ProcessId);
        Assert.Equal("Completed", capturedWork.Status);

        _mockDaprClient.Verify(
            c =>
                c.PublishEventAsync<OrderItem>(
                    "kafka-pubsub",
                    "workCompleted",
                    It.IsAny<OrderItem>(),
                    metadata,
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task ProcessWorkAsync_ShouldSimulateWorkDuration()
    {
        // Arrange
        var workTodo = new OrderItem
        {
            Id = Guid.NewGuid(),
            ProcessId = Guid.NewGuid(),
            Name = "Fast Work",
            Duration = 10, // Very short duration for test
            Status = "Started",
            startAt = DateTime.Now,
        };

        var metadata = new Dictionary<string, string>();

        _mockDaprClient
            .Setup(c =>
                c.PublishEventAsync<OrderItem>(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<OrderItem>(),
                    It.IsAny<Dictionary<string, string>>(),
                    It.IsAny<CancellationToken>()
                )
            )
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
            c =>
                c.PublishEventAsync<OrderItem>(
                    "kafka-pubsub",
                    "workCompleted",
                    It.Is<OrderItem>(w => w.Status == "Completed"),
                    It.IsAny<Dictionary<string, string>>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task ProcessWorkAsync_Should_LogWorkProcessing()
    {
        // Arrange
        var processId = Guid.NewGuid();
        var workId = Guid.NewGuid();

        var workTodo = new OrderItem
        {
            Id = workId,
            ProcessId = processId,
            Name = "Test Work for Logging",
            Duration = 10,
            Status = "Started",
            startAt = DateTime.Now,
        };

        var metadata = new Dictionary<string, string>();

        // We can't directly test logging output, but we can verify the work object is processed correctly
        _mockDaprClient
            .Setup(c =>
                c.PublishEventAsync<OrderItem>(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<OrderItem>(),
                    It.IsAny<Dictionary<string, string>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .Returns(Task.CompletedTask);

        // Act
        await _workerService.ProcessWorkAsync(workTodo, metadata);

        // Assert
        _mockDaprClient.Verify(
            c =>
                c.PublishEventAsync<OrderItem>(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.Is<OrderItem>(w =>
                        w.Id == workId && w.ProcessId == processId && w.Status == "Completed"
                    ),
                    It.IsAny<Dictionary<string, string>>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Theory]
    [InlineData("")]
    [InlineData("Started")]
    [InlineData("In Progress")]
    public async Task ProcessWorkAsync_Should_UpdateStatusToCompleted_RegardlessOfInitialStatus(
        string initialStatus
    )
    {
        // Arrange
        var workTodo = new OrderItem
        {
            Id = Guid.NewGuid(),
            ProcessId = Guid.NewGuid(),
            Name = "Status Test Work",
            Duration = 5,
            Status = initialStatus,
            startAt = DateTime.Now,
        };

        var metadata = new Dictionary<string, string>();

        OrderItem? capturedWork = null;

        _mockDaprClient
            .Setup(c =>
                c.PublishEventAsync<OrderItem>(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<OrderItem>(),
                    It.IsAny<Dictionary<string, string>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .Callback<string, string, OrderItem, Dictionary<string, string>, CancellationToken>(
                (_, _, work, _, _) => capturedWork = work
            )
            .Returns(Task.CompletedTask);

        // Act
        await _workerService.ProcessWorkAsync(workTodo, metadata);

        // Assert
        Assert.NotNull(capturedWork);
        Assert.Equal("Completed", capturedWork!.Status);
    }

    [Fact]
    public async Task ProcessWorkAsync_Should_PublishProcessCompletedEvent_WhenLastWorkItem()
    {
        // Arrange
        var processId = Guid.NewGuid();
        var workTodo = new OrderItem
        {
            Id = Guid.NewGuid(),
            ProcessId = processId,
            Name = "Last Work Item",
            Duration = 5,
            Status = "Started",
            startAt = DateTime.Now,
            Index = 2,
            Total = 3, // This makes Index == Total - 1, so it's the last item
        };

        var metadata = new Dictionary<string, string> { { "traceid", "test-trace-id" } };

        ProcessFinished? capturedProcessFinished = null;

        _mockDaprClient
            .Setup(c =>
                c.PublishEventAsync<OrderItem>(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<OrderItem>(),
                    It.IsAny<Dictionary<string, string>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .Returns(Task.CompletedTask);

        _mockDaprClient
            .Setup(c =>
                c.PublishEventAsync<ProcessFinished>(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<ProcessFinished>(),
                    It.IsAny<Dictionary<string, string>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .Callback<
                string,
                string,
                ProcessFinished,
                Dictionary<string, string>,
                CancellationToken
            >((_, _, process, _, _) => capturedProcessFinished = process)
            .Returns(Task.CompletedTask);

        // Act
        await _workerService.ProcessWorkAsync(workTodo, metadata);

        // Assert
        // Verify work completed event was published
        _mockDaprClient.Verify(
            c =>
                c.PublishEventAsync<OrderItem>(
                    "kafka-pubsub",
                    "workCompleted",
                    It.Is<OrderItem>(w => w.Status == "Completed"),
                    metadata,
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );

        // Verify process completed event was published
        _mockDaprClient.Verify(
            c =>
                c.PublishEventAsync<ProcessFinished>(
                    "kafka-pubsub",
                    "processCompleted",
                    It.IsAny<ProcessFinished>(),
                    metadata,
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );

        // Verify the process finished data is correct
        Assert.NotNull(capturedProcessFinished);
        Assert.Equal(processId, capturedProcessFinished!.Id);
        Assert.Equal("Success", capturedProcessFinished.Status);
    }

    [Fact]
    public async Task ProcessWorkAsync_ShouldNot_PublishProcessCompletedEvent_WhenNotLastWorkItem()
    {
        // Arrange
        var processId = Guid.NewGuid();
        var workTodo = new OrderItem
        {
            Id = Guid.NewGuid(),
            ProcessId = processId,
            Name = "Not Last Work Item",
            Duration = 5,
            Status = "Started",
            startAt = DateTime.Now,
            Index = 1, // Not the last item
            Total = 3,
        };

        var metadata = new Dictionary<string, string>();

        _mockDaprClient
            .Setup(c =>
                c.PublishEventAsync<OrderItem>(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<OrderItem>(),
                    It.IsAny<Dictionary<string, string>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .Returns(Task.CompletedTask);

        // Act
        await _workerService.ProcessWorkAsync(workTodo, metadata);

        // Assert
        // Verify work completed event was published
        _mockDaprClient.Verify(
            c =>
                c.PublishEventAsync<OrderItem>(
                    "kafka-pubsub",
                    "workCompleted",
                    It.Is<OrderItem>(w => w.Status == "Completed"),
                    metadata,
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );

        // Verify process completed event was NOT published
        _mockDaprClient.Verify(
            c =>
                c.PublishEventAsync<ProcessFinished>(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<ProcessFinished>(),
                    It.IsAny<Dictionary<string, string>>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );
    }
}
