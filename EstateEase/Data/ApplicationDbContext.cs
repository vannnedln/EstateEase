﻿using EstateEase.Models.Entities;
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
        public DbSet<UserProfile> UserProfiles { get; set; }
        public DbSet<UserProperty> UserProperties { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<Inquiry> Inquiries { get; set; }
        public DbSet<InquiryMessage> InquiryMessages { get; set; }
    

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

            // Configure one-to-one relationship between UserProfile and IdentityUser
            builder.Entity<UserProfile>()
                .HasOne(up => up.User)
                .WithOne()
                .HasForeignKey<UserProfile>(up => up.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure nullable fields in UserProfile
            builder.Entity<UserProfile>()
                .Property(up => up.Address)
                .IsRequired(false);
                
            builder.Entity<UserProfile>()
                .Property(up => up.Barangay)
                .IsRequired(false);
                
            builder.Entity<UserProfile>()
                .Property(up => up.City)
                .IsRequired(false);
                
            builder.Entity<UserProfile>()
                .Property(up => up.PostalCode)
                .IsRequired(false);
                
            builder.Entity<UserProfile>()
                .Property(up => up.Country)
                .IsRequired(false);
                
            builder.Entity<UserProfile>()
                .Property(up => up.PhoneNumber)
                .IsRequired(false);
                
            builder.Entity<UserProfile>()
                .Property(up => up.ProfilePictureUrl)
                .IsRequired(false);
                
            builder.Entity<UserProfile>()
                .Property(up => up.Bio)
                .IsRequired(false);

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
                
            builder.Entity<UserProperty>()
                .Property(up => up.RelationshipType)
                .IsRequired(false);
                
            // Configure Transaction relationships
            builder.Entity<Transaction>()
                .HasOne(t => t.Property)
                .WithMany()
                .HasForeignKey(t => t.PropertyId)
                .OnDelete(DeleteBehavior.Cascade);
                
            builder.Entity<Transaction>()
                .Property(t => t.Amount)
                .HasColumnType("decimal(18,2)");
                
            builder.Entity<Transaction>()
                .Property(t => t.ReservationAmount)
                .HasColumnType("decimal(18,2)");
                
            builder.Entity<Transaction>()
                .Property(t => t.Notes)
                .IsRequired(false);
                
            builder.Entity<Transaction>()
                .Property(t => t.PaymentId)
                .IsRequired(false);
                
            builder.Entity<Transaction>()
                .Property(t => t.CheckoutSessionId)
                .IsRequired(false);
                
            builder.Entity<Transaction>()
                .Property(t => t.ReferenceNumber)
                .IsRequired(false);
                
            // Configure InquiryMessage relationship
            builder.Entity<InquiryMessage>()
                .HasOne(im => im.Inquiry)
                .WithMany(i => i.Messages)
                .HasForeignKey(im => im.InquiryId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
