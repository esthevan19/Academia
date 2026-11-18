using GymManagementDAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GymManagementDAL.Data.Configurations
{
    public class HealthRecordConfiguration : IEntityTypeConfiguration<HealthRecord>
    {
        public void Configure(EntityTypeBuilder<HealthRecord> builder)
        {

            builder.ToTable("Members");

            builder.HasOne<Member>()
                   .WithOne(m => m.HealthRecord)
                   .HasForeignKey<HealthRecord>(hr => hr.Id);

            builder.Property(hr => hr.Height).HasPrecision(5, 2);
            builder.Property(hr => hr.Weight).HasPrecision(6, 2);

            builder.Ignore(hr => hr.CreatedAt);

        }
    }
}
