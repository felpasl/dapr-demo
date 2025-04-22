using Dapr.Client;
using Serilog;
using Publisher.Services;
using Publisher.Models;

var builder = WebApplication.CreateBuilder(args);

// Setup Serilog
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}]<s:{SourceContext}> {Message:lj} {NewLine}{Exception}",
        restrictedToMinimumLevel: LogEventLevel.Information
    )
    .WriteTo.Logger(bl =>
        bl.Filter.ByIncludingOnly(le => le.Properties.ContainsKey("BusinessEvent"))
            .WriteTo.File(
                "logs/business-events.log",
                rollingInterval: RollingInterval.Day,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}]<s:{SourceContext}> {Message:lj} {Properties:j} {NewLine}{Exception}"
            )
    )
    .CreateLogger();

builder.Logging.ClearProviders().AddSerilog(Log.Logger);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Setup Serilog
var logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();
Log.Logger = logger;

// Add services for DI
builder.Services.AddDaprClient();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IProcessService, ProcessService>();
builder.Services.AddControllers(); // Add controllers

var app = builder.Build();

app.UseCloudEvents();

// needed for Dapr pub/sub routing
app.MapSubscribeHandler();

// Configure the HTTP request pipeline.

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


// Use controllers instead of individual endpoint mapping
app.MapControllers();

app.Run();
