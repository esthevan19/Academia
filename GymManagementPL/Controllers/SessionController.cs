using GymManagementBLL.Services.Classes;
using GymManagementBLL.Services.Interfaces;
using GymManagementBLL.ViewModels.SessionViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GymManagementPL.Controllers
{
    [Authorize]
    public class SessionController : Controller
    {
        private readonly ISessionService _sessionService;

        public SessionController(ISessionService sessionService)
        {
            _sessionService = sessionService;
        }
        public IActionResult Index()
        {
            var sessions = _sessionService.GetAllSessions();
            return View(sessions);
        }

        public IActionResult Details(int id)
        {
            if (id <= 0)
            {
                TempData["ErrorMessage"] = "ID de Aula inválido. Forneça um identificador válido.";
                return RedirectToAction(nameof(Index));
            }

            var session = _sessionService.GetSessionDetails(id);

            if (session == null)
            {
                TempData["ErrorMessage"] = $"Nenhuma Aula encontrada com o ID: {id}. Verifique e tente novamente.";
                return RedirectToAction(nameof(Index));
            }

            return View(session);
        }

        [Authorize(Roles = "SuperAdmin,Admin,Gerente,Recepcionista,Instrutor")]
        public IActionResult Create()
        {
            LoadCategoriesDropDown();
            LoadTrainersDropDown();
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "SuperAdmin,Admin,Gerente,Recepcionista,Instrutor")]
        public IActionResult Create(CreateSessionViewModel createdSession)
        {
            if (!ModelState.IsValid)
            {
                LoadCategoriesDropDown();
                LoadTrainersDropDown();
                return View(createdSession);
            }

            var result = _sessionService.CreateSession(createdSession);

            if (result)
            {
                TempData["SuccessMessage"] = "Aula criada com sucesso";
                return RedirectToAction(nameof(Index));
            }
            else
            {
                ModelState.AddModelError("DataMissed", "Falha ao criar a aula. Verifique se o professor e o tipo de aula existem, e se o horário informado é válido (o término deve ser depois do início e no futuro).");
                LoadCategoriesDropDown();
                LoadTrainersDropDown();
                return View(createdSession);
            }

        }

        [Authorize(Roles = "SuperAdmin,Admin,Gerente,Recepcionista,Instrutor")]
        public IActionResult Edit(int id)
        {
            if (id <= 0)
            {
                TempData["ErrorMessage"] = "ID de Aula inválido. Forneça um identificador válido.";
                return RedirectToAction(nameof(Index));
            }

            var session = _sessionService.GetSessionToUpdate(id);

            if (session == null)
            {
                TempData["ErrorMessage"] = $"Não é possível atualizar esta Aula";
                return RedirectToAction(nameof(Index));
            }

            LoadTrainersDropDown();
            return View(session);

        }

        [HttpPost]
        [Authorize(Roles = "SuperAdmin,Admin,Gerente,Recepcionista,Instrutor")]
        public IActionResult Edit(int id, UpdateSessionViewModel updatedSession)
        {

            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("DataMissed", "Não é possível atualizar esta Aula");
                LoadTrainersDropDown();
                return View(updatedSession);
            }

            var result = _sessionService.UpdateSession(id, updatedSession);

            if (result)
            {
                TempData["SuccessMessage"] = "Aula atualizada com sucesso";
                return RedirectToAction(nameof(Index));
            }
            else
            {
                ModelState.AddModelError("DataMissed", "Falha ao atualizar a aula");
                LoadTrainersDropDown();
                return View(updatedSession);
            }

        }

        [Authorize(Roles = "SuperAdmin,Admin,Gerente,Recepcionista,Instrutor")]
        public IActionResult Delete(int id)
        {
            if (id <= 0)
            {
                TempData["ErrorMessage"] = "ID de Aula inválido. Forneça um identificador válido.";
                return RedirectToAction(nameof(Index));
            }

            var sessionToDelete = _sessionService.GetSessionDetails(id);

            if (sessionToDelete == null)
            {
                TempData["ErrorMessage"] = $"Nenhuma Aula encontrada com o ID: {id}. Verifique e tente novamente.";
                return RedirectToAction(nameof(Index));
            }
            ViewBag.SessionId = id;
            return View(sessionToDelete);
        }

        [HttpPost]
        [Authorize(Roles = "SuperAdmin,Admin,Gerente,Recepcionista,Instrutor")]
        public IActionResult DeleteConfirmed([FromForm] int id)
        {
            var result = _sessionService.RemoveSession(id);

            if (result)
                TempData["SuccessMessage"] = "Aula removida com sucesso";
            else
                TempData["ErrorMessage"] = "A Aula não foi removida";

            return RedirectToAction(nameof(Index));
        }

        #region HelperMethod

        private void LoadCategoriesDropDown()
        {
            var categories = _sessionService.GetCategoriesForDropDown();
            ViewBag.Categories = new SelectList(categories, "Id", "Name");
        }

        private void LoadTrainersDropDown()
        {
            var trainers = _sessionService.GetTrainersForDropDown();
            ViewBag.Trainers = new SelectList(trainers, "Id", "Name");
        }

        #endregion

    }
}
