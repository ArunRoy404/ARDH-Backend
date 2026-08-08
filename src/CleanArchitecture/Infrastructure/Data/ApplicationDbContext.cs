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
    public DbSet<AmcContract> AmcContracts { get; set; }
    public DbSet<MaintenanceRequest> MaintenanceRequests { get; set; }
    public DbSet<IncomeRecord> IncomeRecords { get; set; }
    public DbSet<ExpenseRecord> ExpenseRecords { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<NotificationRecipient> NotificationRecipients { get; set; }
    public DbSet<Activity> Activities { get; set; }
    public DbSet<BulkUpload> BulkUploads { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        builder.Entity<Building>().HasQueryFilter(x => !x.IsDeleted);
        builder.Entity<Owner>().HasQueryFilter(x => !x.IsDeleted);
        builder.Entity<Apartment>().HasQueryFilter(x => !x.IsDeleted);
        builder.Entity<Tenant>().HasQueryFilter(x => !x.IsDeleted);
        builder.Entity<Equipment>().HasQueryFilter(x => !x.IsDeleted);
        builder.Entity<AmcContract>().HasQueryFilter(x => !x.IsDeleted);
        builder.Entity<MaintenanceRequest>().HasQueryFilter(x => !x.IsDeleted);
        builder.Entity<IncomeRecord>().HasQueryFilter(x => !x.IsDeleted);
        builder.Entity<ExpenseRecord>().HasQueryFilter(x => !x.IsDeleted);
        builder.Entity<Vendor>().HasQueryFilter(x => !x.IsDeleted);
        builder.Entity<User>().HasQueryFilter(x => !x.IsDeleted);
    }
}
