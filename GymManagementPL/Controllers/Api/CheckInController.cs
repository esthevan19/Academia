using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymManagementPL.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Aluno")]
    public class CheckInController : ControllerBase
    {
        // O sistema ainda não possui um registro persistente de entrada na academia.
        // Não devolver "acesso liberado" apenas com um nome ou QR Code enviado pelo cliente.
        // O registro de presença em aulas coletivas é feito separadamente por /api/bookings/attendance.
        [HttpPost]
        public IActionResult RegisterCheckIn([FromBody] CheckInRequest request)
        {
            return StatusCode(StatusCodes.Status501NotImplemented, new
            {
                success = false,
                accessGranted = false,
                message = "O check-in de entrada ainda não está disponível. Procure a recepção da academia."
            });
        }
    }

    public class CheckInRequest
    {
        public string? QrCode { get; set; }
        public string? StudentName { get; set; }
    }
}
