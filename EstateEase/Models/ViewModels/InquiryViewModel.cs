using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EstateEase.Models.ViewModels
{
    public class InquiryViewModel
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Client Name")]
        public string ClientName { get; set; } = null!;

        [Required]
        [EmailAddress]
        [Display(Name = "Client Email")]
        public string ClientEmail { get; set; } = null!;

        [Phone]
        [Display(Name = "Client Phone")]
        public string? ClientPhone { get; set; }

        public string? PropertyId { get; set; }

        [Display(Name = "Property Title")]
        public string? PropertyTitle { get; set; }

        [Display(Name = "Property Address")]
        public string? PropertyAddress { get; set; }

        [Required]
        [Display(Name = "Subject")]
        public string Subject { get; set; } = null!;

        [Required]
        [Display(Name = "Message")]
        public string Message { get; set; } = null!;

        [Display(Name = "Reply Message")]
        public string? ReplyMessage { get; set; }

        [Required]
        [Display(Name = "Status")]
        public string Status { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
        
        public bool ReadByUser { get; set; } = false;
        
        // Collection of messages for the chat system
        public List<InquiryMessageViewModel> Messages { get; set; } = new List<InquiryMessageViewModel>();
    }
} 