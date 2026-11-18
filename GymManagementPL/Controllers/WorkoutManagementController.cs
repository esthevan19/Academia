using GymManagementDAL.Data.Contexts;
using GymManagementDAL.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GymManagementPL.Controllers;

[Authorize(Roles = "SuperAdmin,Admin,Gerente,Instrutor")]
public class WorkoutManagementController : Controller
{
    private readonly GymDbContext _db;
    public WorkoutManagementController(GymDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        ViewBag.Members = await _db.Members.AsNoTracking().OrderBy(x => x.Name)
            .ToListAsync();
        return View(await _db.Workouts.AsNoTracking().Include(x => x.Member)
            .Include(x => x.Exercises).OrderByDescending(x => x.CreatedAt).ToListAsync());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int memberId, string name, string? category,
        string? letter, string? notes, List<string> exerciseName, List<int> sets,
        List<string> reps, List<string> loadKg, List<int> restSeconds)
    {
        if (memberId <= 0 || string.IsNullOrWhiteSpace(name) || name.Length > 150 ||
            category?.Length > 80 || letter?.Length > 10 || notes?.Length > 1000 ||
            exerciseName is null || exerciseName.Count is < 1 or > 40 ||
            sets is null || reps is null || loadKg is null || restSeconds is null ||
            sets.Count != exerciseName.Count || reps.Count != exerciseName.Count ||
            loadKg.Count != exerciseName.Count || restSeconds.Count != exerciseName.Count ||
            !await _db.Members.AnyAsync(x => x.Id == memberId))
        {
            TempData["WorkoutError"] = "Confira os dados do treino, os exercícios e o aluno selecionado.";
            return RedirectToAction(nameof(Index));
        }
        var workout = new Workout
        {
            MemberId = memberId, Name = name.Trim(),
            Category = string.IsNullOrWhiteSpace(category) ? "Musculação" : category.Trim(),
            Letter = string.IsNullOrWhiteSpace(letter) ? "A" : letter.Trim(),
            Notes = notes?.Trim()
        };
        for (var i = 0; i < exerciseName.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(exerciseName[i]) || exerciseName[i].Length > 150 ||
                string.IsNullOrWhiteSpace(reps[i]) || reps[i].Length > 50 ||
                sets[i] is < 1 or > 30 || restSeconds[i] is < 0 or > 3600 ||
                !TryParseLoad(loadKg[i], out var parsedLoad))
            {
                TempData["WorkoutError"] = "Confira as séries, repetições, cargas e descansos.";
                return RedirectToAction(nameof(Index));
            }
            workout.Exercises.Add(new WorkoutExercise
            {
                Name = exerciseName[i].Trim(), Sets = sets[i], Reps = reps[i].Trim(),
                LoadKg = parsedLoad, RestSeconds = restSeconds[i], SortOrder = i
            });
        }
        _db.Workouts.Add(workout);
        await _db.SaveChangesAsync();
        TempData["WorkoutSuccess"] = "Treino cadastrado e vinculado ao aluno.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var workout = await _db.Workouts.FindAsync(id);
        if (workout is null) return NotFound();
        _db.Workouts.Remove(workout);
        await _db.SaveChangesAsync();
        TempData["WorkoutSuccess"] = "Treino excluído.";
        return RedirectToAction(nameof(Index));
    }
    // Aceita tanto 2,5 quanto 2.5 sem interpretar ponto como separador de milhar.
    private static bool TryParseLoad(string? text, out decimal? loadKg)
    {
        loadKg = null;
        if (string.IsNullOrWhiteSpace(text)) return true;
        var normalized = text.Trim().Replace(',', '.');
        if (!decimal.TryParse(normalized,
            System.Globalization.NumberStyles.AllowDecimalPoint | System.Globalization.NumberStyles.AllowLeadingSign,
            System.Globalization.CultureInfo.InvariantCulture, out var parsed) ||
            parsed < 0 || parsed > 999999)
            return false;

        loadKg = parsed;
        return true;
    }

}
