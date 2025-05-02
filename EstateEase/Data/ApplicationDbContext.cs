

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

        public DbSet<AddProperty> Properties { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<AddProperty>()
                .Property(p => p.Price)
                .HasColumnType("decimal(18,2)");

            builder.Entity<AddProperty>()
                .Property(p => p.Size)
                .HasColumnType("decimal(18,2)");
        }

    }
}
