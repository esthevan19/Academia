using GymManagementDAL.Entities.Enums;

namespace GymManagementPL.Models;

public static class GymSpecialtiesPtBr
{
    // A lista exibida no cadastro prioriza especializações de academias brasileiras.
    // Especializações legadas continuam no enum para preservar os registros existentes.
    public static IReadOnlyList<(Specialties Value, string Label)> RegistrationOptions { get; } =
        new (Specialties, string)[]
        {
            (Specialties.Musculacao, "Musculação"),
            (Specialties.TreinamentoFuncional, "Treinamento funcional"),
            (Specialties.CondicionamentoFisico, "Condicionamento físico"),
            (Specialties.AvaliacaoFisica, "Avaliação física"),
            (Specialties.TreinamentoPersonalizado, "Treinamento personalizado"),
            (Specialties.Alongamento, "Alongamento e mobilidade"),
            (Specialties.GeneralFitness, "Preparação física geral")
        };

    public static bool IsRegistrationOption(Specialties value) =>
        RegistrationOptions.Any(option => option.Value == value);

    public static string Display(string? value) => value switch
    {
        "GeneralFitness" => "Preparação física geral",
        "CrossFit" => "CrossFit",
        "Yoga" => "Yoga",
        "Boxing" => "Boxe",
        "Musculacao" => "Musculação",
        "TreinamentoFuncional" => "Treinamento funcional",
        "CondicionamentoFisico" => "Condicionamento físico",
        "AvaliacaoFisica" => "Avaliação física",
        "TreinamentoPersonalizado" => "Treinamento personalizado",
        "Alongamento" => "Alongamento e mobilidade",
        _ => string.IsNullOrWhiteSpace(value) ? "Não informada" : value
    };
}
