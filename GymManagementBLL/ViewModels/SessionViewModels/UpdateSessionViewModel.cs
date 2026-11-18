using System.ComponentModel.DataAnnotations;

namespace GymManagementBLL.ViewModels.SessionViewModels
{
	public class UpdateSessionViewModel
	{
		[Required(ErrorMessage = "A descrição é obrigatória.")]
		[StringLength(500, MinimumLength = 10, ErrorMessage = "A descrição deve ter entre 10 e 500 caracteres.")]
		public string Description { get; set; } = null!;

		[Required(ErrorMessage = "Informe a data e o horário de início.")]
		[Display(Name = "Data e Horário de Início")]
		public DateTime StartDate { get; set; }

		[Required(ErrorMessage = "Informe a data e o horário de término.")]
		[Display(Name = "Data e Horário de Término")]
		public DateTime EndDate { get; set; }

		[Required(ErrorMessage = "Selecione o professor responsável.")]
		[Display(Name = "Professor responsável")]
		public int Professor responsávelId { get; set; }

	}
}
