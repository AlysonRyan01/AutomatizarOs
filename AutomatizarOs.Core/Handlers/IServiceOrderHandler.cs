using AutomatizarOs.Core.Models;
using AutomatizarOs.Core.Responses;

namespace AutomatizarOs.Core.Handlers;

public interface IServiceOrderHandler
{
    Task<Response<IEnumerable<ServiceOrder>>> GetLocalServiceOrder();
}