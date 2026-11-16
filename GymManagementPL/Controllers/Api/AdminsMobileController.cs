using GymManagementBLL.Services.Interfaces;
using GymManagementBLL.ViewModels.AdminManamentViewModels;
using GymManagementDAL.Entities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace GymManagementPL.Controllers.Api;

[ApiController, Route("api/admins")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "SuperAdmin")]
public class AdminsMobileController(
    IAdminService admins,
    UserManager<ApplicationUser> users) : ControllerBase
{
    [HttpGet]
    public IActionResult List() => Ok(new { success = true, admins = admins.GetAllAdmins() });

    [HttpPost]
    public IActionResult Create([FromBody] CreateAdminViewModel model)
    {
        // Não permitir que o cliente atribua o perfil SuperAdmin por este endpoint.
        model.Role = "Admin";
        var result = admins.CreateAdmin(model);
        return result.Succeeded
            ? Ok(new { success = true })
            : BadRequest(new { success = false, errors = result.Errors.Select(e => e.Description) });
    }

    [HttpPost("student-account")]
    public async Task<IActionResult> CreateStudent([FromBody] CreateAdminViewModel model)
    {
        var user = new ApplicationUser
        {
            Email = model.Email,
            UserName = model.UserName,
            FirstName = model.FirstName,
            LastName = model.LastName,
            PhoneNumber = model.PhoneNumber
        };
        var result = await users.CreateAsync(user, model.Password);
        if (!result.Succeeded)
            return BadRequest(new { success = false, errors = result.Errors.Select(e => e.Description) });
        var roleResult = await users.AddToRoleAsync(user, "Aluno");
        if (!roleResult.Succeeded)
            return BadRequest(new { success = false, message = "Conta criada, mas não foi possível atribuir o perfil Aluno.", errors = roleResult.Errors.Select(e => e.Description) });
        return Ok(new { success = true, id = user.Id });
    }

    [HttpGet("{id}/roles")]
    public IActionResult Roles(string id)
    {
        var roles = admins.GetRoles(id);
        return roles == null ? NotFound() : Ok(new { success = true, roles });
    }

    [HttpPost("{id}/roles")]
    public IActionResult AddRole(string id, [FromBody] RoleRequest request)
    {
        if (request.Role == "SuperAdmin")
            return BadRequest(new { success = false, message = "Não é permitido atribuir SuperAdmin por esta API." });
        var result = admins.AddRole(id, request.Role);
        return result.Succeeded ? Ok(new { success = true }) : BadRequest(new { success = false, errors = result.Errors.Select(e => e.Description) });
    }

    [HttpDelete("{id}/roles/{role}")]
    public IActionResult RemoveRole(string id, string role)
    {
        if (role == "SuperAdmin")
            return BadRequest(new { success = false, message = "Não é permitido remover SuperAdmin por esta API." });
        var result = admins.RemoveRole(id, role);
        return result.Succeeded ? Ok(new { success = true }) : BadRequest(new { success = false, errors = result.Errors.Select(e => e.Description) });
    }

    public sealed class RoleRequest
    {
        public string Role { get; set; } = string.Empty;
    }
}
