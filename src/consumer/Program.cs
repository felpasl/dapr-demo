using Consumer.Controllers;
using Consumer.Models;
using Consumer.Services;
using Dapr;
using Dapr.Client;
using Google.Api;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

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

builder.Logging.AddSerilog(Log.Logger);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDaprClient();
builder.Services.AddHttpContextAccessor();
builder.Services.AddControllers().AddDapr();

// Register services for DI
builder.Services.AddScoped<IConsumerService, ConsumerService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCloudEvents();

// needed for Dapr pub/sub routing
app.MapSubscribeHandler();

app.MapControllers();

app.Run();
