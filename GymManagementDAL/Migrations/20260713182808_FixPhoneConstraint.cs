using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymManagementDAL.Migrations
{
    /// <inheritdoc />
    public partial class FixPhoneConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_GymUserValidPhone1",
                table: "Trainers");

            migrationBuilder.DropCheckConstraint(
                name: "CK_GymUserValidPhone",
                table: "Members");

            migrationBuilder.AddCheckConstraint(
                name: "CK_GymUserValidPhone1",
                table: "Trainers",
                sql: "Phone LIKE '[1-9][1-9]%' AND Phone NOT LIKE '%[^0-9]%' AND LEN(Phone) = 11");

            migrationBuilder.AddCheckConstraint(
                name: "CK_GymUserValidPhone",
                table: "Members",
                sql: "Phone LIKE '[1-9][1-9]%' AND Phone NOT LIKE '%[^0-9]%' AND LEN(Phone) = 11");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_GymUserValidPhone1",
                table: "Trainers");

            migrationBuilder.DropCheckConstraint(
                name: "CK_GymUserValidPhone",
                table: "Members");

            migrationBuilder.AddCheckConstraint(
                name: "CK_GymUserValidPhone1",
                table: "Trainers",
                sql: "Phone LIKE '01%' AND Phone Not LIKE '%[^0-9]%' AND LEN(Phone) = 11");

            migrationBuilder.AddCheckConstraint(
                name: "CK_GymUserValidPhone",
                table: "Members",
                sql: "Phone LIKE '01%' AND Phone Not LIKE '%[^0-9]%' AND LEN(Phone) = 11");
        }
    }
}
