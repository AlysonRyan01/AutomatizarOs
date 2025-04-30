namespace AutomatizarOs.Web.Services;

public class AccessTokenService
{
    private readonly CookieService _cookieService;
    private readonly string _tokenKey = "access_token";

    public AccessTokenService(CookieService cookieService)
    {
        _cookieService = cookieService;
    }

    public async Task SetToken(string token)
    {
        await _cookieService.SetCookie(_tokenKey, token, 1);
    }
    
    public async Task RemoveToken()
    {
        await _cookieService.RemoveCookie(_tokenKey);
    }
    
    public async Task<string> GetToken()
    {
        return await _cookieService.GetCookie(_tokenKey);
    }
}