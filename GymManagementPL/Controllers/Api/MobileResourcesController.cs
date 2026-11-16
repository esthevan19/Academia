using GymManagementBLL.Services.Interfaces;
using GymManagementBLL.ViewModels.TrainerViewModels;
using GymManagementBLL.ViewModels.SessionViewModels;
using GymManagementBLL.ViewModels.MembershipViewModels;
using GymManagementBLL.ViewModels.BookingViewModels;
using GymManagementBLL.ViewModels.MemberViewModels;
using GymManagementBLL.ViewModels.PlanViewModels;
using GymManagementDAL.Data.Contexts;
using GymManagementDAL.Entities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GymManagementPL.Controllers.Api;

// Os serviços existentes do MVC são reutilizados para gravar no mesmo banco.
[ApiController, Route("api/trainers")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class TrainersMobileController(ITrainerService service) : ControllerBase
{
    [HttpGet, Authorize(Roles = "SuperAdmin,Admin,Gerente,Recepcionista,Instrutor")]
    public IActionResult List() => Ok(new { success = true, trainers = service.GetAllTrainers() });

    [HttpGet("{id:int}"), Authorize(Roles = "SuperAdmin,Admin,Gerente,Recepcionista,Instrutor")]
    public IActionResult Detail(int id) => service.GetTrainerDetails(id) is { } trainer ? Ok(new { success = true, trainer }) : NotFound();

    [HttpGet("{id:int}/edit"), Authorize(Roles = "SuperAdmin,Admin,Gerente")]
    public IActionResult Edit(int id) => service.GetTrainerToUpdate(id) is { } trainer ? Ok(new { success = true, trainer }) : NotFound();

    [HttpPost, Authorize(Roles = "SuperAdmin,Admin,Gerente")]
    public IActionResult Create([FromBody] CreateTrainerViewModel model) =>
        service.CreateTrainer(model) ? Ok(new { success = true }) : BadRequest(new { success = false, message = "Não foi possível cadastrar o instrutor." });

    [HttpPut("{id:int}"), Authorize(Roles = "SuperAdmin,Admin,Gerente")]
    public IActionResult Update(int id, [FromBody] TrainerToUpdateViewModel model) =>
        service.UpdateTrainer(id, model) ? Ok(new { success = true }) : BadRequest(new { success = false, message = "Não foi possível atualizar o instrutor." });

    [HttpDelete("{id:int}"), Authorize(Roles = "SuperAdmin,Admin,Gerente")]
    public IActionResult Delete(int id) =>
        service.RemoveTrainer(id) ? Ok(new { success = true }) : NotFound();
}

[ApiController, Route("api/sessions")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class SessionsMobileController(ISessionService service) : ControllerBase
{
    [HttpGet]
    public IActionResult List() => Ok(new { success = true, sessions = service.GetAllSessions() });

    [HttpGet("lookups"), Authorize(Roles = "SuperAdmin,Admin,Gerente")]
    public IActionResult Lookups() => Ok(new { success = true, trainers = service.GetTrainersForDropDown(), categories = service.GetCategoriesForDropDown() });

    [HttpGet("{id:int}")]
    public IActionResult Detail(int id) => service.GetSessionDetails(id) is { } session ? Ok(new { success = true, session }) : NotFound();

    [HttpGet("{id:int}/edit"), Authorize(Roles = "SuperAdmin,Admin,Gerente")]
    public IActionResult Edit(int id) => service.GetSessionToUpdate(id) is { } session ? Ok(new { success = true, session }) : NotFound();

    [HttpPost, Authorize(Roles = "SuperAdmin,Admin,Gerente")]
    public IActionResult Create([FromBody] CreateSessionViewModel model) =>
        service.CreateSession(model) ? Ok(new { success = true }) : BadRequest(new { success = false, message = "Não foi possível criar a sessão." });

    [HttpPut("{id:int}"), Authorize(Roles = "SuperAdmin,Admin,Gerente")]
    public IActionResult Update(int id, [FromBody] UpdateSessionViewModel model) =>
        service.UpdateSession(id, model) ? Ok(new { success = true }) : BadRequest(new { success = false, message = "Não foi possível atualizar a sessão." });

    [HttpDelete("{id:int}"), Authorize(Roles = "SuperAdmin,Admin,Gerente")]
    public IActionResult Delete(int id) => service.RemoveSession(id) ? Ok(new { success = true }) : NotFound();
}

[ApiController, Route("api/memberships")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "SuperAdmin,Admin,Gerente,Recepcionista")]
public class MembershipsMobileController(IMembershipService service) : ControllerBase
{
    [HttpGet]
    public IActionResult List() => Ok(new { success = true, memberships = service.GetAllMemberships() });

    [HttpPost]
    public IActionResult Create([FromBody] CreateMembershipViewModel model) =>
        service.CreateMembership(model) ? Ok(new { success = true }) : BadRequest(new { success = false, message = "Não foi possível criar matrícula." });

    [HttpDelete("{id:int}")]
    public IActionResult Delete(int id) => service.DeleteMembership(id) ? Ok(new { success = true }) : NotFound();
}

[ApiController, Route("api/bookings")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "SuperAdmin,Admin,Gerente,Recepcionista")]
public class BookingsMobileController(IBookingService service) : ControllerBase
{
    [HttpPost]
    public IActionResult Create([FromBody] CreateBookingViewModel model) =>
        service.CreateNewBooking(model) ? Ok(new { success = true }) : BadRequest(new { success = false, message = "Não foi possível salvar o agendamento." });

    [HttpDelete("{memberId:int}/{sessionId:int}")]
    public IActionResult Cancel(int memberId, int sessionId) =>
        service.CancelBooking(memberId, sessionId) ? Ok(new { success = true }) : NotFound();
}

[ApiController, Route("api/plans")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class PlansMobileController(IPlanService service, GymDbContext db) : ControllerBase
{
    [HttpGet]
    public IActionResult List() => Ok(new { success = true, plans = service.GetAllPlans() });

    [HttpGet("{id:int}")]
    public IActionResult Detail(int id) => service.GetPlanDetails(id) is { } plan ? Ok(new { success = true, plan }) : NotFound();

    [HttpPost, Authorize(Roles = "SuperAdmin,Admin,Gerente")]
    public async Task<IActionResult> Create([FromBody] UpdatePlanViewModel model)
    {
        var plan = new Plan { Name = model.PlanName, Description = model.Description, DurationDays = model.DurationDays, Price = model.Price, IsActive = true };
        db.Plans.Add(plan);
        await db.SaveChangesAsync();
        return Ok(new { success = true, plan });
    }

    [HttpPut("{id:int}"), Authorize(Roles = "SuperAdmin,Admin,Gerente")]
    public IActionResult Update(int id, [FromBody] UpdatePlanViewModel model) =>
        service.UpdatePlan(id, model) ? Ok(new { success = true }) : NotFound();

    [HttpPost("{id:int}/toggle"), Authorize(Roles = "SuperAdmin,Admin,Gerente")]
    public IActionResult Toggle(int id) => service.PlanStatusToggle(id) ? Ok(new { success = true }) : NotFound();
}
