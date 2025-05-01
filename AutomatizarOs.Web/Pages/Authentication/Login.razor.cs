using AutomatizarOs.Core.Handlers;
using AutomatizarOs.Core.Requests.IdentityRequests;
using AutomatizarOs.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;

namespace AutomatizarOs.Web.Pages.Authentication;

public partial class Login : ComponentBase
{
    private bool PageIsBusy { get; set; }
    private bool LoginIsBusy { get; set; }
    private LoginRequest Request { get; set; } = new();
    
    private string? ProductId { get; set; }
    private string? Quantity { get; set; }
    private bool Redirect { get; set; }
    
    [Inject] public ISnackbar Snackbar { get; set; } = null!;
    [Inject] public NavigationManager NavigationManager { get; set; } = null!;
    [Inject] public CustomAuthStateProvider AuthStateProvider { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        PageIsBusy = true;
        try
        {
            var authState = await AuthStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;

            if (user.Identity?.IsAuthenticated == true)
            {
                NavigationManager.NavigateTo("/");
            }
        }
        catch
        {
            Snackbar.Add($"Erro ao carregar a página", Severity.Error);
        }
        finally
        {
            PageIsBusy = false;
        }
    }

    private async Task LoginAsync()
    {
        LoginIsBusy = true;
        try
        {
            var result = await AuthStateProvider.Login(Request);
            
            if (result.IsSuccess)
            {
                Snackbar.Add(result.Message ?? "Login Realizado com sucesso!", Severity.Success);
                NavigationManager.NavigateTo("/");
            }
            else
            {
                Snackbar.Add(result.Message ?? "Falha ao realizar o login", Severity.Error);
            }
        }
        catch (Exception e)
        {
            Snackbar.Add(e.Message, Severity.Error);
        }
        finally
        {
            LoginIsBusy = false;
        }
    }
}