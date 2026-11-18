using Microsoft.AspNetCore.Identity;

namespace GymManagementDAL.Data.DataSeed
{
    /// <summary>
    /// Registra somente os perfis de acesso necessários ao sistema.
    /// Contas de usuário e senhas devem ser cadastradas de forma individual e segura.
    /// </summary>
    public static class IdentityDbContextSeeding
    {
        public static bool SeedRoles(RoleManager<IdentityRole> roleManager)
        {
            try
            {
                var roles = new[] { "SuperAdmin", "Admin", "Gerente", "Recepcionista", "Instrutor", "Aluno" };

                foreach (var roleName in roles)
                {
                    if (roleManager.RoleExistsAsync(roleName).GetAwaiter().GetResult())
                        continue;

                    var result = roleManager.CreateAsync(new IdentityRole(roleName))
                        .GetAwaiter().GetResult();

                    if (!result.Succeeded)
                    {
                        Console.WriteLine($"Não foi possível configurar o perfil {roleName}.");
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                // Não exibir connection strings ou dados de usuários nos logs.
                Console.WriteLine($"Não foi possível verificar os perfis do GymFlow: {ex.GetType().Name}");
                return false;
            }
        }
    }
}
