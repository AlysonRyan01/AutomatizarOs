using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AutomatizarOs.Api.Models;
using Microsoft.IdentityModel.Tokens;

namespace AutomatizarOs.Api.Services
{
    public class JwtService
    {
        public string Create(User user)
        {
            var handler = new JwtSecurityTokenHandler();

            var key = Encoding.ASCII.GetBytes(ApiConfiguration.Key);
            var credentials = new SigningCredentials
            (
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256
            );

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                SigningCredentials = credentials,
                Expires = DateTime.UtcNow.AddHours(2),
                Subject = GenerateClaims(user)
            };

            var token = handler.CreateToken(tokenDescriptor);
            return handler.WriteToken(token);
        }

        private static ClaimsIdentity GenerateClaims(User user)
        {
            var claimsIdentity = new ClaimsIdentity();

            claimsIdentity.AddClaim(new Claim("id", user.Id.ToString()));
            claimsIdentity.AddClaim(new Claim(ClaimTypes.Name, user.Email ?? ""));
            claimsIdentity.AddClaim(new Claim(ClaimTypes.Email, user.Email ?? " "));

            return claimsIdentity;
        }
    }
}