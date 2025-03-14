using Dapr.Client;
using Worker.Models;

namespace Worker.Services
{
    public class WorkService : IWorkService
    {
        private readonly DaprClient _daprClient;
        private readonly ILogger<WorkService> logger;

        public WorkService(DaprClient daprClient, ILogger<WorkService> logger)
        {
            _daprClient = daprClient;
            this.logger = logger;
        }

        public async Task ProcessWorkAsync(WorkTodo work, Dictionary<string, string> metadata)
        {
            logger.LogInformation("New process started: {@work}", work);

            await Task.Delay(work.Duration);

            work.Status = "Completed";

            await _daprClient.PublishEventAsync<WorkTodo>(
                "kafka-pubsub",
                "workCompleted",
                work,
                metadata
            );

            if (work.Index == work.Total - 1)
            {
                logger.LogInformation("Process completed: {ProcessId}", work.ProcessId);
                await _daprClient.PublishEventAsync<ProcessFinished>(
                    "kafka-pubsub",
                    "processCompleted",
                    new ProcessFinished(work.ProcessId, DateTime.Now, "Success"),
                    metadata
                );
            }
        }
    }
}
