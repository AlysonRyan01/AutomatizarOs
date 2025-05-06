using System.Data.Common;
using System.Data.OleDb;
using AutomatizarOs.Api.Data;
using AutomatizarOs.Core.Enums;
using AutomatizarOs.Core.Models;
using AutomatizarOs.Core.Repositories;
using AutomatizarOs.Core.Requests.ServiceOrderRequests;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AutomatizarOs.Api.Repositories
{
    public class ServiceOrderRepository : IServiceOrderRepository
    {
        private readonly AutomatizarDbContext _context;
        private static readonly string ConnectionString = ApiConfiguration.AccessConnection;

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

                const string query = @"
                SELECT 
                    os.os_codigo AS Id,
                    os.os_situacao AS eServiceOrderStatus,
                    os.emp_codigo AS eEnterprise,
                    os.pro_codigo AS productType,
                    os.os_marca AS productBrand,
                    os.os_modelo AS productModel,
                    os.os_ns AS productSerialNumber,
                    os.os_defeito AS productDefect,
                    os.os_solucao AS solution,
                    os.os_valor AS amount,
                    os.os_data_entrada AS entryDate,
                    os.os_data_vistoria AS inspectionDate,
                    os.os_data_concerto AS repairDate,
                    os.os_data_entrega AS deliveryDate,
                    os.cli_codigo AS customerId,
                    os.os_concerto AS eRepair,
                    os.os_semconserto AS eUnrepaired,
                    os.os_valpeca AS partCost,
                    os.os_valmo AS laborCost
                FROM os
                WHERE os.os_codigo IN (
                    SELECT TOP 100 os_codigo
                    FROM os
                    WHERE os_situacao <> 4
                    ORDER BY os_codigo DESC
                )
                ORDER BY os.os_codigo DESC";

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
                return await _context.ServiceOrders.Include(x => x.Customer).AsNoTracking().ToListAsync();
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
                var serviceOrder = await _context.ServiceOrders.Include(x => x.Customer).AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);

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

        public async Task<bool> UpdateLocationServiceOrder(AddLocationRequest request)
        {
            try
            {
                var serviceOrder = await _context.ServiceOrders.FirstOrDefaultAsync(x => x.Id == request.Id);
                if (serviceOrder == null)
                    return false;
                
                serviceOrder.Location = request.Location;
                
                _context.ServiceOrders.Update(serviceOrder);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine($"Erro ao atualizar a localizaçao no Entity Framework: {ex.Message}");
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
                .Include(x => x.Customer)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id && x.EServiceOrderStatus == EServiceOrderStatus.Evaluated);

                if (serviceOrder == null)
                    return null;

                return serviceOrder;
            }
            catch (DbException ex)
            {
                Console.WriteLine($"Erro ao atualizar no Entity Framework: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro inesperado: {ex}");
                return null;
            }
        }

        public async Task<bool> UpdateLocalServiceOrderBasic(ServiceOrder serviceOrder)
        {
            try
            {
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

                command.Parameters.Add(new OleDbParameter("@p1", OleDbType.Integer)
                {
                    Value = (int)serviceOrder.EServiceOrderStatus
                });
                command.Parameters.Add(new OleDbParameter("@p2", OleDbType.Date)
                {
                    Value = serviceOrder.RepairDate
                });
                command.Parameters.Add(new OleDbParameter("@p3", OleDbType.Integer)
                {
                    Value = serviceOrder.Id
                });

                await command.ExecuteNonQueryAsync();
                return true;

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

        public async Task<bool> UpdateDeliveryLocalServiceOrder(ServiceOrder serviceOrder)
        {
            try
            {
                const string accessQuery = @"
                UPDATE [os] 
                SET 
                    [os_situacao] = ?,
                    [os_data_entrega] = ?
                WHERE 
                    [os_codigo] = ?";

                await using var connection = new OleDbConnection(ConnectionString);
                await connection.OpenAsync();

                await using var command = new OleDbCommand(accessQuery, connection);
                command.CommandTimeout = 30;

                command.Parameters.Add(new OleDbParameter("@p1", OleDbType.Integer)
                {
                    Value = (int)serviceOrder.EServiceOrderStatus
                });
                command.Parameters.Add(new OleDbParameter("@p2", OleDbType.Date)
                {
                    Value = serviceOrder.DeliveryDate
                });
                command.Parameters.Add(new OleDbParameter("@p3", OleDbType.Integer)
                {
                    Value = serviceOrder.Id
                });

                await command.ExecuteNonQueryAsync();
                return true;

            }
            catch (OleDbException ex)
            {
                Console.WriteLine($"Erro de banco de dados Access: {ex.Message}");
                return false;
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine($"Erro ao remover no Entity Framework: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro inesperado: {ex}");
                return false;
            }
        }

        public async Task<bool> UpdateStatusLocalServiceOrder(ServiceOrder serviceOrder)
        {
            try
            {
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
                return true;
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

        public async Task<List<Customer?>> GetActiveCustomer(List<long> customerIds)
        {
            var customers = new List<Customer>();
            
            if (customerIds == null || !customerIds.Any())
                return customers;

            try
            {
                using var connection = new OleDbConnection(ConnectionString);
                await connection.OpenAsync();

                // Converter explicitamente para Int32 (que é o tipo mais compatível com Access)
                var intIds = customerIds.Select(id => (int)id).ToList();

                // Criar parâmetros com tipo explícito
                var parameters = intIds.Select((id, i) => 
                {
                    var param = new OleDbParameter($"@p{i}", OleDbType.Integer);
                    param.Value = id;
                    return param;
                }).ToList();

                var paramNames = string.Join(",", parameters.Select(p => p.ParameterName));
                
                // Query simplificada sem aliases problemáticos
                var query = $@"
                    SELECT 
                        cli_codigo,
                        cli_nome,
                        cli_endereco,
                        cli_bairro,
                        cli_cidade,
                        cli_numero,
                        cli_cep,
                        cli_uf,
                        cli_telefone,
                        cli_celular,
                        cli_email
                    FROM cliente 
                    WHERE cli_codigo IN ({paramNames})";

                using var command = new OleDbCommand(query, connection);
                command.Parameters.AddRange(parameters.ToArray());

                using var reader = await command.ExecuteReaderAsync();
                
                while (await reader.ReadAsync())
                {
                    customers.Add(new Customer
                    {
                        CustId = Convert.ToInt64(reader["cli_codigo"]),
                        Name = reader["cli_nome"] as string ?? "",
                        Street = reader["cli_endereco"] as string ?? "",
                        Neighborhood = reader["cli_bairro"] as string ?? "",
                        City = reader["cli_cidade"] as string ?? "",
                        Number = reader["cli_numero"] as string ?? "",
                        ZipCode = reader["cli_cep"] as string ?? "",
                        StateCode = reader["cli_uf"] as string ?? "",
                        Landline = reader["cli_telefone"] as string ?? "",
                        Phone = reader["cli_celular"] as string ?? "",
                        Email = reader["cli_email"] as string ?? ""
                    });
                }
                
                return customers;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERRO: {ex.Message}\nStack: {ex.StackTrace}");
                return new List<Customer>();
            }
        }
    }
}