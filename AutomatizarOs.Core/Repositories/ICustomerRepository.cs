using AutomatizarOs.Core.Models;

namespace AutomatizarOs.Core.Repositories
{
    public interface ICustomerRepository
    {
        Task<List<Customer>> GetLocalCustomers();
    }
}