using Consumer.Models;
using Dapr.Client;

namespace Consumer.Services
{
    public class ConsumerService : IConsumerService
    {
        private readonly DaprClient daprClient;
        private readonly ILogger<ConsumerService> logger;

        public ConsumerService(DaprClient daprClient, ILogger<ConsumerService> logger)
        {
            this.daprClient = daprClient;
            this.logger = logger;
        }

        public async Task ProcessNewWorkAsync(
            ProcessData process,
            Dictionary<string, string> metadata
        )
        {
            this.logger.LogInformation("New process started: {@process}", process);

            var count = System.Environment.GetEnvironmentVariable("WORK_COUNT") ?? "5";

            await this.daprClient.PublishEventAsync<ProcessData>(
                "kafka-pubsub",
                "processing",
                process,
                metadata
            );

            for (int i = 0; i < int.Parse(count); i++)
            {
                var work = new WorkTodo
                {
                    Id = Guid.NewGuid(),
                    ProcessId = process.Id,
                    startAt = DateTime.Now,
                    Total = int.Parse(count),
                    Index = i,
                    Name = $"Work {i}",
                    Duration = new Random().Next(20, 100),
                    Status = "Started",
                };

                this.logger.LogInformation("New work created: {@work}", work);
                await this.daprClient.PublishEventAsync<WorkTodo>(
                    "kafka-pubsub",
                    "newWork",
                    work,
                    metadata
                );
            }
        }
    }
}
