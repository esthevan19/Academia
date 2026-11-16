using GymManagementBLL.Services.Interfaces;
using GymManagementBLL.ViewModels.TrainerViewModels;
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

    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin,Gerente")]
    public IActionResult Create([FromBody] CreateTrainerViewModel model)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        return _trainerService.CreateTrainer(model)
            ? Ok(new { success = true, message = "Instrutor cadastrado com sucesso." })
            : Conflict(new { success = false, message = "E-mail ou telefone já cadastrado." });
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin,Gerente")]
    public IActionResult Update(int id, [FromBody] TrainerToUpdateViewModel model)
    {
        if (id <= 0) return BadRequest(new { success = false, message = "Identificador inválido." });
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        return _trainerService.UpdateTrainer(id, model)
            ? Ok(new { success = true, message = "Instrutor atualizado com sucesso." })
            : Conflict(new { success = false, message = "Não foi possível atualizar o instrutor." });
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin,Gerente")]
    public IActionResult Delete(int id)
    {
        if (id <= 0) return BadRequest(new { success = false, message = "Identificador inválido." });
        return _trainerService.RemoveTrainer(id)
            ? Ok(new { success = true, message = "Instrutor removido com sucesso." })
            : Conflict(new { success = false, message = "Não foi possível remover o instrutor." });
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
