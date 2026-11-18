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
            if (id <= 0) return RedirectToAction(nameof(Index));
            var session = _bookingService.GetAllSessions()
                .FirstOrDefault(s => s.Id == id && s.Status == "Upcoming");
            if (session is null || session.AvailableSlots <= 0)
            {
                TempData["ErrorMessage"] = "Esta aula não está disponível para novas reservas.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.SessionId = id;
            ViewBag.SessionName = session.CategoryName;
            ViewBag.Members = new SelectList(_bookingService.GetMembersForDropDown(id), "Id", "Name");
            return View(new CreateBookingViewModel { SessionId = id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin,Admin,Gerente,Recepcionista")]
        public IActionResult Create(CreateBookingViewModel createdBooking)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Selecione um aluno e uma aula válidos.";
                return RedirectToAction(nameof(GetMembersForUpcomingSession), new { id = createdBooking.SessionId });
            }

            var result = _bookingService.CreateNewBooking(createdBooking);

            if (result)
                TempData["SuccessMessage"] = "Reserva realizada com sucesso.";
            else
                TempData["ErrorMessage"] = "Não foi possível criar a reserva.";

            return RedirectToAction(nameof(GetMembersForUpcomingSession), new { id = createdBooking.SessionId });
        }

        public IActionResult GetMembersForOngoingSession(int id)
        {
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


