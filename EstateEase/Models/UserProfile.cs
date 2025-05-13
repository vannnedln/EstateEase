using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EstateEase.Models
{
    public class UserProfile
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        public string FirstName { get; set; }
        public string LastName { get; set; }
        
        public string Address { get; set; }
        public string Barangay { get; set; }
        public string City { get; set; }
        public string PostalCode { get; set; }
        public string Country { get; set; }
        
        [DataType(DataType.Date)]
        public DateTime? Birthday { get; set; }
        
        public string ProfilePictureUrl { get; set; }
        
        [Column(TypeName = "nvarchar(max)")]
        public string Bio { get; set; }
        
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        
        // Navigation property for relationship with AspNetUsers
        [ForeignKey("UserId")]
        public virtual Microsoft.AspNetCore.Identity.IdentityUser User { get; set; }
    }
} 