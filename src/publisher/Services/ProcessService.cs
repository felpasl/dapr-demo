using Dapr.Client;
using Publisher.Models;
using Serilog;

namespace Publisher.Services;

public class ProcessService : IProcessService
{
    private readonly DaprClient _daprClient;

    public ProcessService(DaprClient daprClient)
    {
        _daprClient = daprClient;
    }

    public async Task<ProcessData> StartProcessAsync(Dictionary<string, string> metadata)
    {
        var process = new ProcessData(Guid.NewGuid(), DateTime.Now, "ProcessData");
        
        await _daprClient.PublishEventAsync<ProcessData>("kafka-pubsub", "newProcess", process, metadata);

        var serializedProcess = System.Text.Json.JsonSerializer.Serialize(process);
        Log.Information("New process started: {process}", serializedProcess);
        
        return process;
    }
}
