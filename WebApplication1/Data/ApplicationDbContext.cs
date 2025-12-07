using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;

namespace WebApplication1.Data;

public class ApplicationDbContext : IdentityDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }
    public DbSet<User> Users { get; set; }
    public DbSet<Stemmer> Stemmers { get; set; }
    
    public DbSet<Vertifikasjon> Vertifikasjons { get; set; }
    

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Ignore<IdentityUser>();

        modelBuilder.Entity<User>().ToTable("users");
        modelBuilder.Entity<Stemmer>().ToTable("stemmer");
        
        modelBuilder.Entity<User>()
            .Property(u => u.BankIdUuid)
            .HasColumnType("varchar(36)")  
            .IsRequired(false);
    }
}