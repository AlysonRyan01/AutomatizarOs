using AutomatizarOs.Api.Hubs;
using AutomatizarOs.Core.Handlers;
using Microsoft.AspNetCore.SignalR;
using Quartz;

namespace AutomatizarOs.Api.Services;

public class ServiceOrderJob : IJob
{
    private readonly IServiceOrderHandler _serviceOrderHandler;
    private readonly ICustomerHandler _customerHandler;
    private readonly ILogger<ServiceOrderJob> _logger;
    private readonly IHubContext<OrdemDeServicoHub> _hubContext;
    
    public ServiceOrderJob(IServiceOrderHandler serviceOrderHandler, ILogger<ServiceOrderJob> logger, IHubContext<OrdemDeServicoHub> hubContext, ICustomerHandler customerHandler)
    {
        _serviceOrderHandler = serviceOrderHandler;
        _logger = logger;
        _hubContext = hubContext;
        _customerHandler = customerHandler;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            var newCustomer = await _customerHandler.GetLocalCustomers();
            
            if (!newCustomer.IsSuccess)
            {
                _logger.LogWarning("Falha ao buscar novas ordens: {Message}", 
                    newCustomer.Message);
            }
            else
            {
                _logger.LogInformation("Nova ordem sincronizada!");
            }
            
            var newOrderResponse = await _serviceOrderHandler.GetLocalServiceOrder();
            
            if (!newOrderResponse.IsSuccess)
            {
                _logger.LogWarning("Falha ao buscar novas ordens: {Message}", 
                    newOrderResponse.Message);
            }
            else if (newOrderResponse.Data != null)
            {
                if (newOrderResponse.Data!.Value)
                    await _hubContext.Clients.All.SendAsync("NovaOSRecebida", newOrderResponse.Message);
                
                _logger.LogInformation("Nova ordem sincronizada!");
            }
            
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado no job de sincronização");
            throw new JobExecutionException(ex, false);
        }
    }
}