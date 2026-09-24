// Category entity'sinin EF Core yapılandırması.
// Lookup tablosu olduğu için Id değerleri sabittir ve HasData ile seed edilir.
// Seed Id'leri değiştirilmemelidir; mevcut aktiviteler bu Id'lere bağlanır.

using AjandaAI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AjandaAI.Infrastructure.Persistence.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    private static readonly DateTimeOffset SeedCreatedAt =
        new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(100);

        // Mevcut satırlar migration'da aktif kalsın diye DB default'u true.
        // Sentinel true: false değeri INSERT'te atlanmaz, açıkça yazılır.
        builder.Property(c => c.IsActive)
            .IsRequired()
            .HasDefaultValue(true)
            .HasSentinel(true);

        builder.Property(c => c.CreatedAt)
            .IsRequired();

        builder.HasIndex(c => c.Name)
            .IsUnique();

        builder.HasData(
            Seed(1, "Yeme & İçme"),
            Seed(2, "Seyahat & Gezi"),
            Seed(3, "Eğlence & Sosyal"),
            Seed(4, "Romantizm & İlişki"),
            Seed(5, "Kültür, Sanat & Gösteri"),
            Seed(6, "Spor, Sağlık & Hareket"),
            Seed(7, "Dinlenme & Zihinsel İyileşme"),
            Seed(8, "Kişisel Gelişim & Hobi"),
            Seed(9, "İş, Kariyer & Finans"),
            Seed(10, "Kişisel Bakım & Rutin"));
    }

    private static Category Seed(int id, string name) => new()
    {
        Id = id,
        Name = name,
        IsActive = true,
        CreatedAt = SeedCreatedAt
    };
}
