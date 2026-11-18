using GymManagementBLL.Services.Classes;
using GymManagementBLL.Services.Interfaces;
using GymManagementBLL.ViewModels.AdminManamentViewModels;
using GymManagementDAL.Entities;
using GymManagementDAL.Data.Contexts;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace GymManagementPL.Controllers
{
    [Authorize(Roles = "SuperAdmin,Admin")]
    public class AdminManagementController : Controller
    {
        private readonly IAdminService _adminService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly GymDbContext _db;

        public AdminManagementController(IAdminService adminService, UserManager<ApplicationUser> userManager, GymDbContext db)
        {
            _adminService = adminService;
            _userManager = userManager;
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var users = _adminService.GetAllUsers();
            if (User.IsInRole("SuperAdmin"))
            {
                ViewBag.MemberOptions = await _db.Members.AsNoTracking()
                    .OrderBy(member => member.Name)
                    .Select(member => new SelectListItem
                    {
                        Value = member.Id.ToString(),
                        Text = member.Name + " (ID " + member.Id + ")"
                    }).ToListAsync();
            }
            return View(users);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> LinkMember(string userId, int memberId)
        {
            if (string.IsNullOrWhiteSpace(userId) || memberId <= 0)
            {
                TempData["ErrorMessage"] = "Selecione uma conta e um aluno válidos.";
                return RedirectToAction(nameof(Index));
            }
            var user = await _userManager.FindByIdAsync(userId);
            if (user is null || !await _userManager.IsInRoleAsync(user, "Aluno"))
            {
                TempData["ErrorMessage"] = "Somente uma conta de Aluno pode ser vinculada.";
                return RedirectToAction(nameof(Index));
            }
            if (!await _db.Members.AnyAsync(member => member.Id == memberId))
            {
                TempData["ErrorMessage"] = "Cadastro do aluno não encontrado.";
                return RedirectToAction(nameof(Index));
            }

            const string claimType = "gymflow:member_id";
            var newLink = new Claim(claimType, memberId.ToString());
            var existingOwners = await _userManager.GetUsersForClaimAsync(newLink);
            if (existingOwners.Any(owner => owner.Id != user.Id))
            {
                TempData["ErrorMessage"] = "Esse cadastro já está vinculado a outra conta.";
                return RedirectToAction(nameof(Index));
            }

            var previous = (await _userManager.GetClaimsAsync(user))
                .Where(claim => claim.Type == claimType).ToList();
            if (previous.Count == 1 && previous[0].Value == memberId.ToString())
            {
                TempData["SuccessMessage"] = "Esse aluno já está vinculado à conta.";
                return RedirectToAction(nameof(Index));
            }

            if (previous.Count > 0)
            {
                var removed = await _userManager.RemoveClaimsAsync(user, previous);
                if (!removed.Succeeded)
                {
                    TempData["ErrorMessage"] = "Não foi possível substituir o vínculo anterior.";
                    return RedirectToAction(nameof(Index));
                }
            }

            var result = await _userManager.AddClaimAsync(user, newLink);
            if (!result.Succeeded && previous.Count > 0)
                await _userManager.AddClaimsAsync(user, previous);

            TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Succeeded
                ? "Aluno vinculado! Ele deve sair e entrar novamente no site e no aplicativo."
                : "Não foi possível salvar o vínculo.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> UnlinkMember(string userId)
        {
            var user = string.IsNullOrWhiteSpace(userId)
                ? null : await _userManager.FindByIdAsync(userId);
            if (user is null)
            {
                TempData["ErrorMessage"] = "Conta não encontrada.";
                return RedirectToAction(nameof(Index));
            }

            var claims = (await _userManager.GetClaimsAsync(user))
                .Where(claim => claim.Type == "gymflow:member_id").ToList();
            var removed = claims.Count == 0
                ? IdentityResult.Success
                : await _userManager.RemoveClaimsAsync(user, claims);

            TempData[removed.Succeeded ? "SuccessMessage" : "ErrorMessage"] = removed.Succeeded
                ? "Vínculo removido. O aluno deve entrar novamente."
                : "Não foi possível remover o vínculo.";
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Create()
        {
            return View();
        } 


        [HttpPost]
        public IActionResult Create(CreateAdminViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            if (!User.IsInRole("SuperAdmin") && model.Role == "SuperAdmin") return Forbid();

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


        public async Task<IActionResult> Edit(string id)
        {
            if (await IsSuperAdminTargetForOtherRole(id)) return Forbid();
            var model = _adminService.GetAdminForUpdate(id);

            if (model == null)
                return NotFound();

            return View(model);
        }


        [HttpPost]
        public async Task<IActionResult> Edit(EditAdminViewModel model)
        {
            if (await IsSuperAdminTargetForOtherRole(model.Id)) return Forbid();
            if (!ModelState.IsValid) return View(model);

            var result = _adminService.EditAdmin(model);

            if (result.Succeeded)
                return RedirectToAction("Index");

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return View(model);
        }


        public async Task<IActionResult> Delete(string id)
        {
            if (await IsSuperAdminTargetForOtherRole(id)) return Forbid();
            ViewBag.AdminId = id;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            if (await IsSuperAdminTargetForOtherRole(id)) return Forbid();
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

        [Authorize(Roles = "SuperAdmin")]
        public IActionResult ResetPassword(string id)
        {
            return View(new ResetPasswordViewModel { Id = id });
        }


        [HttpPost]
        [Authorize(Roles = "SuperAdmin")]
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

        [Authorize(Roles = "SuperAdmin")]
        public IActionResult ManageRoles(string id)
        {
            var model = _adminService.GetRoles(id);
            if (model == null) return NotFound();

            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "SuperAdmin")]
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
        [Authorize(Roles = "SuperAdmin")]
        public IActionResult RemoveRole(string userId, string role)
        {
            var result = _adminService.RemoveRole(userId, role);

            if (!result.Succeeded)
                TempData["ErrorMessage"] = string.Join(", ", result.Errors.Select(e => e.Description));
            else
                TempData["SuccessMessage"] = "Perfil removido com sucesso.";

            return RedirectToAction("ManageRoles", new { id = userId });
        }
        private async Task<bool> IsSuperAdminTargetForOtherRole(string? id)
        {
            if (User.IsInRole("SuperAdmin")) return false;
            if (string.IsNullOrWhiteSpace(id)) return true;
            var user = await _userManager.FindByIdAsync(id);
            return user is not null && await _userManager.IsInRoleAsync(user, "SuperAdmin");
        }

    }
}
