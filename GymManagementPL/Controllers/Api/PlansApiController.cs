using GymManagementBLL.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymManagementPL.Controllers.Api;

[ApiController]
[Route("api/plans")]
[Authorize(
    AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
    Roles = "SuperAdmin,Admin,Gerente")]
public class PlansApiController : ControllerBase
{
    private readonly IPlanService _planService;

    public PlansApiController(IPlanService planService)
    {
        _planService = planService;
    }

    [HttpGet]
    public IActionResult GetAll()
    {
        return Ok(new
        {
            success = true,
            plans = _planService.GetAllPlans()
        });
    }

    [HttpGet("{id:int}")]
    public IActionResult GetById(int id)
    {
        if (id <= 0) return BadRequest(new { success = false, message = "Identificador inválido." });

        var plan = _planService.GetPlanDetails(id);
        return plan is null
            ? NotFound(new { success = false, message = "Plano não encontrado." })
            : Ok(new { success = true, plan });
    }
}
