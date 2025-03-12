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
        
        List<WorkTodo> capturedWorkItems = new List<WorkTodo>();

        _mockDaprClient
            .Setup(c => c.PublishEventAsync<ProcessData>(
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.IsAny<ProcessData>(),
                It.IsAny<Dictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockDaprClient
            .Setup(c => c.PublishEventAsync<WorkTodo>(
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.IsAny<WorkTodo>(),
                It.IsAny<Dictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, string, WorkTodo, Dictionary<string, string>, CancellationToken>(
                (_, _, item, _, _) => capturedWorkItems.Add(item))
            .Returns(Task.CompletedTask);

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
        
        _mockDaprClient.Verify(
            c => c.PublishEventAsync<WorkTodo>(
                "kafka-pubsub", 
                "newWork", 
                It.IsAny<WorkTodo>(),
                metadata,
                It.IsAny<CancellationToken>()), 
            Times.Exactly(3));
        
        Assert.Equal(3, capturedWorkItems.Count);
        Assert.All(capturedWorkItems, item => Assert.Equal(process.Id, item.ProcessId));
        
        // Verify each work item has correct index and total
        for (int i = 0; i < capturedWorkItems.Count; i++)
        {
            Assert.Equal(i, capturedWorkItems[i].Index);
            Assert.Equal(3, capturedWorkItems[i].Total);
        }
        
        // Verify each work item has a unique ID
        var uniqueIds = capturedWorkItems.Select(w => w.Id).Distinct();
        Assert.Equal(3, uniqueIds.Count());
    }

    [Fact]
    public async Task ProcessNewWorkAsync_ShouldUseDefaultWorkCount_WhenEnvironmentVariableNotSet()
    {
        // Arrange
        var process = new ProcessData(Guid.NewGuid(), DateTime.Now, "Test Process");
        var metadata = new Dictionary<string, string>();
        
        // Clear environment variable to test default
        Environment.SetEnvironmentVariable("WORK_COUNT", null);
        
        List<WorkTodo> capturedWorkItems = new List<WorkTodo>();

        _mockDaprClient
            .Setup(c => c.PublishEventAsync<ProcessData>(
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.IsAny<ProcessData>(),
                It.IsAny<Dictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockDaprClient
            .Setup(c => c.PublishEventAsync<WorkTodo>(
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.IsAny<WorkTodo>(),
                It.IsAny<Dictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, string, WorkTodo, Dictionary<string, string>, CancellationToken>(
                (_, _, item, _, _) => capturedWorkItems.Add(item))
            .Returns(Task.CompletedTask);

        // Act
        await _processService.ProcessNewWorkAsync(process, metadata);

        // Assert
        Assert.Equal(5, capturedWorkItems.Count);
        Assert.All(capturedWorkItems, item => Assert.Equal(process.Id, item.ProcessId));
        
        // Verify each work item has correct index and total
        for (int i = 0; i < capturedWorkItems.Count; i++)
        {
            Assert.Equal(i, capturedWorkItems[i].Index);
            Assert.Equal(5, capturedWorkItems[i].Total);
        }
        
        _mockDaprClient.Verify(
            c => c.PublishEventAsync<WorkTodo>(
                "kafka-pubsub", 
                "newWork", 
                It.IsAny<WorkTodo>(),
                metadata,
                It.IsAny<CancellationToken>()), 
            Times.Exactly(5));
    }
    
    [Fact]
    public async Task ProcessNewWorkAsync_Should_GenerateUniqueWorkItems()
    {
        // Arrange
        var process = new ProcessData(Guid.NewGuid(), DateTime.Now, "Test Process");
        var metadata = new Dictionary<string, string>();
        Environment.SetEnvironmentVariable("WORK_COUNT", "2");
        
        List<WorkTodo> capturedWorkItems = new List<WorkTodo>();

        _mockDaprClient
            .Setup(c => c.PublishEventAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockDaprClient
            .Setup(c => c.PublishEventAsync<WorkTodo>(
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.IsAny<WorkTodo>(),
                It.IsAny<Dictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, string, WorkTodo, Dictionary<string, string>, CancellationToken>(
                (_, _, item, _, _) => capturedWorkItems.Add(item))
            .Returns(Task.CompletedTask);

        // Act
        await _processService.ProcessNewWorkAsync(process, metadata);

        // Assert
        Assert.Equal(2, capturedWorkItems.Count);
        
        // Check work item properties
        Assert.Equal(0, capturedWorkItems[0].Index);
        Assert.Equal(2, capturedWorkItems[0].Total);
        Assert.Equal(process.Id, capturedWorkItems[0].ProcessId);
        Assert.NotEqual(Guid.Empty, capturedWorkItems[0].Id);
        
        Assert.Equal(1, capturedWorkItems[1].Index);
        Assert.Equal(2, capturedWorkItems[1].Total);
        Assert.Equal(process.Id, capturedWorkItems[1].ProcessId);
        Assert.NotEqual(Guid.Empty, capturedWorkItems[1].Id);
        
        // Verify IDs are different
        Assert.NotEqual(capturedWorkItems[0].Id, capturedWorkItems[1].Id);
    }
}
