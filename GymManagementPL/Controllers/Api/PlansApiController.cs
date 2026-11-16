using GymManagementBLL.Services.Interfaces;
using GymManagementBLL.ViewModels.PlanViewModels;
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

    [HttpPost]
    public IActionResult Create([FromBody] CreatePlanViewModel model)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        return _planService.CreatePlan(model)
            ? Ok(new { success = true, message = "Plano criado com sucesso." })
            : Conflict(new { success = false, message = "Não foi possível criar o plano." });
    }

    [HttpPut("{id:int}")]
    public IActionResult Update(int id, [FromBody] UpdatePlanViewModel model)
    {
        if (id <= 0) return BadRequest(new { success = false, message = "Identificador inválido." });
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        return _planService.UpdatePlan(id, model)
            ? Ok(new { success = true, message = "Plano atualizado com sucesso." })
            : Conflict(new { success = false, message = "Não foi possível atualizar o plano." });
    }

    [HttpPost("{id:int}/toggle-status")]
    public IActionResult ToggleStatus(int id)
    {
        if (id <= 0) return BadRequest(new { success = false, message = "Identificador inválido." });
        return _planService.PlanStatusToggle(id)
            ? Ok(new { success = true, message = "Status do plano atualizado." })
            : Conflict(new { success = false, message = "Não foi possível alterar o status do plano." });
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
