using System.ComponentModel.DataAnnotations;

namespace AutomatizarOs.Core.Requests.ServiceOrderRequests;

public class AddQuoteRequest
{
    [Required]
    public long Id { get; set; }

    [Required]
    [MinLength(2, ErrorMessage = "A solução precisa ter pelo menos 2 caracteres")]
    [StringLength(500, ErrorMessage = "A solução não pode exceder 500 caracteres.")]
    public string Solution { get; set; } = string.Empty;

    [Range(0, double.MaxValue, ErrorMessage = "O valor deve ser um número positivo.")]
    public decimal Amount { get; set; }

    [Required] public bool CanBeRepaired { get; set; } = true;
}