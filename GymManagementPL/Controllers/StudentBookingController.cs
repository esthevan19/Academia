using System.Security.Claims;
using GymManagementBLL.Services.Classes;
using GymManagementBLL.Services.Interfaces;
using GymManagementBLL.ViewModels.BookingViewModels;
using GymManagementDAL.Data.Contexts;
using GymManagementPL.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GymManagementPL.Controllers;

/// <summary>
/// Reservas do próprio aluno pela sessão Identity do site MVC.
/// O ID do aluno vem exclusivamente do vínculo feito pelo SuperAdmin.
/// </summary>
[Authorize(Roles = "Aluno")]
public sealed class StudentBookingController : Controller
{
    private readonly IBookingService _bookings;
    private readonly GymDbContext _db;

    public StudentBookingController(IBookingService bookings, GymDbContext db)
    {
        _bookings = bookings;
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var memberId = await GetLinkedMemberId();
        if (memberId is null)
            return View(new StudentBookingPageViewModel { NeedsLink = true });

        var hasActiveMembership = await _db.Memberships.AsNoTracking()
            .AnyAsync(m => m.MemberId == memberId.Value && m.EndDate >= BrazilClock.Now);

        var bookings = _bookings.GetMemberBookings(memberId.Value)
            .OrderByDescending(b => b.StartDate).ToList();
        var sessions = _bookings.GetAllSessions()
            .Where(s => s.Status == "Upcoming")
            .OrderBy(s => s.StartDate).ToList();

        return View(new StudentBookingPageViewModel
        {
            MyBookings = bookings,
            AvailableSessions = sessions,
            HasActiveMembership = hasActiveMembership
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reserve(int sessionId)
    {
        var memberId = await GetLinkedMemberId();
        if (memberId is null)
        {
            TempData["ErrorMessage"] = "Vincule sua conta ao cadastro de aluno antes de reservar.";
            return RedirectToAction(nameof(Index));
        }

        if (sessionId <= 0)
        {
            TempData["ErrorMessage"] = "Aula inválida. Escolha uma aula disponível.";
            return RedirectToAction(nameof(Index));
        }

        var result = _bookings.CreateNewBooking(new CreateBookingViewModel
        {
            MemberId = memberId.Value,
            SessionId = sessionId
        });

        TempData[result ? "SuccessMessage" : "ErrorMessage"] = result
            ? "Reserva confirmada! A aula aparece em suas reservas."
            : "Não foi possível reservar. Confira se a matrícula está ativa, se há vagas e se a aula ainda não começou.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int sessionId)
    {
        var memberId = await GetLinkedMemberId();
        if (memberId is null || sessionId <= 0)
        {
            TempData["ErrorMessage"] = "Não foi possível identificar sua reserva.";
            return RedirectToAction(nameof(Index));
        }

        var result = _bookings.CancelBooking(memberId.Value, sessionId);
        TempData[result ? "SuccessMessage" : "ErrorMessage"] = result
            ? "Reserva cancelada com sucesso."
            : "Não foi possível cancelar. O cancelamento é permitido somente antes do início da aula.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<int?> GetLinkedMemberId()
    {
        var claim = User.FindFirstValue("gymflow:member_id");
        if (!int.TryParse(claim, out var memberId) || memberId <= 0)
            return null;
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) return null;

        // O vínculo pode ser removido pelo SuperAdmin enquanto o cookie antigo ainda existe.
        // Revalidar no banco evita acessar ou reservar aulas usando um vínculo revogado.
        var isStillLinked = await _db.UserClaims.AsNoTracking().AnyAsync(claim =>
            claim.UserId == userId && claim.ClaimType == "gymflow:member_id" &&
            claim.ClaimValue == memberId.ToString());
        if (!isStillLinked) return null;

        return await _db.Members.AsNoTracking().AnyAsync(m => m.Id == memberId)
            ? memberId : null;
    }
}
