using AutomatizarOs.Core.Requests.IdentityRequests;
using AutomatizarOs.Web.Security;
using AutomatizarOs.Web.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace AutomatizarOs.Web.Components.Pages.Auth;

public partial class Login : ComponentBase
{
    public LoginRequest Request { get; set; } = new();
    private bool PageIsBusy { get; set; }
    private bool LoginIsBusy { get; set; }
    
    
    [Inject] private AuthService AuthService { get; set; } = null!;
    [Inject] public JwtAuthenticationStateProvider AuthStateProvider { get; set; } = null!;
    [Inject] public NavigationManager NavigationManager { get; set; } = null!;
    [Inject] public ISnackbar Snackbar { get; set; } = null!;
    
    
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
            Snackbar.Add("Erro ao carregar a página", Severity.Error);
        }
        finally
        {
            PageIsBusy = false;
        }
    }

    private async Task LoginFunc()
    {
        LoginIsBusy = true;
        try
        {
            var result = await AuthService.Login(Request);
            if (result.IsSuccess)
            {
                Snackbar.Add("Login realizado com sucesso!", Severity.Success);
                NavigationManager.NavigateTo("/");
            }
            else
            {
                Snackbar.Add(result.Message ?? "Erro ao fazer o login", Severity.Error);
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