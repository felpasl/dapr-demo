using OrderProcessing.Models;
using Dapr.Client;

namespace OrderProcessing.Services
{
    public class OrderProcessingService : IOrderProcessingService
    {
        private readonly DaprClient daprClient;
        private readonly ILogger<OrderProcessingService> logger;

        public OrderProcessingService(DaprClient daprClient, ILogger<OrderProcessingService> logger)
        {
            this.daprClient = daprClient;
            this.logger = logger;
        }

        public async Task ProcessNewWorkAsync(
            Order order,
            Dictionary<string, string> metadata
        )
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
}
