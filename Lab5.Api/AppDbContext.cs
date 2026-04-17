using Microsoft.EntityFrameworkCore;
using Lab5.Api.Models;

namespace Lab5.Api.Data;

public class AppDbContext : DbContext {
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Student> Students => Set<Student>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        
        modelBuilder.Entity<Student>()
            .HasIndex(s => s.Email)
            .IsUnique();
        
        modelBuilder.Entity<Enrollment>()
            .HasOne(e => e.Student)
            .WithMany(s => s.Enrollments)
            .OnDelete(DeleteBehavior.Cascade);
    }
}