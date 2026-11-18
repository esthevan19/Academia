using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GymManagementBLL.ViewModels.PlanViewModels
{
	public class UpdatePlanViewModel
	{
		[Required(ErrorMessage = "Informe o nome do plano.")]
		[StringLength(100, MinimumLength = 2, ErrorMessage = "O nome do plano deve ter de 2 a 100 caracteres.")]
		[Display(Name = "Nome do plano")]
		public string PlanName { get; set; } = null!;

		[Required(ErrorMessage = "Informe a descrição do plano.")]
		[StringLength(200, MinimumLength = 5, ErrorMessage = "A descrição deve ter de 5 a 200 caracteres.")]
		[Display(Name = "Descrição")]
		public string Description { get; set; } = null!;

		[Required(ErrorMessage = "Informe a duração do plano em dias.")]
		[Range(1, 365, ErrorMessage = "A duração deve ser de 1 a 365 dias.")]
		[Display(Name = "Duração (dias)")]
		public int DurationDays { get; set; }

		[Required(ErrorMessage = "Informe o preço em reais.")]
		[Range(typeof(decimal), "0.01", "10000", ErrorMessage = "Informe um valor entre R$ 0,01 e R$ 10.000,00.")]
		[Display(Name = "Preço (R$)")]
		public decimal Price { get; set; }
	}
}
