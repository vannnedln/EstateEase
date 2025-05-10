using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace EstateEase.Models.Entities
{
    public class Favorite
    {
        [Key]
        public string Id { get; set; }
        
        [Required]
        public string UserId { get; set; }
        
        [Required]
        public string PropertyId { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        // Navigation properties
        [ForeignKey("UserId")]
        public virtual IdentityUser User { get; set; }
        
        [ForeignKey("PropertyId")]
        public virtual Property Property { get; set; }
    }
} 