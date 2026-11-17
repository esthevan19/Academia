using GymManagementDAL.Data.Contexts;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace GymManagementDAL.Migrations;

[DbContext(typeof(GymDbContext))]
[Migration("20261117180000_AddStudentWorkouts")]
public class AddStudentWorkouts : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Workouts",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                MemberId = table.Column<int>(type: "int", nullable: false),
                Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                Category = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                Letter = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Workouts", x => x.Id);
                table.ForeignKey("FK_Workouts_Members_MemberId", x => x.MemberId, "Members", "Id", onDelete: ReferentialAction.Cascade);
            });
        migrationBuilder.CreateIndex("IX_Workouts_MemberId", "Workouts", "MemberId");
        migrationBuilder.CreateTable(
            name: "WorkoutExercises",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                WorkoutId = table.Column<int>(type: "int", nullable: false),
                Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                Sets = table.Column<int>(type: "int", nullable: false),
                Reps = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                LoadKg = table.Column<decimal>(type: "decimal(8,2)", nullable: true),
                RestSeconds = table.Column<int>(type: "int", nullable: false),
                SortOrder = table.Column<int>(type: "int", nullable: false),
                Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_WorkoutExercises", x => x.Id);
                table.ForeignKey("FK_WorkoutExercises_Workouts_WorkoutId", x => x.WorkoutId, "Workouts", "Id", onDelete: ReferentialAction.Cascade);
            });
        migrationBuilder.CreateIndex("IX_WorkoutExercises_WorkoutId", "WorkoutExercises", "WorkoutId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "WorkoutExercises");
        migrationBuilder.DropTable(name: "Workouts");
    }
}
