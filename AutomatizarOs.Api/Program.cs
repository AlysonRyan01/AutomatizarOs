using AutomatizarOs.Api;
using AutomatizarOs.Api.Common;
using AutomatizarOs.Api.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors();
builder.AddConfigurationApiUrl();
builder.Services.AddAuthorization();
builder.AddServices();
builder.AddCorsConfiguration();
builder.AddJwtConfiguration();
builder.AddSwaggerGen();
builder.AddIdentity();
builder.AddDbConfiguration();
builder.AddControllers();

var app = builder.Build();

app.UseCors(ApiConfiguration.CorsPolicyName);
app.UseAuthentication();
app.UseAuthorization();
app.MapHub<OrdemDeServicoHub>("/osHub");

if (app.Environment.IsDevelopment())
    app.ConfigureDevEnvironment();

app.MapControllers();
app.MapGet("/", () => "Api rodando!");
app.Run();

