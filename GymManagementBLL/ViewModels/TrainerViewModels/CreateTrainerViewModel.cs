using GymManagementDAL.Entities.Enums;
using System.ComponentModel.DataAnnotations;
namespace GymManagementBLL.ViewModels.TrainerViewModels
{
    public class CreateTrainerViewModel
    {
        [Required(ErrorMessage = "O Nome é obrigatório")]
        [RegularExpression(@"^[A-Za-zÀ-ÖØ-öø-ÿ][A-Za-zÀ-ÖØ-öø-ÿ\s.'-]*$", ErrorMessage = "Informe um nome válido, permitindo acentos, espaços, hífen e apóstrofo.")]
        [Display(Name = "Nome")]
        public string Name { get; set; } = null!;

        [Required(ErrorMessage = "O Email é obrigatório")]
        [EmailAddress(ErrorMessage = "Formato de email inválido")]
        [Display(Name = "Email")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "O Telefone é obrigatório")]
        [Phone(ErrorMessage = "Número de telefone inválido")]
        [RegularExpression(@"^[1-9][0-9]9[0-9]{8}$", ErrorMessage = "O telefone deve estar no formato DDD + 9 dígitos, ex: 11987654321")]
        [Display(Name = "Telefone")]
        public string Phone { get; set; } = null!;

        [Required(ErrorMessage = "A Data de Nascimento é obrigatória")]
        [DataType(DataType.Date)]
        [Display(Name = "Data de Nascimento")]
        public DateOnly DateOfBirth { get; set; }

        [Required(ErrorMessage = "O Gênero é obrigatório")]
        [Display(Name = "Gênero")]
        public Gender Gender { get; set; }

        [Required(ErrorMessage = "O número do endereço é obrigatório")]
        [Range(1, int.MaxValue, ErrorMessage = "O número do endereço deve ser maior que 0")]
        [Display(Name = "Número")]
        public int BuildingNumber { get; set; }

        [Required(ErrorMessage = "A Cidade é obrigatória")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "A Cidade deve ter entre 2 e 100 caracteres")]
        [RegularExpression(@"^[A-Za-zÀ-ÖØ-öø-ÿ][A-Za-zÀ-ÖØ-öø-ÿ\s.'-]*$", ErrorMessage = "Informe uma cidade válida, permitindo letras, acentos, espaços, hífen e apóstrofo.")]
        [Display(Name = "Cidade")]
        public string City { get; set; } = null!;

        [Required(ErrorMessage = "A Rua é obrigatória")]
        [StringLength(150, MinimumLength = 2, ErrorMessage = "A Rua deve ter entre 2 e 150 caracteres")]
        [RegularExpression(@"^[A-Za-zÀ-ÖØ-öø-ÿ0-9][A-Za-zÀ-ÖØ-öø-ÿ0-9\s.,ºª°/()'-]*$", ErrorMessage = "Informe uma rua válida, permitindo acentos, números e sinais usuais de endereço.")]
        [Display(Name = "Rua")]
        public string Street { get; set; } = null!;

        [Required(ErrorMessage = "A Especialidade é obrigatória")]
        [EnumDataType(typeof(Specialties))]
        [Display(Name = "Especializações")]
        public Specialties Specialties { get; set; }
    }
}
