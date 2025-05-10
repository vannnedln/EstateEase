using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace EstateEase.Models.Entities
{
    public class Offer
    {
        [Key]
        public string Id { get; set; }
        
        [Required]
        public string UserId { get; set; }
        
        [Required]
        public string PropertyId { get; set; }
        
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal OfferAmount { get; set; }
        
        [Required]
        public DateTime OfferDate { get; set; }
        
        [Required]
        public string Status { get; set; } // "Accepted", "Pending", "Rejected"
        
        public string Notes { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public DateTime? UpdatedAt { get; set; }
        
        // Navigation properties
        [ForeignKey("UserId")]
        public virtual IdentityUser User { get; set; }
        
        [ForeignKey("PropertyId")]
        public virtual Property Property { get; set; }
    }
} 