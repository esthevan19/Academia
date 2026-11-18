using System.Security.Claims;
using GymManagementDAL.Data.Contexts;
using GymManagementDAL.Entities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GymManagementPL.Controllers.Api;

[ApiController]
[Route("api/workouts")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class WorkoutsController : ControllerBase
{
    private readonly GymDbContext _db;
    public WorkoutsController(GymDbContext db) => _db = db;

    public sealed record ExerciseInput(string Name, int Sets, string Reps, decimal? LoadKg, int RestSeconds, string? Notes);
    public sealed record WorkoutInput(int MemberId, string Name, string? Category, string? Letter, string? Notes, List<ExerciseInput> Exercises);

    // Nunca confiar apenas no vínculo gravado em um JWT antigo.
    // O SuperAdmin pode revogar ou alterar o cadastro durante a validade do token.
    private async Task<int?> LinkedMemberIdAsync()
    {
        if (!int.TryParse(User.FindFirstValue("gymflow:member_id"), out var memberId) || memberId <= 0)
            return null;

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (string.IsNullOrWhiteSpace(userId))
            return null;

        var linked = await _db.UserClaims.AsNoTracking().AnyAsync(claim =>
            claim.UserId == userId &&
            claim.ClaimType == "gymflow:member_id" &&
            claim.ClaimValue == memberId.ToString());
        return linked ? memberId : null;
    }

    private bool CanManage() => User.IsInRole("SuperAdmin") || User.IsInRole("Admin") ||
                                User.IsInRole("Gerente") || User.IsInRole("Instrutor");

    private IQueryable<Workout> AccessibleWorkouts(int? linkedMemberId)
    {
        var query = _db.Workouts.AsNoTracking().Include(x => x.Exercises).AsQueryable();
        if (CanManage()) return query;
        return User.IsInRole("Aluno") && linkedMemberId.HasValue
            ? query.Where(x => x.MemberId == linkedMemberId.Value)
            : query.Where(x => false);
    }

    private static object ToResponse(Workout workout) => new
    {
        workout.Id, workout.MemberId, workout.Name, workout.Category, workout.Letter, workout.Notes,
        exercises = workout.Exercises.OrderBy(e => e.SortOrder).Select(e => new
        {
            e.Id, e.Name, e.Sets, e.Reps, e.LoadKg, e.RestSeconds, e.SortOrder, e.Notes
        }).ToList()
    };

    // Cadastro mínimo de alunos para selecionar a ficha no aplicativo,
    // sem expor dados pessoais ou informações de saúde.
    [HttpGet("member-lookup")]
    public async Task<IActionResult> GetMemberLookup()
    {
        if (!CanManage()) return Forbid();
        var members = await _db.Members.AsNoTracking()
            .OrderBy(member => member.Name)
            .Select(member => new { member.Id, member.Name })
            .ToListAsync();
        return Ok(new { success = true, members });
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int? memberId)
    {
        if (!CanManage() && !User.IsInRole("Aluno")) return Forbid();
        var linkedMemberId = CanManage() ? null : await LinkedMemberIdAsync();
        if (!CanManage() && linkedMemberId is null)
            return Conflict(new { success = false, message = "Vínculo com cadastro de aluno ausente ou revogado. Entre novamente após falar com o SuperAdmin." });
        var query = AccessibleWorkouts(linkedMemberId);
        if (memberId.HasValue) query = query.Where(x => x.MemberId == memberId.Value);
        var data = await query.OrderByDescending(x => x.CreatedAt).ToListAsync();
        return Ok(new { success = true, workouts = data.Select(ToResponse).ToList() });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        if (!CanManage() && !User.IsInRole("Aluno")) return Forbid();
        var linkedMemberId = CanManage() ? null : await LinkedMemberIdAsync();
        if (!CanManage() && linkedMemberId is null)
            return Conflict(new { success = false, message = "Vínculo com cadastro de aluno ausente ou revogado. Entre novamente após falar com o SuperAdmin." });
        var workout = await AccessibleWorkouts(linkedMemberId).FirstOrDefaultAsync(x => x.Id == id);
        return workout is null ? NotFound(new { success = false, message = "Treino não encontrado." })
            : Ok(new { success = true, workout = ToResponse(workout) });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] WorkoutInput input)
    {
        if (!CanManage()) return Forbid();
        var validation = Validate(input);
        if (validation is not null) return BadRequest(new { success = false, message = validation });
        if (!await _db.Members.AnyAsync(x => x.Id == input.MemberId))
            return NotFound(new { success = false, message = "Aluno não encontrado." });
        var workout = Build(input);
        _db.Workouts.Add(workout);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = workout.Id }, new { success = true, workout = ToResponse(workout) });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] WorkoutInput input)
    {
        if (!CanManage()) return Forbid();
        var validation = Validate(input);
        if (validation is not null) return BadRequest(new { success = false, message = validation });
        var workout = await _db.Workouts.Include(x => x.Exercises).FirstOrDefaultAsync(x => x.Id == id);
        if (workout is null) return NotFound(new { success = false, message = "Treino não encontrado." });
        if (!await _db.Members.AnyAsync(x => x.Id == input.MemberId))
            return NotFound(new { success = false, message = "Aluno não encontrado." });
        workout.MemberId = input.MemberId;
        workout.Name = input.Name.Trim();
        workout.Category = string.IsNullOrWhiteSpace(input.Category) ? "Musculação" : input.Category.Trim();
        workout.Letter = string.IsNullOrWhiteSpace(input.Letter) ? "A" : input.Letter.Trim();
        workout.Notes = input.Notes?.Trim();
        workout.UpdatedAt = DateTime.UtcNow;
        _db.WorkoutExercises.RemoveRange(workout.Exercises);
        workout.Exercises = BuildExercises(input.Exercises);
        await _db.SaveChangesAsync();
        return Ok(new { success = true, workout = ToResponse(workout) });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (!CanManage()) return Forbid();
        var workout = await _db.Workouts.FindAsync(id);
        if (workout is null) return NotFound(new { success = false, message = "Treino não encontrado." });
        _db.Workouts.Remove(workout);
        await _db.SaveChangesAsync();
        return Ok(new { success = true, message = "Treino removido." });
    }

    private static string? Validate(WorkoutInput? input)
    {
        if (input is null) return "Informe os dados do treino.";
        if (input.MemberId <= 0) return "Informe um aluno válido.";
        if (string.IsNullOrWhiteSpace(input.Name) || input.Name.Length > 150) return "Nome do treino inválido.";
        if (input.Category?.Length > 80 || input.Letter?.Length > 10 || input.Notes?.Length > 1000)
            return "Um dos campos do treino excede o tamanho permitido.";
        if (input.Exercises is null || input.Exercises.Count is < 1 or > 40)
            return "Informe entre 1 e 40 exercícios.";
        foreach (var e in input.Exercises)
            if (e is null || string.IsNullOrWhiteSpace(e.Name) || e.Name.Length > 150 ||
                string.IsNullOrWhiteSpace(e.Reps) || e.Reps.Length > 50 ||
                e.Sets is < 1 or > 30 || e.RestSeconds is < 0 or > 3600 ||
                e.LoadKg is < 0 or > 999999 || e.Notes?.Length > 1000)
                return "Confira nome, séries, repetições, carga e descanso dos exercícios.";
        return null;
    }

    private static List<WorkoutExercise> BuildExercises(List<ExerciseInput> exercises) =>
        exercises.Select((e, i) => new WorkoutExercise
        {
            Name = e.Name.Trim(), Sets = e.Sets, Reps = e.Reps.Trim(), LoadKg = e.LoadKg,
            RestSeconds = e.RestSeconds, Notes = e.Notes?.Trim(), SortOrder = i
        }).ToList();

    private static Workout Build(WorkoutInput input) => new()
    {
        MemberId = input.MemberId,
        Name = input.Name.Trim(),
        Category = string.IsNullOrWhiteSpace(input.Category) ? "Musculação" : input.Category.Trim(),
        Letter = string.IsNullOrWhiteSpace(input.Letter) ? "A" : input.Letter.Trim(),
        Notes = input.Notes?.Trim(),
        Exercises = BuildExercises(input.Exercises)
    };
}
