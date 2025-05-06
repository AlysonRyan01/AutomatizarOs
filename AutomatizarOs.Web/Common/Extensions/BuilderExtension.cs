using AutomatizarOs.Core;
using AutomatizarOs.Core.Handlers;
using AutomatizarOs.Web.Handlers;
using AutomatizarOs.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;

namespace AutomatizarOs.Web.Common.Extensions;

public static class BuilderExtension
{
    public static void AddServices(this WebAssemblyHostBuilder builder)
    {
        builder.Services.AddScoped<CustomAuthStateProvider>();
        builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<CustomAuthStateProvider>());
        builder.Services.AddScoped<IServiceOrderHandler, ServiceOrderHandler>();
        builder.Services.AddSingleton<HubConnection>(sp =>
        {
            return new HubConnectionBuilder()
                .WithUrl(new Uri($"{Configuration.BackendUrl}/osHub"), options =>
                {
                    options.SkipNegotiation = true;
                    options.Transports = HttpTransportType.WebSockets;
                })
                .WithAutomaticReconnect()
                .Build();
        });
    }
    
    public static void AddHttpClient(this WebAssemblyHostBuilder builder)
    {
        builder.Services.AddHttpClient("api", client =>
        {
            client.BaseAddress = new Uri(Configuration.BackendUrl);
            client.Timeout = TimeSpan.FromSeconds(60);
        });
    }
}