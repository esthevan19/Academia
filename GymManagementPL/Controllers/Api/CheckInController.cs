using Microsoft.AspNetCore.Mvc;

namespace GymManagementPL.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class CheckInController : ControllerBase
    {
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

    public class CheckInRequest
    {
        public string? QrCode { get; set; }
        public string? StudentName { get; set; }
    }
}
