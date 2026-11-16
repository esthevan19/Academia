using GymManagementBLL.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymManagementPL.Controllers.Api;

[ApiController]
[Route("api/trainers")]
[Authorize(
    AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
    Roles = "SuperAdmin,Admin,Gerente,Instrutor")]
public class TrainersApiController : ControllerBase
{
    private readonly ITrainerService _trainerService;

    public TrainersApiController(ITrainerService trainerService)
    {
        _trainerService = trainerService;
    }

    [HttpGet]
    public IActionResult GetAll()
    {
        return Ok(new
        {
            success = true,
            trainers = _trainerService.GetAllTrainers()
        });
    }

    [HttpGet("{id:int}")]
    public IActionResult GetById(int id)
    {
        if (id <= 0) return BadRequest(new { success = false, message = "Identificador inválido." });

        var trainer = _trainerService.GetTrainerDetails(id);
        return trainer is null
            ? NotFound(new { success = false, message = "Instrutor não encontrado." })
            : Ok(new { success = true, trainer });
    }
}
