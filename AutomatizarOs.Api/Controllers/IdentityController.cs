using AutomatizarOs.Api.Models;
using AutomatizarOs.Core.Handlers;
using AutomatizarOs.Core.Requests.IdentityRequests;
using AutomatizarOs.Core.Responses;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AutomatizarOs.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class IdentityController : ControllerBase
    {
        private readonly IIdentityHandler _identityHandler;

        public IdentityController(IIdentityHandler identityHandler)
        {
            _identityHandler = identityHandler;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(new Response<string>("Erro de validação", 400, string.Join(", ", errors)));
            }

            try
            {
                var result = await _identityHandler.Login(request);

                if (string.IsNullOrEmpty(result.Data))
                    return Unauthorized(result);

                return result.IsSuccess ? Ok(result) : Unauthorized(result);
            }
            catch (ArgumentNullException ex)
            {
                Console.WriteLine($"Erro de argumento nulo: {ex}");
                return StatusCode(500, new Response<object>(
                    null,
                    500,
                    "Erro interno: dados necessários não foram fornecidos."
                ));
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"Erro de operação inválida: {ex}");
                return StatusCode(500, new Response<object>(
                    null,
                    500,
                    "Erro interno: operação não pode ser concluída."
                ));
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Erro de comunicação: {ex}");
                return StatusCode(503, new Response<object>(
                    null,
                    503,
                    "Serviço temporariamente indisponível. Tente novamente mais tarde."
                ));
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine($"Erro de autorização: {ex}");
                return Unauthorized(new Response<object>(
                    null,
                    401,
                    "Acesso negado. Você não tem permissão para acessar este recurso."
                ));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro inesperado: {ex}");
                return StatusCode(500, new Response<object>(
                    null,
                    500,
                    "Ocorreu um erro inesperado. Por favor, contate o suporte técnico."
                ));
            }
        }
    }
}
