// Uygulamanın EF Core veritabanı bağlamıdır.
// Entity yapılandırmaları buraya YAZILMAZ; her entity kendi
// IEntityTypeConfiguration dosyasını Configurations/ altında alır.
// Bu sınıf yalnızca DbSet'leri, assembly taramasını ve tip konvansiyonlarını (UTC tarih) tanımlar.

using AjandaAI.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AjandaAI.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Activity> Activities => Set<Activity>();

    public DbSet<Reminder> Reminders => Set<Reminder>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<EmailVerificationToken> EmailVerificationTokens => Set<EmailVerificationToken>();

    // Tüm DateTimeOffset (ve DateTimeOffset?) alanları UTC olarak yazılır; bkz. UtcDateTimeOffsetConverter.
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTimeOffset>().HaveConversion<UtcDateTimeOffsetConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
