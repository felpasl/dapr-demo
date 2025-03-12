using Dapr;
using Dapr.Client;
using Serilog;
using Worker.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
var logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();
Log.Logger = logger;

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDaprClient();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IWorkService, WorkService>();
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
