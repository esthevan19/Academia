using System;

namespace GymManagementBLL.Services.Classes;

/// <summary>
/// Horário civil utilizado nas aulas da academia no Brasil.
/// Os horários de aula são persistidos como DateTime sem fuso no banco existente.
/// </summary>
public static class BrazilClock
{
    private static readonly TimeZoneInfo BrazilZone = ResolveBrazilZone();

    private static TimeZoneInfo ResolveBrazilZone()
    {
        foreach (var zoneId in new[] { "America/Sao_Paulo", "E. South America Standard Time" })
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(zoneId); }
            catch (TimeZoneNotFoundException) { }
            catch (InvalidTimeZoneException) { }
        }
        return TimeZoneInfo.CreateCustomTimeZone("BrazilStandardFallback", TimeSpan.FromHours(-3), "Horário de Brasília", "Horário de Brasília");
    }

    public static DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, BrazilZone);
}
