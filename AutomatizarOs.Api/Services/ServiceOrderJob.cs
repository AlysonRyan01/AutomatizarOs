using AutomatizarOs.Core.Handlers;
using Quartz;

namespace AutomatizarOs.Api.Services;

public class ServiceOrderJob : IJob
{
    private readonly IServiceOrderHandler _serviceOrderHandler;
    
    public ServiceOrderJob(IServiceOrderHandler serviceOrderHandler)
    {
        _serviceOrderHandler = serviceOrderHandler;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        await _serviceOrderHandler.GetLocalServiceOrder();
    }
}