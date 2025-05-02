using AutomatizarOs.Core.Handlers;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using AutomatizarOs.Web;
using AutomatizarOs.Web.Handlers;
using AutomatizarOs.Web.Services;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop.Infrastructure;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddHttpClient("api", client =>
{
    client.BaseAddress = new Uri("http://localhost:5020/");
    client.Timeout = TimeSpan.FromSeconds(60);
});

builder.Services.AddMudServices();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<CustomAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<CustomAuthStateProvider>());
builder.Services.AddScoped<IServiceOrderHandler, ServiceOrderHandler>();
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddSingleton<HubConnection>(sp =>
{
    return new HubConnectionBuilder()
        .WithUrl(new Uri("http://localhost:5020/osHub"), options =>
        {
            options.SkipNegotiation = true;
            options.Transports = HttpTransportType.WebSockets;
        })
        .WithAutomaticReconnect()
        .Build();
});

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

await builder.Build().RunAsync();
