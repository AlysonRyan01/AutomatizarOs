using System.Data.OleDb;
using AutomatizarOs.Api.Data;
using AutomatizarOs.Core.Enums;
using AutomatizarOs.Core.Handlers;
using AutomatizarOs.Core.Models;
using AutomatizarOs.Core.Requests.ServiceOrderRequests;
using AutomatizarOs.Core.Responses;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AutomatizarOs.Api.Handlers;

public class ServiceOrderHandler(AutomatizarDbContext context) : IServiceOrderHandler
{
    private static readonly string ConnectionString = @"Provider=Microsoft.ACE.OLEDB.16.0;Data Source=C:\sisos\os.mdb;";

    public async Task<Response<ServiceOrder>> GetLocalServiceOrder()
    {
        Console.WriteLine("Rodando a query");
        try
        {
            using var connection = new OleDbConnection(ConnectionString);
            connection.Open();  // OLEDB não suporta operações assíncronas

            const string query = @"SELECT TOP 1 os_codigo AS Id, os_situacao AS eServiceOrderStatus, 
                                emp_codigo AS eEnterprise, pro_codigo AS productType, 
                                os_marca AS productBrand, os_modelo AS productModel, 
                                os_ns AS productSerialNumber, os_defeito AS productDefect, 
                                os_solucao AS solution, os_valor AS amount, os_data_entrada AS entryDate, 
                                os_data_vistoria AS inspectionDate, os_data_concerto AS repairDate, 
                                os_data_entrega AS deliveryDate, cli_codigo AS customerId, 
                                os_concerto AS eRepair, os_semconserto AS eUnrepaired, 
                                os_valpeca AS partCost, os_valmo AS laborCost 
                                FROM os 
                                ORDER BY os_codigo DESC";

            var serviceOrders = (await connection.QueryAsync<ServiceOrder>(query)).ToList();

            var latestOrder = serviceOrders.FirstOrDefault();
            
            if (latestOrder != null && !await context.ServiceOrders.AnyAsync(x => x.Id == latestOrder.Id))
            {
                await context.ServiceOrders.AddAsync(latestOrder);
                await context.SaveChangesAsync();
            }

            return new Response<ServiceOrder>(latestOrder);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro: {ex.Message}");
            throw;
        }
    }

    public async Task<Response<List<ServiceOrder>>> GetAllServiceOrder()
    {
        try
        {
            var serviceOrders = await context.ServiceOrders.ToListAsync();
            
            if (!serviceOrders.Any())
                return new Response<List<ServiceOrder>>(null, 404, "Nenhuma ordem de servico foi encontrada");
            
            return new Response<List<ServiceOrder>>(serviceOrders);
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"Operação inválida no DbContext: {ex.Message}");
            return new Response<List<ServiceOrder>>(null, 500, "Erro interno: contexto de banco de dados inválido");
        }
        catch (DbUpdateException ex)
        {
            Console.WriteLine($"Erro ao consultar o banco de dados: {ex.Message}");
            return new Response<List<ServiceOrder>>(null, 500, "Erro ao acessar o banco de dados");
        }
        catch (SqlException ex) when (ex.Number == -2)  // Timeout
        {
            Console.WriteLine($"Timeout na consulta: {ex.Message}");
            return new Response<List<ServiceOrder>>(null, 504, "Tempo excedido ao buscar ordens de serviço");
        }
        catch (SqlException ex)
        {
            Console.WriteLine($"Erro específico do SQL Server: {ex.Message}");
            return new Response<List<ServiceOrder>>(null, 500, "Erro no banco de dados");
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
            var serviceOrder = await context.ServiceOrders.FirstOrDefaultAsync(x => x.Id == request.Id);
            
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
            
            context.ServiceOrders.Update(serviceOrder);
            await context.SaveChangesAsync();
            
            const string accessQuery = @"
                UPDATE [os] 
                SET 
                    [os_situacao] = ?,
                    [os_solucao] = ?,
                    [os_valor] = ?,
                    [os_data_vistoria] = ?,
                    [os_concerto] = ?,
                    [os_semconserto] = ?,
                    [os_valmo] = ?
                WHERE 
                    [os_codigo] = ?";

            await using var connection = new OleDbConnection(ConnectionString);
            await connection.OpenAsync();
            
            await using var command = new OleDbCommand(accessQuery, connection);
            command.CommandTimeout = 30;
            
            command.Parameters.Add(new OleDbParameter("@p1", OleDbType.Integer) { Value = (int)serviceOrder.EServiceOrderStatus });
            command.Parameters.Add(new OleDbParameter("@p2", OleDbType.VarChar) { Value = serviceOrder.Solution ?? string.Empty });
            command.Parameters.Add(new OleDbParameter("@p3", OleDbType.Currency) { Value = serviceOrder.Amount });
            command.Parameters.Add(new OleDbParameter("@p4", OleDbType.Date) { Value = serviceOrder.InspectionDate ?? DateTime.Now });
            command.Parameters.Add(new OleDbParameter("@p5", OleDbType.Integer) { Value = (int)serviceOrder.ERepair });
            command.Parameters.Add(new OleDbParameter("@p6", OleDbType.Integer) { Value = (int)serviceOrder.EUnrepaired });
            command.Parameters.Add(new OleDbParameter("@p7", OleDbType.Currency) { Value = serviceOrder.Amount ?? 0m });
            command.Parameters.Add(new OleDbParameter("@p8", OleDbType.Integer) { Value = serviceOrder.Id });
            
            await command.ExecuteNonQueryAsync();

            return new Response<ServiceOrder>(serviceOrder, 200, "Sucesso ao atualizar a ordem de serviço");
        }
        catch (OleDbException ex) when (ex.ErrorCode == -2147217900)
        {
            Console.WriteLine($"Erro de sintaxe SQL: {ex.Message}");
            return new Response<ServiceOrder>(null, 400, "Erro na sintaxe do comando SQL");
        }
        catch (OleDbException ex)
        {
            Console.WriteLine($"Erro de banco de dados Access: {ex.Message}");
            return new Response<ServiceOrder>(null, 500, "Erro ao acessar o banco de dados Access");
        }
        catch (DbUpdateException ex)
        {
            Console.WriteLine($"Erro ao atualizar no Entity Framework: {ex.Message}");
            return new Response<ServiceOrder>(null, 500, "Erro ao salvar no banco de dados principal");
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
            var serviceOrder = await context
                .ServiceOrders
                .FirstOrDefaultAsync(x => x.Id == id && x.EServiceOrderStatus == EServiceOrderStatus.Evaluated);
            
            if (serviceOrder == null)
                return new Response<ServiceOrder>(null, 404, "Nenhuma ordem de serviço foi encontrada.");
            
            serviceOrder.EServiceOrderStatus = EServiceOrderStatus.Repaired;
            serviceOrder.RepairDate = DateTime.Now;
            
            context.ServiceOrders.Update(serviceOrder);
            await context.SaveChangesAsync();
            
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
            var serviceOrder = await context.ServiceOrders.FirstOrDefaultAsync(x => x.Id == id);
            
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
            
            context.ServiceOrders.Remove(serviceOrder);
            await context.SaveChangesAsync();

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
            var serviceOrder = await context.ServiceOrders.FirstOrDefaultAsync(x => x.Id == request.Id);
            
            if (serviceOrder == null)
                return new Response<ServiceOrder>(null, 404, "Nenhuma ordem de serviço foi encontrada.");
            
            serviceOrder.ERepair = request.Repair;
            
            context.ServiceOrders.Update(serviceOrder);
            await context.SaveChangesAsync();
            
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
