using GymManagementBLL.Services.Classes;
using GymManagementBLL.Services.Interfaces;
using GymManagementBLL.ViewModels.AdminManamentViewModels;
using GymManagementDAL.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace GymManagementPL.Controllers
{
    [Authorize(Roles = "SuperAdmin,Admin")]
    public class AdminManagementController : Controller
    {
        private readonly IAdminService _adminService;

        public AdminManagementController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        public IActionResult Index()
        {
            var admins = _adminService.GetAllAdmins();
            return View(admins);
        }

        public IActionResult Create()
        {
            return View();
        } 


        [HttpPost]
        public IActionResult Create(CreateAdminViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var result = _adminService.CreateAdmin(model);

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Usuário criado com sucesso.";
                return RedirectToAction("Index");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return View(model);
        }


        public IActionResult Edit(string id)
        {
            var model = _adminService.GetAdminForUpdate(id);

            if (model == null)
                return NotFound();

            return View(model);
        }


        [HttpPost]
        public IActionResult Edit(EditAdminViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var result = _adminService.EditAdmin(model);

            if (result.Succeeded)
                return RedirectToAction("Index");

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return View(model);
        }


        public IActionResult Delete(string id)
        {
            ViewBag.AdminId = id;
            return View();
        }

        [HttpPost]
        public IActionResult DeleteConfirmed(string id)
        {
            var result = _adminService.DeleteAdmin(id);

            if (!result.Succeeded)
            {
                TempData["ErrorMessage"] = string.Join(", ", result.Errors.Select(e => e.Description));
            }
            else
            {
                TempData["SuccessMessage"] = "Usuário excluído com sucesso.";
            }

            return RedirectToAction("Index");
        }

        public IActionResult ResetPassword(string id)
        {
            return View(new ResetPasswordViewModel { Id = id });
        }


        [HttpPost]
        public IActionResult ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var result = _adminService.ResetPassword(model);

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Senha redefinida com sucesso.";
                return RedirectToAction("Index");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return View(model);
        }

        public IActionResult ManageRoles(string id)
        {
            var model = _adminService.GetRoles(id);
            if (model == null) return NotFound();

            return View(model);
        }

        [HttpPost]
        public IActionResult AddRole(string userId, string role)
        {
            var result = _adminService.AddRole(userId, role);

            if (!result.Succeeded)
                TempData["ErrorMessage"] = string.Join(", ", result.Errors.Select(e => e.Description));
            else
                TempData["SuccessMessage"] = "Perfil adicionado com sucesso.";

            return RedirectToAction("ManageRoles", new { id = userId });
        }


        [HttpPost]
        public IActionResult RemoveRole(string userId, string role)
        {
            var result = _adminService.RemoveRole(userId, role);

            if (!result.Succeeded)
                TempData["ErrorMessage"] = string.Join(", ", result.Errors.Select(e => e.Description));
            else
                TempData["SuccessMessage"] = "Perfil removido com sucesso.";

            return RedirectToAction("ManageRoles", new { id = userId });
        }
    }
}
