using System.ComponentModel.DataAnnotations;

namespace GymManagementDAL.Entities.Enums
{
    // Manter os valores existentes para não alterar as especializações já gravadas.
    public enum Specialties
    {
        [Display(Name = "Preparação física geral")]
        GeneralFitness = 1,
        [Display(Name = "CrossFit")]
        CrossFit = 2,
        [Display(Name = "Yoga")]
        Yoga = 3,
        [Display(Name = "Boxe")]
        Boxing = 4,

        [Display(Name = "Musculação")]
        Musculacao = 5,
        [Display(Name = "Treinamento funcional")]
        TreinamentoFuncional = 6,
        [Display(Name = "Condicionamento físico")]
        CondicionamentoFisico = 7,
        [Display(Name = "Avaliação física")]
        AvaliacaoFisica = 8,
        [Display(Name = "Treinamento personalizado")]
        TreinamentoPersonalizado = 9,
        [Display(Name = "Alongamento e mobilidade")]
        Alongamento = 10
    }
}
