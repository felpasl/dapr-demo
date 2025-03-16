using Dapr;
using Dapr.Client;
using Serilog;
using Serilog.Events;
using OrderItemProcessing.Services;

var builder = WebApplication.CreateBuilder(args);

// Setup Serilog
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}]<s:{SourceContext}> {Message:lj} {NewLine}{Exception}",
        restrictedToMinimumLevel: LogEventLevel.Information
    )
    .CreateLogger();

builder.Logging.ClearProviders().AddSerilog(Log.Logger);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDaprClient();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IOrderItemService, OrderItemService>();
builder.Services.AddControllers().AddDapr();

var app = builder.Build();

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
