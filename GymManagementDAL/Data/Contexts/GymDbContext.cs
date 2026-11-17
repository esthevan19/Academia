using GymManagementDAL.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace GymManagementDAL.Data.Contexts
{
    public class GymDbContext : IdentityDbContext<ApplicationUser>
    {

        public GymDbContext(DbContextOptions<GymDbContext> options) : base(options)
        {

        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
            modelBuilder.Entity<Workout>(entity =>
            {
                entity.Property(x => x.Name).HasMaxLength(150).IsRequired();
                entity.Property(x => x.Category).HasMaxLength(80).IsRequired();
                entity.Property(x => x.Letter).HasMaxLength(10).IsRequired();
                entity.Property(x => x.Notes).HasMaxLength(1000);
                entity.HasOne(x => x.Member).WithMany().HasForeignKey(x => x.MemberId).OnDelete(DeleteBehavior.Cascade);
            });
            modelBuilder.Entity<WorkoutExercise>(entity =>
            {
                entity.Property(x => x.Name).HasMaxLength(150).IsRequired();
                entity.Property(x => x.Reps).HasMaxLength(50).IsRequired();
                entity.Property(x => x.Notes).HasMaxLength(1000);
                entity.Property(x => x.LoadKg).HasColumnType("decimal(8,2)");
                entity.HasOne(x => x.Workout).WithMany(x => x.Exercises).HasForeignKey(x => x.WorkoutId).OnDelete(DeleteBehavior.Cascade);
            });
            modelBuilder.Entity<ApplicationUser>(Eb =>
            {

                Eb.Property(x => x.FirstName)
                  .HasColumnType("varchar")
                  .HasMaxLength(50);

                Eb.Property(x => x.LastName)
                  .HasColumnType("varchar")
                  .HasMaxLength(50);

            });
        }

        #region DbSets
        public DbSet<Workout> Workouts { get; set; } = null!;
        public DbSet<WorkoutExercise> WorkoutExercises { get; set; } = null!;

        public DbSet<Member> Members { get; set; } = null!;
        public DbSet<HealthRecord> HealthRecords { get; set; } = null!;
        public DbSet<Trainer> Trainers { get; set; } = null!;
        public DbSet<Category> Categories { get; set; } = null!;
        public DbSet<Plan> Plans { get; set; } = null!;
        public DbSet<Session> Sessions { get; set; } = null!;
        public DbSet<Membership> Memberships { get; set; } = null!;
        public DbSet<Booking> Bookings { get; set; } = null!;

        #endregion

    }
}
