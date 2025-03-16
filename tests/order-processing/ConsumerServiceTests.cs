using OrderProcessing.Models;
using OrderProcessing.Services;
using Dapr.Client;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Tests.OrderProcessing;

public class OrderProcessingServiceTests
{
    private readonly Mock<DaprClient> mockDaprClient;
    private readonly OrderProcessingService consumeService;

    public OrderProcessingServiceTests()
    {
        this.mockDaprClient = new Mock<DaprClient>();
        this.consumeService = new OrderProcessingService(
            this.mockDaprClient.Object,
            Mock.Of<ILogger<OrderProcessingService>>()
        );
    }

    [Fact]
    public async Task ProcessNewWorkAsync_Should_PublishProcessingEvent()
    {
        // Arrange
        var process = new Order(Guid.NewGuid(), DateTime.Now, 3, "Test Process");
        var metadata = new Dictionary<string, string>
        {
            { "cloudevent.traceparent", "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01" },
        };

        List<global::OrderProcessing.Models.OrderItem> capturedWorkItems = new List<global::OrderProcessing.Models.OrderItem>();

        this.mockDaprClient.Setup(c =>
                c.PublishEventAsync<Order>(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<Order>(),
                    It.IsAny<Dictionary<string, string>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .Returns(Task.CompletedTask);

        this.mockDaprClient.Setup(c =>
                c.PublishEventAsync<global::OrderProcessing.Models.OrderItem>(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<global::OrderProcessing.Models.OrderItem>(),
                    It.IsAny<Dictionary<string, string>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .Callback<string, string, global::OrderProcessing.Models.OrderItem, Dictionary<string, string>, CancellationToken>(
                (_, _, item, _, _) => capturedWorkItems.Add(item)
            )
            .Returns(Task.CompletedTask);

        // Act
        await this.consumeService.ProcessNewWorkAsync(process, metadata);

        // Assert
        this.mockDaprClient.Verify(
            c =>
                c.PublishEventAsync<Order>(
                    "kafka-pubsub",
                    "processing",
                    process,
                    metadata,
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );

        this.mockDaprClient.Verify(
            c =>
                c.PublishEventAsync<global::OrderProcessing.Models.OrderItem>(
                    "kafka-pubsub",
                    "newOrderItem",
                    It.IsAny<global::OrderProcessing.Models.OrderItem>(),
                    metadata,
                    It.IsAny<CancellationToken>()
                ),
            Times.Exactly(3)
        );

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
    public async Task ProcessNewWorkAsync_Should_GenerateUniqueWorkItems()
    {
        // Arrange
        var process = new Order(Guid.NewGuid(), DateTime.Now, 2, "Test Process");
        var metadata = new Dictionary<string, string>();

        List<global::OrderProcessing.Models.OrderItem> capturedWorkItems = new List<global::OrderProcessing.Models.OrderItem>();

        this.mockDaprClient.Setup(c =>
                c.PublishEventAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<object>(),
                    It.IsAny<Dictionary<string, string>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .Returns(Task.CompletedTask);

        this.mockDaprClient.Setup(c =>
                c.PublishEventAsync<global::OrderProcessing.Models.OrderItem>(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<global::OrderProcessing.Models.OrderItem>(),
                    It.IsAny<Dictionary<string, string>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .Callback<string, string, global::OrderProcessing.Models.OrderItem, Dictionary<string, string>, CancellationToken>(
                (_, _, item, _, _) => capturedWorkItems.Add(item)
            )
            .Returns(Task.CompletedTask);

        // Act
        await this.consumeService.ProcessNewWorkAsync(process, metadata);

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
