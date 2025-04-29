using AutomatizarOs.Core.Requests.IdentityRequests;
using AutomatizarOs.Core.Responses;

namespace AutomatizarOs.Core.Handlers
{
    public interface IIdentityHandler
    {
        Task<Response<string>> Login(LoginRequest request);
    }
}