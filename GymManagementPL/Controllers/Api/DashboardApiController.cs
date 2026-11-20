using GymManagementBLL.Services.Classes;
using GymManagementDAL.Data.Contexts;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GymManagementPL.Controllers.Api;

/// <summary>
/// Resumo de operação para colaboradores autorizados.
/// Os valores são contagens de dados REAIS; não há pagamentos registrados neste banco.
/// </summary>
[ApiController]
[Route("api/dashboard")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
    Roles = "SuperAdmin,Admin,Gerente,Recepcionista")]
public sealed class DashboardApiController : ControllerBase
{
    private readonly GymDbContext _db;
    public DashboardApiController(GymDbContext db) => _db = db;

    [HttpGet("overview")]
    public async Task<IActionResult> Overview(CancellationToken cancellationToken)
    {
        var now = BrazilClock.Now;
        var soon = now.AddDays(7);
        var today = now.Date;

        // Não conta alunos com matrículas vencidas ou agendadas como ativos.
        var activeMemberIds = _db.Memberships.AsNoTracking()
            .Where(x => x.CreatedAt <= now && x.EndDate >= now)
            .Select(x => x.MemberId).Distinct();

        var expiringQuery = _db.Memberships.AsNoTracking()
            .Where(x => x.CreatedAt <= now && x.EndDate >= now && x.EndDate <= soon);

        var nextExpiring = await expiringQuery
            .OrderBy(x => x.EndDate)
            .Select(x => new
            {
                memberId = x.MemberId,
                memberName = x.Member.Name,
                planName = x.Plan.Name,
                endDate = x.EndDate
            }).Take(8).ToListAsync(cancellationToken);

        var overview = new
        {
            totalMembers = await _db.Members.AsNoTracking().CountAsync(cancellationToken),
            activeMembers = await activeMemberIds.CountAsync(cancellationToken),
            expiringInSevenDays = await expiringQuery.Select(x => x.MemberId).Distinct()
                .CountAsync(cancellationToken),
            totalTrainers = await _db.Trainers.AsNoTracking().CountAsync(cancellationToken),
            classesToday = await _db.Sessions.AsNoTracking()
                .CountAsync(x => x.StartDate >= today && x.StartDate < today.AddDays(1), cancellationToken),
            upcomingClasses = await _db.Sessions.AsNoTracking()
                .CountAsync(x => x.StartDate > now, cancellationToken),
            upcomingExpirations = nextExpiring,
            financialDataAvailable = false,
            financialNotice = "Cobranças e pagamentos não são registrados neste banco. Não exibir receita como valor recebido."
        };
        return Ok(new { success = true, overview });
    }
}
