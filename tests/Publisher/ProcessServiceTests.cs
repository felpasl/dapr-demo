using System.Text;
using Dapr.Client;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Moq;
using Publisher.Models;
using Publisher.Services;
using Xunit;

namespace Tests.Publisher;

public class ProcessServiceTests
{
    private readonly Mock<DaprClient> _mockDaprClient;
    private readonly ProcessService _processService;

    public ProcessServiceTests()
    {
        _mockDaprClient = new Mock<DaprClient>();
        _processService = new ProcessService(
            _mockDaprClient.Object,
            Mock.Of<ILogger<ProcessService>>()
        );
    }

    [Fact]
    public async Task StartProcessAsync_Should_PublishEvent_And_ReturnProcess()
    {
        // Arrange
        var metadata = new Dictionary<string, string>();

        _mockDaprClient
            .Setup(c =>
                c.PublishEventAsync<ProcessData>(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<ProcessData>(),
                    It.IsAny<Dictionary<string, string>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .Returns(Task.CompletedTask);

        // Act
        var result = await _processService.StartProcessAsync(metadata);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("ProcessData", result.Name);

        _mockDaprClient.Verify(
            c =>
                c.PublishEventAsync<ProcessData>(
                    "kafka-pubsub",
                    "newProcess",
                    It.IsAny<ProcessData>(),
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
                c.PublishEventAsync<ProcessData>(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<ProcessData>(),
                    It.IsAny<Dictionary<string, string>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .Callback<string, string, ProcessData, Dictionary<string, string>, CancellationToken>(
                (_, _, _, meta, _) => capturedMetadata = meta
            )
            .Returns(Task.CompletedTask);

        // Act
        var result = await _processService.StartProcessAsync(metadata);

        // Assert
        Assert.NotNull(capturedMetadata);
        Assert.True(capturedMetadata.ContainsKey("cloudevent.traceparent"));
        Assert.Equal(
            "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01",
            capturedMetadata["cloudevent.traceparent"]
        );
    }
}
