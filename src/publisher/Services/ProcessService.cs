using Dapr.Client;
using Publisher.Models;
using Serilog;

namespace Publisher.Services;

public class ProcessService : IProcessService
{
    private readonly DaprClient _daprClient;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private const string TRACEPARENT = "traceparent";

    public ProcessService(DaprClient daprClient, IHttpContextAccessor httpContextAccessor)
    {
        _daprClient = daprClient;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<ProcessData> StartProcessAsync()
    {
        Dictionary<string, string> metadata = new Dictionary<string, string>();
        // Get the traceparent header from the current request context
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null && httpContext.Request.Headers.TryGetValue(TRACEPARENT, out var parentValue))
        {
            metadata.Add("cloudevent.traceparent", parentValue.ToString());
            Log.Information("traceparent: {traceparent}", metadata["cloudevent.traceparent"]);
        }

        var process = new ProcessData(Guid.NewGuid(), DateTime.Now, "ProcessData");
        
        await _daprClient.PublishEventAsync<ProcessData>("kafka-pubsub", "newProcess", process, metadata);

        var serializedProcess = System.Text.Json.JsonSerializer.Serialize(process);
        Log.Information("New process started: {process}", serializedProcess);
        
        return process;
    }
}
