using Dapr.Client;
using Publisher.Models;

namespace Publisher.Services;

public class ProcessService : IProcessService
{
    private readonly ILogger<ProcessService> logger;
    private readonly DaprClient daprClient;

    public ProcessService(DaprClient daprClient, ILogger<ProcessService> logger)
    {
        this.daprClient = daprClient;
        this.logger = logger;
    }

    public async Task<ProcessData> StartProcessAsync(Dictionary<string, string> metadata)
    {
        var process = new ProcessData(Guid.NewGuid(), DateTime.Now, "ProcessData");

        await daprClient.PublishEventAsync<ProcessData>(
            "kafka-pubsub",
            "newProcess",
            process,
            metadata
        );

        this.logger.LogInformation("New process started: {@process}", process);

        return process;
    }
}
