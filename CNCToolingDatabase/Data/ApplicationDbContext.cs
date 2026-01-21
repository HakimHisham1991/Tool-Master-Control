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
    }
}
