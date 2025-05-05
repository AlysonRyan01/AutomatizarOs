namespace AutomatizarOs.Core.Requests.ServiceOrderRequests;

public class AddLocationRequest
{
    public long Id { get; set; }
    public string Location { get; set; } = String.Empty;
}