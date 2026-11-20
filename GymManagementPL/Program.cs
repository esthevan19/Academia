using GymManagementBLL;
using GymManagementPL.Services;
using GymManagementBLL.Services.AttachmentService;
using GymManagementBLL.Services.Classes;
using GymManagementBLL.Services.Interfaces;
using GymManagementDAL.Data.Contexts;
using GymManagementDAL.Data.DataSeed;
using GymManagementDAL.Entities;
using GymManagementDAL.Repositories.Classes;
using GymManagementDAL.Repositories.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Globalization;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection;

namespace GymManagementPL
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            #region Add services to the container.
            
            // O IIS/MonsterASP pode não disponibilizar perfil de usuário para as chaves
            // antiforgery/cookies. Configure uma pasta PRIVADA, fora do diretório web,
            // persistente entre publicações: DataProtection__KeysDirectory.
            var keyDirectory = builder.Configuration["DataProtection:KeysDirectory"];
            var dataProtection = builder.Services.AddDataProtection()
                .SetApplicationName("GymFlow");
            if (!string.IsNullOrWhiteSpace(keyDirectory))
            {
                if (!Path.IsPathFullyQualified(keyDirectory))
                    throw new InvalidOperationException(
                        "DataProtection__KeysDirectory precisa apontar para um diretório absoluto e privado.");
                var keyFolder = new DirectoryInfo(keyDirectory);
                keyFolder.Create();
                dataProtection.PersistKeysToFileSystem(keyFolder);
                if (OperatingSystem.IsWindows())
                    dataProtection.ProtectKeysWithDpapi(protectToLocalMachine: true);
            }

            builder.Services.AddControllersWithViews();
            // Exibição e validação numérica padronizadas para o Brasil.
            var brazilCulture = CultureInfo.GetCultureInfo("pt-BR");
            CultureInfo.DefaultThreadCurrentCulture = brazilCulture;
            CultureInfo.DefaultThreadCurrentUICulture = brazilCulture;
            builder.Services.Configure<Microsoft.AspNetCore.Builder.RequestLocalizationOptions>(options =>
            {
                options.DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture(brazilCulture);
                options.SupportedCultures = new[] { brazilCulture };
                options.SupportedUICultures = new[] { brazilCulture };
            });
            builder.Services.AddDbContext<GymDbContext>(optoins =>
            {
                optoins.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
            });
            builder.Services.AddScoped<ClassAttendanceService>();
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
            builder.Services.AddScoped<ISessionRepository, SessionRepository>();
            builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
            builder.Services.AddScoped<IMemberService, MemberService>();
            builder.Services.AddScoped<ITrainerService, TrainerService>();
            builder.Services.AddScoped<IPlanService, PlanService>();
            builder.Services.AddScoped<ISessionService, SessionService>();
            builder.Services.AddScoped<IAttachmentService, AttachmentService>();
            builder.Services.AddScoped<IAccountService, AccountService>();
            builder.Services.AddScoped<IMembershipRepository, MembershipRepository>();
            builder.Services.AddScoped<IMembershipService, MembershipService>();
            builder.Services.AddScoped<IBookingRepository, BookingRepository>();
            builder.Services.AddScoped<IBookingService, BookingService>();
            builder.Services.AddScoped<IAdminService, AdminService>();
            builder.Services.AddAutoMapper(x => x.AddProfile(new MappingProfiles()));
            builder.Services.AddIdentity<ApplicationUser, IdentityRole>(Config =>
            {
                Config.User.RequireUniqueEmail = true;
            }).AddEntityFrameworkStores<GymDbContext>()
            .AddDefaultTokenProviders(); ;

            // JWT para o aplicativo mobile. Configure Jwt__Key como variável de ambiente.
            var jwtKey = builder.Configuration["Jwt:Key"];
            var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "GymFlow";
            var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "GymFlow.Mobile";

            builder.Services.AddAuthentication()
                .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = string.IsNullOrWhiteSpace(jwtKey)
                            ? null
                            : new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                        ValidateIssuer = true,
                        ValidIssuer = jwtIssuer,
                        ValidateAudience = true,
                        ValidAudience = jwtAudience,
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.FromMinutes(1),
                        RoleClaimType = System.Security.Claims.ClaimTypes.Role,
                        NameClaimType = System.Security.Claims.ClaimTypes.Name
                    };
                    // Tokens carregam papéis/vínculos do momento do login. Após revogação,
                    // a API deve exigir novo login, sem aceitar direitos antigos por 8 horas.
                    options.Events = new JwtBearerEvents
                    {
                        OnTokenValidated = async context =>
                        {
                            var principal = context.Principal;
                            var userId = principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                            if (principal is null || string.IsNullOrWhiteSpace(userId))
                            {
                                context.Fail("Sessão inválida.");
                                return;
                            }

                            var userManager = context.HttpContext.RequestServices
                                .GetRequiredService<UserManager<ApplicationUser>>();
                            var currentUser = await userManager.FindByIdAsync(userId);
                            if (currentUser is null || await userManager.IsLockedOutAsync(currentUser))
                            {
                                context.Fail("Conta indisponível. Faça login novamente.");
                                return;
                            }

                            var tokenRoles = principal.FindAll(ClaimTypes.Role)
                                .Select(claim => claim.Value)
                                .ToHashSet(StringComparer.OrdinalIgnoreCase);
                            var currentRoles = (await userManager.GetRolesAsync(currentUser))
                                .ToHashSet(StringComparer.OrdinalIgnoreCase);

                            var tokenMembers = principal.FindAll("gymflow:member_id")
                                .Select(claim => claim.Value)
                                .ToHashSet(StringComparer.Ordinal);
                            var currentMembers = (await userManager.GetClaimsAsync(currentUser))
                                .Where(claim => claim.Type == "gymflow:member_id")
                                .Select(claim => claim.Value)
                                .ToHashSet(StringComparer.Ordinal);

                            if (!tokenRoles.SetEquals(currentRoles) ||
                                !tokenMembers.SetEquals(currentMembers))
                            {
                                context.Fail("As permissões da conta foram alteradas. Entre novamente.");
                            }
                        }
                    };
                });

            //builder.Services.ConfigureApplicationCookie(opt =>
            //{
            //    opt.LoginPath = "/Account/Login";
            //    opt.AccessDeniedPath = "/Account/AccessDenied";
            //});

            #endregion

            var app = builder.Build();
            if (app.Environment.IsProduction() && string.IsNullOrWhiteSpace(keyDirectory))
                app.Logger.LogWarning("Chaves de sessão sem pasta persistente explícita. Configure DataProtection__KeysDirectory fora da pasta de publicação.");


            #region DataSeed

            using var scope = app.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<GymDbContext>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            // Production schema changes require an explicit deployment opt-in.
            // Back up the SQL Server database before setting Database__ApplyMigrations=true.
            var applyMigrations = app.Environment.IsDevelopment() ||
                app.Configuration.GetValue<bool>("Database:ApplyMigrations");
            if (applyMigrations)
            {
                var pendingMigrations = dbContext.Database.GetPendingMigrations();
                if (pendingMigrations.Any())
                    dbContext.Database.Migrate();
            }
            // Dados de exemplo (planos/categorias) apenas no desenvolvimento.
            // Produção mantém os cadastros reais e nunca cria contas com senhas fixas.
            if (app.Environment.IsDevelopment())
                GymDataSeeding.SeedData(dbContext);

            IdentityDbContextSeeding.SeedRoles(roleManager);

            #endregion

             
            #region Configure the HTTP request pipeline.

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseRequestLocalization();
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();

            app.UseAuthentication();

            app.UseAuthorization();

            // Diagnóstico público limitado: nenhum dado do banco, chave ou conta é exposto.
            // O marcador de revisão ajuda a confirmar se a nova publicação chegou ao servidor.
            app.MapGet("/api/health", async (GymDbContext db, CancellationToken cancellationToken) =>
            {
                bool databaseOk;
                try
                {
                    databaseOk = await db.Database.CanConnectAsync(cancellationToken);
                }
                catch
                {
                    databaseOk = false;
                }
                return Results.Json(new
                {
                    application = "GymFlow",
                    status = databaseOk ? "ok" : "unavailable",
                    revision = "20261120-review-1"
                }, statusCode: databaseOk ? StatusCodes.Status200OK : StatusCodes.Status503ServiceUnavailable);
            }).AllowAnonymous();

            app.MapStaticAssets();
            app.MapControllers();
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Account}/{action=Login}/{id?}")
                .WithStaticAssets();

            #endregion

            app.Run();
        }
    }
}
