using System.Security.Claims;
using GymManagementBLL.Services.Interfaces;
using GymManagementBLL.ViewModels.AdminManamentViewModels;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymManagementPL.Controllers.Api;

[ApiController]
[Route("api/admins")]
[Authorize(
    AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
    Roles = "SuperAdmin")]
public class AdminsApiController : ControllerBase
{
    private readonly IAdminService _adminService;

    public AdminsApiController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    public sealed record RoleRequest(string Role);

    [HttpGet]
    public IActionResult GetAll()
    {
        return Ok(new { success = true, admins = _adminService.GetAllAdmins() });
    }

    [HttpPost]
    public IActionResult Create([FromBody] CreateAdminViewModel model)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var result = _adminService.CreateAdmin(model);
        return result.Succeeded
            ? Ok(new { success = true, message = "Administrador criado com sucesso." })
            : Conflict(new { success = false, errors = result.Errors.Select(error => error.Description) });
    }

    [HttpPut]
    public IActionResult Update([FromBody] EditAdminViewModel model)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var result = _adminService.EditAdmin(model);
        return result.Succeeded
            ? Ok(new { success = true, message = "Administrador atualizado com sucesso." })
            : Conflict(new { success = false, errors = result.Errors.Select(error => error.Description) });
    }

    [HttpDelete("{id}")]
    public IActionResult Delete(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return BadRequest(new { success = false, message = "Identificador inválido." });
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.Equals(currentUserId, id, StringComparison.Ordinal))
            return BadRequest(new { success = false, message = "Você não pode excluir a própria conta." });

        var result = _adminService.DeleteAdmin(id);
        return result.Succeeded
            ? Ok(new { success = true, message = "Administrador removido." })
            : Conflict(new { success = false, errors = result.Errors.Select(error => error.Description) });
    }

    [HttpPost("{id}/reset-password")]
    public IActionResult ResetPassword(string id, [FromBody] ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        if (!string.Equals(id, model.Id, StringComparison.Ordinal))
            return BadRequest(new { success = false, message = "O identificador da rota não corresponde ao corpo." });

        var result = _adminService.ResetPassword(model);
        return result.Succeeded
            ? Ok(new { success = true, message = "Senha redefinida." })
            : Conflict(new { success = false, errors = result.Errors.Select(error => error.Description) });
    }

    [HttpGet("{id}/roles")]
    public IActionResult GetRoles(string id)
    {
        var roles = _adminService.GetRoles(id);
        return roles is null
            ? NotFound(new { success = false, message = "Usuário não encontrado." })
            : Ok(new { success = true, roles });
    }

    [HttpPost("{id}/roles")]
    public IActionResult AddRole(string id, [FromBody] RoleRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Role))
            return BadRequest(new { success = false, message = "Informe o perfil." });

        var result = _adminService.AddRole(id, request.Role);
        return result.Succeeded
            ? Ok(new { success = true, message = "Perfil concedido." })
            : Conflict(new { success = false, errors = result.Errors.Select(error => error.Description) });
    }

    [HttpDelete("{id}/roles/{role}")]
    public IActionResult RemoveRole(string id, string role)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.Equals(currentUserId, id, StringComparison.Ordinal) &&
            string.Equals(role, "SuperAdmin", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { success = false, message = "Você não pode remover o próprio perfil SuperAdmin." });

        var result = _adminService.RemoveRole(id, role);
        return result.Succeeded
            ? Ok(new { success = true, message = "Perfil removido." })
            : Conflict(new { success = false, errors = result.Errors.Select(error => error.Description) });
    }
}
