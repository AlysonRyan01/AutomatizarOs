using System.Net.Http.Json;
using AutomatizarOs.Core.Handlers;
using AutomatizarOs.Core.Models;
using AutomatizarOs.Core.Requests.ServiceOrderRequests;
using AutomatizarOs.Core.Responses;
using AutomatizarOs.Web.Exceptions;

namespace AutomatizarOs.Web.Handlers;

public class ServiceOrderHandler : IServiceOrderHandler
{
    private readonly HttpClient _httpClient;
    
    public ServiceOrderHandler(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("api");
    }
    
    public Task<Response<bool?>> GetLocalServiceOrder()
    {
        throw new NotImplementedException();
    }

    public async Task<Response<List<ServiceOrder>>> GetAllServiceOrder()
    {
        try
        {
            var result = await _httpClient.GetFromJsonAsync<Response<List<ServiceOrder>>>("v1/ServiceOrder/get-all-orders");

            if (result == null)
                return new Response<List<ServiceOrder>>(null, 500, "Erro ao obter as ordens de servico");
            
            if (result.Data == null || !result.Data.Any())
                return new Response<List<ServiceOrder>>(null, 500, "Erro ao obter as ordens de servico");
            
            if (!result.IsSuccess)
                return new Response<List<ServiceOrder>>(null, 500, "Erro no servidor");
            
            return new Response<List<ServiceOrder>>(result.Data, 200, "Sucesso ao obter as ordens de servico");
        }
        catch (Exception ex)
        {
            return HttpExceptionHandler.Handle<List<ServiceOrder>>(ex);
        }
    }

    public async Task<Response<ServiceOrder>> AddQuoteById(AddQuoteRequest request)
    {
        try
        {
            var result = await _httpClient.PutAsJsonAsync("v1/ServiceOrder/add-quote", request);
            
            if (!result.IsSuccessStatusCode)
                return new Response<ServiceOrder>(null, 500, "Erro ao adicionar o orçamento de servico");

            var content = await result.Content.ReadFromJsonAsync<Response<ServiceOrder>>();

            return content ?? new Response<ServiceOrder>(null, 500, "Erro ao processar a resposta");
        }
        catch (Exception ex)
        {
            return HttpExceptionHandler.Handle<ServiceOrder>(ex);
        }
    }

    public async Task<Response<ServiceOrder>> AddRepairById(long id)
    {
        try
        {
            var result = await _httpClient.PutAsJsonAsync("v1/ServiceOrder/add-repair", id);
            
            if (!result.IsSuccessStatusCode)
                return new Response<ServiceOrder>(null, 500, "Erro ao adicionar o conserto na ordem de servico");

            var content = await result.Content.ReadFromJsonAsync<Response<ServiceOrder>>();

            return content ?? new Response<ServiceOrder>(null, 500, "Erro ao processar a resposta");
        }
        catch (Exception ex)
        {
            return HttpExceptionHandler.Handle<ServiceOrder>(ex);
        }
    }

    public async Task<Response<ServiceOrder>> AddDeliveryById(long id)
    {
        try
        {
            var result = await _httpClient.PutAsJsonAsync("v1/ServiceOrder/add-delivery", id);
            
            if (!result.IsSuccessStatusCode)
                return new Response<ServiceOrder>(null, 500, "Erro ao adicionar a entrega na ordem de servico");

            var content = await result.Content.ReadFromJsonAsync<Response<ServiceOrder>>();

            return content ?? new Response<ServiceOrder>(null, 500, "Erro ao processar a resposta");
        }
        catch (Exception ex)
        {
            return HttpExceptionHandler.Handle<ServiceOrder>(ex);
        }
    }

    public async Task<Response<ServiceOrder>> AddStatusById(AddStatusByIdRequest request)
    {
        try
        {
            var result = await _httpClient.PutAsJsonAsync("v1/ServiceOrder/add-status", request);
            
            if (!result.IsSuccessStatusCode)
                return new Response<ServiceOrder>(null, 500, "Erro ao adicionar um status na ordem de servico");

            var content = await result.Content.ReadFromJsonAsync<Response<ServiceOrder>>();

            return content ?? new Response<ServiceOrder>(null, 500, "Erro ao processar a resposta");
        }
        catch (Exception ex)
        {
            return HttpExceptionHandler.Handle<ServiceOrder>(ex);
        }
    }
    
    public async Task<Response<bool>> AddLocation(AddLocationRequest request)
    {
        try
        {
            var result = await _httpClient.PutAsJsonAsync("v1/ServiceOrder/add-location", request);
            
            if (!result.IsSuccessStatusCode)
                return new Response<bool>(false, 500, "Erro ao adicionar uma prateleira na ordem de servico");

            var content = await result.Content.ReadFromJsonAsync<Response<bool>>();

            return content ?? new Response<bool>(false, 500, "Erro ao processar a resposta");
        }
        catch (Exception ex)
        {
            return HttpExceptionHandler.Handle<bool>(ex);
        }
    }
}