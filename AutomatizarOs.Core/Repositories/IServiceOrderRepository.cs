using AutomatizarOs.Core.Models;
using AutomatizarOs.Core.Requests.ServiceOrderRequests;

namespace AutomatizarOs.Core.Repositories
{
    public interface IServiceOrderRepository
    {
        Task<List<ServiceOrder>> GetActiveServiceOrders();
        Task<List<ServiceOrder>> GetAllServiceOrdersFromCloud();
        Task<ServiceOrder?> GetServiceOrderById(long id);
        Task<bool> UpdateCloudServiceOrder(ServiceOrder serviceOrder);
        Task<bool> UpdateLocationServiceOrder(AddLocationRequest request);
        Task<bool> UpdateLocalServiceOrder(ServiceOrder serviceOrder);
        Task<ServiceOrder?> GetEvaluetedServiceOrderById(long id);
        Task<bool> UpdateLocalServiceOrderBasic(ServiceOrder serviceOrder);
        Task<bool> UpdateDeliveryLocalServiceOrder(ServiceOrder serviceOrder);
        Task<bool> UpdateStatusLocalServiceOrder(ServiceOrder serviceOrder);
    }
}