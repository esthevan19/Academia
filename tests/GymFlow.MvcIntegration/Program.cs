using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using GymManagementDAL.Data.Contexts;
using GymManagementDAL.Entities;
using GymManagementDAL.Entities.Enums;
using GymManagementBLL.Services.Classes;
using GymManagementPL.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

var connection = Environment.GetEnvironmentVariable("GYMFLOW_TEST_SQL") ?? "";
if (!connection.Contains("Database=GymFlowMvcIntegration", StringComparison.OrdinalIgnoreCase))
    throw new InvalidOperationException("Use exclusivamente o banco efêmero GymFlowMvcIntegration.");

var options = new DbContextOptionsBuilder<GymDbContext>()
    .UseSqlServer(connection, sql => sql.EnableRetryOnFailure(3)).Options;

// A suíte de testes só roda em banco local do GitHub Actions e apaga todos os dados.
using (var db = new GymDbContext(options))
{
    Exception? last = null;
    var created = false;
    for (var attempt = 0; attempt < 20 && !created; attempt++)
    {
        try { db.Database.EnsureCreated(); created = true; }
        catch (Exception ex) { last = ex; Thread.Sleep(TimeSpan.FromSeconds(3)); }
    }
    if (!created) throw new Exception("SQL Server de testes indisponível.", last);
}

try
{
    using var factory = new WebApplicationFactory<GymManagementPL.Program>()
        .WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Production");
            builder.UseSetting("ConnectionStrings:DefaultConnection", connection);
            builder.UseSetting("Database:ApplyMigrations", "false");
            builder.UseSetting("Jwt:Key", "IntegrationOnlyJwtForStudentBookings-TestHost-2026");
            builder.ConfigureTestServices(services =>
            {
                // Autenticação falsa APENAS no processo de teste; nunca no site publicado.
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = "CiTest";
                    options.DefaultChallengeScheme = "CiTest";
                    options.DefaultForbidScheme = "CiTest";
                }).AddScheme<AuthenticationSchemeOptions, CiAuthenticationHandler>("CiTest", _ => { });
            });
            builder.ConfigureLogging(logging => { logging.AddConsole(); logging.SetMinimumLevel(LogLevel.Information); });
        });

    using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false,
        BaseAddress = new Uri("https://localhost")
    });

    using var healthResponse = await client.GetAsync("/api/health");
    var health = await healthResponse.Content.ReadAsStringAsync();
    if (healthResponse.StatusCode != HttpStatusCode.OK || !health.Contains("20261120-review-1"))
        throw new InvalidOperationException($"Verificação pública de saúde falhou: HTTP {(int)healthResponse.StatusCode}.");
    Console.WriteLine("OK: /api/health confirma conexão ao SQL de teste e revisão publicada.");

    using var pageResponse = await client.GetAsync("/Member/Create");
    var html = await pageResponse.Content.ReadAsStringAsync();
    if (pageResponse.StatusCode != HttpStatusCode.OK || !html.Contains("Cadastrar aluno", StringComparison.OrdinalIgnoreCase)
        || !html.Contains("HealthRecordViewModel.Height", StringComparison.OrdinalIgnoreCase))
        throw new InvalidOperationException($"GET /Member/Create falhou: HTTP {(int)pageResponse.StatusCode}. Html={html[..Math.Min(600, html.Length)]}");

    Console.WriteLine("OK: GET /Member/Create renderiza o formulário real para SuperAdmin.");

    var tokenInput = Regex.Match(html, @"<input(?=[^>]*name=""__RequestVerificationToken"")[^>]*>", RegexOptions.IgnoreCase);
    var tokenValue = Regex.Match(tokenInput.Value, @"value=""([^""]+)""", RegexOptions.IgnoreCase);
    if (!tokenValue.Success) throw new InvalidOperationException("Formulário não possui antiforgery token.");
    var suffix = Guid.NewGuid().ToString("N")[..8];
    var formData = new Dictionary<string, string>
    {
        ["__RequestVerificationToken"] = WebUtility.HtmlDecode(tokenValue.Groups[1].Value),
        ["Name"] = "Aluno Teste Integracao",
        ["Email"] = $"aluno-{suffix}@example.com",
        ["Phone"] = "51987654321",
        ["DateOfBirth"] = "2002-05-20",
        ["Gender"] = "1",
        ["Street"] = "Rua Teste",
        ["City"] = "Parobe",
        ["BuildingNumber"] = "123",
        ["HealthRecordViewModel.Height"] = "1,75",
        ["HealthRecordViewModel.Weight"] = "70,5",
        ["HealthRecordViewModel.BloodType"] = "O+",
        ["HealthRecordViewModel.Note"] = "Cadastro de integração em banco descartável"
    };

    using var postResponse = await client.PostAsync("/Member/CreateMember", new FormUrlEncodedContent(formData));
    var postHtml = await postResponse.Content.ReadAsStringAsync();
    if (postResponse.StatusCode != HttpStatusCode.Redirect ||
        postResponse.Headers.Location?.ToString() is not ("/Member" or "/Member/Index"))
        throw new InvalidOperationException($"POST /Member/CreateMember falhou: HTTP {(int)postResponse.StatusCode}. Location={postResponse.Headers.Location}. Html={postHtml[..Math.Min(1000, postHtml.Length)]}");

    // Testa persistência contra OUTRA instância/consulta EF, sem dados simulados.
    using (var verify = new GymDbContext(options))
    {
        var student = await verify.Members.AsNoTracking().Include(x => x.HealthRecord)
            .SingleOrDefaultAsync(x => x.Email == formData["Email"]);
        if (student is null || student.Name != formData["Name"] || student.HealthRecord is null ||
            student.HealthRecord.BloodType != "O+")
            throw new InvalidOperationException("Cadastro MVC não foi persistido com ficha de saúde.");
    }
    using var listResponse = await client.GetAsync("/Member/Index");
    var listHtml = await listResponse.Content.ReadAsStringAsync();
    if (listResponse.StatusCode != HttpStatusCode.OK || !listHtml.Contains(formData["Name"]))
        throw new InvalidOperationException("Aluno salvo no banco não aparece na listagem MVC.");

    Console.WriteLine("OK: POST cadastro com antiforgery, persistência SQL e listagem MVC.");



    using (var createPlanPage = await client.GetAsync("/Plan/Create"))
    {
        var planHtml = await createPlanPage.Content.ReadAsStringAsync();
        if (createPlanPage.StatusCode != HttpStatusCode.OK || !planHtml.Contains("Cadastrar novo plano"))
            throw new Exception("Tela de cadastro de plano não abriu.");
        var planTokenInput = Regex.Match(planHtml, @"<input(?=[^>]*name=""__RequestVerificationToken"")[^>]*>", RegexOptions.IgnoreCase);
        var planToken = Regex.Match(planTokenInput.Value, @"value=""([^""]+)""", RegexOptions.IgnoreCase);
        if (!planToken.Success) throw new Exception("Cadastro de plano sem antiforgery.");
        var newPlanName = "Plano MVC " + Guid.NewGuid().ToString("N")[..7];
        using var createPlanReply = await client.PostAsync("/Plan/Create", new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = WebUtility.HtmlDecode(planToken.Groups[1].Value),
                ["PlanName"] = newPlanName,
                ["Description"] = "Plano de integração automatizada",
                ["DurationDays"] = "30",
                ["Price"] = "129,90"
            }));
        if (createPlanReply.StatusCode != HttpStatusCode.Redirect)
            throw new Exception("Falha no POST de criação de plano: " + (int)createPlanReply.StatusCode);
        using var plansDb = new GymDbContext(options);
        if (!await plansDb.Plans.AsNoTracking().AnyAsync(x => x.Name == newPlanName && x.Price == 129.90m))
            throw new Exception("Plano não persistiu preço em reais corretamente no SQL.");
    }
    Console.WriteLine("OK: cadastro MVC de plano com antiforgery, preço pt-BR e persistência SQL.");

    // Em banco SQL descartável, validar o ciclo real de presença em aula via QR,
    // sem manipular matrículas ou alunos da MonsterASP.
    using (var attendanceDb = new GymDbContext(options))
    {
        var student = await attendanceDb.Members.SingleAsync(x => x.Email == formData["Email"]);
        var instructor = new Trainer
        {
            Name = "Professor Integração",
            Email = "professor-" + Guid.NewGuid().ToString("N")[..8] + "@example.com",
            Phone = "51999999998",
            DateOfBirth = new DateOnly(1995, 2, 1), Gender = Gender.Male,
            Specialties = Specialties.Musculacao,
            Address = new Address { BuildingNumber = 20, City = "Parobe", Street = "Rua Teste" }
        };
        var category = new Category { CategoryName = "Funcional Teste" };
        var plan = new Plan { Name = "Mensal Integração QR", Description = "Plano de testes", DurationDays = 30, Price = 100m, IsActive = true };
        attendanceDb.Trainers.Add(instructor);
        attendanceDb.Categories.Add(category);
        attendanceDb.Plans.Add(plan);
        await attendanceDb.SaveChangesAsync();

        var brazilTime = BrazilClock.Now;
        var activeClass = new Session
        {
            Description = "Aula de integração QR", Capacity = 10,
            StartDate = brazilTime.AddMinutes(-5), EndDate = brazilTime.AddMinutes(50),
            TrainerId = instructor.Id, CategoryId = category.Id
        };
        attendanceDb.Sessions.Add(activeClass);
        attendanceDb.Memberships.Add(new Membership
        {
            MemberId = student.Id, PlanId = plan.Id, CreatedAt = brazilTime.AddDays(-61),
            EndDate = brazilTime.AddDays(-31)
        });
        await attendanceDb.SaveChangesAsync();
        using var enrollmentPage = await client.GetAsync("/Membership/Create");
        enrollmentPage.EnsureSuccessStatusCode();
        var enrollmentHtml = await enrollmentPage.Content.ReadAsStringAsync();
        var enrollmentInput = Regex.Match(enrollmentHtml, @"<input(?=[^>]*name=""__RequestVerificationToken"")[^>]*>");
        var enrollmentToken = Regex.Match(enrollmentInput.Value, @"value=""([^""]+)""");
        if (!enrollmentToken.Success) throw new Exception("Matrícula sem token antiforgery.");
        var renewal = new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = WebUtility.HtmlDecode(enrollmentToken.Groups[1].Value),
            ["MemberId"] = student.Id.ToString(),
            ["PlanId"] = plan.Id.ToString()
        };
        using var renewed = await client.PostAsync("/Membership/Create", new FormUrlEncodedContent(renewal));
        if (renewed.StatusCode != HttpStatusCode.Redirect)
            throw new Exception($"Renovação MVC do mesmo plano falhou com HTTP {(int)renewed.StatusCode}.");
        using var repeatedRenewal = await client.PostAsync("/Membership/Create", new FormUrlEncodedContent(renewal));
        if (repeatedRenewal.StatusCode != HttpStatusCode.OK ||
            (await repeatedRenewal.Content.ReadAsStringAsync()).Contains("Matrícula criada com sucesso.", StringComparison.Ordinal))
            throw new Exception("O MVC deve recusar outra renovação enquanto existe matrícula vigente.");
        var enrollmentHistory = await attendanceDb.Memberships.AsNoTracking()
            .Where(x => x.MemberId == student.Id && x.PlanId == plan.Id).OrderBy(x => x.CreatedAt).ToListAsync();
        if (enrollmentHistory.Count != 2 || enrollmentHistory[0].EndDate != brazilTime.AddDays(-31) ||
            enrollmentHistory[1].EndDate <= BrazilClock.Now)
            throw new Exception("A renovação não preservou o histórico ou não ativou a matrícula.");
        Console.WriteLine("OK: o MVC renovou o mesmo plano, preservou o histórico e recusou uma matrícula repetida.");

        attendanceDb.Bookings.Add(new Booking { MemberId = student.Id, SessionId = activeClass.Id });
        await attendanceDb.SaveChangesAsync();

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["Jwt:Key"] = "IntegrationOnlySecretForQrCheckin2026-Long-Key-456" }).Build();
        var attendance = new ClassAttendanceService(attendanceDb, configuration);
        var ticket = await attendance.IssueAsync(student.Id, activeClass.Id);
        if (!ticket.Success || ticket.Code?.Length != 8)
            throw new Exception("Falha ao gerar código de presença para aluno com reserva.");
        var png = QRCoder.PngByteQRCodeHelper.GetQRCode(
            $"GYMFLOW-CLASS:{activeClass.Id}:{ticket.Code}", QRCoder.QRCodeGenerator.ECCLevel.Q, 6);
        if (png.Length < 100 || png[0] != 137 || png[1] != 80 || png[2] != 78 || png[3] != 71)
            throw new Exception("A imagem PNG do QR não foi gerada corretamente.");
        var rejected = await attendance.ConfirmAsync(activeClass.Id, "0000000X");
        if (rejected.Success) throw new Exception("Código de QR inválido foi aceito.");
        var valid = await attendance.ConfirmAsync(activeClass.Id, ticket.Code);
        if (!valid.Success) throw new Exception("Código de QR válido não registrou presença: " + valid.Message);
        var repeat = await attendance.ConfirmAsync(activeClass.Id, ticket.Code);
        if (repeat.Success) throw new Exception("Código de presença foi reutilizado após presença marcada.");
        attendanceDb.ChangeTracker.Clear();
        if (!await attendanceDb.Bookings.AsNoTracking().AnyAsync(x =>
                x.MemberId == student.Id && x.SessionId == activeClass.Id && x.IsAttended))
            throw new Exception("Presença de QR não foi persistida no SQL Server.");
        using var createPage = await client.GetAsync("/Session/Create");
        createPage.EnsureSuccessStatusCode();
        var classForm = await createPage.Content.ReadAsStringAsync();
        var classInput = Regex.Match(classForm, @"<input(?=[^>]*name=""__RequestVerificationToken"")[^>]*>");
        var classToken = Regex.Match(classInput.Value, @"value=""([^""]+)""");
        if (!classToken.Success) throw new Exception("O formulário de aula não emitiu o token antiforgery.");

        const string description = "Funcional integrado no site e aplicativo";
        var future = brazilTime.AddDays(1);
        using var classResponse = await client.PostAsync("/Session/Create", new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = WebUtility.HtmlDecode(classToken.Groups[1].Value),
                ["Description"] = description,
                ["TrainerId"] = instructor.Id.ToString(),
                ["CategoryId"] = category.Id.ToString(),
                ["Capacity"] = "1",
                ["StartDate"] = future.ToString("yyyy-MM-ddTHH:mm"),
                ["EndDate"] = future.AddHours(1).ToString("yyyy-MM-ddTHH:mm")
            }));
        if (classResponse.StatusCode != HttpStatusCode.Redirect)
            throw new Exception($"Cadastro MVC da aula falhou com HTTP {(int)classResponse.StatusCode}.");
        var newClass = await attendanceDb.Sessions.AsNoTracking().SingleAsync(x => x.Description == description);

        var testEmail = $"app-{suffix}@example.com";
        var password = "Gf1!" + Guid.NewGuid().ToString("N");
        using (var scope = factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var account = new ApplicationUser { UserName = testEmail, Email = testEmail, FirstName = "Aluno", LastName = "Teste" };
            if (!(await users.CreateAsync(account, password)).Succeeded ||
                !(await users.AddToRoleAsync(account, "Aluno")).Succeeded ||
                !(await users.AddClaimAsync(account, new Claim("gymflow:member_id", student.Id.ToString()))).Succeeded)
                throw new Exception("Falha ao preparar a conta de aluno do teste.");
        }

        using var mobile = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        });
        using var login = await mobile.PostAsJsonAsync("/api/auth/login", new { email = testEmail, password });
        login.EnsureSuccessStatusCode();
        using var sessionJson = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        mobile.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", sessionJson.RootElement.GetProperty("token").GetString());

        using var classes = await mobile.GetFromJsonAsync<JsonDocument>("/api/sessions")
            ?? throw new Exception("A API não devolveu as aulas.");
        if (!classes.RootElement.GetProperty("sessions").EnumerateArray()
                .Any(x => x.GetProperty("id").GetInt32() == newClass.Id &&
                          x.GetProperty("status").GetString() == "Upcoming"))
            throw new Exception("A aula criada no site não apareceu como agendada no app do aluno.");

        using var reserve = await mobile.PostAsJsonAsync("/api/me/bookings", new { sessionId = newClass.Id });
        reserve.EnsureSuccessStatusCode();
        if (!await attendanceDb.Bookings.AsNoTracking().AnyAsync(b => b.SessionId == newClass.Id && b.MemberId == student.Id))
            throw new Exception("A reserva pelo app não chegou ao banco.");
        using var duplicate = await mobile.PostAsJsonAsync("/api/me/bookings", new { sessionId = newClass.Id });
        if (duplicate.StatusCode != HttpStatusCode.Conflict)
            throw new Exception("A API deve recusar a segunda reserva do mesmo aluno.");
        using var cancel = await mobile.DeleteAsync($"/api/me/bookings/{newClass.Id}");
        cancel.EnsureSuccessStatusCode();
        if (await attendanceDb.Bookings.AsNoTracking().AnyAsync(b => b.SessionId == newClass.Id && b.MemberId == student.Id))
            throw new Exception("A reserva cancelada pelo app continuou no banco.");
        Console.WriteLine("OK: SuperAdmin cadastra aula no MVC, aluno entra com JWT, consulta, reserva e cancela pela API.");
    }
    Console.WriteLine("OK: check-in de aula por QR validado com código temporário, antifraude e gravação SQL.");

    // Testa também telas reais de todos os perfis, sem alterar produção.
    // Cada caso usa uma identidade simulada somente no test host isolado.
    var pageChecks = new (string Role, string Path, HttpStatusCode Status)[]
    {
        ("SuperAdmin", "/Home/Index", HttpStatusCode.OK),
        ("SuperAdmin", "/Member/Index", HttpStatusCode.OK),
        ("SuperAdmin", "/Plan/Index", HttpStatusCode.OK),
        ("SuperAdmin", "/Plan/Create", HttpStatusCode.OK),
        ("SuperAdmin", "/Membership/Index", HttpStatusCode.OK),
        ("SuperAdmin", "/Membership/Create", HttpStatusCode.OK),
        ("SuperAdmin", "/Session/Index", HttpStatusCode.OK),
        ("SuperAdmin", "/Session/Create", HttpStatusCode.OK),
        ("SuperAdmin", "/Trainer/Index", HttpStatusCode.OK),
        ("SuperAdmin", "/Trainer/Create", HttpStatusCode.OK),
        ("SuperAdmin", "/Booking/Index", HttpStatusCode.OK),
        ("SuperAdmin", "/WorkoutManagement/Index", HttpStatusCode.OK),
        ("SuperAdmin", "/AdminManagement/Index", HttpStatusCode.OK),
        ("SuperAdmin", "/ClassCheckIn/Index", HttpStatusCode.OK),
        ("SuperAdmin", "/Student/Index", HttpStatusCode.Forbidden),
        ("Admin", "/Member/Create", HttpStatusCode.OK),
        ("Admin", "/AdminManagement/Index", HttpStatusCode.OK),
        ("Gerente", "/Member/Create", HttpStatusCode.OK),
        ("Gerente", "/Plan/Create", HttpStatusCode.OK),
        ("Gerente", "/Trainer/Index", HttpStatusCode.OK),
        ("Gerente", "/AdminManagement/Index", HttpStatusCode.Forbidden),
        ("Recepcionista", "/Member/Create", HttpStatusCode.OK),
        ("Recepcionista", "/Plan/Create", HttpStatusCode.Forbidden),
        ("Recepcionista", "/Membership/Create", HttpStatusCode.OK),
        ("Recepcionista", "/Trainer/Index", HttpStatusCode.Forbidden),
        ("Recepcionista", "/AdminManagement/Index", HttpStatusCode.Forbidden),
        ("Recepcionista", "/WorkoutManagement/Index", HttpStatusCode.Forbidden),
        ("Instrutor", "/Member/Create", HttpStatusCode.Forbidden),
        ("Instrutor", "/Booking/Index", HttpStatusCode.OK),
        ("Instrutor", "/WorkoutManagement/Index", HttpStatusCode.OK),
        ("Instrutor", "/AdminManagement/Index", HttpStatusCode.Forbidden),
        ("Aluno", "/ClassCheckIn/Index", HttpStatusCode.Forbidden),
        ("Aluno", "/Member/Create", HttpStatusCode.Forbidden),
        ("Aluno", "/Membership/Index", HttpStatusCode.Forbidden),
        ("Aluno", "/AdminManagement/Index", HttpStatusCode.Forbidden),
        ("Aluno", "/WorkoutManagement/Index", HttpStatusCode.Forbidden),
        ("Aluno", "/Home/Index", HttpStatusCode.OK),
        ("Aluno", "/Student/Index", HttpStatusCode.OK),
        ("Aluno", "/StudentBooking/Index", HttpStatusCode.OK),
        ("Aluno", "/Plan/Index", HttpStatusCode.OK),
        ("Aluno", "/Session/Index", HttpStatusCode.OK)
    };
    foreach (var check in pageChecks)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, check.Path);
        request.Headers.Add("X-GymFlow-Ci-Role", check.Role);
        using var response = await client.SendAsync(request);
        if (response.StatusCode != check.Status)
            throw new InvalidOperationException(
                $"Falha MVC: perfil {check.Role} GET {check.Path} retornou HTTP {(int)response.StatusCode} em vez de {(int)check.Status}.");
    }
    Console.WriteLine($"OK: {pageChecks.Length} telas/controles de acesso MVC testados em todos os seis perfis.");

}
finally
{
    using var cleanup = new GymDbContext(options);
    cleanup.Database.EnsureDeleted();
}

sealed class CiAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public CiAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger, UrlEncoder encoder) : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "ci-test-superadmin"),
            new Claim(ClaimTypes.Name, "CI Teste"),
            new Claim(ClaimTypes.Role, Context.Request.Headers.TryGetValue("X-GymFlow-Ci-Role", out var role) && role.Count == 1 ? role.ToString() : "SuperAdmin")
        };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(
            new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
    }
}
