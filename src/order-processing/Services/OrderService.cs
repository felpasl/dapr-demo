using Dapr.Client;
using Dapr.Common.Logging;
using OrderProcessing.Models;

namespace OrderProcessing.Services;

public class OrderService : IOrderService
{
    private readonly DaprClient daprClient;
    private readonly ILogger<OrderService> logger;

    public OrderService(
        DaprClient daprClient,
        ILogger<OrderService> logger
    )
    {
        this.daprClient = daprClient;
        this.logger = logger;
    }

    public async Task NewOrderAsync(Order order, Dictionary<string, string> metadata)
    {
        using (this.logger.BeginScope(order.Id.ToString(), "ProcessingStarted"))
        {
            this.logger.LogEvent("New process started", order);

            await this.daprClient.PublishEventAsync<Order>(
                "kafka-pubsub",
                "processing",
                order,
                metadata
            );

            // Log the Dapr publish event
            this.logger.LogDaprPubSubEvent(
                order.Id.ToString(),
                "kafka-pubsub",
                "processing",
                "Publish",
                order
            );

            for (int i = 0; i < order.Quantity; i++)
            {
                var work = new OrderItem
                {
                    Id = Guid.NewGuid(),
                    ProcessId = order.Id,
                    startAt = DateTime.Now,
                    Total = order.Quantity,
                    Index = i,
                    Name = $"Work {i}",
                    Duration = new Random().Next(20, 100),
                    Status = "Started",
                };

                using (this.logger.BeginScope(work.Id.ToString(), "WorkCreated"))
                {
                    this.logger.LogEvent("New work item created", work);

                    await this.daprClient.PublishEventAsync<OrderItem>(
                        "kafka-pubsub",
                        "newOrderItem",
                        work,
                        metadata
                    );

                    // Log the Dapr publish event for each work item
                    this.logger.LogDaprPubSubEvent(
                        work.Id.ToString(),
                        "kafka-pubsub",
                        "newOrderItem",
                        "Publish",
                        work
                    );
                }
            }
        }
    }
}
