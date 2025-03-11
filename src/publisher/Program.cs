using Dapr.Client;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

using var log = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
string traceparent = "traceparent";

app.MapGet("/process", () =>
{
    using var client = new DaprClientBuilder().Build();
    
    Dictionary<string, string> metadata = new Dictionary<string, string>();
    // Get the traceparent header from the current request context
    var httpContext = app.Services.GetService<IHttpContextAccessor>()?.HttpContext;
    if (httpContext != null && httpContext.Request.Headers.TryGetValue(traceparent, out var parentValue))
    {
        metadata.Add("cloudevent.traceparent", parentValue.ToString());
    }
    else
    {
        // If no trace header found, initialize with empty string
        metadata.Add("cloudevent.traceparent", "");
    }


    var process = new ProcessData(Guid.NewGuid(), DateTime.Now, "ProcessData");
    client.PublishEventAsync<ProcessData>("kafka-pubsub", "newProcess", process, metadata);
    

    var serializedProcess = System.Text.Json.JsonSerializer.Serialize(process);
    log.Information("New process started: {process}", serializedProcess);
    
    return process;
})
.WithName("Start a new process");

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
