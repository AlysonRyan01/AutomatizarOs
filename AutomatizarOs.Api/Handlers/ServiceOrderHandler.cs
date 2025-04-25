using System.Data.OleDb;
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
    private static readonly string ConnectionString = @"Provider=Microsoft.ACE.OLEDB.16.0;Data Source=C:\sisos\os.mdb;";

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

            if (newServiceOrders.Any())
            {
                _context.ServiceOrders.AddRange(newServiceOrders);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return new Response<bool?>(true, 200, "Sucesso");
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
                    serviceOrder.Solution = request.Solution ?? string.Empty;
                    break;
                
                case true:
                    serviceOrder.EServiceOrderStatus = EServiceOrderStatus.Evaluated;
                    serviceOrder.ERepair = ERepair.Waiting;
                    serviceOrder.InspectionDate = DateTime.Now;
                    serviceOrder.Amount = request.Amount;
                    serviceOrder.LaborCost = request.Amount;
                    serviceOrder.Solution = request.Solution ?? string.Empty;
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
            
            const string accessQuery = @"
            UPDATE [os] 
            SET 
                [os_situacao] = ?,
                [os_data_concerto] = ?
            WHERE 
                [os_codigo] = ?";

            await using var connection = new OleDbConnection(ConnectionString);
            await connection.OpenAsync();
        
            await using var command = new OleDbCommand(accessQuery, connection);
            command.CommandTimeout = 30;
            
            command.Parameters.Add(new OleDbParameter("@p1", OleDbType.Integer) { 
                Value = (int)serviceOrder.EServiceOrderStatus 
            });
            command.Parameters.Add(new OleDbParameter("@p2", OleDbType.Date) { 
                Value = serviceOrder.RepairDate 
            });
            command.Parameters.Add(new OleDbParameter("@p3", OleDbType.Integer) { 
                Value = serviceOrder.Id 
            });
        
            await command.ExecuteNonQueryAsync();

            return new Response<ServiceOrder>(serviceOrder, 200, "Ordem de serviço marcada como reparada com sucesso");
        }
        catch (OleDbException ex)
        {
            Console.WriteLine($"Erro de banco de dados Access: {ex.Message}");
            return new Response<ServiceOrder>(null, 500, "Erro ao atualizar status no banco de dados Access");
        }
        catch (DbUpdateException ex)
        {
            Console.WriteLine($"Erro ao atualizar no Entity Framework: {ex.Message}");
            return new Response<ServiceOrder>(null, 500, "Erro ao atualizar status no banco de dados principal");
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
            var serviceOrder = await _context.ServiceOrders.FirstOrDefaultAsync(x => x.Id == id);
            
            if (serviceOrder == null)
                return new Response<ServiceOrder>(null, 404, "Nenhuma ordem de serviço foi encontrada.");
            
            serviceOrder.EServiceOrderStatus = EServiceOrderStatus.Delivered;
            serviceOrder.DeliveryDate = DateTime.Now;
            
            const string accessQuery = @"
            UPDATE [os] 
            SET 
                [os_situacao] = ?,
                [os_data_entrega] = ?
            WHERE 
                [os_codigo] = ?";

            await using (var connection = new OleDbConnection(ConnectionString))
            {
                await connection.OpenAsync();
            
                await using var command = new OleDbCommand(accessQuery, connection);
                command.CommandTimeout = 30;
            
                command.Parameters.Add(new OleDbParameter("@p1", OleDbType.Integer) { 
                    Value = (int)serviceOrder.EServiceOrderStatus 
                });
                command.Parameters.Add(new OleDbParameter("@p2", OleDbType.Date) { 
                    Value = serviceOrder.DeliveryDate 
                });
                command.Parameters.Add(new OleDbParameter("@p3", OleDbType.Integer) { 
                    Value = serviceOrder.Id 
                });
            
                await command.ExecuteNonQueryAsync();
            }
            
            _context.ServiceOrders.Remove(serviceOrder);
            await _context.SaveChangesAsync();

            return new Response<ServiceOrder>(serviceOrder, 200, "Ordem de serviço marcada como entregue e removida localmente");
        }
        catch (OleDbException ex)
        {
            Console.WriteLine($"Erro de banco de dados Access: {ex.Message}");
            return new Response<ServiceOrder>(null, 500, "Erro ao atualizar status no banco de dados Access");
        }
        catch (DbUpdateException ex)
        {
            Console.WriteLine($"Erro ao remover no Entity Framework: {ex.Message}");
            return new Response<ServiceOrder>(null, 500, "Erro ao remover ordem de serviço");
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
            var serviceOrder = await _context.ServiceOrders.FirstOrDefaultAsync(x => x.Id == request.Id);
            
            if (serviceOrder == null)
                return new Response<ServiceOrder>(null, 404, "Nenhuma ordem de serviço foi encontrada.");
            
            serviceOrder.ERepair = request.Repair;
            
            _context.ServiceOrders.Update(serviceOrder);
            await _context.SaveChangesAsync();
            
            const string accessQuery = @"
            UPDATE [os] 
            SET 
                [os_concerto] = ?
            WHERE 
                [os_codigo] = ?";
            
            await using var connection = new OleDbConnection(ConnectionString);
            await connection.OpenAsync();
            
            await using var command = new OleDbCommand(accessQuery, connection);
            command.CommandTimeout = 30;
            
            command.Parameters.Add(new OleDbParameter("@p1", OleDbType.Integer) { Value = (int)serviceOrder.ERepair });
            command.Parameters.Add(new OleDbParameter("@p2", OleDbType.Integer) { Value = serviceOrder.Id });
        
            await command.ExecuteNonQueryAsync();

            return new Response<ServiceOrder>(serviceOrder, 200, "Status atualizado com sucesso");
        }
        catch (OleDbException ex)
        {
            Console.WriteLine($"Erro de banco de dados Access: {ex.Message}");
            return new Response<ServiceOrder>(null, 500, "Erro ao atualizar status no banco de dados Access");
        }
        catch (DbUpdateException ex)
        {
            Console.WriteLine($"Erro ao atualizar no Entity Framework: {ex.Message}");
            return new Response<ServiceOrder>(null, 500, "Erro ao atualizar status no banco de dados principal");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro inesperado: {ex}");
            return new Response<ServiceOrder>(null, 500, "Erro interno no servidor");
        }
    }
}
