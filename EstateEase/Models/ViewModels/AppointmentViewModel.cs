using System;
using System.ComponentModel.DataAnnotations;

namespace EstateEase.Models.ViewModels
{
    public class AppointmentViewModel
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

        [Required]
        [Display(Name = "Property")]
        public int PropertyId { get; set; }

        [Display(Name = "Property Title")]
        public string PropertyTitle { get; set; }

        [Display(Name = "Property Address")]
        public string PropertyAddress { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Appointment Date")]
        public DateTime AppointmentDate { get; set; }

        [Required]
        [Display(Name = "Appointment Time")]
        public string AppointmentTime { get; set; }

        [Required]
        [Display(Name = "Appointment Type")]
        public string AppointmentType { get; set; }

        [Required]
        [Display(Name = "Status")]
        public string Status { get; set; }

        [Display(Name = "Notes")]
        public string Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
    }
} 