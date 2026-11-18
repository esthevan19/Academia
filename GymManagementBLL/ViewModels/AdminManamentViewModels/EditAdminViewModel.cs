using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GymManagementBLL.ViewModels.AdminManamentViewModels
{
    public class EditAdminViewModel
    {
        [Required]
        public string Id { get; set; } = null!;

        [Required(ErrorMessage = "Informe o e-mail.")]
        [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Informe o nome de usuário.")]
        public string UserName { get; set; } = null!;
    }
}
