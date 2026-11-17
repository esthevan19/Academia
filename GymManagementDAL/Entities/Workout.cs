namespace GymManagementDAL.Entities;

public class Workout : BaseEntity
{
    public int MemberId { get; set; }
    public Member Member { get; set; } = null!;
    public string Name { get; set; } = "";
    public string Category { get; set; } = "Musculação";
    public string Letter { get; set; } = "A";
    public string? Notes { get; set; }
    public ICollection<WorkoutExercise> Exercises { get; set; } = new List<WorkoutExercise>();
}

public class WorkoutExercise : BaseEntity
{
    public int WorkoutId { get; set; }
    public Workout Workout { get; set; } = null!;
    public string Name { get; set; } = "";
    public int Sets { get; set; }
    public string Reps { get; set; } = "";
    public decimal? LoadKg { get; set; }
    public int RestSeconds { get; set; }
    public int SortOrder { get; set; }
    public string? Notes { get; set; }
}
