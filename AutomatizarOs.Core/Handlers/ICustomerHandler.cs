using AutomatizarOs.Core.Responses;

namespace AutomatizarOs.Core.Handlers;

public interface ICustomerHandler
{
    Task<Response<string>> GetLocalCustomers();
}