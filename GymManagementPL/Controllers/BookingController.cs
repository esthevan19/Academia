using GymManagementBLL.Services.Classes;
using GymManagementBLL.Services.Interfaces;
using GymManagementBLL.ViewModels.BookingViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GymManagementPL.Controllers
{
    [Authorize(Roles = "SuperAdmin,Admin,Gerente,Recepcionista,Instrutor")]
    public class BookingController : Controller
    {
        private readonly IBookingService _bookingService;

        public BookingController(IBookingService bookingService)
        {
            _bookingService = bookingService;
        }

        public IActionResult Index()
        {
            var sessionToBook = _bookingService.GetAllSessions();
            return View(sessionToBook);
        }

        public IActionResult GetMembersForUpcomingSession(int id)
        {
            if (id <= 0) return RedirectToAction(nameof(Index));
            var members = _bookingService.GetMembersSession(id);
            return View(members);
        }

        [Authorize(Roles = "SuperAdmin,Admin,Gerente,Recepcionista")]
        public IActionResult Create(int id)
        {
            if (!PrepareCreateForm(id))
            {
                TempData["ErrorMessage"] = "Esta aula não está disponível para novas reservas.";
                return RedirectToAction(nameof(Index));
            }
            return View(new CreateBookingViewModel { SessionId = id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin,Admin,Gerente,Recepcionista")]
        public IActionResult Create(CreateBookingViewModel createdBooking)
        {
            if (!PrepareCreateForm(createdBooking.SessionId))
            {
                TempData["ErrorMessage"] = "Esta aula não está mais disponível para reservas.";
                return RedirectToAction(nameof(Index));
            }

            if (!ModelState.IsValid)
                return View(createdBooking);

            if (!_bookingService.CreateNewBooking(createdBooking))
            {
                ModelState.AddModelError("", "Não foi possível reservar. Confira a matrícula ativa, as vagas e se o aluno já possui reserva nesta aula.");
                return View(createdBooking);
            }

            TempData["SuccessMessage"] = "Reserva realizada com sucesso.";
            return RedirectToAction(nameof(GetMembersForUpcomingSession), new { id = createdBooking.SessionId });
        }

        private bool PrepareCreateForm(int sessionId)
        {
            if (sessionId <= 0) return false;
            var session = _bookingService.GetAllSessions()
                .FirstOrDefault(s => s.Id == sessionId && s.Status == "Upcoming" && s.AvailableSlots > 0);
            if (session is null) return false;

            ViewBag.SessionId = sessionId;
            ViewBag.SessionName = session.CategoryName;
            ViewBag.Members = new SelectList(_bookingService.GetMembersForDropDown(sessionId), "Id", "Name");
            return true;
        }

        public IActionResult GetMembersForOngoingSession(int id)
        {
            if (id <= 0) return RedirectToAction(nameof(Index));

            var ongoing = _bookingService.GetAllSessions()
                .Any(session => session.Id == id && session.Status == "Ongoing");
            if (!ongoing)
            {
                TempData["ErrorMessage"] = "A presença só pode ser registrada durante o horário da aula.";
                return RedirectToAction(nameof(Index));
            }

            var members = _bookingService.GetMembersSession(id);
            return View(members);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Attended(int MemberId, int SessionId)
        {
            var result = _bookingService.IsMemberAttended(MemberId, SessionId);

            if (result)
                TempData["SuccessMessage"] = "Presença registrada com sucesso.";
            else
                TempData["ErrorMessage"] = "Não foi possível registrar a presença.";

            return RedirectToAction(nameof(GetMembersForOngoingSession), new { id = SessionId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin,Admin,Gerente,Recepcionista")]
        public IActionResult Cancel(int MemberId, int SessionId)
        {
            var result = _bookingService.CancelBooking(MemberId, SessionId);

            if (result)
                TempData["SuccessMessage"] = "Reserva cancelada com sucesso.";
            else
                TempData["ErrorMessage"] = "Não foi possível cancelar a reserva.";

            return RedirectToAction(nameof(GetMembersForUpcomingSession), new { id = SessionId });
        }

    }
}


