using AutomatizarOs.Api.Data;
using AutomatizarOs.Api.Models;
using AutomatizarOs.Api.Services;
using AutomatizarOs.Core.Handlers;
using AutomatizarOs.Core.Requests.IdentityRequests;
using AutomatizarOs.Core.Responses;
using Microsoft.AspNetCore.Identity;

namespace AutomatizarOs.Api.Handlers
{
    public class IdentityHandler(
    UserManager<User> userManager,
    AutomatizarDbContext context,
    JwtService tokenService) : IIdentityHandler
    {
        public async Task<Response<string>> Login(LoginRequest request)
        {
            try
            {
                var user = await userManager.FindByEmailAsync(request.Email);
                if (user == null)
                    return new Response<string>("Senha ou email incorretos", 401, "Senha ou email incorretos");

                var isPasswordValid = await userManager.CheckPasswordAsync(user, request.Password);
                if (!isPasswordValid)
                    return new Response<string>("Senha ou email incorretos", 401, "Senha ou email incorretos");

                var token = tokenService.Create(user);

                return new Response<string>(token, 200, "Login realizado com sucesso");
            }
            catch (Exception e)
            {
                return new Response<string>(e.Message, 500, "Erro no login");
            }
        }
    }
}