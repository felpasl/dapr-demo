using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Dapr;
using Serilog;
using Dapr.Client;
using Google.Api;

var builder = WebApplication.CreateBuilder(args);
using var log = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();
// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCloudEvents();

// needed for Dapr pub/sub routing
app.MapSubscribeHandler();

string traceparent = "traceparent";

app.MapPost("/processing",[Topic("kafka-pubsub", "newProcess")] async (ProcessData process) => {
    
    var serializedProcess = System.Text.Json.JsonSerializer.Serialize(process);
    log.Information("New process started: {process}", serializedProcess);

    var count = System.Environment.GetEnvironmentVariable("WORK_COUNT") ?? "5";
    
    using var client = new DaprClientBuilder().Build();
    
    Dictionary<string, string> metadata = new Dictionary<string, string>();

    // Get the traceparent header from the current request context
    var httpContext = app.Services.GetService<IHttpContextAccessor>()?.HttpContext;
    if (httpContext != null && httpContext.Request.Headers.TryGetValue(traceparent, out var parentValue))
    {
        metadata.Add("cloudevent.traceparent", parentValue.ToString());
        log.Information("traceparent: {traceparent}", metadata["cloudevent.traceparent"]);
    }
    else
    {
    }

    await client.PublishEventAsync<ProcessData>("kafka-pubsub", "processing", process, metadata);
    
    List<WorkTodo> workList = new List<WorkTodo>();
    for(int i = 0; i < int.Parse(count); i++)
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
        log.Information("New process started: {serializedWork}", serializedWork);
        workList.Add(work);
    }
    await client.BulkPublishEventAsync<WorkTodo>("kafka-pubsub", "newWork", workList, metadata);

    return  Results.Ok();
});

app.Run();


class ProcessData
{
    public ProcessData(Guid id, DateTime startAt, string name)
    {
        Id = id;
        StartAt = startAt;
        Name = name;
        EndAt = null;
        Status = "Started";
    }
    public Guid Id { get; set; }
    public DateTime StartAt { get; set; }
    public string Name { get; set; }
    public DateTime? EndAt { get; set; }
    public string Status { get; set; }
}

class WorkTodo
{
    public Guid Id { get; set; }
    public Guid ProcessId { get; set; }
    public DateTime startAt { get; set; }
    public string Name { get; set; }
    public int Duration { get; set; }
    public string Status { get; set; }
}