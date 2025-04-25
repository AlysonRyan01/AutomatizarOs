using System.Data.OleDb;
using AutomatizarOs.Api.Data;
using AutomatizarOs.Core.Enums;
using AutomatizarOs.Core.Models;
using AutomatizarOs.Core.Repositories;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AutomatizarOs.Api.Repositories
{
    public class ServiceOrderRepository : IServiceOrderRepository
    {
        private readonly AutomatizarDbContext _context;
        private static readonly string ConnectionString = @"Provider=Microsoft.ACE.OLEDB.16.0;Data Source=C:\sisos\os.mdb;";

        public ServiceOrderRepository(AutomatizarDbContext context)
        {
            _context = context;
        }

        public async Task<List<ServiceOrder>> GetActiveServiceOrders()
        {
            try
            {
                using var connection = new OleDbConnection(ConnectionString);
                await connection.OpenAsync();

                const string query = @"SELECT TOP 10 os_codigo AS Id, os_situacao AS eServiceOrderStatus, 
                                    emp_codigo AS eEnterprise, pro_codigo AS productType, 
                                    os_marca AS productBrand, os_modelo AS productModel, 
                                    os_ns AS productSerialNumber, os_defeito AS productDefect, 
                                    os_solucao AS solution, os_valor AS amount, os_data_entrada AS entryDate, 
                                    os_data_vistoria AS inspectionDate, os_data_concerto AS repairDate, 
                                    os_data_entrega AS deliveryDate, cli_codigo AS customerId, 
                                    os_concerto AS eRepair, os_semconserto AS eUnrepaired, 
                                    os_valpeca AS partCost, os_valmo AS laborCost 
                                    FROM os 
                                    WHERE os_situacao != 4
                                    ORDER BY os_codigo DESC";

                var serviceOrders = (await connection.QueryAsync<ServiceOrder>(query)).ToList();

                return serviceOrders;
            }
            catch (OleDbException ex) when (ex.ErrorCode == -2147217871)
            {
                Console.WriteLine($"Erro na query SQL: {ex.Message}");
                return new List<ServiceOrder>();
            }
            catch (OleDbException ex)
            {
                Console.WriteLine($"Erro no banco Access: {ex.Message}");
                return new List<ServiceOrder>();
            }
            catch (InvalidCastException ex)
            {
                Console.WriteLine($"Erro de conversão de tipos: {ex.Message}");
                return new List<ServiceOrder>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro inesperado: {ex.Message}");
                return new List<ServiceOrder>();
            }
        }

        public async Task<List<ServiceOrder>> GetAllServiceOrdersFromCloud()
        {
            try
            {
                return await _context.ServiceOrders.ToListAsync();
            }
            catch (SqlException ex) when (ex.Number == -2)
            {
                Console.WriteLine("Timeout ao acessar o banco de dados");
                return new List<ServiceOrder>();
            }
            catch (SqlException ex) when (ex.Number == 53)
            {
                Console.WriteLine("Falha na conexão com o banco de dados");
                return new List<ServiceOrder>();
            }
            catch (SqlException)
            {
                Console.WriteLine("Erro no banco de dados (SQL Server)");
                return new List<ServiceOrder>();
            }
            catch (InvalidOperationException)
            {
                Console.WriteLine("DbContext foi descartado ou não inicializado");
                return new List<ServiceOrder>();
            }
            catch (Exception)
            {
                Console.WriteLine("Erro inesperado ao buscar ordens de serviço");
                return new List<ServiceOrder>();
            }
        }

        public async Task<ServiceOrder?> GetServiceOrderById(long id)
        {
            try
            {
                var serviceOrder = await _context.ServiceOrders.FirstOrDefaultAsync(x => x.Id == id);

                return serviceOrder ?? null;
            }
            catch (SqlException ex) when (ex.Number == -2)
            {
                Console.WriteLine("Timeout ao acessar o banco de dados");
                return null;
            }
            catch (SqlException ex) when (ex.Number == 53)
            {
                Console.WriteLine("Falha na conexão com o banco de dados");
                return null;
            }
            catch (SqlException)
            {
                Console.WriteLine("Erro no banco de dados (SQL Server)");
                return null;
            }
            catch (InvalidOperationException)
            {
                Console.WriteLine("DbContext foi descartado ou não inicializado");
                return null;
            }
            catch (Exception)
            {
                Console.WriteLine("Erro inesperado ao buscar ordens de serviço");
                return null;
            }
        }

        public async Task<bool> UpdateCloudServiceOrder(ServiceOrder serviceOrder)
        {
            try
            {
                _context.ServiceOrders.Update(serviceOrder);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine($"Erro ao atualizar no Entity Framework: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro inesperado: {ex}");
                return false;
            }
        }

        public async Task<bool> UpdateLocalServiceOrder(ServiceOrder serviceOrder)
        {
            try
            {
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

                return true;
            }
            catch (OleDbException ex) when (ex.ErrorCode == -2147217900)
            {
                Console.WriteLine($"Erro de sintaxe SQL: {ex.Message}");
                return false;
            }
            catch (OleDbException ex)
            {
                Console.WriteLine($"Erro de banco de dados Access: {ex.Message}");
                return false;
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine($"Erro ao atualizar no Entity Framework: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro inesperado: {ex}");
                return false;
            }
        }

        public async Task<ServiceOrder?> GetEvaluetedServiceOrderById(long id)
        {
            try
            {
                var serviceOrder = await _context
                .ServiceOrders
                .FirstOrDefaultAsync(x => x.Id == id && x.EServiceOrderStatus == EServiceOrderStatus.Evaluated);

                if (serviceOrder == null)
                    return null;

                return serviceOrder;
            }
            catch (System.Exception)
            {

                throw;
            }
        }
    }
}