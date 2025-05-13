using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace EstateEase.Models.Entities
{
    public class UserProperty
    {
        [Key]
        public string Id { get; set; }
        
        [Required]
        public string UserId { get; set; }
        
        [Required]
        public string PropertyId { get; set; }
        
        [Required]
        public string OwnershipType { get; set; } // "Bought" or "Rented"
        
        [Required]
        public string RelationshipType { get; set; } // "Owner" or "Renter"
        
        [Required]
        public DateTime AcquisitionDate { get; set; }
        
        public DateTime? ExpiryDate { get; set; } // For rentals
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public DateTime? UpdatedAt { get; set; }
        
        // Navigation properties
        [ForeignKey("UserId")]
        public virtual IdentityUser User { get; set; }
        
        [ForeignKey("PropertyId")]
        public virtual Property Property { get; set; }
    }
} 