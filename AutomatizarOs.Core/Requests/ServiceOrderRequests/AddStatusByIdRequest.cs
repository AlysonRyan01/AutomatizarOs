using AutomatizarOs.Core.Enums;

namespace AutomatizarOs.Core.Requests.ServiceOrderRequests;

public class AddStatusByIdRequest
{
    public long Id { get; set; }
    public ERepair Repair { get; set; }
}