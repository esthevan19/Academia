using GymManagementBLL.Services.Interfaces;
using GymManagementBLL.ViewModels.MemberViewModels;
using Microsoft.AspNetCore.Mvc;

namespace GymManagementPL.Controllers
{
    // Controller público - qualquer pessoa pode acessar sem estar logada,
    // por isso NÃO tem [Authorize] aqui em cima.
    public class RegisterController : Controller
    {
        #region Fields

        private readonly IMemberService _memberService;

        #endregion

        #region Constructors

        public RegisterController(IMemberService memberService)
        {
            _memberService = memberService;
        }

        #endregion

        #region Register

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Index(CreateMemberViewModel createdMember)
        {
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("DataMissed", "Verifique os campos obrigatórios");
                return View(createdMember);
            }

            bool result = _memberService.CreateMember(createdMember);

            if (result)
            {
                TempData["SuccessMessage"] = "Cadastro realizado com sucesso! Faça login para continuar.";
                return RedirectToAction("Login", "Account");
            }

            ModelState.AddModelError("DataMissed", "Não foi possível concluir o cadastro. Verifique se o Email ou Telefone já estão cadastrados.");
            return View(createdMember);
        }

        #endregion
    }
}
