using GymManagementDAL.Data.Contexts;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace GymManagementDAL.Migrations;

/// <summary>
/// Preserva matrículas existentes e permite várias contratações do mesmo plano.
/// Fazer backup completo do banco antes de executar em produção.
/// </summary>
[DbContext(typeof(GymDbContext))]
[Migration("20261119190000_AllowMembershipRenewals")]
public class AllowMembershipRenewals : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropPrimaryKey(
            name: "PK_Memberships",
            table: "Memberships");

        migrationBuilder.AddColumn<int>(
            name: "Id",
            table: "Memberships",
            type: "int",
            nullable: false)
            .Annotation("SqlServer:Identity", "1, 1");

        migrationBuilder.AddPrimaryKey(
            name: "PK_Memberships",
            table: "Memberships",
            column: "Id");

        migrationBuilder.CreateIndex(
            name: "IX_Memberships_MemberId",
            table: "Memberships",
            column: "MemberId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Não permitir rollback que perderia a possibilidade de armazenar renovações.
        migrationBuilder.Sql(@"
IF EXISTS (
    SELECT 1 FROM Memberships
    GROUP BY MemberId, PlanId
    HAVING COUNT(*) > 1
)
    THROW 51001, 'Rollback bloqueado: existem renovações do mesmo plano.', 1;");

        migrationBuilder.DropIndex(
            name: "IX_Memberships_MemberId",
            table: "Memberships");
        migrationBuilder.DropPrimaryKey(
            name: "PK_Memberships",
            table: "Memberships");
        migrationBuilder.DropColumn(
            name: "Id",
            table: "Memberships");
        migrationBuilder.AddPrimaryKey(
            name: "PK_Memberships",
            table: "Memberships",
            columns: new[] { "MemberId", "PlanId" });
    }
}
