using GymManagementBLL.Services.Interfaces;
using GymManagementBLL.ViewModels.MemberViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace GymManagementPL.Controllers
{
    [Authorize(Roles = "SuperAdmin,Admin,Gerente,Recepcionista")]
    public class MemberController : Controller
    {

        #region Fields

        private readonly IMemberService _memberService;

        #endregion

        #region Constructors

        public MemberController(IMemberService memberService)
        {
            _memberService = memberService;
        }

        #endregion

        #region GetAllMembers

        public IActionResult Index()
        {
            var members = _memberService.GetAllMembers();
            return View(members);
        }

        #endregion

        #region CreateMember

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateMember(CreateMemberViewModel createdMember)
        {

            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("DataMissed", "Confira os campos obrigatórios do aluno.");
                return View(nameof(Create), createdMember);
            }

            bool result = _memberService.CreateMember(createdMember);

            if (result)
            {
                TempData["SuccessMessage"] = "Aluno cadastrado com sucesso.";
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError(string.Empty,
                "Não foi possível salvar o aluno. Confira os dados ou tente novamente. Se persistir, contate o administrador.");
            return View(nameof(Create), createdMember);
        }

        #endregion

        #region Get Member Details

        public IActionResult MemberDetails(int id)
        {
            if (id <= 0)
            {
                TempData["ErrorMessage"] = "Identificador do aluno inválido.";
                return RedirectToAction(nameof(Index));
            }

            var memberDetails = _memberService.GetMemberDetails(id);

            if (memberDetails == null)
            {
                TempData["ErrorMessage"] = $"Nenhum aluno encontrado com o código {id}.";
                return RedirectToAction(nameof(Index));
            }

            return View(memberDetails);

        }

        public IActionResult HealthRecordDetails(int id)
        {

            if (id <= 0)
            {
                TempData["ErrorMessage"] = "Identificador do aluno inválido.";
                return RedirectToAction(nameof(Index));
            }

            var memberHealthRecordData = _memberService.GetMemberHealthRecord(id);

            if (memberHealthRecordData == null)
            {
                TempData["ErrorMessage"] = $"Nenhum aluno encontrado com o código {id}.";
                return RedirectToAction(nameof(Index));
            }

            return View(memberHealthRecordData);

        }

        #endregion

        #region EditMember

        public IActionResult EditMember(int id)
        {
            if (id <= 0)
            {
                TempData["ErrorMessage"] = "Identificador do aluno inválido.";
                return RedirectToAction(nameof(Index));
            }

            var member = _memberService.GetMemberToUpdate(id);

            if (member == null)
            {
                TempData["ErrorMessage"] = $"Nenhum aluno encontrado com o código {id}.";
                return RedirectToAction(nameof(Index));
            }
            return View(member);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditMember([FromRoute] int id, MemberToUpdateViewModel memberToUpdate)
        {

            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("DataMissed", "Confira os dados e tente novamente.");
                return View(memberToUpdate);
            }
            bool result = _memberService.UpdateMember(id, memberToUpdate);

            if (result)
                TempData["SuccessMessage"] = "Dados do aluno atualizados com sucesso.";
            else
                TempData["ErrorMessage"] = "Não foi possível atualizar os dados do aluno.";

            return RedirectToAction(nameof(Index));

        }

        #endregion

        #region DeleteMember

        [Authorize(Roles = "SuperAdmin,Admin,Gerente")]
        public IActionResult Delete([FromRoute] int id)
        {
            if (id <= 0)
            {
                TempData["ErrorMessage"] = "Identificador do aluno inválido.";
                return RedirectToAction(nameof(Index));
            }

            var memberDetails = _memberService.GetMemberDetails(id);

            if (memberDetails == null)
            {
                TempData["ErrorMessage"] = $"Nenhum aluno encontrado com o código {id}.";
                return RedirectToAction(nameof(Index));
            }
            ViewBag.MemberId = id;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin,Admin,Gerente")]
        public IActionResult DeleteConfirmed([FromForm] int id)
        {

            var result = _memberService.RemoveMember(id);

            if (result)
                TempData["SuccessMessage"] = "Aluno excluído com sucesso.";
            else
                TempData["ErrorMessage"] = "Não foi possível excluir o aluno. Verifique vínculos ativos.";

            return RedirectToAction(nameof(Index));

        }

        #endregion

    }
}
