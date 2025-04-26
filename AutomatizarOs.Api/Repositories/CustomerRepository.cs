using System.Data.OleDb;
using AutomatizarOs.Core.Models;
using AutomatizarOs.Core.Repositories;
using Dapper;

namespace AutomatizarOs.Api.Repositories
{
    public class CustomerRepository : ICustomerRepository
    {
        private static readonly string ConnectionString = @"Provider=Microsoft.ACE.OLEDB.16.0;Data Source=C:\sisos\os.mdb;";
        
        public async Task<List<Customer>> GetLocalCustomers()
        {
            try
            {
                using var connection = new OleDbConnection(ConnectionString);
                await connection.OpenAsync();

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


                var customers = (await connection.QueryAsync<Customer>(query)).ToList();

                return customers;
            }
            catch (OleDbException ex) when (ex.ErrorCode == -2147217871)
            {
                Console.WriteLine($"Erro na query SQL: {ex.Message}");
                return new List<Customer>();
            }
            catch (OleDbException ex)
            {
                Console.WriteLine($"Erro no banco Access: {ex.Message}");
                return new List<Customer>();
            }
            catch (InvalidCastException ex)
            {
                Console.WriteLine($"Erro de conversão de tipos: {ex.Message}");
                return new List<Customer>();
            }
        }
    }
}