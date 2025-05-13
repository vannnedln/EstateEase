using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace EstateEase.Models.Entities
{
    public class UserProfile
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        [Required]
        public string UserId { get; set; }
        
        [ForeignKey("UserId")]
        public IdentityUser User { get; set; }
        
        [Required]
        [StringLength(50)]
        public string FirstName { get; set; }
        
        [Required]
        [StringLength(50)]
        public string LastName { get; set; }
        
        [StringLength(20)]
        public string PhoneNumber { get; set; }
        
        [StringLength(100)]
        public string Address { get; set; } = null;
        
        [StringLength(50)]
        public string Barangay { get; set; } = null;
        
        [StringLength(50)]
        public string City { get; set; } = null;
        
        [StringLength(20)]
        public string PostalCode { get; set; } = null;
        
        [StringLength(50)]
        public string Country { get; set; } = null;
        
        [DataType(DataType.Date)]
        public DateTime? Birthday { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public DateTime? UpdatedAt { get; set; }
        
        [StringLength(500)]
        public string ProfilePictureUrl { get; set; }
        
        [StringLength(1000)]
        public string Bio { get; set; } = null;
        
        // Computed property for full name
        [NotMapped]
        public string FullName => $"{FirstName} {LastName}";
    }
} 