using AutomatizarOs.Api.Data;
using AutomatizarOs.Core.Handlers;
using AutomatizarOs.Core.Models;
using AutomatizarOs.Core.Requests.ServiceOrderRequests;
using AutomatizarOs.Core.Responses;
using Microsoft.AspNetCore.Mvc;

namespace AutomatizarOs.Api.Controllers;

[Route("v1/ServiceOrder")]
public class ServiceOrderController(IServiceOrderHandler handler) : ControllerBase
{
    [HttpGet("orders")]
    public async Task<IActionResult> GetOrders()
    {
        try
        {
            var orders = await handler.GetLocalServiceOrder();
            
            return orders.IsSuccess ? Ok(orders) : BadRequest(orders);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    [HttpPut("add-quote")]
    public async Task<IActionResult> AddQuote(AddQuoteRequest request)
    {
        try
        {
            var result = await handler.AddQuoteById(request);
            
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}