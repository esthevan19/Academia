using GymManagementDAL.Entities.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GymManagementBLL.ViewModels.TrainerViewModels
{
	public class TrainerToUpdateViewModel
	{
		[Required(ErrorMessage = "O Nome é obrigatório")]
		[RegularExpression(@"^[\p{L}\s'-]+$", ErrorMessage = "O nome pode conter apenas letras e espaços")]
		public string Name { get; set; } = null!;

		[Required(ErrorMessage = "O Email é obrigatório")]
		[EmailAddress(ErrorMessage = "Formato de email inválido")]
		public string Email { get; set; } = null!;

		[Required(ErrorMessage = "O Telefone é obrigatório")]
		[Phone(ErrorMessage = "Número de telefone inválido")]
		[RegularExpression(@"^(\+?55\s?)?(\(?\d{2}\)?\s?)?9?\d{4}[-\s]?\d{4}$|^\d{8,15}$", ErrorMessage = "Informe um telefone válido (ex: 11987654321)")]
		public string Phone { get; set; } = null!;

		[Required(ErrorMessage = "O Número é obrigatório")]
		[Range(1, int.MaxValue, ErrorMessage = "O número deve ser maior que 0")]
		public int BuildingNumber { get; set; }

		[Required(ErrorMessage = "A Cidade é obrigatória")]
		[StringLength(100, MinimumLength = 2, ErrorMessage = "A cidade deve ter entre 2 e 100 caracteres")]
		[RegularExpression(@"^[\p{L}0-9\s'.-]+$", ErrorMessage = "A cidade pode conter apenas letras e espaços")]
		public string City { get; set; } = null!;

		[Required(ErrorMessage = "A Rua é obrigatória")]
		[StringLength(150, MinimumLength = 2, ErrorMessage = "A rua deve ter entre 2 e 150 caracteres")]
		[RegularExpression(@"^[\p{L}0-9\s'.,-]+$", ErrorMessage = "A rua pode conter letras, números e espaços")]
		public string Street { get; set; } = null!;

		[Required(ErrorMessage = "A Especialidade é obrigatória")]
		public Specialties Specialties { get; set; }
	}
}
