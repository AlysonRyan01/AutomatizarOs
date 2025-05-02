using System.Text.Json;
using AutomatizarOs.Core.Responses;

namespace AutomatizarOs.Web.Exceptions;

public static class HttpExceptionHandler
{
    public static Response<T> Handle<T>(Exception ex)
    {
        return ex switch
        {
            HttpRequestException httpEx => new Response<T>(default, 503, $"Erro de conexão com o servidor: {httpEx.Message}"),
            NotSupportedException nsEx => new Response<T>(default, 500, $"Formato de resposta não suportado: {nsEx.Message}"),
            JsonException jsonEx => new Response<T>(default, 500, $"Erro ao interpretar a resposta do servidor: {jsonEx.Message}"),
            TaskCanceledException tcEx => new Response<T>(default, 408, $"Tempo de resposta esgotado: {tcEx.Message}"),
            _ => new Response<T>(default, 500, $"Erro inesperado: {ex.Message}")
        };
    }
}