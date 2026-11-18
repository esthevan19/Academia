using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GymManagementBLL.ViewModels.AdminManamentViewModels
{
    public class CreateAdminViewModel
    {
        [Required(ErrorMessage = "Informe o e-mail.")]
        [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
        [Display(Name = "E-mail")]
        [Display(Name = "E-mail")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Informe o nome de usuário.")]
        [Display(Name = "Nome de usuário")]
        [Display(Name = "Nome de usuário")]
        public string UserName { get; set; } = null!;

        [Required(ErrorMessage = "Informe o primeiro nome.")]
        [Display(Name = "Nome")]
        [Display(Name = "Nome")]
        public string FirstName { get; set; } = null!;

        [Required(ErrorMessage = "Informe o sobrenome.")]
        [Display(Name = "Sobrenome")]
        [Display(Name = "Sobrenome")]
        public string LastName { get; set; } = null!;

        [Phone(ErrorMessage = "Informe um telefone válido.")]
        [Display(Name = "Celular com DDD")]
        [Display(Name = "Celular com DDD")]
        public string? PhoneNumber { get; set; }

        [Required(ErrorMessage = "Informe a senha.")]
        [MinLength(8, ErrorMessage = "A senha precisa ter pelo menos 8 caracteres.")]
        [DataType(DataType.Password)]
        [Display(Name = "Senha")]
        [Display(Name = "Senha")]
        public string Password { get; set; } = null!;

        [Required(ErrorMessage = "Confirm Informe a senha.")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "As senhas não coincidem.")]
        [Display(Name = "Confirmar senha")]
        [Display(Name = "Confirmar senha")]
        public string ConfirmPassword { get; set; } = null!;

        [Required(ErrorMessage = "O Perfil/Função é obrigatório")]
        [Display(Name = "Perfil / Função")]
        public string Role { get; set; } = "Admin";
    }
}
