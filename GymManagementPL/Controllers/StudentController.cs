using System.Security.Claims;
using GymManagementDAL.Data.Contexts;
using GymManagementDAL.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GymManagementPL.Controllers;

[Authorize(Roles = "Aluno")]
public sealed class StudentController : Controller
{
    private readonly GymDbContext _db;

    public StudentController(GymDbContext db) => _db = db;

    // O ID da matrícula nunca é recebido por query string: vem do vínculo
    // atribuído por um SuperAdmin à conta autenticada.
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var claim = User.FindFirstValue("gymflow:member_id");
        if (!int.TryParse(claim, out var memberId) || memberId <= 0)
        {
            ViewBag.NeedsLink = true;
            return View(new List<Workout>());
        }

        if (!await _db.Members.AnyAsync(m => m.Id == memberId))
        {
            ViewBag.NeedsLink = true;
            return View(new List<Workout>());
        }

        var workouts = await _db.Workouts.AsNoTracking()
            .Include(w => w.Exercises)
            .Where(w => w.MemberId == memberId)
            .OrderByDescending(w => w.CreatedAt)
            .ToListAsync();
        ViewBag.NeedsLink = false;
        return View(workouts);
    }
}
