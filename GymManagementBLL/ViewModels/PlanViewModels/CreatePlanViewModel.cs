using System.ComponentModel.DataAnnotations;

namespace GymManagementBLL.ViewModels.PlanViewModels;

public class CreatePlanViewModel
{
    [Required(ErrorMessage = "O nome do plano é obrigatório.")]
    [StringLength(100, MinimumLength = 2)]
    public string PlanName { get; set; } = null!;

    [Required(ErrorMessage = "A descrição é obrigatória.")]
    [StringLength(200, MinimumLength = 5)]
    public string Description { get; set; } = null!;

    [Range(1, 365, ErrorMessage = "A duração deve estar entre 1 e 365 dias.")]
    public int DurationDays { get; set; }

    [Range(typeof(decimal), "0.01", "10000", ErrorMessage = "O valor deve ser maior que zero.")]
    public decimal Price { get; set; }
}
