using System;
using System.ComponentModel.DataAnnotations;

namespace EstateEase.Models.ViewModels
{
    public class InquiryReplyViewModel
    {
        public int InquiryId { get; set; }
        
        [Display(Name = "Inquiry Subject")]
        public string InquirySubject { get; set; }
        
        [Display(Name = "Client Name")]
        public string ClientName { get; set; }
        
        [Display(Name = "Client Email")]
        public string ClientEmail { get; set; }
        
        [Required]
        [Display(Name = "Reply Message")]
        public string ReplyMessage { get; set; }
        
        public DateTime RepliedAt { get; set; } = DateTime.Now;
    }
} 