using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Dapr;
using Serilog;
using Dapr.Client;
using Google.Api;
using Consumer.Services;
using Consumer.Models;
using Consumer.Controllers;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);
using var log = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();
Log.Logger = log;

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDaprClient();
builder.Services.AddHttpContextAccessor();
builder.Services.AddControllers().AddDapr();

// Register services for DI
builder.Services.AddScoped<IProcessService, ProcessService>();

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