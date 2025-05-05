using AutomatizarOs.Core.Handlers;
using AutomatizarOs.Core.Requests.ServiceOrderRequests;
using AutomatizarOs.Core.Responses;
using Microsoft.AspNetCore.Mvc;

namespace AutomatizarOs.Api.Controllers;

[Route("v1/ServiceOrder")]
public class ServiceOrderController(IServiceOrderHandler handler) : ControllerBase
{
    [HttpGet("get-local-orders")]
    public async Task<IActionResult> GetOrders()
    {
        try
        {
            var orders = await handler.GetLocalServiceOrder();
            return orders.IsSuccess ? Ok(orders) : BadRequest(orders);
        }
        catch (Exception e)
        {
            var errorResponse = new Response<string?>(
                null, 
                500, 
                "Ocorreu um erro interno ao processar a solicitação de ordens de serviço locais.");
            
            Console.WriteLine($"Erro ao obter ordens locais: {e}");
            return StatusCode(500, errorResponse);
        }
    }
    
    [HttpGet("get-all-orders")]
    public async Task<IActionResult> GetAllOrders()
    {
        try
        {
            var orders = await handler.GetAllServiceOrder();
            return orders.IsSuccess ? Ok(orders) : BadRequest(orders);
        }
        catch (Exception e)
        {
            var errorResponse = new Response<string?>(
                null, 
                500, 
                "Ocorreu um erro interno ao tentar recuperar todas as ordens de serviço.");
            
            Console.WriteLine($"Erro ao obter todas as ordens: {e}");
            return StatusCode(500, errorResponse);
        }
    }

    [HttpPut("add-quote")]
    public async Task<IActionResult> AddQuote([FromBody] AddQuoteRequest request)
    {
        try
        {
            var result = await handler.AddQuoteById(request);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }
        catch (Exception e)
        {
            var errorResponse = new Response<string?>(
                null, 
                500, 
                "Falha ao adicionar o orçamento à ordem de serviço.");
            
            Console.WriteLine($"Erro ao adicionar orçamento: {e}");
            return StatusCode(500, errorResponse);
        }
    }
    
    [HttpPut("add-repair")]
    public async Task<IActionResult> AddRepair([FromBody] long id)
    {
        try
        {
            var result = await handler.AddRepairById(id);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }
        catch (Exception e)
        {
            var errorResponse = new Response<string?>(
                null, 
                500, 
                "Erro ao tentar adicionar reparo à ordem de serviço.");
            
            Console.WriteLine($"Erro ao adicionar reparo: {e}");
            return StatusCode(500, errorResponse);
        }
    }
    
    [HttpPut("add-delivery")]
    public async Task<IActionResult> AddDelivery([FromBody] long id)
    {
        try
        {
            var result = await handler.AddDeliveryById(id);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }
        catch (Exception e)
        {
            var errorResponse = new Response<string?>(
                null, 
                500, 
                "Não foi possível registrar a entrega da ordem de serviço.");
            
            Console.WriteLine($"Erro ao adicionar entrega: {e}");
            return StatusCode(500, errorResponse);
        }
    }
    
    [HttpPut("add-status")]
    public async Task<IActionResult> AddStatus([FromBody] AddStatusByIdRequest request)
    {
        try
        {
            var result = await handler.AddStatusById(request);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }
        catch (Exception e)
        {
            var errorResponse = new Response<string?>(
                null, 
                500, 
                "Falha ao atualizar o status da ordem de serviço.");
            
            Console.WriteLine($"Erro ao atualizar status: {e}");
            return StatusCode(500, errorResponse);
        }
    }
    
    [HttpPut("add-location")]
    public async Task<IActionResult> AddStatus([FromBody] AddLocationRequest request)
    {
        try
        {
            var result = await handler.AddLocation(request);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }
        catch (Exception e)
        {
            var errorResponse = new Response<string?>(
                null, 
                500, 
                "Falha ao atualizar a prateleira da ordem de serviço.");
            
            Console.WriteLine($"Erro ao atualizar prateleira: {e}");
            return StatusCode(500, errorResponse);
        }
    }
}