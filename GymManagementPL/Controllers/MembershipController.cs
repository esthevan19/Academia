using GymManagementBLL.Services.Interfaces;
using GymManagementBLL.ViewModels.MembershipViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GymManagementPL.Controllers
{
    [Authorize(Roles = "SuperAdmin,Admin,Gerente,Recepcionista")]
    public class MembershipController : Controller
    {
        private readonly IMembershipService _membershipService;

        public MembershipController(IMembershipService membershipService)
        {
            _membershipService = membershipService;
        }

        #region GetAllMembers

        public IActionResult Index()
        {
            var mebmerships = _membershipService.GetAllMemberships();
            return View(mebmerships);
        }

        #endregion

        #region CreateMembership

        public IActionResult Create()
        {
            LoadDropdownsData();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(CreateMembershipViewModel createdMember)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Não foi possível criar a matrícula. Confira os dados e se o aluno já possui matrícula ativa.";
                LoadDropdownsData();
                return View(createdMember);
            }

            bool isCreated = _membershipService.CreateMembership(createdMember);

            if (isCreated)
                TempData["SuccessMessage"] = "Matrícula criada com sucesso.";
            else
            {
                TempData["ErrorMessage"] = "Não foi possível criar a matrícula. O aluno pode já possuir uma matrícula ativa.";
                LoadDropdownsData();
                return View(createdMember);
            }

            return RedirectToAction(nameof(Index));
        }
        #endregion

        #region CancelMembership

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin,Admin,Gerente")]
        public IActionResult Cancel(int id)
        {
            var result = _membershipService.DeleteMembership(id);

            if (result)
            {
                TempData["SuccessMessage"] = "Matrícula cancelada com sucesso.";
            }
            else
            {
                TempData["ErrorMessage"] = "Não foi possível cancelar a matrícula.";
            }

            return RedirectToAction(nameof(Index));
        }

        #endregion

        #region HelperMethod

        private void LoadDropdownsData()
        {
            var members = _membershipService.GetMembersForDropDown();
            ViewBag.members = new SelectList(members, "Id", "Name");

            var plans = _membershipService.GetPlansForDropDown();
            ViewBag.plans = new SelectList(plans, "Id", "Name");

        }

        #endregion

    }
}
