var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var app = builder.Build();

app.UseHttpsRedirection();

app.MapGet("/", () => "Api rodando!");

app.Run();

