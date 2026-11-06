using AgriStream.Models;
using Microsoft.EntityFrameworkCore;

namespace AgriStream.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Device> Devices => Set<Device>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasIndex(u => u.Username).IsUnique();
            entity.Property(u => u.Username).HasMaxLength(100);
            entity.Property(u => u.PasswordHash).HasMaxLength(256);
            entity.Property(u => u.Role).HasMaxLength(20);
        });

        modelBuilder.Entity<Device>(entity =>
        {
            entity.ToTable("DeviceRegistry");
            entity.HasKey(d => d.DeviceId);
            entity.Property(d => d.DeviceId).HasMaxLength(50);
            entity.Property(d => d.FieldId).HasMaxLength(50);
            entity.Property(d => d.DeviceType).HasMaxLength(50);
            entity.Property(d => d.OperationalStatus).HasMaxLength(30);
            entity.Property(d => d.LastCommand).HasMaxLength(200);
        });
    }
}
