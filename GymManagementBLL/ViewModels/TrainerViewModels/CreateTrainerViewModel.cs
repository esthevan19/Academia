using GymManagementDAL.Entities.Enums;
using System.ComponentModel.DataAnnotations;

namespace GymManagementBLL.ViewModels.TrainerViewModels
{
    public class CreateTrainerViewModel
    {
        [Required(ErrorMessage = "O Nome é obrigatório")]
        [RegularExpression(@"^[\p{L}\s'-]+$", ErrorMessage = "O nome pode conter apenas letras e espaços")]
        [Display(Name = "Nome")]
        public string Name { get; set; } = null!;

        [Required(ErrorMessage = "O Email é obrigatório")]
        [EmailAddress(ErrorMessage = "Formato de email inválido")]
        [Display(Name = "Email")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "O Telefone é obrigatório")]
        [Phone(ErrorMessage = "Número de telefone inválido")]
        [RegularExpression(@"^(\+?55\s?)?(\(?\d{2}\)?\s?)?9?\d{4}[-\s]?\d{4}$|^\d{8,15}$", ErrorMessage = "Informe um telefone válido (ex: 11987654321)")]
        [Display(Name = "Telefone")]
        public string Phone { get; set; } = null!;

        [Required(ErrorMessage = "A Data de Nascimento é obrigatória")]
        [DataType(DataType.Date)]
        [Display(Name = "Data de Nascimento")]
        public DateOnly DateOfBirth { get; set; }

        [Required(ErrorMessage = "O Gênero é obrigatório")]
        [Display(Name = "Gênero")]
        public Gender Gender { get; set; }

        [Required(ErrorMessage = "O Número é obrigatório")]
        [Range(1, int.MaxValue, ErrorMessage = "O número deve ser maior que 0")]
        [Display(Name = "Número")]
        public int BuildingNumber { get; set; }

        [Required(ErrorMessage = "A Cidade é obrigatória")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "A cidade deve ter entre 2 e 100 caracteres")]
        [RegularExpression(@"^[\p{L}0-9\s'.-]+$", ErrorMessage = "A cidade pode conter apenas letras e espaços")]
        [Display(Name = "Cidade")]
        public string City { get; set; } = null!;

        [Required(ErrorMessage = "A Rua é obrigatória")]
        [StringLength(150, MinimumLength = 2, ErrorMessage = "A rua deve ter entre 2 e 150 caracteres")]
        [RegularExpression(@"^[\p{L}0-9\s'.,-]+$", ErrorMessage = "A rua pode conter letras, números e espaços")]
        [Display(Name = "Rua")]
        public string Street { get; set; } = null!;

        [Required(ErrorMessage = "A Especialidade é obrigatória")]
        [EnumDataType(typeof(Specialties))]
        [Display(Name = "Especializações")]
        public Specialties Specialties { get; set; }
    }
}
