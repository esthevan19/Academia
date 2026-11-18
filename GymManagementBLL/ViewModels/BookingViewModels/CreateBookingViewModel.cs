using System.ComponentModel.DataAnnotations;

namespace GymManagementBLL.ViewModels.BookingViewModels
{
	public class CreateBookingViewModel
	{
		[Range(1, int.MaxValue, ErrorMessage = "Selecione um aluno válido.")]
		public int MemberId { get; set; }
		[Range(1, int.MaxValue, ErrorMessage = "Selecione uma aula válida.")]
		public int SessionId { get; set; }

	}
}
