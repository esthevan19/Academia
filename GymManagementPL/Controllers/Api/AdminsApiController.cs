using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using GymManagementDAL.Entities;
using Microsoft.AspNetCore.Identity;
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
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IMemberService _memberService;

    private const string MemberClaimType = "gymflow:member_id";

    public AdminsApiController(IAdminService adminService, UserManager<ApplicationUser> userManager, IMemberService memberService)
    {
        _adminService = adminService;
        _userManager = userManager;
        _memberService = memberService;
    }

    public sealed record RoleRequest(string Role);

    [HttpGet]
    public IActionResult GetAll()
    {
        return Ok(new { success = true, admins = _adminService.GetAllUsers() });
    }

    public sealed record CreateStudentAccountRequest(
        [Required, EmailAddress] string Email,
        [Required, StringLength(50, MinimumLength = 3)] string UserName,
        [Required] string FirstName,
        [Required] string LastName,
        string? PhoneNumber,
        [Required, MinLength(6)] string Password,
        [Required] string ConfirmPassword);

    [HttpPost("student-account")]
    public async Task<IActionResult> CreateStudentAccount([FromBody] CreateStudentAccountRequest request)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        if (!string.Equals(request.Password, request.ConfirmPassword, StringComparison.Ordinal))
            return BadRequest(new { success = false, message = "A confirmação da senha não corresponde." });

        var user = new ApplicationUser
        {
            Email = request.Email.Trim(),
            UserName = request.UserName.Trim(),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim()
        };

        var created = await _userManager.CreateAsync(user, request.Password);
        if (!created.Succeeded)
            return Conflict(new { success = false, errors = created.Errors.Select(error => error.Description) });

        var roleResult = await _userManager.AddToRoleAsync(user, "Aluno");
        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);
            return Conflict(new { success = false, errors = roleResult.Errors.Select(error => error.Description) });
        }

        return Ok(new
        {
            success = true,
            message = "Conta de aluno criada. Agora vincule-a ao cadastro do aluno.",
            user = new { id = user.Id, email = user.Email, userName = user.UserName }
        });
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

    [HttpGet("{id}/member-link")]
    public async Task<IActionResult> GetMemberLink(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null) return NotFound(new { success = false, message = "Usuário não encontrado." });
        var claims = await _userManager.GetClaimsAsync(user);
        var linkedId = claims.FirstOrDefault(claim => claim.Type == MemberClaimType)?.Value;
        return Ok(new { success = true, memberId = int.TryParse(linkedId, out var memberId) ? memberId : (int?)null });
    }

    [HttpPost("{id}/member-link")]
    public async Task<IActionResult> LinkMember(string id, [FromBody] MemberLinkRequest request)
    {
        if (request.MemberId <= 0)
            return BadRequest(new { success = false, message = "Informe um ID de aluno válido." });

        var user = await _userManager.FindByIdAsync(id);
        if (user is null) return NotFound(new { success = false, message = "Usuário não encontrado." });
        if (!await _userManager.IsInRoleAsync(user, "Aluno"))
            return BadRequest(new { success = false, message = "Somente contas com perfil Aluno podem ser vinculadas." });
        if (_memberService.GetMemberToUpdate(request.MemberId) is null)
            return NotFound(new { success = false, message = "Cadastro de aluno não encontrado." });

        var requestedClaim = new Claim(MemberClaimType, request.MemberId.ToString());
        var alreadyLinked = await _userManager.GetUsersForClaimAsync(requestedClaim);
        if (alreadyLinked.Any(existing => existing.Id != user.Id))
            return Conflict(new { success = false, message = "Este cadastro já está vinculado a outra conta." });

        var currentClaims = await _userManager.GetClaimsAsync(user);
        var oldLinks = currentClaims.Where(claim => claim.Type == MemberClaimType).ToList();
        if (oldLinks.Count > 0)
        {
            var removed = await _userManager.RemoveClaimsAsync(user, oldLinks);
            if (!removed.Succeeded)
                return Conflict(new { success = false, errors = removed.Errors.Select(error => error.Description) });
        }

        var result = await _userManager.AddClaimAsync(user, requestedClaim);
        if (!result.Succeeded && oldLinks.Count > 0)
            await _userManager.AddClaimsAsync(user, oldLinks);

        return result.Succeeded
            ? Ok(new { success = true, message = "Conta de aluno vinculada com segurança.", memberId = request.MemberId })
            : Conflict(new { success = false, errors = result.Errors.Select(error => error.Description) });
    }

    [HttpDelete("{id}/member-link")]
    public async Task<IActionResult> UnlinkMember(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null) return NotFound(new { success = false, message = "Usuário não encontrado." });
        var claims = await _userManager.GetClaimsAsync(user);
        var links = claims.Where(claim => claim.Type == MemberClaimType).ToList();
        if (links.Count == 0) return NotFound(new { success = false, message = "Esta conta não possui vínculo com aluno." });
        var result = await _userManager.RemoveClaimsAsync(user, links);
        return result.Succeeded
            ? Ok(new { success = true, message = "Vínculo removido." })
            : Conflict(new { success = false, errors = result.Errors.Select(error => error.Description) });
    }

    public sealed record MemberLinkRequest(int MemberId);

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
