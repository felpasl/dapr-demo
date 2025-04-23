using System.Text;
using Dapr.Client;
using Dapr.Common.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Moq;
using OrderApi.Models;
using OrderApi.Services;
using Xunit;

namespace Tests.OrderApi;

public class OrderServiceTests
{
    private readonly Mock<DaprClient> _mockDaprClient;
    private readonly OrderService _orderService;

    public OrderServiceTests()
    {
        _mockDaprClient = new Mock<DaprClient>();
        var logger = Mock.Of<ILogger<OrderService>>();
        var businessLogger = new BusinessEventLogger<OrderService>(logger);
        _orderService = new OrderService(_mockDaprClient.Object, logger, businessLogger);
    }

    [Fact]
    public async Task StartProcessAsync_Should_PublishEvent_And_ReturnProcess()
    {
        // Arrange
        var metadata = new Dictionary<string, string>();

        _mockDaprClient
            .Setup(c =>
                c.PublishEventAsync<Order>(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<Order>(),
                    It.IsAny<Dictionary<string, string>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .Returns(Task.CompletedTask);
        var order = new Order(Guid.NewGuid(), DateTime.Now, 10, "ProcessData");
        // Act
        var result = await _orderService.StartProcessAsync(order, metadata);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("ProcessData", result.Name);

        _mockDaprClient.Verify(
            c =>
                c.PublishEventAsync<Order>(
                    "kafka-pubsub",
                    "newOrder",
                    It.IsAny<Order>(),
                    metadata,
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task StartProcessAsync_ShouldPassMetadataToPublishEvent()
    {
        // Arrange
        var metadata = new Dictionary<string, string>
        {
            { "cloudevent.traceparent", "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01" },
        };

        Dictionary<string, string>? capturedMetadata = null;

        _mockDaprClient
            .Setup(c =>
                c.PublishEventAsync<Order>(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<Order>(),
                    It.IsAny<Dictionary<string, string>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .Callback<string, string, Order, Dictionary<string, string>, CancellationToken>(
                (_, _, _, meta, _) => capturedMetadata = meta
            )
            .Returns(Task.CompletedTask);
        var order = new Order(Guid.NewGuid(), DateTime.Now, 10, "ProcessData");

        // Act
        var result = await _orderService.StartProcessAsync(order, metadata);

        // Assert
        Assert.NotNull(capturedMetadata);
        Assert.True(capturedMetadata.ContainsKey("cloudevent.traceparent"));
        Assert.Equal(
            "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01",
            capturedMetadata["cloudevent.traceparent"]
        );
    }
}
