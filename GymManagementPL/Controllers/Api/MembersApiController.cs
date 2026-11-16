using GymManagementBLL.Services.Interfaces;
using GymManagementBLL.ViewModels.MemberViewModels;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GymManagementPL.Controllers.Api
{
    [ApiController]
    [Route("api/MembersApi")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class MembersApiController : ControllerBase
    {
        private readonly IMemberService _memberService;

        public MembersApiController(IMemberService memberService)
        {
            _memberService = memberService;
        }

        [HttpGet]
        [Authorize(Roles = "SuperAdmin,Admin,Gerente,Recepcionista,Instrutor")]
        public IActionResult GetAllMembers() => Ok(new { success = true, members = _memberService.GetAllMembers() });

        [HttpGet("me")]
        [Authorize(Roles = "Aluno")]
        public IActionResult GetMyProfile()
        {
            // Não mostrar o primeiro aluno nem um perfil fictício: usar e-mail do JWT validado.
            var email = User.FindFirstValue(ClaimTypes.Email)
                ?? User.FindFirstValue("email");
            if (string.IsNullOrWhiteSpace(email)) return Unauthorized();
            var member = _memberService.GetAllMembers().FirstOrDefault(m =>
                string.Equals(m.Email, email, StringComparison.OrdinalIgnoreCase));
            if (member == null)
                return NotFound(new { success = false, message = "Esta conta ainda não está vinculada a um cadastro de aluno com o mesmo e-mail." });
            return Ok(new { success = true, member, student = member });
        }

        [HttpGet("{id:int}")]
        [Authorize(Roles = "SuperAdmin,Admin,Gerente,Recepcionista,Instrutor")]
        public IActionResult GetMemberById(int id) =>
            _memberService.GetMemberDetails(id) is { } member ? Ok(new { success = true, member }) : NotFound();

        [HttpGet("{id:int}/edit")]
        [Authorize(Roles = "SuperAdmin,Admin,Gerente,Recepcionista")]
        public IActionResult Edit(int id) =>
            _memberService.GetMemberToUpdate(id) is { } member ? Ok(new { success = true, member }) : NotFound();

        [HttpPut("{id:int}")]
        [Authorize(Roles = "SuperAdmin,Admin,Gerente,Recepcionista")]
        public IActionResult Update(int id, [FromBody] MemberToUpdateViewModel model) =>
            _memberService.UpdateMember(id, model) ? Ok(new { success = true }) : BadRequest(new { success = false, message = "Não foi possível atualizar o aluno." });

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "SuperAdmin,Admin,Gerente")]
        public IActionResult Delete(int id) =>
            _memberService.RemoveMember(id) ? Ok(new { success = true }) : NotFound();
    }
}
