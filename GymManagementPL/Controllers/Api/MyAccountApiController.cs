using System.Security.Claims;
using GymManagementBLL.Services.Classes;
using GymManagementBLL.Services.Interfaces;
using GymManagementBLL.ViewModels.BookingViewModels;
using GymManagementDAL.Data.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymManagementPL.Controllers.Api;

[ApiController]
[Route("api/me")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Aluno")]
public class MyAccountApiController : ControllerBase
{
    private readonly IMemberService _memberService;
    private readonly IBookingService _bookingService;
    private readonly GymDbContext _db;

    public MyAccountApiController(IMemberService memberService, IBookingService bookingService, GymDbContext db)
    {
        _memberService = memberService;
        _bookingService = bookingService;
        _db = db;
    }

    public sealed record StudentBookingRequest(int SessionId);

    private int? GetLinkedMemberId()
    {
        var value = User.FindFirstValue("gymflow:member_id");
        if (!int.TryParse(value, out var memberId) || memberId <= 0) return null;
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (string.IsNullOrWhiteSpace(userId)) return null;

        // O JWT pode continuar válido após o SuperAdmin revogar o vínculo.
        // Confirme no banco que a conta ainda é proprietária do cadastro.
        return _db.UserClaims.AsNoTracking().Any(claim =>
            claim.UserId == userId && claim.ClaimType == "gymflow:member_id" &&
            claim.ClaimValue == memberId.ToString())
            ? memberId : null;
    }

    [HttpGet]
    public IActionResult GetMyProfile()
    {
        var memberId = GetLinkedMemberId();
        if (memberId is null)
            return Conflict(new { success = false, message = "Sua conta ainda não está vinculada ao cadastro de aluno. Peça ao SuperAdmin para fazer o vínculo e entre novamente no aplicativo." });

        var member = _memberService.GetMemberDetails(memberId.Value);
        return member is null
            ? NotFound(new { success = false, message = "Cadastro de aluno vinculado não encontrado." })
            : Ok(new { success = true, member });
    }


    // Evolução com dados REAIS: presenças em aulas e fichas de treino.
    // Não representar fichas como treinos executados nem reservas como presença.
    [HttpGet("progress")]
    public async Task<IActionResult> Progress(CancellationToken cancellationToken)
    {
        var memberId = GetLinkedMemberId();
        if (memberId is null)
            return Conflict(new { success = false, message = "Sua conta ainda não está vinculada a um aluno. Solicite o vínculo ao SuperAdmin." });

        var now = BrazilClock.Now;
        var member = await _db.Members.AsNoTracking()
            .Where(x => x.Id == memberId.Value)
            .Select(x => new { x.Id, x.Name }).FirstOrDefaultAsync(cancellationToken);
        if (member is null)
            return NotFound(new { success = false, message = "Cadastro de aluno não encontrado." });

        var currentPlan = await _db.Memberships.AsNoTracking()
            .Where(x => x.MemberId == memberId && x.CreatedAt <= now && x.EndDate >= now)
            .OrderByDescending(x => x.EndDate)
            .Select(x => new { planName = x.Plan.Name, endDate = x.EndDate })
            .FirstOrDefaultAsync(cancellationToken);

        var nextClass = await _db.Bookings.AsNoTracking()
            .Where(x => x.MemberId == memberId && x.Session.StartDate > now)
            .OrderBy(x => x.Session.StartDate)
            .Select(x => new {
                sessionId = x.SessionId, description = x.Session.Description,
                startDate = x.Session.StartDate, endDate = x.Session.EndDate
            }).FirstOrDefaultAsync(cancellationToken);

        var allAttendance = await _db.Bookings.AsNoTracking()
            .CountAsync(x => x.MemberId == memberId && x.IsAttended, cancellationToken);
        var attendanceLast30Days = await _db.Bookings.AsNoTracking()
            .CountAsync(x => x.MemberId == memberId && x.IsAttended &&
                x.Session.StartDate >= now.AddDays(-30) && x.Session.StartDate <= now,
                cancellationToken);
        var sheets = await _db.Workouts.AsNoTracking()
            .CountAsync(x => x.MemberId == memberId, cancellationToken);
        var exercises = await _db.WorkoutExercises.AsNoTracking()
            .CountAsync(x => x.Workout.MemberId == memberId, cancellationToken);

        int? remainingDays = currentPlan is null ? null :
            Math.Max(0, (int)Math.Ceiling((currentPlan.endDate - now).TotalDays));
        return Ok(new
        {
            success = true,
            progress = new
            {
                member,
                membership = currentPlan is null ? null : (object)new
                {
                    currentPlan.planName, currentPlan.endDate, remainingDays
                },
                attendance = new { totalClassesAttended = allAttendance, last30Days = attendanceLast30Days },
                training = new { workoutSheets = sheets, prescribedExercises = exercises },
                nextClass,
                reminder = currentPlan is null
                    ? "Nenhuma matrícula vigente encontrada. Procure a recepção."
                    : remainingDays <= 7
                        ? "Seu plano vence em " + remainingDays + " dia(s). Consulte a recepção para renovar."
                        : nextClass is not null
                            ? "Você possui uma aula agendada. Confira o horário na agenda."
                            : "Consulte seus treinos e reserve uma aula coletiva."
            }
        });
    }

    [HttpGet("bookings")]
    public IActionResult GetMyBookings()
    {
        var memberId = GetLinkedMemberId();
        if (memberId is null)
            return Conflict(new { success = false, message = "Sua conta ainda não está vinculada ao cadastro de aluno." });

        return Ok(new { success = true, bookings = _bookingService.GetMemberBookings(memberId.Value) });
    }

    [HttpPost("bookings")]
    public IActionResult CreateMyBooking([FromBody] StudentBookingRequest request)
    {
        var memberId = GetLinkedMemberId();
        if (memberId is null)
            return Conflict(new { success = false, message = "Sua conta ainda não está vinculada ao cadastro de aluno." });
        if (request.SessionId <= 0)
            return BadRequest(new { success = false, message = "Selecione uma sessão válida." });

        var booking = new CreateBookingViewModel { MemberId = memberId.Value, SessionId = request.SessionId };
        return _bookingService.CreateNewBooking(booking)
            ? Ok(new { success = true, message = "Agendamento realizado com sucesso." })
            : Conflict(new { success = false, message = "Não foi possível agendar. Confira sua matrícula ativa, vagas e horário da sessão." });
    }

    [HttpDelete("bookings/{sessionId:int}")]
    public IActionResult CancelMyBooking(int sessionId)
    {
        var memberId = GetLinkedMemberId();
        if (memberId is null)
            return Conflict(new { success = false, message = "Sua conta ainda não está vinculada ao cadastro de aluno." });
        if (sessionId <= 0)
            return BadRequest(new { success = false, message = "Sessão inválida." });

        return _bookingService.CancelBooking(memberId.Value, sessionId)
            ? Ok(new { success = true, message = "Agendamento cancelado." })
            : Conflict(new { success = false, message = "Não foi possível cancelar. O cancelamento só é permitido antes do início da sessão." });
    }
}
