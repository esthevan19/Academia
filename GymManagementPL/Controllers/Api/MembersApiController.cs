using GymManagementBLL.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GymManagementPL.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class MembersApiController : ControllerBase
    {
        private readonly IMemberService _memberService;
        private readonly IMembershipService _membershipService;
        private readonly IPlanService _planService;

        public MembersApiController(
            IMemberService memberService,
            IMembershipService membershipService,
            IPlanService planService)
        {
            _memberService = memberService;
            _membershipService = membershipService;
            _planService = planService;
        }

        [HttpGet]
        public IActionResult GetAllMembers()
        {
            var members = _memberService.GetAllMembers();
            return Ok(new { success = true, members });
        }

        [HttpGet("{id}")]
        public IActionResult GetMemberById(int id)
        {
            var member = _memberService.GetMemberDetails(id);
            if (member == null) return NotFound(new { success = false, message = "Aluno não encontrado" });

            return Ok(new { success = true, member });
        }

        [HttpGet("me")]
        public IActionResult GetMyProfile()
        {
            // Retorna o perfil do aluno para o aplicativo
            var members = _memberService.GetAllMembers();
            var firstMember = members.FirstOrDefault();

            return Ok(new
            {
                success = true,
                student = firstMember != null ? new
                {
                    name = firstMember.Name,
                    email = firstMember.Email,
                    phone = firstMember.Phone,
                    planName = firstMember.PlanName ?? "Plano Ouro Anual",
                    status = "Ativo",
                    matricula = "GF-" + firstMember.Id.ToString("D5"),
                    qrCode = "GYMFLOW-" + firstMember.Id + "-ACCESS-VALID",
                    expiresAt = firstMember.MembershipEndDate ?? DateTime.Today.AddMonths(6).ToString("dd/MM/yyyy")
                } : new
                {
                    name = "Aluno GymFlow",
                    email = "aluno@gymflow.com",
                    phone = "(51) 99426-2915",
                    planName = "Plano Ouro Anual",
                    status = "Ativo",
                    matricula = "GF-00001",
                    qrCode = "GYMFLOW-001-ACCESS-VALID",
                    expiresAt = DateTime.Today.AddMonths(6).ToString("dd/MM/yyyy")
                }
            });
        }
    }
}
