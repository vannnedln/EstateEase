using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EstateEase.Models.Entities
{
    public class Agent
    {
        [Key]
        public string Id { get; set; }

        [Required]
        [StringLength(50)]
        public string FirstName { get; set; }

        [Required]
        [StringLength(50)]
        public string LastName { get; set; }

        [StringLength(100)]
        public string AddressLine1 { get; set; }

        [StringLength(50)]
        public string Barangay { get; set; }

        [StringLength(50)]
        public string City { get; set; }

        [StringLength(20)]
        public string PostalCode { get; set; }

        [StringLength(50)]
        public string Country { get; set; }

        [StringLength(50)]
        public string LicenseNumber { get; set; }

        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; }

        [Phone]
        [StringLength(20)]
        public string PhoneNumber { get; set; }

        public DateTime? DateOfBirth { get; set; }

        [StringLength(500)]
        public string Bio { get; set; }

        public string ProfilePictureUrl { get; set; }
        
        // Relationship with IdentityUser
        public string UserId { get; set; }
        [ForeignKey("UserId")]
        public virtual IdentityUser User { get; set; }
        
        // Relationship with Properties
        public virtual ICollection<Property> Properties { get; set; }

        // Navigation property for full name
        [NotMapped]
        public string FullName => $"{FirstName} {LastName}";
    }
} 