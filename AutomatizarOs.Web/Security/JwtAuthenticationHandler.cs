using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace AutomatizarOs.Web.Security;

public class JwtAuthenticationHandler : AuthenticationHandler<CustomOption>
{
    public JwtAuthenticationHandler(
        IOptionsMonitor<CustomOption> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock)
        : base(options, logger, encoder, clock)
    {
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        try
        {
            var token = Request.Cookies["access_token"];
            if (string.IsNullOrEmpty(token))
                return AuthenticateResult.NoResult();

            var readJwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
            var identity = new ClaimsIdentity(readJwt.Claims, "JWT");
            var principal = new ClaimsPrincipal(identity);

            var ticket = new AuthenticationTicket(principal, Scheme.Name);
            
            return AuthenticateResult.Success(ticket);
        }
        catch
        {
            return AuthenticateResult.NoResult();
        }
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.Redirect("/login");
        return Task.CompletedTask;
    }

    protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        Response.Redirect("/access-denied");
        return Task.CompletedTask;
    }
}

public class CustomOption : AuthenticationSchemeOptions
{
    
}