using AutomatizarOs.Core.DTO;
using AutomatizarOs.Core.Requests.IdentityRequests;
using Microsoft.AspNetCore.Components;
using Newtonsoft.Json;

namespace AutomatizarOs.Web.Services;

public class AuthService
{
    private readonly AccessTokenService _accessTokenService;
    private readonly NavigationManager _nav;
    private HttpClient _httpClient;
    
    public AuthService(
        AccessTokenService accessTokenService,
        NavigationManager nav, 
        IHttpClientFactory httpClientFactory)
    {
        _accessTokenService = accessTokenService;
        _nav = nav;
        _httpClient = httpClientFactory.CreateClient("ApiClient");
    }

    public async Task<bool> Login(LoginRequest request)
    {
        var status = await _httpClient.PostAsJsonAsync("login", request);
        if (status.IsSuccessStatusCode)
        {
            var token = await status.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<AuthResponse>(token);
            if (result == null)
                return false;
            
            await _accessTokenService.SetToken(result.AccessToken);
            return true;
        }
        
        return false;
    }
}