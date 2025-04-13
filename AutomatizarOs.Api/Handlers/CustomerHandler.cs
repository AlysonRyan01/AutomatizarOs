using System.Data.OleDb;
using AutomatizarOs.Api.Data;
using AutomatizarOs.Core.Handlers;
using AutomatizarOs.Core.Models;
using AutomatizarOs.Core.Responses;
using Dapper;
using Microsoft.EntityFrameworkCore;

namespace AutomatizarOs.Api.Handlers;

public class CustomerHandler(AutomatizarDbContext context) : ICustomerHandler
{
    private static readonly string ConnectionString = @"Provider=Microsoft.ACE.OLEDB.16.0;Data Source=C:\sisos\os.mdb;";
    public async Task<Response<string>> GetLocalCustomers()
    {
        using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            using var connection = new OleDbConnection(ConnectionString);
            connection.Open();
            
            const string query = @"
            SELECT TOP 10 
                [cli_codigo] AS [Id],
                [cli_nome] AS [Name],
                [cli_endereco] AS [Street],
                [cli_bairro] AS [Neighborhood],
                [cli_cidade] AS [City],
                [cli_numero] AS [Number],
                [cli_cep] AS [ZipCode],
                [cli_uf] AS [StateCode],
                [cli_telefone] AS [Landline],
                [cli_celular] AS [Phone],
                [cli_email] AS [Email]
            FROM [cliente]
            ORDER BY [cli_codigo] DESC";

            
            List<Customer> customers;
            
            try
            {
                customers = (await connection.QueryAsync<Customer>(query)).ToList();
            }
            catch (OleDbException ex) when (ex.ErrorCode == -2147217871)
            {
                Console.WriteLine($"Erro na query SQL: {ex.Message}");
                return new Response<string>(null, 500, "Erro na consulta ao banco de dados");
            }
            catch (OleDbException ex)
            {
                Console.WriteLine($"Erro no banco Access: {ex.Message}");
                return new Response<string>(null, 500, "Erro ao acessar o banco de dados");
            }
            catch (InvalidCastException ex)
            {
                Console.WriteLine($"Erro de conversão de tipos: {ex.Message}");
                return new Response<string>(null, 500, "Erro no formato dos dados");
            }

            var customerIds = customers.Select(c => c.Id).ToList();

            var existingIds = await context.Customers
                .Where(c => customerIds.Contains(c.Id))
                .Select(c => c.Id)
                .ToListAsync();

            var newCustomers = customers.Where(c => !existingIds.Contains(c.Id)).ToList();

            if (newCustomers.Any())
            {
                context.Customers.AddRange(newCustomers);
                await context.SaveChangesAsync();
                await transaction.CommitAsync();
            }

            return new Response<string>("OK", 200, "Sucesso");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return new Response<string>(ex.Message, 500, "Erro interno no servidor");
        }
    }
}