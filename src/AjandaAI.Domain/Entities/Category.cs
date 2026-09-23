// Aktivitelerin sınıflandırıldığı lookup tablosudur (spor, iş, sosyal vb.).
// Admin panelinden yönetilir; kayıtlar silinmek yerine IsActive ile pasife alınır.

namespace AjandaAI.Domain.Entities;

public class Category
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<Activity> Activities { get; set; } = new List<Activity>();
}
