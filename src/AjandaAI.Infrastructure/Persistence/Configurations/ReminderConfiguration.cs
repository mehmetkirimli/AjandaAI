// Reminder entity'sinin EF Core yapılandırması.
// Aktivite silinince hatırlatıcıları da silinir (Cascade).
// Gönderilmemiş hatırlatıcıların taranması için RemindAt indekslenir.

using AjandaAI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AjandaAI.Infrastructure.Persistence.Configurations;

public class ReminderConfiguration : IEntityTypeConfiguration<Reminder>
{
    public void Configure(EntityTypeBuilder<Reminder> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.RemindAt)
            .IsRequired();

        builder.Property(r => r.IsSent)
            .IsRequired();

        builder.Property(r => r.Note)
            .HasMaxLength(500);

        builder.Property(r => r.CreatedAt)
            .IsRequired();

        builder.HasOne(r => r.Activity)
            .WithMany(a => a.Reminders)
            .HasForeignKey(r => r.ActivityId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(r => new { r.IsSent, r.RemindAt });
    }
}
