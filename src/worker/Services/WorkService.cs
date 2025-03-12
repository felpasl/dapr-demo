using Dapr.Client;
using Serilog;
using Worker.Models;

namespace Worker.Services
{
    public class WorkService : IWorkService
    {
        private readonly DaprClient _daprClient;

        public WorkService(DaprClient daprClient)
        {
            _daprClient = daprClient;
        }

        public async Task ProcessWorkAsync(WorkTodo work, Dictionary<string, string> metadata)
        {
            var serializedWork = System.Text.Json.JsonSerializer.Serialize(work);
            Log.Information("New process started: {serializedWork}", serializedWork);

            await Task.Delay(work.Duration);

            work.Status = "Completed";

            await _daprClient.PublishEventAsync<WorkTodo>("kafka-pubsub", "workCompleted", work, metadata);
        }
    }
}
