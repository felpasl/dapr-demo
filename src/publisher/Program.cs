using Dapr.Client;
using Serilog;
using Publisher.Services;
using Publisher.Models;

var builder = WebApplication.CreateBuilder(args);

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
