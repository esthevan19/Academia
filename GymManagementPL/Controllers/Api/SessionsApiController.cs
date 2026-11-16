using GymManagementBLL.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymManagementPL.Controllers.Api;

[ApiController]
[Route("api/sessions")]
[Authorize(
    AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
    Roles = "SuperAdmin,Admin,Gerente,Recepcionista,Instrutor,Aluno")]
public class SessionsApiController : ControllerBase
{
    private readonly ISessionService _sessionService;

    public SessionsApiController(ISessionService sessionService)
    {
        _sessionService = sessionService;
    }

    [HttpGet]
    public IActionResult GetAll()
    {
        return Ok(new
        {
            success = true,
            sessions = _sessionService.GetAllSessions()
        });
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
