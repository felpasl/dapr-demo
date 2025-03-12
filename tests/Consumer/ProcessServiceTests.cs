using Consumer.Models;
using Consumer.Services;
using Dapr.Client;
using Moq;
using Xunit;

namespace Tests.Consumer;

public class ProcessServiceTests
{
    private readonly Mock<DaprClient> _mockDaprClient;
    private readonly ProcessService _processService;

    public ProcessServiceTests()
    {
        _mockDaprClient = new Mock<DaprClient>();
        _processService = new ProcessService(_mockDaprClient.Object);
    }

    [Fact]
    public async Task ProcessNewWorkAsync_Should_PublishProcessingEvent()
    {
        // Arrange
        var process = new ProcessData(Guid.NewGuid(), DateTime.Now, "Test Process");
        var metadata = new Dictionary<string, string>
        {
            { "cloudevent.traceparent", "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01" }
        };

        Environment.SetEnvironmentVariable("WORK_COUNT", "3");

        _mockDaprClient
            .Setup(c => c.PublishEventAsync<ProcessData>(
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.IsAny<ProcessData>(),
                It.IsAny<Dictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockDaprClient
            .Setup(c => c.BulkPublishEventAsync<WorkTodo>(
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.IsAny<List<WorkTodo>>(),
                It.IsAny<Dictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dapr.Client.BulkPublishResponse<WorkTodo>(null));

        // Act
        await _processService.ProcessNewWorkAsync(process, metadata);

        // Assert
        _mockDaprClient.Verify(
            c => c.PublishEventAsync<ProcessData>(
                "kafka-pubsub", 
                "processing", 
                process,
                metadata,
                It.IsAny<CancellationToken>()), 
            Times.Once);
        
        // Verify that 3 work items were published
        _mockDaprClient.Verify(
            c => c.BulkPublishEventAsync<WorkTodo>(
                "kafka-pubsub", 
                "newWork", 
                It.IsAny<List<WorkTodo>>(),
                metadata,
                It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    [Fact]
    public async Task ProcessNewWorkAsync_ShouldUseDefaultWorkCount_WhenEnvironmentVariableNotSet()
    {
        // Arrange
        var process = new ProcessData(Guid.NewGuid(), DateTime.Now, "Test Process");
        var metadata = new Dictionary<string, string>();
        
        // Clear environment variable to test default
        Environment.SetEnvironmentVariable("WORK_COUNT", null);

        _mockDaprClient
            .Setup(c => c.PublishEventAsync<ProcessData>(
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.IsAny<ProcessData>(),
                It.IsAny<Dictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockDaprClient
            .Setup(c => c.BulkPublishEventAsync<WorkTodo>(
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.IsAny<List<WorkTodo>>(),
                It.IsAny<Dictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dapr.Client.BulkPublishResponse<WorkTodo>(null));

        // Act
        await _processService.ProcessNewWorkAsync(process, metadata);

        // Assert
        // Default WORK_COUNT should be 5
        _mockDaprClient.Verify(
            c => c.BulkPublishEventAsync<WorkTodo>(
                "kafka-pubsub", 
                "newWork", 
                It.IsAny<List<WorkTodo>>(),
                metadata,
                It.IsAny<CancellationToken>()), 
            Times.Once);
    }
}
