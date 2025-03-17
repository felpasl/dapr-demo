using Dapr.Client;
using OrderProcessing.Models;

namespace OrderProcessing.Services;

public class OrderService : IOrderService
{
    private readonly DaprClient daprClient;
    private readonly ILogger<OrderService> logger;

    public OrderService(DaprClient daprClient, ILogger<OrderService> logger)
    {
        this.daprClient = daprClient;
        this.logger = logger;
    }

    public async Task NewOrderAsync(Order order, Dictionary<string, string> metadata)
    {
        this.logger.LogInformation("New process started: {@process}", order);

        await this.daprClient.PublishEventAsync<Order>(
            "kafka-pubsub",
            "processing",
            order,
            metadata
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

            this.logger.LogInformation("New work created: {@work}", work);
            await this.daprClient.PublishEventAsync<OrderItem>(
                "kafka-pubsub",
                "newOrderItem",
                work,
                metadata
            );
        }
    }
}
