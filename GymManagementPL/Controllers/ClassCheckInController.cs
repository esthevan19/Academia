using GymManagementPL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymManagementPL.Controllers;

[Authorize(Roles = "SuperAdmin,Admin,Gerente,Recepcionista,Instrutor")]
public sealed class ClassCheckInController : Controller
{
    private readonly ClassAttendanceService _attendance;
    public ClassCheckInController(ClassAttendanceService attendance) => _attendance = attendance;

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
        View(await _attendance.ActiveSessionsAsync(cancellationToken));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(int sessionId, string? code, CancellationToken cancellationToken)
    {
        var result = await _attendance.ConfirmAsync(sessionId, code, cancellationToken);
        if (result.Success)
            TempData["SuccessMessage"] = result.MemberName + " — " + result.Message;
        else
            TempData["ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }
}
