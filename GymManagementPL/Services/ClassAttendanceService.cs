using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using GymManagementBLL.Services.Classes;
using GymManagementDAL.Data.Contexts;
using Microsoft.EntityFrameworkCore;

namespace GymManagementPL.Services;

/// <summary>
/// Presença em aula coletiva: código de 8 dígitos válido por até 120 segundos,
/// associado à matrícula autenticada + sessão. Não substitui o controle de
/// entrada física da academia e não cria alterações de esquema no SQL.
/// </summary>
public sealed class ClassAttendanceService
{
    private const long SecondsPerCode = 120;
    private readonly GymDbContext _db;
    private readonly string? _secret;

    public ClassAttendanceService(GymDbContext db, IConfiguration configuration)
    {
        _db = db;
        _secret = configuration["Jwt:Key"];
    }

    public sealed record TicketResult(bool Success, string Message, string? Code = null,
        int ExpiresInSeconds = 0, string? SessionName = null);
    public sealed record CheckInResult(bool Success, string Message, string? MemberName = null);
    public sealed record SessionItem(int Id, string Description, DateTime StartDate, DateTime EndDate);

    private bool Configured => !string.IsNullOrWhiteSpace(_secret) && Encoding.UTF8.GetByteCount(_secret) >= 32;

    private string CodeFor(int memberId, int sessionId, long bucket)
    {
        // JWT secret é usado apenas como material para uma chave derivada por propósito.
        // O identificador do aluno jamais é enviado dentro do QR.
        var key = HMACSHA256.HashData(
            Encoding.UTF8.GetBytes(_secret!),
            Encoding.UTF8.GetBytes("GymFlow-ClassAttendance-v1"));
        var message = Encoding.UTF8.GetBytes($"{memberId}:{sessionId}:{bucket}");
        var digest = HMACSHA256.HashData(key, message);
        var value = BinaryPrimitives.ReadUInt32BigEndian(digest.AsSpan(0, 4)) % 100_000_000;
        return value.ToString("D8", CultureInfo.InvariantCulture);
    }

    public async Task<TicketResult> IssueAsync(int memberId, int sessionId, CancellationToken ct = default)
    {
        if (!Configured) return new(false, "Serviço de presença indisponível: configuração da academia incompleta.");
        if (memberId <= 0 || sessionId <= 0) return new(false, "Aluno ou aula inválida.");
        var now = BrazilClock.Now;
        var reservation = await _db.Bookings.AsNoTracking()
            .Where(x => x.MemberId == memberId && x.SessionId == sessionId &&
                        x.Session.StartDate <= now.AddMinutes(15) && x.Session.EndDate >= now)
            .Select(x => new { x.IsAttended, Name = x.Session.Description })
            .SingleOrDefaultAsync(ct);
        if (reservation is null)
            return new(false, "O código ficará disponível apenas 15 minutos antes da aula e durante seu horário, com reserva válida.");
        if (reservation.IsAttended)
            return new(false, "Sua presença nesta aula já foi confirmada.");

        var seconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var period = seconds / SecondsPerCode;
        var ttl = (int)(SecondsPerCode - seconds % SecondsPerCode);
        return new(true, "Apresente o QR ou código ao instrutor durante a aula.",
            CodeFor(memberId, sessionId, period), ttl, reservation.Name);
    }

    public async Task<IReadOnlyList<SessionItem>> ActiveSessionsAsync(CancellationToken ct = default)
    {
        var now = BrazilClock.Now;
        return await _db.Sessions.AsNoTracking()
            .Where(x => x.StartDate <= now && x.EndDate >= now)
            .OrderBy(x => x.EndDate)
            .Select(x => new SessionItem(x.Id, x.Description, x.StartDate, x.EndDate))
            .ToListAsync(ct);
    }

    public async Task<CheckInResult> ConfirmAsync(int sessionId, string? code, CancellationToken ct = default)
    {
        if (!Configured) return new(false, "Presença indisponível: configuração da academia incompleta.");
        if (sessionId <= 0 || code is null || code.Length != 8 || !code.All(char.IsAsciiDigit))
            return new(false, "Selecione uma aula e informe o código de 8 dígitos.");

        var now = BrazilClock.Now;
        var isRunning = await _db.Sessions.AsNoTracking().AnyAsync(
            x => x.Id == sessionId && x.StartDate <= now && x.EndDate >= now, ct);
        if (!isRunning) return new(false, "Só é possível confirmar a presença durante a aula.");

        var bookings = await _db.Bookings
            .Include(x => x.Member)
            .Where(x => x.SessionId == sessionId)
            .Take(100)
            .ToListAsync(ct);
        var period = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / SecondsPerCode;
        var input = Encoding.UTF8.GetBytes(code);
        var matches = bookings.Where(x => CryptographicOperations.FixedTimeEquals(
            input, Encoding.UTF8.GetBytes(CodeFor(x.MemberId, sessionId, period)))).ToList();
        if (matches.Count != 1) return new(false, "Código inválido ou expirado. Solicite outro código ao aluno.");

        var booking = matches[0];
        if (booking.IsAttended) return new(false, "Presença já registrada para este aluno.");
        var activePlan = await _db.Memberships.AsNoTracking()
            .AnyAsync(x => x.MemberId == booking.MemberId && x.CreatedAt <= now && x.EndDate >= now, ct);
        if (!activePlan) return new(false, "Aluno sem matrícula vigente. Procure a recepção.");

        booking.IsAttended = true;
        booking.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return new(true, "Presença registrada com sucesso.", booking.Member.Name);
    }
}
