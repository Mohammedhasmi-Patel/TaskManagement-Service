using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Infrastructure.Persistence.Configurations;

public class TaskTechnologyConfiguration : IEntityTypeConfiguration<TaskTechnology>
{
    public void Configure(EntityTypeBuilder<TaskTechnology> builder)
    {
        builder.ToTable("TaskTechnologies");

        builder.HasKey(tt => new { tt.TaskId, tt.TechnologyId });

        builder.Property(tt => tt.CreatedAt)
            .IsRequired();

        builder.Property(tt => tt.UpdatedAt)
            .IsRequired(false);

        builder.HasOne(tt => tt.Task)
            .WithMany(t => t.TaskTechnologies)
            .HasForeignKey(tt => tt.TaskId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(tt => tt.Technology)
            .WithMany(t => t.TaskTechnologies)
            .HasForeignKey(tt => tt.TechnologyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

