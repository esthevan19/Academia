using Microsoft.AspNetCore.Mvc;

namespace GymManagementPL.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    /// <summary>API de simulação de check-in do aluno.</summary>
    /// <remarks>A implementação atual não verifica o QR Code, não autentica o aluno e não persiste a entrada.</remarks>
    public class CheckInController : ControllerBase
    {
        /// <summary>POST /api/CheckIn: recebe dados opcionais do aluno e devolve uma confirmação simulada com data e hora.</summary>
        [HttpPost]
        public IActionResult RegisterCheckIn([FromBody] CheckInRequest request)
        {
            var now = DateTime.Now;
            return Ok(new
            {
                success = true,
                message = "Entrada liberada! Bom treino na GymFlow!",
                accessGranted = true,
                timestamp = now.ToString("dd/MM/yyyy HH:mm:ss"),
                student = request.StudentName ?? "Aluno GymFlow",
                unit = "Unidade Principal"
            });
        }
    }

    /// <summary>Corpo JSON do check-in: código QR e nome do aluno, ambos opcionais na implementação atual.</summary>
    public class CheckInRequest
    {
        public string? QrCode { get; set; }
        public string? StudentName { get; set; }
    }
}
