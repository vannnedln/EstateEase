using System;
using System.ComponentModel.DataAnnotations;

namespace EstateEase.Models.ViewModels
{
    public class InquiryViewModel
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Client Name")]
        public string ClientName { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Client Email")]
        public string ClientEmail { get; set; }

        [Phone]
        [Display(Name = "Client Phone")]
        public string ClientPhone { get; set; }

        public string PropertyId { get; set; }

        [Display(Name = "Property Title")]
        public string PropertyTitle { get; set; }

        [Display(Name = "Property Address")]
        public string PropertyAddress { get; set; }

        [Required]
        [Display(Name = "Subject")]
        public string Subject { get; set; }

        [Required]
        [Display(Name = "Message")]
        public string Message { get; set; }

        [Required]
        [Display(Name = "Status")]
        public string Status { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
    }
} 