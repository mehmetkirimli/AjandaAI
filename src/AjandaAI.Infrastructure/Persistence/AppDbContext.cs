// Uygulamanın EF Core veritabanı bağlamıdır.
// Entity yapılandırmaları buraya YAZILMAZ; her entity kendi
// IEntityTypeConfiguration dosyasını Configurations/ altında alır.
// Bu sınıf yalnızca DbSet'leri ve assembly taramasını tanımlar.

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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
