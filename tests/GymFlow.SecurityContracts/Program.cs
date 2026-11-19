using System.Reflection;
using GymManagementPL.Controllers;
using GymManagementPL.Controllers.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

// Verifica permissões diretamente nos metadados compilados dos controllers.
// Não acessa nem altera o banco de dados da academia.
var assertions = 0;

void AssertRole(Type controller, string? action, params string[] expected)
{
    var attributes = action is null
        ? controller.GetCustomAttributes<AuthorizeAttribute>(inherit: true).ToArray()
        : controller.GetMethods().Single(m => m.Name == action)
            .GetCustomAttributes<AuthorizeAttribute>(inherit: true).ToArray();

    var roleLists = attributes
        .Where(a => !string.IsNullOrWhiteSpace(a.Roles))
        .Select(a => a.Roles!.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        .ToList();
    if (!roleLists.Any(actual => new HashSet<string>(actual, StringComparer.OrdinalIgnoreCase)
            .SetEquals(expected)))
        throw new Exception($"Permissão inesperada: {controller.Name}.{action ?? "(controller)"}, esperado: {string.Join(",", expected)}");

    assertions++;
}

void AssertProtectedWithJwt(Type controller)
{
    if (!controller.GetCustomAttributes<AuthorizeAttribute>(true)
        .Any(a => a.AuthenticationSchemes?.Contains("Bearer", StringComparison.OrdinalIgnoreCase) == true))
        throw new Exception($"Controller sem proteção JWT: {controller.Name}");
    assertions++;
}

AssertRole(typeof(MembersApiController), null, "SuperAdmin", "Admin", "Gerente", "Recepcionista");
AssertRole(typeof(MembersApiController), nameof(MembersApiController.Delete), "SuperAdmin", "Admin", "Gerente");
AssertRole(typeof(MembershipsApiController), null, "SuperAdmin", "Admin", "Gerente", "Recepcionista");
AssertRole(typeof(MembershipsApiController), nameof(MembershipsApiController.Delete), "SuperAdmin", "Admin", "Gerente");
AssertRole(typeof(PlansApiController), null, "SuperAdmin", "Admin", "Gerente");
AssertRole(typeof(AdminsApiController), null, "SuperAdmin");
AssertRole(typeof(MyAccountApiController), null, "Aluno");
AssertRole(typeof(CheckInController), null, "Aluno");
AssertRole(typeof(BookingsApiController), nameof(BookingsApiController.Create), "SuperAdmin", "Admin", "Gerente", "Recepcionista");
AssertRole(typeof(BookingsApiController), nameof(BookingsApiController.MarkAttendance), "SuperAdmin", "Admin", "Gerente", "Recepcionista", "Instrutor");
AssertRole(typeof(BookingsApiController), nameof(BookingsApiController.Cancel), "SuperAdmin", "Admin", "Gerente", "Recepcionista");
AssertRole(typeof(SessionsApiController), nameof(SessionsApiController.Create), "SuperAdmin", "Admin", "Gerente", "Recepcionista");

foreach (var controller in new[] {
    typeof(MembersApiController), typeof(MembershipsApiController),
    typeof(PlansApiController), typeof(AdminsApiController),
    typeof(MyAccountApiController), typeof(CheckInController),
    typeof(BookingsApiController), typeof(WorkoutsController),
    typeof(SessionsApiController)
}) AssertProtectedWithJwt(controller);

var logout = typeof(AccountController).GetMethod(nameof(AccountController.Logout))!;
if (!logout.IsDefined(typeof(HttpPostAttribute), true) ||
    !logout.IsDefined(typeof(ValidateAntiForgeryTokenAttribute), true))
    throw new Exception("Logout do site deve exigir POST e token antifalsificação.");
assertions++;

Console.WriteLine($"GymFlow: {assertions} verificações de segurança aprovadas.");
