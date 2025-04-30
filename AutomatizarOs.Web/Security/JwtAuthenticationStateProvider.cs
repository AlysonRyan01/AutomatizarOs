using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AutomatizarOs.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;

namespace AutomatizarOs.Web.Security;

public class JwtAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly AccessTokenService _accessTokenService;
    
    public JwtAuthenticationStateProvider(AccessTokenService accessTokenService)
    {
        _accessTokenService = accessTokenService;
    }
    
    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var token = await _accessTokenService.GetToken();
            if (string.IsNullOrWhiteSpace(token))
                return await MarkAsAnauthorize();

            var readJwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
            var identity = new ClaimsIdentity(readJwt.Claims, "JWT");
            var principal = new ClaimsPrincipal(identity);

            return await Task.FromResult(new AuthenticationState(principal));
        }
        catch
        {
            return await MarkAsAnauthorize();
        }
    }

    private async Task<AuthenticationState> MarkAsAnauthorize()
    {
        try
        {
            var state = new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            NotifyAuthenticationStateChanged(Task.FromResult(state));
            return state;
        }
        catch
        {
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }
    }
}