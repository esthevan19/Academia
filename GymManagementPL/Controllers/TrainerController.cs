using GymManagementBLL.Services.Classes;
using GymManagementBLL.Services.Interfaces;
using GymManagementBLL.ViewModels.TrainerViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymManagementPL.Controllers
{
    [Authorize(Roles = "SuperAdmin,Admin,Gerente")]
    public class TrainerController : Controller
    {
        private readonly ITrainerService _trainerService;

        public TrainerController(ITrainerService trainerService)
        {
            _trainerService = trainerService;
        }
        public IActionResult Index()
        {
            var trainers = _trainerService.GetAllTrainers();
            return View(trainers);
        }

        public IActionResult TrainerDetails(int id)
        {
            if (id <= 0)
            {
                TempData["ErrorMessage"] = "Identificador do professor inválido.";
                return RedirectToAction(nameof(Index));
            }

            var trainer = _trainerService.GetTrainerDetails(id);

            if (trainer == null)
            {
                TempData["ErrorMessage"] = $"Nenhum professor encontrado com o código {id}.";
                return RedirectToAction(nameof(Index));
            }

            return View(trainer);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateTrainer(CreateTrainerViewModel createdTrainer)
        {

            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("DataMissed", "Confira os campos obrigatórios do cadastro.");
                return View(nameof(Create), createdTrainer);
            }

            var result = _trainerService.CreateTrainer(createdTrainer);

            if (result)
                TempData["SuccessMessage"] = $"Professor cadastrado com sucesso.";
            else
                TempData["ErrorMessage"] = $"Não foi possível cadastrar. Verifique se o e-mail ou telefone já estão cadastrados.";

            return RedirectToAction(nameof(Index));
        }

        public IActionResult EditTrainer(int id)
        {
            if (id <= 0)
            {
                TempData["ErrorMessage"] = "Identificador do professor inválido.";
                return RedirectToAction(nameof(Index));
            }

            var trainer = _trainerService.GetTrainerToUpdate(id);

            if (trainer == null)
            {
                TempData["ErrorMessage"] = $"Nenhum professor encontrado com o código {id}.";
                return RedirectToAction(nameof(Index));
            }

            return View(trainer);

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditTrainer([FromRoute] int id, TrainerToUpdateViewModel trainerToUpdate)
        {

            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("DataMissed", "Confira os dados preenchidos antes de salvar.");
                return View(trainerToUpdate);
            }

            var result = _trainerService.UpdateTrainer(id, trainerToUpdate);

            if (result)
                TempData["SuccessMessage"] = "Dados do professor atualizados com sucesso.";
            else
                TempData["ErrorMessage"] = "Não foi possível atualizar o professor. Verifique se e-mail ou telefone já existem.";

            return RedirectToAction(nameof(Index));

        }

        public IActionResult Delete([FromRoute] int id)
        {

            if (id <= 0)
            {
                TempData["ErrorMessage"] = "Identificador do professor inválido.";
                return RedirectToAction(nameof(Index));
            }

            var member = _trainerService.GetTrainerDetails(id);

            var trainer = _trainerService.GetTrainerDetails(id);

            if (trainer == null)
            {
                TempData["ErrorMessage"] = $"Nenhum professor encontrado com o código {id}.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.TrainerId = id;
            return View(trainer);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed([FromForm] int id)
        {

            var result = _trainerService.RemoveTrainer(id);

            if (result)
                TempData["SuccessMessage"] = "Professor excluído com sucesso.";
            else
                TempData["ErrorMessage"] = "Não foi possível excluir o professor. Verifique se há aulas futuras vinculadas.";

            return RedirectToAction(nameof(Index));

        }
    }
}
