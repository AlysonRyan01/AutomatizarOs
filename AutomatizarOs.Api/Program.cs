using AutomatizarOs.Api;
using AutomatizarOs.Api.Common;
using AutomatizarOs.Api.Handlers;
using AutomatizarOs.Api.Services;
using AutomatizarOs.Core.Handlers;
using Quartz;

var builder = WebApplication.CreateBuilder(args);

builder.AddConfigurationApiUrl();
builder.Services.AddAuthorization();
builder.Services.AddScoped<IServiceOrderHandler, ServiceOrderHandler>();
builder.AddCorsConfiguration();
builder.AddJwtConfiguration();
builder.AddSwaggerGen();
builder.AddIdentity();
builder.AddDbConfiguration();
builder.AddControllers();

builder.Services.AddQuartz(q =>
{
    q.UseSimpleTypeLoader();
    q.UseInMemoryStore();
    
    var jobKey = new JobKey("ServiceOrderSyncJob");
    q.AddJob<ServiceOrderJob>(jobKey, j => j
        .StoreDurably()
        .WithDescription("Job de sincronização de ordens de serviço")
    );
    
    q.AddTrigger(t => t
        .WithIdentity("ServiceOrderTrigger")
        .ForJob(jobKey)
        .StartNow()
        .WithSimpleSchedule(x => x
            .WithIntervalInSeconds(30)
            .RepeatForever())
    );
});

builder.Services.AddQuartzHostedService(q => 
{
    q.WaitForJobsToComplete = true;
    q.StartDelay = TimeSpan.FromSeconds(5);
});

builder.Services.AddScoped<ServiceOrderJob>();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.UseCors(ApiConfiguration.CorsPolicyName);

if (app.Environment.IsDevelopment())
    app.ConfigureDevEnvironment();

app.MapControllers();


app.MapGet("/", () => "Api rodando!");


app.Run();

