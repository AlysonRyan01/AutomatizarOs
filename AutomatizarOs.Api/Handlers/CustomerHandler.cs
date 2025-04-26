using AutomatizarOs.Api.Data;
using AutomatizarOs.Core.Handlers;
using AutomatizarOs.Core.Repositories;
using AutomatizarOs.Core.Responses;
using Microsoft.EntityFrameworkCore;

namespace AutomatizarOs.Api.Handlers;

public class CustomerHandler : ICustomerHandler
{
    private readonly ICustomerRepository _customerRepository;
    private readonly AutomatizarDbContext _context;

    public CustomerHandler(ICustomerRepository customerRepository, AutomatizarDbContext context)
    {
        _customerRepository = customerRepository;
        _context = context;
    }

    public async Task<Response<string>> GetLocalCustomers()
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var customers = await _customerRepository.GetLocalCustomers();

            if (!customers.Any())
                return new Response<string>("Ocorreu um erro ao tentar recuperar os clientes do access", 500, "Ocorreu um erro ao tentar recuperar os clientes do access");

            var customerIds = customers.Select(c => c.Id).ToList();

            var existingIds = await _context.Customers
                .Where(c => customerIds.Contains(c.Id))
                .Select(c => c.Id)
                .ToListAsync();

            var newCustomers = customers.Where(c => !existingIds.Contains(c.Id)).ToList();

            if (newCustomers.Any())
            {
                _context.Customers.AddRange(newCustomers);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }

            return new Response<string>("clientes recuperados com sucesso!", 200, "Sucesso");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return new Response<string>(ex.Message, 500, "Erro interno no servidor");
        }
    }
}