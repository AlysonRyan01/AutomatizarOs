using AutomatizarOs.Api;
using AutomatizarOs.Api.Common;
using AutomatizarOs.Core.Handlers;
using AutomatizarOs.Core.Models;

var builder = WebApplication.CreateBuilder(args);

builder.AddConfigurationApiUrl();
builder.Services.AddAuthorization();
builder.Services.AddSingleton<IServiceOrderHandler, ServiceOrderHandler>();
builder.AddCorsConfiguration();
builder.AddJwtConfiguration();
builder.AddSwaggerGen();
builder.AddIdentity();
builder.AddDbConfiguration();
builder.AddControllers();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.UseCors(ApiConfiguration.CorsPolicyName);

if (app.Environment.IsDevelopment())
    app.ConfigureDevEnvironment();

app.MapControllers();


app.MapGet("/", () => "Api rodando!");


app.Run();

