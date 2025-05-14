using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EstateEase.Models.Entities
{
    public class InquiryMessage
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int InquiryId { get; set; }
        
        [ForeignKey("InquiryId")]
        public Inquiry Inquiry { get; set; } = null!;
        
        [Required]
        public string SenderId { get; set; } = null!;
        
        [Required]
        [StringLength(20)]
        public string SenderType { get; set; } = null!; // "User", "Agent", "Admin"
        
        [Required]
        [StringLength(1000)]
        public string Message { get; set; } = null!;
        
        public bool IsRead { get; set; } = false;
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
} 