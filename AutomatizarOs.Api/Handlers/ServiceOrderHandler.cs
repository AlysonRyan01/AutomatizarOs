using AutomatizarOs.Api.Data;
using AutomatizarOs.Core.Enums;
using AutomatizarOs.Core.Handlers;
using AutomatizarOs.Core.Models;
using AutomatizarOs.Core.Repositories;
using AutomatizarOs.Core.Requests.ServiceOrderRequests;
using AutomatizarOs.Core.Responses;
using Microsoft.EntityFrameworkCore;

namespace AutomatizarOs.Api.Handlers;

public class ServiceOrderHandler : IServiceOrderHandler
{
    private readonly AutomatizarDbContext _context;
    private readonly IServiceOrderRepository _serviceOrderRepository;

    public ServiceOrderHandler(AutomatizarDbContext context, IServiceOrderRepository serviceOrderRepository)
    {
        _context = context;
        _serviceOrderRepository = serviceOrderRepository;
    }

    public async Task<Response<bool?>> GetLocalServiceOrder()
    {
        Console.WriteLine("Rodando a query");
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var serviceOrders = await _serviceOrderRepository.GetActiveServiceOrders();

            if (!serviceOrders.Any())
                return new Response<bool?>(null, 500, "Erro ao obter as ordens de servico do access");

            var serviceOrdersIds = serviceOrders.Select(c => c.Id).ToList();

            var existingIds = await _context.ServiceOrders
                .Where(c => serviceOrdersIds.Contains(c.Id))
                .Select(c => c.Id)
                .ToListAsync();

            var newServiceOrders = serviceOrders.Where(c => !existingIds.Contains(c.Id)).ToList();
            
            var customerIds = newServiceOrders
                .Select(o => o.Customer.CustId)
                .Distinct()
                .ToList();
            
            var existingCustomers = await _context.Customers
                .Where(c => customerIds.Contains(c.CustId))
                .ToListAsync();

            if (newServiceOrders.Any())
            {
                foreach (var serviceOrder in newServiceOrders)
                {
                    var existingCustomer = existingCustomers.FirstOrDefault(x => x.CustId == serviceOrder.Customer.CustId);
                    if (existingCustomer != null)
                    {
                        serviceOrder.Customer = existingCustomer;
                        serviceOrder.CustomerId = existingCustomer.CustId;
                    }
                }
                
                _context.ServiceOrders.AddRange(newServiceOrders);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }

            return new Response<bool?>(false, 200, "Sucesso");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro inesperado: {ex.Message}");
            await transaction.RollbackAsync();
            return new Response<bool?>(null, 500, "Erro interno no servidor");
        }
    }

    public async Task<Response<List<ServiceOrder>>> GetAllServiceOrder()
    {
        try
        {
            var serviceOrders = await _serviceOrderRepository.GetAllServiceOrdersFromCloud();
            
            if (!serviceOrders.Any())
                return new Response<List<ServiceOrder>>(null, 404, "Nenhuma ordem de servico foi encontrada");
            
            return new Response<List<ServiceOrder>>(serviceOrders);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro inesperado: {ex.Message}");
            return new Response<List<ServiceOrder>>(null, 500, "Erro interno no servidor");
        }
    }

    public async Task<Response<ServiceOrder>> AddQuoteById(AddQuoteRequest request)
    {
        try
        {
            var serviceOrder = await _serviceOrderRepository.GetServiceOrderById(request.Id);
            
            if (serviceOrder == null)
                return new Response<ServiceOrder>(null, 404, "Nenhuma ordem de serviço foi encontrada.");
            
            switch (request.CanBeRepaired)
            {
                case false:
                    serviceOrder.EUnrepaired = EUnrepaired.Unrepaired;
                    serviceOrder.EServiceOrderStatus = EServiceOrderStatus.Evaluated;
                    serviceOrder.ERepair = ERepair.Disapproved;
                    serviceOrder.InspectionDate = DateTime.Now;
                    serviceOrder.Amount = request.Amount;
                    serviceOrder.LaborCost = request.Amount;
                    serviceOrder.Solution = request.Solution;
                    break;
                
                case true:
                    serviceOrder.EServiceOrderStatus = EServiceOrderStatus.Evaluated;
                    serviceOrder.ERepair = ERepair.Waiting;
                    serviceOrder.InspectionDate = DateTime.Now;
                    serviceOrder.Amount = request.Amount;
                    serviceOrder.LaborCost = request.Amount;
                    serviceOrder.Solution = request.Solution;
                    serviceOrder.EUnrepaired = EUnrepaired.Repair;
                    break;
            }

            var cloudUpdateResult = await _serviceOrderRepository.UpdateCloudServiceOrder(serviceOrder);
            if (cloudUpdateResult == false)
                return new Response<ServiceOrder>(null, 500, "Erro ao atualizar a ordem de servico na nuvem");

            var localUpdateResult = await _serviceOrderRepository.UpdateLocalServiceOrder(serviceOrder);
            if (localUpdateResult == false)
                return new Response<ServiceOrder>(null, 500, "Erro ao atualizar a ordem de servico localmente");

            return new Response<ServiceOrder>(serviceOrder, 200, "Sucesso ao atualizar a ordem de serviço");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro inesperado: {ex}");
            return new Response<ServiceOrder>(null, 500, "Erro interno no servidor");
        }
    }

    public async Task<Response<ServiceOrder>> AddRepairById(long id)
    {
        try
        {
            var serviceOrder = await _serviceOrderRepository.GetEvaluetedServiceOrderById(id);
            
            if (serviceOrder == null)
                return new Response<ServiceOrder>(null, 404, "Nenhuma ordem de serviço foi encontrada.");
            
            serviceOrder.EServiceOrderStatus = EServiceOrderStatus.Repaired;
            serviceOrder.RepairDate = DateTime.Now;
            
            var cloudUpdateResult = await _serviceOrderRepository.UpdateCloudServiceOrder(serviceOrder);
            if (cloudUpdateResult == false)
                return new Response<ServiceOrder>(null, 500, "Erro ao atualizar a ordem de servico na nuvem");

            var localUpdateResult = await _serviceOrderRepository.UpdateLocalServiceOrderBasic(serviceOrder);
            if (localUpdateResult == false)
                return new Response<ServiceOrder>(null, 500, "Erro ao atualizar a ordem de servico localmente");
            
            return new Response<ServiceOrder>(serviceOrder, 200, "Ordem de serviço marcada como reparada com sucesso");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro inesperado: {ex}");
            return new Response<ServiceOrder>(null, 500, "Erro interno no servidor");
        }
    }

    public async Task<Response<ServiceOrder>> AddDeliveryById(long id)
    {
        try
        {
            var serviceOrder = await _serviceOrderRepository.GetServiceOrderById(id);
            
            if (serviceOrder == null)
                return new Response<ServiceOrder>(null, 404, "Nenhuma ordem de serviço foi encontrada.");
            
            serviceOrder.EServiceOrderStatus = EServiceOrderStatus.Delivered;
            serviceOrder.DeliveryDate = DateTime.Now;
            
            var localUpdateResult = await _serviceOrderRepository.UpdateDeliveryLocalServiceOrder(serviceOrder);
            if (localUpdateResult == false)
                return new Response<ServiceOrder>(null, 500, "Erro ao atualizar a ordem de servico localmente");

            return new Response<ServiceOrder>(serviceOrder, 200, "Ordem de serviço marcada como entregue");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro inesperado: {ex}");
            return new Response<ServiceOrder>(null, 500, "Erro interno no servidor");
        }
    }

    public async Task<Response<ServiceOrder>> AddStatusById(AddStatusByIdRequest request)
    {
        try
        {
            var serviceOrder = await _serviceOrderRepository.GetServiceOrderById(request.Id);
            
            if (serviceOrder == null)
                return new Response<ServiceOrder>(null, 404, "Nenhuma ordem de serviço foi encontrada.");
            
            serviceOrder.ERepair = request.Repair;

            var cloudUpdateResult = await _serviceOrderRepository.UpdateCloudServiceOrder(serviceOrder);
            if (cloudUpdateResult == false)
                return new Response<ServiceOrder>(null, 500, "Erro ao atualizar a ordem de servico na nuvem");
                
            var updateStatusResult = await _serviceOrderRepository.UpdateStatusLocalServiceOrder(serviceOrder);
            if (updateStatusResult == false)
                return new Response<ServiceOrder>(null, 500, "Erro ao atualizar a ordem de servico localmente");

            return new Response<ServiceOrder>(serviceOrder, 200, "Status atualizado com sucesso");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro inesperado: {ex}");
            return new Response<ServiceOrder>(null, 500, "Erro interno no servidor");
        }
    }
}
