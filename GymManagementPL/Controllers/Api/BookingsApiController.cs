using GymManagementBLL.Services.Interfaces;
using GymManagementBLL.ViewModels.BookingViewModels;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymManagementPL.Controllers.Api;

[ApiController]
[Route("api/bookings")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class BookingsApiController : ControllerBase
{
    private readonly IBookingService _bookingService;

    public BookingsApiController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    [HttpGet("sessions/{sessionId:int}/members")]
    [Authorize(Roles = "SuperAdmin,Admin,Gerente,Recepcionista,Instrutor")]
    public IActionResult GetSessionMembers(int sessionId)
    {
        if (sessionId <= 0)
            return BadRequest(new { success = false, message = "Identificador da sessão inválido." });

        return Ok(new
        {
            success = true,
            members = _bookingService.GetMembersSession(sessionId)
        });
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin,Gerente,Recepcionista")]
    public IActionResult Create([FromBody] CreateBookingViewModel model)
    {
        if (model.MemberId <= 0 || model.SessionId <= 0)
            return BadRequest(new { success = false, message = "Selecione um aluno e uma sessão válidos." });

        return _bookingService.CreateNewBooking(model)
            ? Ok(new { success = true, message = "Agendamento criado com sucesso." })
            : Conflict(new { success = false, message = "Não foi possível criar o agendamento." });
    }

    [HttpPost("attendance")]
    [Authorize(Roles = "SuperAdmin,Admin,Gerente,Recepcionista,Instrutor")]
    public IActionResult MarkAttendance([FromBody] CreateBookingViewModel model)
    {
        if (model.MemberId <= 0 || model.SessionId <= 0)
            return BadRequest(new { success = false, message = "Selecione um aluno e uma sessão válidos." });

        return _bookingService.IsMemberAttended(model.MemberId, model.SessionId)
            ? Ok(new { success = true, message = "Presença registrada." })
            : Conflict(new { success = false, message = "Não foi possível registrar a presença." });
    }

    [HttpDelete("{sessionId:int}/{memberId:int}")]
    [Authorize(Roles = "SuperAdmin,Admin,Gerente,Recepcionista")]
    public IActionResult Cancel(int sessionId, int memberId)
    {
        if (sessionId <= 0 || memberId <= 0)
            return BadRequest(new { success = false, message = "Identificadores inválidos." });

        return _bookingService.CancelBooking(memberId, sessionId)
            ? Ok(new { success = true, message = "Agendamento cancelado." })
            : Conflict(new { success = false, message = "Não foi possível cancelar o agendamento." });
    }
}
