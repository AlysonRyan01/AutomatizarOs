using AutomatizarOs.Api.Data;
using AutomatizarOs.Core.Handlers;
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
            
            return orders.IsSuccess ? Ok(orders) : NotFound(orders);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}