using System.Security.Claims;
using GymManagementDAL.Data.Contexts;
using GymManagementPL.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRCoder;

namespace GymManagementPL.Controllers.Api;

[ApiController]
[Route("api/class-checkin")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public sealed class ClassCheckInApiController : ControllerBase
{
    private readonly ClassAttendanceService _attendance;
    private readonly GymDbContext _db;
    public ClassCheckInApiController(ClassAttendanceService attendance, GymDbContext db)
    {
        _attendance = attendance;
        _db = db;
    }

    public sealed record ConfirmRequest(int SessionId, string? Code);

    [HttpGet("mine/{sessionId:int}")]
    [Authorize(Roles = "Aluno")]
    public async Task<IActionResult> MyTicket(int sessionId, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(User.FindFirstValue("gymflow:member_id"), out var memberId) ||
            memberId <= 0 || string.IsNullOrWhiteSpace(userId) ||
            !await _db.UserClaims.AsNoTracking().AnyAsync(x =>
                x.UserId == userId && x.ClaimType == "gymflow:member_id" &&
                x.ClaimValue == memberId.ToString(), ct))
            return Conflict(new { success = false, message = "Seu cadastro precisa estar vinculado a esta conta." });

        var result = await _attendance.IssueAsync(memberId, sessionId, ct);
        if (!result.Success) return Conflict(new { success = false, message = result.Message });
        // O QR inclui apenas a aula e o código de curta duração; nenhum dado do aluno.
        var payload = $"GYMFLOW-CLASS:{sessionId}:{result.Code}";
        var png = PngByteQRCodeHelper.GetQRCode(payload, QRCodeGenerator.ECCLevel.Q, 6);
        return Ok(new
        {
            success = true, sessionId, result.SessionName, result.Code,
            result.ExpiresInSeconds, result.Message,
            qrImage = "data:image/png;base64," + Convert.ToBase64String(png)
        });
    }

    [HttpGet("active-sessions")]
    [Authorize(Roles = "SuperAdmin,Admin,Gerente,Recepcionista,Instrutor")]
    public async Task<IActionResult> ActiveSessions(CancellationToken ct) =>
        Ok(new { success = true, sessions = await _attendance.ActiveSessionsAsync(ct) });

    [HttpPost("confirm")]
    [Authorize(Roles = "SuperAdmin,Admin,Gerente,Recepcionista,Instrutor")]
    public async Task<IActionResult> Confirm([FromBody] ConfirmRequest input, CancellationToken ct)
    {
        if (input is null) return BadRequest(new { success = false, message = "Informe os dados da presença." });
        var result = await _attendance.ConfirmAsync(input.SessionId, input.Code, ct);
        return result.Success
            ? Ok(new { success = true, message = result.Message, memberName = result.MemberName })
            : Conflict(new { success = false, message = result.Message });
    }
}
