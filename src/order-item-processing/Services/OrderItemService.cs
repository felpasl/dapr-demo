using Dapr.Client;
using OrderItemProcessing.Models;

namespace OrderItemProcessing.Services
{
    public class OrderItemService : IOrderItemService
    {
        private readonly DaprClient _daprClient;
        private readonly ILogger<OrderItemService> logger;

        public OrderItemService(DaprClient daprClient, ILogger<OrderItemService> logger)
        {
            _daprClient = daprClient;
            this.logger = logger;
        }

        public async Task ProcessWorkAsync(Models.OrderItem orderItem, Dictionary<string, string> metadata)
        {
            logger.LogInformation("New process started: {@orderItem}", orderItem);

            await Task.Delay(orderItem.Duration);

            orderItem.Status = "Completed";

            await _daprClient.PublishEventAsync<Models.OrderItem>(
                "kafka-pubsub",
                "orderItemCompleted",
                orderItem,
                metadata
            );

            if (orderItem.Index == orderItem.Total - 1)
            {
                logger.LogInformation("Process completed: {ProcessId}", orderItem.ProcessId);
                await _daprClient.PublishEventAsync<ProcessFinished>(
                    "kafka-pubsub",
                    "orderCompleted",
                    new ProcessFinished(orderItem.ProcessId, DateTime.Now, "Success"),
                    metadata
                );
            }
        }
    }
}
