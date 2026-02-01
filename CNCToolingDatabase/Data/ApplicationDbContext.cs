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
    public DbSet<MachineModel> MachineModels { get; set; }
    public DbSet<CamLeader> CamLeaders { get; set; }
    public DbSet<CamProgrammer> CamProgrammers { get; set; }
    public DbSet<Operation> Operations { get; set; }
    public DbSet<Revision> Revisions { get; set; }
    public DbSet<PartNumber> PartNumbers { get; set; }
    public DbSet<MaterialSpec> MaterialSpecs { get; set; }
    public DbSet<ToolCodeUnique> ToolCodeUniques { get; set; }
    
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
            entity.HasOne(e => e.MaterialSpec)
                .WithMany()
                .HasForeignKey(e => e.MaterialSpecId)
                .OnDelete(DeleteBehavior.SetNull);
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
            entity.HasOne(e => e.MachineModel)
                .WithMany()
                .HasForeignKey(e => e.MachineModelId)
                .OnDelete(DeleteBehavior.SetNull);
        });
        
        modelBuilder.Entity<MachineWorkcenter>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Workcenter).IsUnique();
            entity.Property(e => e.Axis).HasColumnName("Description");
        });
        
        modelBuilder.Entity<MachineModel>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Model).IsUnique();
        });
        
        modelBuilder.Entity<CamLeader>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name).IsUnique();
        });
        
        modelBuilder.Entity<CamProgrammer>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name).IsUnique();
        });
        
        modelBuilder.Entity<Operation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name).IsUnique();
        });
        
        modelBuilder.Entity<Revision>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name).IsUnique();
        });
        
        modelBuilder.Entity<PartNumber>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name);
            entity.HasOne(e => e.ProjectCode)
                .WithMany()
                .HasForeignKey(e => e.ProjectCodeId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.MaterialSpec)
                .WithMany()
                .HasForeignKey(e => e.MaterialSpecId)
                .OnDelete(DeleteBehavior.SetNull);
        });
        
        modelBuilder.Entity<MaterialSpec>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.Spec, e.Material }).IsUnique();
        });
        
        modelBuilder.Entity<ToolCodeUnique>(entity =>
        {
            entity.HasKey(e => e.Id);
        });
    }
}
