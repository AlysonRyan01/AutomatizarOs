using AutomatizarOs.Web.Components;
using AutomatizarOs.Web.Security;
using AutomatizarOs.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices();
builder.Services.AddScoped<CookieService>();
builder.Services.AddScoped<AccessTokenService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddHttpClient("ApiClient", opt =>
{
    opt.BaseAddress = new Uri("http://localhost:5020/api/Identity/");
});

builder.Services.AddAuthentication()
    .AddScheme<CustomOption, JwtAuthenticationHandler>(
        "JWTAuth", options =>{ });
builder.Services.AddAuthorization();
builder.Services.AddScoped<JwtAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider, JwtAuthenticationStateProvider>();

builder.Services.AddCascadingAuthenticationState();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();


app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
