using GymManagementBLL.Services.Interfaces;
using GymManagementBLL.ViewModels.MembershipViewModels;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymManagementPL.Controllers.Api;

[ApiController]
[Route("api/memberships")]
[Authorize(
    AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
    Roles = "SuperAdmin,Admin,Gerente,Recepcionista")]
public class MembershipsApiController : ControllerBase
{
    private readonly IMembershipService _membershipService;

    public MembershipsApiController(IMembershipService membershipService)
    {
        _membershipService = membershipService;
    }

    [HttpGet]
    public IActionResult GetAll()
    {
        return Ok(new
        {
            success = true,
            memberships = _membershipService.GetAllMemberships()
        });
    }

    [HttpGet("lookups")]
    public IActionResult GetLookups()
    {
        return Ok(new
        {
            success = true,
            members = _membershipService.GetMembersForDropDown(),
            plans = _membershipService.GetPlansForDropDown()
        });
    }

    [HttpPost]
    public IActionResult Create([FromBody] CreateMembershipViewModel model)
    {
        if (model.MemberId <= 0 || model.PlanId <= 0)
            return BadRequest(new { success = false, message = "Selecione um aluno e um plano válidos." });

        return _membershipService.CreateMembership(model)
            ? Ok(new { success = true, message = "Matrícula criada com sucesso." })
            : Conflict(new { success = false, message = "Não foi possível criar a matrícula. Verifique os dados e se já existe vínculo ativo." });
    }

    [HttpDelete("{memberId:int}")]
    [Authorize(Roles = "SuperAdmin,Admin,Gerente")]
    public IActionResult Delete(int memberId)
    {
        if (memberId <= 0)
            return BadRequest(new { success = false, message = "Identificador inválido." });

        return _membershipService.DeleteMembership(memberId)
            ? Ok(new { success = true, message = "Matrícula ativa cancelada." })
            : NotFound(new { success = false, message = "Não existe matrícula ativa para esse aluno." });
}
