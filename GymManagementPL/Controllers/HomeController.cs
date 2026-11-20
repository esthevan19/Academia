using GymManagementBLL.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Diagnostics;

namespace GymManagementPL.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly IAnalyticsService _analyticsService;
        private readonly ILogger<HomeController> _logger;

        public HomeController(IAnalyticsService analyticsService, ILogger<HomeController> logger)
        {
            _analyticsService = analyticsService;
            _logger = logger;
        }
        [AllowAnonymous]
        public IActionResult Error()
        {
            Response.StatusCode = StatusCodes.Status500InternalServerError;
            // A página de erro mostra apenas um identificador seguro; os detalhes ficam no servidor.
            var incidentId = HttpContext.TraceIdentifier;
            ViewData["IncidentId"] = incidentId;
            var failure = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
            if (failure?.Error is not null)
                _logger.LogError(failure.Error, "Falha MVC {IncidentId} na rota {Route}.", incidentId, failure.Path);
            return View();
        }

        public IActionResult Index()
        {
            // Aluno não visualiza indicadores administrativos da academia.
            if (User.IsInRole("Aluno")) return View("Aluno");
            var data = _analyticsService.GetAnalyticsData();
            return View(data);
        }
    }
}
