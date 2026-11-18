using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GymManagementBLL.ViewModels.MemberViewModels
{
	public class HealthRecordViewModel
	{
		[Range(typeof(decimal), "0.50", "2.80", ParseLimitsInInvariantCulture = true, ErrorMessage = "Informe a altura em metros, entre 0,50 e 2,80 m.")]
		[Display(Name = "Altura (m)")]
		public decimal Height { get; set; }

		[Range(typeof(decimal), "1", "500", ErrorMessage = "Informe um peso entre 1 e 500 kg.")]
		[Display(Name = "Peso (kg)")]
		public decimal Weight { get; set; }

		[Required(ErrorMessage = "Informe o tipo sanguíneo.")]
		[StringLength(3, ErrorMessage = "O tipo sanguíneo deve ter no máximo 3 caracteres.")]
		[Display(Name = "Tipo sanguíneo")]
		public string BloodType { get; set; } = null!;
		[Display(Name = "Observações")]
		public string? Note { get; set; } = null!;
	}
}
