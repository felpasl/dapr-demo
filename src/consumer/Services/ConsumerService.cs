using Consumer.Models;
using Dapr.Client;
using Serilog;

namespace Consumer.Services
{
    public class ConsumerService : IConsumerService
    {
        private readonly DaprClient _daprClient;

        public ConsumerService(DaprClient daprClient)
        {
            _daprClient = daprClient;
        }

        public async Task ProcessNewWorkAsync(ProcessData process, Dictionary<string, string> metadata)
        {
            var serializedProcess = System.Text.Json.JsonSerializer.Serialize(process);
            Log.Information("New process started: {process}", serializedProcess);

            var count = System.Environment.GetEnvironmentVariable("WORK_COUNT") ?? "5";

            await _daprClient.PublishEventAsync<ProcessData>("kafka-pubsub", "processing", process, metadata);
            
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
                    Status = "Started"
                };

                var serializedWork = System.Text.Json.JsonSerializer.Serialize(work);
                Log.Information("New work created: {serializedWork}", serializedWork);
                await _daprClient.PublishEventAsync<WorkTodo>("kafka-pubsub", "newWork", work, metadata);
            }
        }
    }
}
