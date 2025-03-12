using Consumer.Models;
using Dapr.Client;
using Serilog;

namespace Consumer.Services
{
    public class ProcessService : IProcessService
    {
        private readonly DaprClient _daprClient;

        public ProcessService(DaprClient daprClient)
        {
            _daprClient = daprClient;
        }

        public async Task ProcessNewWorkAsync(ProcessData process, Dictionary<string, string> metadata)
        {
            var serializedProcess = System.Text.Json.JsonSerializer.Serialize(process);
            Log.Information("New process started: {process}", serializedProcess);

            var count = System.Environment.GetEnvironmentVariable("WORK_COUNT") ?? "5";

            await _daprClient.PublishEventAsync<ProcessData>("kafka-pubsub", "processing", process, metadata);

            List<WorkTodo> workList = new List<WorkTodo>();
            for (int i = 0; i < int.Parse(count); i++)
            {
                var work = new WorkTodo
                {
                    Id = Guid.NewGuid(),
                    ProcessId = process.Id,
                    startAt = DateTime.Now,
                    Name = $"Work {i}",
                    Duration = new Random().Next(20, 100),
                    Status = "Started"
                };

                var serializedWork = System.Text.Json.JsonSerializer.Serialize(work);
                Log.Information("New work created: {serializedWork}", serializedWork);
                workList.Add(work);
            }
            
            await _daprClient.BulkPublishEventAsync<WorkTodo>("kafka-pubsub", "newWork", workList, metadata);
        }
    }
}
