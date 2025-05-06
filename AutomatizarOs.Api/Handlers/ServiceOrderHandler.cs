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
        var response = new Response<bool?>(false, 200, "Sucesso");
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var serviceOrders = await _serviceOrderRepository.GetActiveServiceOrders();

            if (!serviceOrders.Any())
                return new Response<bool?>(null, 500, "Erro ao obter as ordens de servico do access");

            var serviceOrdersIds = serviceOrders.Select(c => c.Id).ToList();

            var existingIds = await _context.ServiceOrders
                .Where(so => serviceOrdersIds.Contains(so.Id))
                .Select(so => so.Id)
                .ToListAsync();

            var newServiceOrders = serviceOrders.Where(c => !existingIds.Contains(c.Id)).ToList();
            
            if (!newServiceOrders.Any())
            {
                Console.WriteLine("Todas as ordens já existem no banco local");
                return response;
            }
            
            var customerIds = newServiceOrders
                .Select(o => o.CustomerId)
                .Distinct()
                .ToList();
            
            var accessCustomers = await _serviceOrderRepository.GetActiveCustomer(customerIds);
            
            var existingCustomers  = await _context.Customers
                .Where(c => customerIds.Contains(c.CustId))
                .ToDictionaryAsync(c => c.CustId);

            if (newServiceOrders.Any())
            {
                response.Data = true;
                foreach (var serviceOrder in newServiceOrders)
                {
                    if (!existingCustomers.TryGetValue(serviceOrder.CustomerId, out var customer))
                    {
                        var accessCustomer = accessCustomers?.FirstOrDefault(c => c.CustId == serviceOrder.CustomerId);
                        
                        if (accessCustomer != null)
                        {
                            customer = new Customer
                            {
                                CustId = serviceOrder.CustomerId,
                                Name = serviceOrder.Customer.Name,
                                Street = serviceOrder.Customer.Street,
                                Neighborhood = serviceOrder.Customer.Neighborhood,
                                City = serviceOrder.Customer.City,
                                Number = serviceOrder.Customer.Number,
                                ZipCode = serviceOrder.Customer.ZipCode,
                                StateCode = serviceOrder.Customer.StateCode,
                                Landline = serviceOrder.Customer.Landline,
                                Phone = serviceOrder.Customer.Phone,
                                Email = serviceOrder.Customer.Email
                            };
                            _context.Customers.Add(customer);
                            existingCustomers.Add(customer.CustId, customer);
                        }
                    }
                    if (customer != null)
                    {
                        serviceOrder.CustomerId = customer.CustId;
                        serviceOrder.Customer = customer;
                    }
                    else
                    {
                        Console.WriteLine($"Cliente {serviceOrder.CustomerId} não encontrado para a ordem {serviceOrder.Id}");
                        serviceOrder.CustomerId = 0;
                    }
                }
                
                _context.ServiceOrders.AddRange(newServiceOrders);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }

            return response;
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
            
            var cloudUpdateResult = await _serviceOrderRepository.UpdateCloudServiceOrder(serviceOrder);
            if (cloudUpdateResult == false)
                return new Response<ServiceOrder>(null, 500, "Erro ao atualizar a ordem de servico na nuvem");
            
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

    public async Task<Response<bool>> AddLocation(AddLocationRequest request)
    {
        try
        {
            var result = await _serviceOrderRepository.UpdateLocationServiceOrder(request);
            if (result == false)
                return new Response<bool>(false, 500, "Erro ao adicionar uma localizaçao.");
            
            return new Response<bool>(true, 200, "Localizaçao adicionada com sucesso");
        }
        catch (Exception e)
        {
            return new Response<bool>(false, 500, e.Message);
        }
    }
    
    public async Task<Response<ServiceOrder?>> GetCloudServiceOrder(long id)
    {
        try
        {
            var serviceOrder = await _serviceOrderRepository.GetServiceOrderById(id);
            
            if (serviceOrder == null)
                return new Response<ServiceOrder?>(null, 404, "Nenhuma ordem de serviço foi encontrada.");

            return new Response<ServiceOrder?>(serviceOrder, 200, "Ordem de servico obtida com sucesso!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro inesperado: {ex}");
            return new Response<ServiceOrder?>(null, 500, "Erro interno no servidor");
        }
    }
}
