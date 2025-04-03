using AutomatizarOs.Core.Handlers;
using Quartz;

namespace AutomatizarOs.Api.Services;

public class ServiceOrderJob : IJob
{
    private readonly IServiceOrderHandler _serviceOrderHandler;
    private readonly ILogger<ServiceOrderJob> _logger;
    
    public ServiceOrderJob(IServiceOrderHandler serviceOrderHandler, ILogger<ServiceOrderJob> logger)
    {
        _serviceOrderHandler = serviceOrderHandler;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            var newOrderResponse = await _serviceOrderHandler.GetLocalServiceOrder();
            
            if (!newOrderResponse.IsSuccess)
            {
                _logger.LogWarning("Falha ao buscar novas ordens: {Message}", 
                    newOrderResponse.Message);
            }
            else if (newOrderResponse.Data != null)
            {
                _logger.LogInformation("Nova ordem sincronizada: ID {OrderId}", 
                    newOrderResponse.Data.Id);
            }
            
            var allOrdersResponse = await _serviceOrderHandler.GetAllServiceOrder();
            
            if (!allOrdersResponse.IsSuccess)
            {
                _logger.LogError("Falha ao atualizar lista completa: {Message}", 
                    allOrdersResponse.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado no job de sincronização");
            throw new JobExecutionException(ex, false);
        }
    }
}