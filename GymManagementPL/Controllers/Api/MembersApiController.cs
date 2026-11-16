using GymManagementBLL.Services.Interfaces;
using GymManagementBLL.ViewModels.MemberViewModels;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymManagementPL.Controllers.Api;

[ApiController]
[Route("api/members")]
[Authorize(
    AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
    Roles = "SuperAdmin,Admin,Gerente,Recepcionista")]
public class MembersApiController : ControllerBase
{
    private readonly IMemberService _memberService;

    public MembersApiController(IMemberService memberService)
    {
        _memberService = memberService;
    }

    [HttpGet]
    public IActionResult GetAll()
    {
        var members = _memberService.GetAllMembers();
        return Ok(new { success = true, members });
    }

    [HttpGet("{id:int}")]
    public IActionResult GetById(int id)
    {
        if (id <= 0) return BadRequest(new { success = false, message = "Identificador inválido." });
        var member = _memberService.GetMemberDetails(id);
        return member is null
            ? NotFound(new { success = false, message = "Aluno não encontrado." })
            : Ok(new { success = true, member });
    }

    [HttpPost]
    public IActionResult Create([FromForm] CreateMemberViewModel model)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        return _memberService.CreateMember(model)
            ? Ok(new { success = true, message = "Aluno cadastrado com sucesso." })
            : Conflict(new { success = false, message = "E-mail ou telefone já cadastrado." });
    }

    [HttpPut("{id:int}")]
    public IActionResult Update(int id, [FromBody] MemberToUpdateViewModel model)
    {
        if (id <= 0) return BadRequest(new { success = false, message = "Identificador inválido." });
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        return _memberService.UpdateMember(id, model)
            ? Ok(new { success = true, message = "Aluno atualizado com sucesso." })
            : Conflict(new { success = false, message = "Não foi possível atualizar o aluno." });
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin,Gerente")]
    public IActionResult Delete(int id)
    {
        if (id <= 0) return BadRequest(new { success = false, message = "Identificador inválido." });
        return _memberService.RemoveMember(id)
            ? Ok(new { success = true, message = "Aluno removido com sucesso." })
            : Conflict(new { success = false, message = "Não foi possível remover o aluno." });
    }
}
