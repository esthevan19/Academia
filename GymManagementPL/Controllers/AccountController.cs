using GymManagementBLL.ViewModels.AccountViewModels;
using GymManagementDAL.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace GymManagementPL.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [HttpGet]
    public IActionResult Login() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel viewModel)
    {
        if (!ModelState.IsValid) return View(viewModel);

        // Nunca conferir a senha antecipadamente: o Identity precisa contabilizar
        // tentativas inválidas para aplicar o bloqueio temporário da conta.
        var email = viewModel.Email?.Trim();
        var user = string.IsNullOrWhiteSpace(email)
            ? null
            : await _userManager.FindByEmailAsync(email);

        if (user is null)
        {
            ModelState.AddModelError("InvalidLogin", "E-mail ou senha incorretos.");
            return View(viewModel);
        }

        var result = await _signInManager.PasswordSignInAsync(
            user, viewModel.Password, viewModel.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
            return RedirectToAction("Index", "Home");

        ModelState.AddModelError("InvalidLogin", result.IsLockedOut
            ? "Conta temporariamente bloqueada. Tente novamente mais tarde."
            : result.IsNotAllowed
                ? "Sua conta não está autorizada a entrar."
                : "E-mail ou senha incorretos.");

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult AccessDenied() => View();
}
