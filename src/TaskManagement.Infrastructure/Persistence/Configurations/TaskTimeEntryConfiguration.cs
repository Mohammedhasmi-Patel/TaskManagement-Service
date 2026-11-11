using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Infrastructure.Persistence.Configurations;

public class TaskTimeEntryConfiguration : IEntityTypeConfiguration<TaskTimeEntry>
{
    public void Configure(EntityTypeBuilder<TaskTimeEntry> builder)
    {
        builder.ToTable("TaskTimeEntries");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .ValueGeneratedNever();

        builder.Property(e => e.TaskId)
            .IsRequired();

        builder.Property(e => e.StartTime)
            .IsRequired();

        builder.Property(e => e.EndTime)
            .IsRequired(false);

        builder.Property(e => e.DurationSeconds)
            .IsRequired();

        builder.Property(e => e.Description)
            .IsRequired(false)
            .HasMaxLength(1000);

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .IsRequired(false);

        builder.HasOne(e => e.Task)
            .WithMany(t => t.TimeEntries)
            .HasForeignKey(e => e.TaskId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

