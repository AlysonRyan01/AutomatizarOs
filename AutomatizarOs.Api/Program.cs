using AutomatizarOs.Api;
using AutomatizarOs.Api.Common;
using AutomatizarOs.Api.Services;
using AutomatizarOs.Core.Handlers;
using AutomatizarOs.Core.Models;
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
    q.UseSimpleTypeLoader(); // Opcional, mas recomendado
    q.UseInMemoryStore();    // Pode ser substituído por um banco de dados, se necessário

    // Registrar o Job e o Trigger
    q.ScheduleJob<ServiceOrderJob>(trigger => trigger
        .WithIdentity("ServiceOrderTrigger")
        .StartNow()
        .WithSimpleSchedule(x => x
            .WithIntervalInSeconds(30)
            .RepeatForever())
    );
});

// Adicionar o Quartz Hosted Service
builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

// Registrar o Job
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

