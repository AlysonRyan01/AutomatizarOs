using AutomatizarOs.Core.Models;

namespace AutomatizarOs.Core.Repositories
{
    public interface IServiceOrderRepository
    {
        Task<List<ServiceOrder>> GetActiveServiceOrders();
        Task<List<ServiceOrder>> GetAllServiceOrdersFromCloud();
        Task<ServiceOrder?> GetServiceOrderById(long id);
        Task<bool> UpdateCloudServiceOrder(ServiceOrder serviceOrder);
        Task<bool> UpdateLocalServiceOrder(ServiceOrder serviceOrder);
        Task<ServiceOrder?> GetEvaluetedServiceOrderById(long id);
    }
}