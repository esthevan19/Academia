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
	[Display(Name = "Nome completo")]
	public string Name { get; set; } = null!;

		[Required(ErrorMessage = "Informe o e-mail.")]
		[EmailAddress(ErrorMessage = "Informe um e-mail válido.")]

		public string Email { get; set; } = null!;

		[Required(ErrorMessage = "Informe o telefone.")]
		[Phone(ErrorMessage = "Informe um telefone brasileiro válido.")]
		[RegularExpression(@"^[1-9][0-9]9[0-9]{8}$", ErrorMessage = "Informe um telefone brasileiro com DDD e 9 dígitos")]

		[Display(Name = "Telefone (DDD + número)")]
		public string Phone { get; set; } = null!;

		[Required(ErrorMessage = "Informe o número do endereço.")]
		[Range(1, int.MaxValue, ErrorMessage = "Informe um número de endereço maior que zero.")]
		[Display(Name = "Número do endereço")]
		public int BuildingNumber { get; set; }

		[Required(ErrorMessage = "Informe a cidade.")]
		[StringLength(100, MinimumLength = 2, ErrorMessage = "A cidade deve ter entre 2 e 100 caracteres.")]
		[RegularExpression(@"^[A-Za-zÀ-ÖØ-öø-ÿ][A-Za-zÀ-ÖØ-öø-ÿ\s.\'-]*$", ErrorMessage = "Informe uma cidade válida, permitindo letras, acentos, espaços, hífen e apóstrofo.")]
		[Display(Name = "Cidade")]
		public string City { get; set; } = null!;

		[Required(ErrorMessage = "Informe a rua.")]
		[StringLength(150, MinimumLength = 2, ErrorMessage = "A rua deve ter entre 2 e 150 caracteres.")]
		[RegularExpression(@"^[A-Za-zÀ-ÖØ-öø-ÿ0-9][A-Za-zÀ-ÖØ-öø-ÿ0-9\s.,ºª°/()\'-]*$", ErrorMessage = "Informe uma rua válida, permitindo acentos, números e sinais usuais de endereço.")]
		[Display(Name = "Rua")]
		public string Street { get; set; } = null!;

		[Required(ErrorMessage = "Selecione a especialização.")]
		[EnumDataType(typeof(Specialties), ErrorMessage = "Selecione uma especialização válida.")]
		[Display(Name = "Especialização")]
		public Specialties Specialties { get; set; }
	}
}
