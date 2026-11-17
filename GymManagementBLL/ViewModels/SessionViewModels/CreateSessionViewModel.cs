using System.ComponentModel.DataAnnotations;
namespace GymManagementBLL.ViewModels.SessionViewModels
{
    public class CreateSessionViewModel
    {
        [Required(ErrorMessage = "A Descrição é obrigatória")]
        [StringLength(500, MinimumLength = 10, ErrorMessage = "A Descrição deve ter entre 10 e 500 caracteres")]
        [Display(Name = "Descrição")]
        public string Description { get; set; } = null!;

        [Required(ErrorMessage = "Informe a capacidade da aula")]
        [Range(1, 25, ErrorMessage = "A capacidade deve ser de 1 a 25 participantes.")]
        [Display(Name = "Capacidade")]
        public int Capacity { get; set; }

        [Required(ErrorMessage = "A Data de Início é obrigatória")]
        [Display(Name = "Data e Horário de Início")]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "A Data de Término é obrigatória")]
        [Display(Name = "Data e Horário de Término")]
        public DateTime EndDate { get; set; }

        [Required(ErrorMessage = "Selecione o professor responsável")]
        [Display(Name = "Treinador")]
        public int TrainerId { get; set; }

        [Required(ErrorMessage = "Selecione o tipo de aula")]
        [Display(Name = "Categoria")]
        public int CategoryId { get; set; }
    }
}
