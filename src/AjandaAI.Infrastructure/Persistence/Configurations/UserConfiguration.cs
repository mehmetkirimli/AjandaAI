// User entity'sinin EF Core yapılandırması.
// Tablo adı, alan kısıtları ve Email üzerindeki benzersizlik indeksi burada tanımlanır.

using AjandaAI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AjandaAI.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(u => u.DisplayName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.TimeZoneId)
            .IsRequired()
            .HasMaxLength(64);

        // Mevcut satırlar migration'da aktif kalsın diye DB default'u true.
        // Sentinel true: false değeri INSERT'te atlanmaz, açıkça yazılır.
        builder.Property(u => u.IsActive)
            .IsRequired()
            .HasDefaultValue(true)
            .HasSentinel(true);

        builder.Property(u => u.CreatedAt)
            .IsRequired();

        builder.Property(u => u.UpdatedAt)
            .IsRequired();

        builder.HasIndex(u => u.Email)
            .IsUnique();
    }
}
