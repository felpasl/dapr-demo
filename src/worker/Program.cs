using Dapr;
using Dapr.Client;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

using var log = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();
// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();



var app = builder.Build();

app.UseCloudEvents();

// needed for Dapr pub/sub routing
app.MapSubscribeHandler();


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/work",[Topic("kafka-pubsub", "newProcess")] async (WorkTodo work)=>
{
    var serializedWork = System.Text.Json.JsonSerializer.Serialize(work);
    log.Information("New process started: {serializedWork}", serializedWork);

    await Task.Delay(work.Duration);

    work.Status = "Completed";

    using var client = new DaprClientBuilder().Build();
    await client.PublishEventAsync<WorkTodo>("kafka-pubsub", "workCompleted", work);

    return Results.Ok(work);
});


class WorkTodo
{
    public Guid Id { get; set; }
    public Guid ProcessId { get; set; }
    public DateTime startAt { get; set; }
    public string Name { get; set; }
    public int Duration { get; set; }
    public string Status { get; set; }
}
