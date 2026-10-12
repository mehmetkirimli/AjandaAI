// ActivitySeries entity'sinin EF Core yapılandırması (ADR 0022).
// Frequency string olarak saklanır; StartDate/EndDate DateOnly -> date.
// Kullanıcı silinirse seri silinemez (Restrict); kullanıcı aktiviteleriyle birlikte
// silinirken önce aktiviteler, sonra seriler temizlenmelidir.

using AjandaAI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AjandaAI.Infrastructure.Persistence.Configurations;

public class ActivitySeriesConfiguration : IEntityTypeConfiguration<ActivitySeries>
{
    public void Configure(EntityTypeBuilder<ActivitySeries> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Frequency)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(s => s.DaysOfWeek)
            .IsRequired();

        builder.Property(s => s.StartDate)
            .IsRequired();

        builder.Property(s => s.EndDate)
            .IsRequired();

        builder.Property(s => s.CreatedAt)
            .IsRequired();

        builder.HasOne(s => s.User)
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => s.UserId);
    }
}
