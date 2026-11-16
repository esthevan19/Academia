using AutoMapper;
using GymManagementBLL.Services.Interfaces;
using GymManagementBLL.ViewModels.AdminManamentViewModels;
using GymManagementDAL.Entities;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GymManagementBLL.Services.Classes
{
    public class AdminService : IAdminService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IMapper _mapper;

        public AdminService(UserManager<ApplicationUser> userManager,
                            RoleManager<IdentityRole> roleManager,
                            IMapper mapper)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _mapper = mapper;
        }

        // =========================
        // Get all Admins
        // =========================
        public IEnumerable<AdminListViewModel> GetAllAdmins()
        {
            var users = _userManager.Users.ToList();
            var list = new List<AdminListViewModel>();
            foreach (var user in users)
            {
                var roles = _userManager.GetRolesAsync(user).Result;
                list.Add(new AdminListViewModel
                {
                    Id = user.Id,
                    Email = user.Email ?? string.Empty,
                    UserName = user.UserName ?? string.Empty,
                    Role = roles.FirstOrDefault() ?? "Sem Perfil"
                });
            }
            return list;
        }

        // =========================
        // Create Admin / User with Role
        // =========================
        public IdentityResult CreateAdmin(CreateAdminViewModel model)
        {
            var user = _mapper.Map<ApplicationUser>(model);

            var result = _userManager.CreateAsync(user, model.Password).Result;

            if (result.Succeeded)
            {
                var role = string.IsNullOrWhiteSpace(model.Role) ? "Admin" : model.Role;
                if (!_roleManager.RoleExistsAsync(role).Result)
                {
                    _roleManager.CreateAsync(new IdentityRole(role)).Wait();
                }
                _userManager.AddToRoleAsync(user, role).Wait();
            }

            return result;
        }



        // =========================
        // Edit Admin
        // =========================

        public EditAdminViewModel GetAdminForUpdate(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null!;

            var user = _userManager.FindByIdAsync(id).Result;

            if (user == null)
                return null!;

            return _mapper.Map<EditAdminViewModel>(user);
        }


        public IdentityResult EditAdmin(EditAdminViewModel model)
        {
            var user = _userManager.FindByIdAsync(model.Id).Result;
            if (user == null)
                return IdentityResult.Failed(new IdentityError { Description = "User not found" });

            _mapper.Map(model, user);

            return _userManager.UpdateAsync(user).Result;
        }

        // =========================
        // Delete Admin
        // =========================
        public IdentityResult DeleteAdmin(string id)
        {
            var user = _userManager.FindByIdAsync(id).Result;
            if (user == null)
                return IdentityResult.Failed(new IdentityError { Description = "User not found" });

            return _userManager.DeleteAsync(user).Result;
        }

        // =========================
        // Reset Password
        // =========================
        public IdentityResult ResetPassword(ResetPasswordViewModel model)
        {
            var user = _userManager.FindByIdAsync(model.Id).Result;
            if (user == null)
                return IdentityResult.Failed(new IdentityError { Description = "User not found" });

            var token = _userManager.GeneratePasswordResetTokenAsync(user).Result;
            return _userManager.ResetPasswordAsync(user, token, model.NewPassword).Result;
        }

        // =========================
        // Get Roles
        // =========================
        public ManageRolesViewModel GetRoles(string userId)
        {
            var user = _userManager.FindByIdAsync(userId).Result;
            if (user == null) return null;

            var roles = _roleManager.Roles.Select(r => r.Name).ToList();
            var userRoles = _userManager.GetRolesAsync(user).Result;

            var model = _mapper.Map<ManageRolesViewModel>(user);
            model.AllRoles = roles;
            model.UserRoles = userRoles.ToList();

            return model;
        }

        // =========================
        // Add Role
        // =========================
        public IdentityResult AddRole(string userId, string role)
        {
            var user = _userManager.FindByIdAsync(userId).Result;
            if (user == null)
                return IdentityResult.Failed(new IdentityError { Description = "User not found" });

            var result = _userManager.AddToRoleAsync(user, role).Result;
            return result;
        }

        // =========================
        // Remove Role
        // =========================
        public IdentityResult RemoveRole(string userId, string role)
        {
            var user = _userManager.FindByIdAsync(userId).Result;
            if (user == null)
                return IdentityResult.Failed(new IdentityError { Description = "User not found" });

            var result = _userManager.RemoveFromRoleAsync(user, role).Result;
            return result;
        }

    }
}
