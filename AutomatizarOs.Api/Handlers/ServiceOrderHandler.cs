using System.Data.Odbc;
using AutomatizarOs.Api.Data;
using AutomatizarOs.Core.Handlers;
using AutomatizarOs.Core.Models;
using AutomatizarOs.Core.Responses;
using Dapper;

public class ServiceOrderHandler(AutomatizarDbContext context) : IServiceOrderHandler
{
    private static readonly string ConnectionString = @"Driver={Microsoft Access Driver (*.mdb, *.accdb)};Dbq=C:\sisos\os.mdb;";
    private ServiceOrder LastServiceOrder { get; set; } = new();

    public async Task<Response<IEnumerable<ServiceOrder>>> GetLocalServiceOrder()
    {
        try
        {
            await using var connection = new OdbcConnection(ConnectionString);
            await connection.OpenAsync();

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
            if (latestOrder != null && LastServiceOrder.Id != latestOrder.Id)
            {
                LastServiceOrder = latestOrder;
                await context.ServiceOrders.AddAsync(latestOrder);
                await context.SaveChangesAsync();
            }

            return new Response<IEnumerable<ServiceOrder>>(serviceOrders);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Erro ao recuperar a ordem de serviço: {e.Message}");
            throw;
        }
    }
}
