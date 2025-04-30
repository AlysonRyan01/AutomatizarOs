using AutomatizarOs.Core.DTO;
using AutomatizarOs.Core.Requests.IdentityRequests;
using AutomatizarOs.Core.Responses;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Newtonsoft.Json;

namespace AutomatizarOs.Web.Services;

public class AuthService
{
    private readonly AccessTokenService _accessTokenService;
    private HttpClient _httpClient;
    
    public AuthService(
        AccessTokenService accessTokenService,
        IHttpClientFactory httpClientFactory)
    {
        _accessTokenService = accessTokenService;
        _httpClient = httpClientFactory.CreateClient("ApiClient");
    }

    public async Task<Response<string>> Login(LoginRequest request)
    {
        var status = await _httpClient.PostAsJsonAsync("login", request);
        if (status.IsSuccessStatusCode)
        {
            if (!status.IsSuccessStatusCode)
            {
                var errorResponse = await status.Content.ReadFromJsonAsync<Response<string>>();
                var errorMessage = errorResponse?.Message ?? "Falha no login";
                return new Response<string>(errorMessage, (int)status.StatusCode, errorMessage);
            }
            
            var baseResponse = await status.Content.ReadFromJsonAsync<Response<string>>();
            
            if (baseResponse == null)
                return new Response<string>("Token inválido", 500, "Resposta de login inválida");

            var jwtToken = baseResponse.Data ?? "";
            
            await _accessTokenService.SetToken(jwtToken);
            
            return new Response<string>("Login realizado com sucesso", 200, "Login realizado com sucesso");
        }
        
        return new Response<string>("Erro ao realizar o login", 500, "Login realizado com sucesso");

    }
}