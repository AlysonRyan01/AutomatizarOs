using AutomatizarOs.Core.Models;
using AutomatizarOs.Core.Requests.ServiceOrderRequests;
using AutomatizarOs.Core.Responses;

namespace AutomatizarOs.Core.Handlers;

public interface IServiceOrderHandler
{
    Task<Response<bool?>> GetLocalServiceOrder();
    Task<Response<List<ServiceOrder>>> GetAllServiceOrder();
    Task<Response<ServiceOrder>> AddQuoteById(AddQuoteRequest request);
    Task<Response<ServiceOrder>> AddRepairById(long id);
    Task<Response<ServiceOrder>> AddDeliveryById(long id);
    Task<Response<ServiceOrder>> AddStatusById(AddStatusByIdRequest request);
    Task<Response<bool>> AddLocation(AddLocationRequest request);
    Task<Response<ServiceOrder?>> GetCloudServiceOrder(long id);
}