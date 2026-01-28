using Microsoft.EntityFrameworkCore;
using CNCToolingDatabase.Models;

namespace CNCToolingDatabase.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }
    
    public DbSet<User> Users { get; set; }
    public DbSet<ToolListHeader> ToolListHeaders { get; set; }
    public DbSet<ToolListDetail> ToolListDetails { get; set; }
    public DbSet<ToolMaster> ToolMasters { get; set; }
    public DbSet<ProjectCode> ProjectCodes { get; set; }
    public DbSet<MachineName> MachineNames { get; set; }
    public DbSet<MachineWorkcenter> MachineWorkcenters { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Username).IsUnique();
        });
        
        modelBuilder.Entity<ToolListHeader>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.PartNumber, e.Operation });
            entity.HasMany(e => e.Details)
                  .WithOne(d => d.Header)
                  .HasForeignKey(d => d.ToolListHeaderId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
        
        modelBuilder.Entity<ToolListDetail>(entity =>
        {
            entity.HasKey(e => e.Id);
        });
        
        modelBuilder.Entity<ToolMaster>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ConsumableCode).IsUnique();
        });
        
        modelBuilder.Entity<ProjectCode>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Code).IsUnique();
        });
        
        modelBuilder.Entity<MachineName>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name).IsUnique();
        });
        
        modelBuilder.Entity<MachineWorkcenter>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Workcenter).IsUnique();
        });
    }
}
