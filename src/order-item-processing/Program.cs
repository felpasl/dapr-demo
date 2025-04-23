using Dapr;
using Dapr.Client;
using Dapr.Common.Logging;
using OrderItemProcessing.Services;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

// Register DaprClient for dependency injection
builder.Services.AddDaprClient();

// Configure Serilog for initial setup
ConfigureSerilog(builder, initialSetup: true);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IOrderItemService, OrderItemService>();
builder.Services.AddControllers().AddDapr();

var app = builder.Build();

// Update Serilog configuration with DI-based Dapr sink
ConfigureSerilog(builder, initialSetup: false, app: app);

app.UseCloudEvents();

// needed for Dapr pub/sub routing
app.MapSubscribeHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Use controllers instead of individual endpoint mapping
app.MapControllers();

app.Run();

// Local method to configure Serilog with or without DI
void ConfigureSerilog(WebApplicationBuilder builder, bool initialSetup, WebApplication? app = null)
{
    var loggerConfig = new LoggerConfiguration()
        .Enrich.FromLogContext()
        .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
        .WriteTo.Console(
            outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}]<s:{SourceContext}> {Message:lj} {NewLine}{Exception}",
            restrictedToMinimumLevel: LogEventLevel.Information
        )
        .WriteTo.Logger(bl =>
        {
            var subLogger = bl
                .Filter.ByIncludingOnly(le => le.Properties.ContainsKey("BusinessEvent"))
                .WriteTo.File(
                    "logs/business-events.log",
                    rollingInterval: RollingInterval.Day,
                    outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}]<s:{SourceContext}> {Message:lj} {Properties:j} {NewLine}{Exception}"
                );

            // Add DaprPubSub sink only for the final setup when app is available
            if (!initialSetup && app != null)
            {
                subLogger.WriteTo.DaprPubSub(app.Services, "kafka-pubsub", "logs");
            }
        });

    Log.Logger = loggerConfig.CreateLogger();

    if (initialSetup)
    {
        // Initial setup
        builder.Logging.ClearProviders().AddSerilog(Log.Logger);
        builder.Host.UseSerilog();
    }
    else if (app != null)
    {
        // After app is built, ensure proper cleanup
        app.Lifetime.ApplicationStopped.Register(Log.CloseAndFlush);
    }
}
