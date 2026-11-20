using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using GymManagementDAL.Data.Contexts;
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
            new Claim(ClaimTypes.Role, "SuperAdmin")
        };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(
            new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
    }
}
