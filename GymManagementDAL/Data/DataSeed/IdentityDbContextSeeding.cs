using GymManagementDAL.Entities;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GymManagementDAL.Data.DataSeed
{
    public static class IdentityDbContextSeeding
    {

        public static bool SeedData(RoleManager<IdentityRole> roleManager, UserManager<ApplicationUser> userManager)
        {
            try
            {
                // Garante que as roles existem, sem apagar nada
                var roles = new List<string> { "SuperAdmin", "Admin", "Gerente", "Recepcionista", "Instrutor", "Aluno" };

                foreach (var roleName in roles)
                {
                    if (!roleManager.RoleExistsAsync(roleName).Result)
                    {
                        roleManager.CreateAsync(new IdentityRole(roleName)).Wait();
                    }
                }

                // Cria cada usuário só se ele ainda não existir (checagem individual por email)
                CreateUserIfNotExists(
                    userManager,
                    email: "esthevan2@gmail.com",
                    userName: "esthevan2",
                    firstName: "Esthevan",
                    lastName: "Santos",
                    phone: "51994262910",
                    password: "Senha1234!",
                    role: "SuperAdmin"
                );

                CreateUserIfNotExists(
                    userManager,
                    email: "esthevan@gmail.com",
                    userName: "esthevan",
                    firstName: "Esthevan",
                    lastName: "Santos",
                    phone: "51994262911",
                    password: "Senha1234!",
                    role: "Admin"
                );

                CreateUserIfNotExists(
                    userManager,
                    email: "gerente@gymflow.com",
                    userName: "gerente",
                    firstName: "Gerente",
                    lastName: "Geral",
                    phone: "51994262912",
                    password: "Senha1234!",
                    role: "Gerente"
                );

                CreateUserIfNotExists(
                    userManager,
                    email: "recepcao@gymflow.com",
                    userName: "recepcao",
                    firstName: "Recepcionista",
                    lastName: "Atendimento",
                    phone: "51994262913",
                    password: "Senha1234!",
                    role: "Recepcionista"
                );

                CreateUserIfNotExists(
                    userManager,
                    email: "instrutor@gymflow.com",
                    userName: "instrutor",
                    firstName: "Instrutor",
                    lastName: "Fitness",
                    phone: "51994262914",
                    password: "Senha1234!",
                    role: "Instrutor"
                );

                CreateUserIfNotExists(
                    userManager,
                    email: "aluno@gymflow.com",
                    userName: "aluno",
                    firstName: "Aluno",
                    lastName: "GymFlow",
                    phone: "51994262915",
                    password: "Senha1234!",
                    role: "Aluno"
                );

                return true;

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed To Seed Data : {ex}");
                return false;
            }
        }

        private static void CreateUserIfNotExists(
            UserManager<ApplicationUser> userManager,
            string email,
            string userName,
            string firstName,
            string lastName,
            string phone,
            string password,
            string role)
        {
            var existingUser = userManager.FindByEmailAsync(email).Result;

            if (existingUser != null)
                return; // já existe, não faz nada

            var newUser = new ApplicationUser()
            {
                FirstName = firstName,
                LastName = lastName,
                UserName = userName,
                Email = email,
                PhoneNumber = phone
            };

            var result = userManager.CreateAsync(newUser, password).Result;

            if (result.Succeeded)
            {
                userManager.AddToRoleAsync(newUser, role).Wait();
            }
            else
            {
                var errors = string.Join(" | ", result.Errors.Select(e => e.Description));
                Console.WriteLine($"Falha ao criar usuário {email}: {errors}");
            }
        }

    }
}
