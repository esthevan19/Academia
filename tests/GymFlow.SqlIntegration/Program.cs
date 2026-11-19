using GymManagementDAL.Data.Contexts;
using GymManagementDAL.Entities;
using GymManagementDAL.Entities.Enums;
using Microsoft.EntityFrameworkCore;

var connection = Environment.GetEnvironmentVariable("GYMFLOW_TEST_SQL");
if (string.IsNullOrWhiteSpace(connection) || !connection.Contains("Database=GymFlowIntegration", StringComparison.OrdinalIgnoreCase))
    throw new InvalidOperationException("Informe somente uma conexão SQL de teste com Database=GymFlowIntegration. Nunca utilize produção.");

var options = new DbContextOptionsBuilder<GymDbContext>()
    .UseSqlServer(connection, sql => sql.EnableRetryOnFailure(3))
    .Options;

// O banco é criado e apagado exclusivamente no SQL Server efêmero deste workflow.
using (var db = new GymDbContext(options))
{
    Exception? last = null;
    var created = false;
    for (var attempt = 0; attempt < 20 && !created; attempt++)
    {
        try
        {
            db.Database.Migrate();
            created = true;
        }
        catch (Exception ex)
        {
            last = ex;
            Thread.Sleep(TimeSpan.FromSeconds(3));
        }
    }
    if (!created) throw new Exception("SQL Server de testes indisponível.", last);
}

try
{
    int memberId;
    int planId;
    using (var db = new GymDbContext(options))
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var member = new Member
        {
            Name = "Aluno Integração",
            Email = $"teste-{suffix}@exemplo.com",
            Phone = "51987654321",
            DateOfBirth = new DateOnly(2000, 1, 1),
            Gender = Gender.Male,
            Photo = "default-avatar.png",
            Address = new Address { BuildingNumber = 123, Street = "Rua Teste", City = "Parobé" },
            HealthRecord = new HealthRecord { Weight = 76.5m, Height = 1.76m, BloodType = "O+" }
        };
        var plan = new Plan
        {
            Name = "Plano de integração",
            Description = "Testes automatizados",
            DurationDays = 30,
            Price = 99m,
            IsActive = true
        };
        db.Members.Add(member);
        db.Plans.Add(plan);
        db.SaveChanges();
        memberId = member.Id;
        planId = plan.Id;
        if (memberId <= 0 || planId <= 0)
            throw new Exception("IDs de aluno/plano não foram persistidos.");

        db.Memberships.Add(new Membership
        {
            MemberId = memberId,
            PlanId = planId,
            CreatedAt = DateTime.Today,
            EndDate = DateTime.Today.AddDays(30)
        });
        db.Workouts.Add(new Workout
        {
            MemberId = memberId,
            Name = "Treino A",
            Category = "Musculação",
            Letter = "A",
            Exercises = new List<WorkoutExercise>
            {
                new() { Name = "Agachamento", Sets = 3, Reps = "12", RestSeconds = 60, SortOrder = 0 }
            }
        });
        db.SaveChanges();
    }

    using (var verify = new GymDbContext(options))
    {
        var member = verify.Members.AsNoTracking().Include(x => x.HealthRecord).SingleOrDefault(x => x.Id == memberId);
        var enrollment = verify.Memberships.AsNoTracking().SingleOrDefault(x => x.MemberId == memberId && x.PlanId == planId);
        var workout = verify.Workouts.AsNoTracking().Include(x => x.Exercises).SingleOrDefault(x => x.MemberId == memberId);
        var plan = verify.Plans.AsNoTracking().SingleOrDefault(x => x.Id == planId);

        if (member is null || member.HealthRecord?.BloodType != "O+" ||
            enrollment is null || plan is null || !plan.IsActive ||
            workout is null || workout.Exercises.Count != 1)
            throw new Exception("Falha de gravação ou leitura de aluno, saúde, plano, matrícula ou treino.");

        Console.WriteLine("SQL Server: aluno, ficha de saúde, plano, matrícula e treino gravados e lidos de outra conexão.");
    }
}
finally
{
    using var cleanup = new GymDbContext(options);
    cleanup.Database.EnsureDeleted();
}
