using System.ComponentModel.DataAnnotations;

namespace AutomatizarOs.Core.Requests.IdentityRequests
{
    public class LoginRequest
    {
        [Required(ErrorMessage = "O e-mail é obrigatório.")]
        [EmailAddress(ErrorMessage = "O e-mail fornecido não é válido.")]
        [StringLength(255, ErrorMessage = "O e-mail deve ter no máximo 255 caracteres.")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "A senha é obrigatória.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "A senha deve ter pelo menos 6 caracteres e no máximo 100.")]
        public string Password { get; set; } = null!;
    }
}