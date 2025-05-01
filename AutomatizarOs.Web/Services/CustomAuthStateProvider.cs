using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using AutomatizarOs.Core.Requests.IdentityRequests;
using AutomatizarOs.Core.Responses;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;

namespace AutomatizarOs.Web.Services;

public class CustomAuthStateProvider : AuthenticationStateProvider
{
    private readonly HttpClient _httpClient;
    private readonly ISyncLocalStorageService _localStorage;

    public CustomAuthStateProvider(HttpClient httpClient, IHttpClientFactory httpClientFactory, ISyncLocalStorageService localStorageService)
    {
        _httpClient = httpClientFactory.CreateClient("api");
        _localStorage = localStorageService;
        
        var token = _localStorage.GetItem<string>("jwtToken");
        if (!string.IsNullOrEmpty(token))
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }
    
    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = _localStorage.GetItem<string>("jwtToken");

        var user = new ClaimsPrincipal(new ClaimsIdentity());

        if (!string.IsNullOrEmpty(token))
        {
            user = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>(), "Bearer"));
        }

        return new AuthenticationState(user);
    }

    public async Task<Response<string>> Login(LoginRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("Identity/login", request);

            if (!response.IsSuccessStatusCode)
            {
                var errorResponse = await response.Content.ReadFromJsonAsync<Response<string>>();
                var errorMessage = errorResponse?.Message ?? "Falha no login";
                return new Response<string>(errorMessage, (int)response.StatusCode, errorMessage);
            }
            
            var baseResponse = await response.Content.ReadFromJsonAsync<Response<string>>();
            
            if (baseResponse == null)
                return new Response<string>("Token inválido", 500, "Resposta de login inválida");

            var jwtToken = baseResponse.Data ?? "";
            
            _localStorage.SetItem("jwtToken", jwtToken);
            
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken);
            
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());

            return new Response<string>("Login realizado", 200, "Autenticação bem-sucedida");
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
        {
            return new Response<string>("Credenciais inválidas", 401, "Usuário ou senha incorretos");
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.TooManyRequests)
        {
            return new Response<string>("Muitas tentativas", 429, "Muitas tentativas de login. Tente novamente mais tarde");
        }
        catch (HttpRequestException ex)
        {
            return new Response<string>("Serviço indisponível", 503, $"Serviço de autenticação offline: {ex.Message}");
        }
        catch (JsonException)
        {
            return new Response<string>("Formato inválido", 500, "Resposta de login em formato inesperado");
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            return new Response<string>("Tempo esgotado", 504, "Serviço de autenticação não respondeu a tempo");
        }
        catch (Exception ex)
        {
            return new Response<string>("Falha no login", 500, $"Erro durante o login: {ex.Message}");
        }
    }
}
