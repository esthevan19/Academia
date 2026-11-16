using GymManagementBLL.Services.Interfaces;
using GymManagementBLL.ViewModels.SessionViewModels;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymManagementPL.Controllers.Api;

[ApiController]
[Route("api/sessions")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class SessionsApiController : ControllerBase
{
    private readonly ISessionService _sessionService;

    public SessionsApiController(ISessionService sessionService)
    {
        _sessionService = sessionService;
    }

    [HttpGet]
    [Authorize(Roles = "SuperAdmin,Admin,Gerente,Recepcionista,Instrutor,Aluno")]
    public IActionResult GetAll()
    {
        return Ok(new
        {
            success = true,
            sessions = _sessionService.GetAllSessions()
        });
    }

    [HttpGet("lookups")]
    [Authorize(Roles = "SuperAdmin,Admin,Gerente,Recepcionista")]
    public IActionResult GetLookups()
    {
        return Ok(new
        {
            success = true,
            trainers = _sessionService.GetTrainersForDropDown(),
            categories = _sessionService.GetCategoriesForDropDown()
        });
    }

    [HttpGet("{id:int}/edit")]
    [Authorize(Roles = "SuperAdmin,Admin,Gerente,Recepcionista")]
    public IActionResult GetForEdit(int id)
    {
        if (id <= 0) return BadRequest(new { success = false, message = "Identificador inválido." });
        var session = _sessionService.GetSessionToUpdate(id);
        return session is null
            ? NotFound(new { success = false, message = "Sessão não encontrada." })
            : Ok(new { success = true, session });
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin,Gerente,Recepcionista")]
    public IActionResult Create([FromBody] CreateSessionViewModel model)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        return _sessionService.CreateSession(model)
            ? Ok(new { success = true, message = "Sessão criada com sucesso." })
            : Conflict(new { success = false, message = "Não foi possível criar a sessão. Confira treinador, categoria e horários." });
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin,Gerente,Recepcionista")]
    public IActionResult Update(int id, [FromBody] UpdateSessionViewModel model)
    {
        if (id <= 0) return BadRequest(new { success = false, message = "Identificador inválido." });
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        return _sessionService.UpdateSession(id, model)
            ? Ok(new { success = true, message = "Sessão atualizada com sucesso." })
            : Conflict(new { success = false, message = "Não foi possível atualizar a sessão." });
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin,Gerente")]
    public IActionResult Delete(int id)
    {
        if (id <= 0) return BadRequest(new { success = false, message = "Identificador inválido." });
        return _sessionService.RemoveSession(id)
            ? Ok(new { success = true, message = "Sessão removida com sucesso." })
            : Conflict(new { success = false, message = "Não foi possível remover a sessão." });
    }

    [HttpGet("{id:int}")]
    public IActionResult GetById(int id)
    {
        if (id <= 0) return BadRequest(new { success = false, message = "Identificador inválido." });

        var session = _sessionService.GetSessionDetails(id);
        return session is null
            ? NotFound(new { success = false, message = "Sessão não encontrada." })
            : Ok(new { success = true, session });
    }
}
