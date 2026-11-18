using GymManagementBLL.Services.Interfaces;
using GymManagementBLL.ViewModels.PlanViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymManagementPL.Controllers
{
    [Authorize]
    public class PlanController : Controller
    {

        private readonly IPlanService _planService;

        public PlanController(IPlanService planService)
        {
            _planService = planService;
        }

        public IActionResult Index()
        {
            var plans = _planService.GetAllPlans();
            return View(plans);
        }

        public IActionResult Details(int id)
        {
            if (id <= 0)
            {
                TempData["ErrorMessage"] = "Identificador do plano inválido.";
                return RedirectToAction(nameof(Index));
            }

            var plan = _planService.GetPlanDetails(id);

            if (plan == null)
            {
                TempData["ErrorMessage"] = $"Plano não encontrado: {id}";
                return RedirectToAction(nameof(Index));
            }

            return View(plan);

        }

        [Authorize(Roles = "SuperAdmin,Admin,Gerente,Recepcionista")]
        public IActionResult Edit(int id)
        {
            if (id <= 0)
            {
                TempData["ErrorMessage"] = "Identificador do plano inválido.";
                return RedirectToAction(nameof(Index));
            }

            var plan = _planService.GetPlanToUpdate(id);

            if (plan == null)
            {
                TempData["ErrorMessage"] = "Não foi possível localizar o plano para editar.";
                return RedirectToAction(nameof(Index));
            }

            return View(plan);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin,Admin,Gerente,Recepcionista")]
        public IActionResult Edit(int id, UpdatePlanViewModel updatedPlan)
        {
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("WrongData", "Confira as informações do plano.");
                return View(updatedPlan);
            }

            var result = _planService.UpdatePlan(id, updatedPlan);

            if (result)
                TempData["SuccessMessage"] = "Plano atualizado com sucesso!";
            else
                TempData["ErrorMessage"] = "Não foi possível atualizar o plano.";

            return RedirectToAction(nameof(Index));

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin,Admin,Gerente,Recepcionista")]
        public IActionResult Activate(int id)
        {

            if (id <= 0)
            {
                TempData["ErrorMessage"] = "Identificador do plano inválido.";
                return RedirectToAction(nameof(Index));
            }

            var result = _planService.PlanStatusToggle(id);

            if (result)
                TempData["SuccessMessage"] = "Situação do plano atualizada com sucesso!";
            else
                TempData["ErrorMessage"] = "Não foi possível alterar a situação do plano.";

            return RedirectToAction(nameof(Index));

        }
    }
}
