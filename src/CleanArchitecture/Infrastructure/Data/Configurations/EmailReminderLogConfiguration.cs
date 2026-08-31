using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CleanArchitecture.Domain.Entities;

namespace CleanArchitecture.Infrastructure.Data.Configurations;

public class EmailReminderLogConfiguration : IEntityTypeConfiguration<EmailReminderLog>
{
    public void Configure(EntityTypeBuilder<EmailReminderLog> builder)
    {
        builder.ToTable("email_reminder_logs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(x => x.ReminderType)
            .HasColumnName("reminder_type")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.EntityId)
            .HasColumnName("entity_id")
            .IsRequired();

        builder.Property(x => x.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(x => x.SentAt)
            .HasColumnName("sent_at")
            .IsRequired();

        // Index to help quickly find if a reminder was already sent
        builder.HasIndex(x => new { x.ReminderType, x.EntityId, x.UserId });
    }
}
