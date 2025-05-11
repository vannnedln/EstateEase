using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace EstateEase.Models.Entities
{
    public class Appointment
    {
        [Key]
        public string Id { get; set; }
        
        [Required]
        public string UserId { get; set; }
        
        [Required]
        public string PropertyId { get; set; }
        
        [Required]
        public string AgentId { get; set; }
        
        [Required]
        public DateTime AppointmentDate { get; set; }
        
        [Required]
        public string Status { get; set; } // "Confirmed", "Pending", "Completed", "Cancelled"
        
        public string Notes { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public DateTime? UpdatedAt { get; set; }
        
        // Navigation properties
        [ForeignKey("UserId")]
        public virtual IdentityUser User { get; set; }
        
        [ForeignKey("PropertyId")]
        public virtual Property Property { get; set; }
        
        [ForeignKey("AgentId")]
        public virtual Agent Agent { get; set; }
    }
} 