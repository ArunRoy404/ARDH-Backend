using System.Reflection;
using Microsoft.EntityFrameworkCore;
using CleanArchitecture.Domain.Entities;

namespace CleanArchitecture.Infrastructure.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }
    public DbSet<ForgotPassword> ForgotPassword { get; set; }
    public DbSet<Building> Buildings { get; set; }
    public DbSet<Setting> Settings { get; set; }
    public DbSet<DeletedHistory> DeletedHistories { get; set; }
    public DbSet<Owner> Owners { get; set; }
    public DbSet<Apartment> Apartments { get; set; }
    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<TenantMoveOutRecord> TenantMoveOutRecords { get; set; }
    public DbSet<Vendor> Vendors { get; set; }
    public DbSet<Equipment> Equipment { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
