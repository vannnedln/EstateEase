using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace EstateEase.Models.Entities
{
    public class Inquiry
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = null!;

        [ForeignKey("UserId")]
        public IdentityUser User { get; set; } = null!;

        public string? PropertyId { get; set; }

        [ForeignKey("PropertyId")]
        public Property? Property { get; set; }

        public string? AgentId { get; set; }

        [ForeignKey("AgentId")]
        public Agent? Agent { get; set; }

        [Required]
        [StringLength(100)]
        public string Subject { get; set; } = null!;

        [Required]
        [StringLength(1000)]
        public string Message { get; set; } = null!;
        
        [StringLength(1000)]
        public string? ReplyMessage { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "New";

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? ReadAt { get; set; }
        
        public bool ReadByUser { get; set; } = false;
        
        // Collection of messages for the chat system
        public virtual ICollection<InquiryMessage> Messages { get; set; } = new List<InquiryMessage>();
    }
} 