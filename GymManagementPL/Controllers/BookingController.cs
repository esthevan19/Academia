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
            var members = _bookingService.GetMembersSession(id);
            return View(members);
        }

        public IActionResult Create(int id)
        {
            var members = _bookingService.GetMembersForDropDown(id);
            ViewBag.members = new SelectList(members, "Id", "Name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(CreateBookingViewModel createdBooking)
        {
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


