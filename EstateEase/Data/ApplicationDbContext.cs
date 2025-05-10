using EstateEase.Models.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EstateEase.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Property> Properties { get; set; }
        public DbSet<PropertyImage> PropertyImages { get; set; }
        public DbSet<Agent> Agents { get; set; }
        public DbSet<UserProperty> UserProperties { get; set; }
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<Offer> Offers { get; set; }
        public DbSet<Favorite> Favorites { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure Property entity
            builder.Entity<Property>()
                .Property(p => p.Price)
                .HasColumnType("decimal(18,2)");

            builder.Entity<Property>()
                .Property(p => p.Size)
                .HasColumnType("decimal(18,2)");

            // Configure one-to-one relationship between Agent and IdentityUser
            builder.Entity<Agent>()
                .HasOne(a => a.User)
                .WithOne()
                .HasForeignKey<Agent>(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure one-to-many relationship between Agent and Properties
            builder.Entity<Property>()
                .HasOne(p => p.Agent)
                .WithMany(a => a.Properties)
                .HasForeignKey(p => p.AgentId)
                .OnDelete(DeleteBehavior.SetNull); // Set to null if agent is deleted

            // Configure PropertyImage relationship
            builder.Entity<PropertyImage>()
                .HasOne(pi => pi.Property)
                .WithMany(p => p.PropertyImages)
                .HasForeignKey(pi => pi.PropertyId)
                .OnDelete(DeleteBehavior.Cascade);
                
            // Configure UserProperty relationships
            builder.Entity<UserProperty>()
                .HasOne(up => up.User)
                .WithMany()
                .HasForeignKey(up => up.UserId)
                .OnDelete(DeleteBehavior.Cascade);
                
            builder.Entity<UserProperty>()
                .HasOne(up => up.Property)
                .WithMany()
                .HasForeignKey(up => up.PropertyId)
                .OnDelete(DeleteBehavior.Cascade);
                
            // Configure Appointment relationships
            builder.Entity<Appointment>()
                .HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);
                
            builder.Entity<Appointment>()
                .HasOne(a => a.Property)
                .WithMany()
                .HasForeignKey(a => a.PropertyId)
                .OnDelete(DeleteBehavior.Cascade);
                
            builder.Entity<Appointment>()
                .HasOne(a => a.Agent)
                .WithMany()
                .HasForeignKey(a => a.AgentId)
                .OnDelete(DeleteBehavior.NoAction); // Prevent circular cascade delete
                
            // Configure Offer relationships
            builder.Entity<Offer>()
                .HasOne(o => o.User)
                .WithMany()
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.Cascade);
                
            builder.Entity<Offer>()
                .HasOne(o => o.Property)
                .WithMany()
                .HasForeignKey(o => o.PropertyId)
                .OnDelete(DeleteBehavior.Cascade);
                
            // Configure Favorite relationships
            builder.Entity<Favorite>()
                .HasOne(f => f.User)
                .WithMany()
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Cascade);
                
            builder.Entity<Favorite>()
                .HasOne(f => f.Property)
                .WithMany()
                .HasForeignKey(f => f.PropertyId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
