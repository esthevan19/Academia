using GymManagementDAL.Entities;
using GymManagementPL.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace GymManagementPL.Controllers;

[AllowAnonymous]
public sealed class RegisterController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public RegisterController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    [HttpGet]
    public IActionResult Index()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");
        return View(new CadastroPublicoViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(CadastroPublicoViewModel model)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");

        if (!ModelState.IsValid) return View(model);

        var email = model.Email.Trim();
        if (await _userManager.FindByEmailAsync(email) is not null)
        {
            ModelState.AddModelError(nameof(model.Email), "Este e-mail já possui uma conta. Faça login.");
            return View(model);
        }

        // Cadastro público nunca cria perfis com permissões administrativas.
        if (!await _roleManager.RoleExistsAsync("Aluno"))
        {
            ModelState.AddModelError(string.Empty, "Cadastro temporariamente indisponível. Contate a academia.");
            return View(model);
        }

        var usuario = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FirstName = string.Empty,
            LastName = string.Empty
        };

        var resultado = await _userManager.CreateAsync(usuario, model.Password);
        if (!resultado.Succeeded)
        {
            foreach (var erro in resultado.Errors)
            {
                var mensagem = erro.Code switch
                {
                    "DuplicateEmail" or "DuplicateUserName" => "Este e-mail já possui uma conta.",
                    "PasswordTooShort" => "A senha precisa ter pelo menos 8 caracteres.",
                    "PasswordRequiresDigit" => "Inclua ao menos um número na senha.",
                    "PasswordRequiresUpper" => "Inclua ao menos uma letra maiúscula na senha.",
                    "PasswordRequiresLower" => "Inclua ao menos uma letra minúscula na senha.",
                    "PasswordRequiresNonAlphanumeric" => "Inclua ao menos um caractere especial na senha.",
                    _ => "Não foi possível concluir o cadastro. Confira seus dados."
                };
                ModelState.AddModelError(string.Empty, mensagem);
            }
            return View(model);
        }

        var vinculo = await _userManager.AddToRoleAsync(usuario, "Aluno");
        if (!vinculo.Succeeded)
        {
            await _userManager.DeleteAsync(usuario);
            ModelState.AddModelError(string.Empty, "Não foi possível finalizar o cadastro. Tente novamente.");
            return View(model);
        }

        // Uma conta de acesso não cria matrícula, ficha clínica ou vínculo a outro aluno.
        // O vínculo ao cadastro de aluno existente é realizado somente pela equipe autorizada.
        TempData["SuccessMessage"] = "Conta criada! Entre com seu e-mail e senha. Para consultar sua matrícula, peça à recepção para vincular sua conta ao cadastro de aluno.";
        return RedirectToAction("Login", "Account");
    }
}
