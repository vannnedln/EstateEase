using System;
using System.ComponentModel.DataAnnotations;

namespace EstateEase.Models.ViewModels
{
    public class PaymentViewModel
    {
        public int Id { get; set; }
        
        [Display(Name = "Property")]
        public int PropertyId { get; set; }
        
        [Display(Name = "Property Title")]
        public string PropertyTitle { get; set; }
        
        [Display(Name = "Property Address")]
        public string PropertyAddress { get; set; }
        
        public string PropertyImageUrl { get; set; }
        
        [Display(Name = "Transaction Type")]
        public string TransactionType { get; set; }
        
        [Required]
        [Display(Name = "Amount")]
        [DataType(DataType.Currency)]
        public decimal Amount { get; set; }
        
        [Required]
        [Display(Name = "Commission")]
        [DataType(DataType.Currency)]
        public decimal Commission { get; set; }
        
        [Required]
        [Display(Name = "Commission Percentage")]
        public decimal CommissionPercentage { get; set; }
        
        [Display(Name = "Client Name")]
        public string ClientName { get; set; }
        
        [Display(Name = "Client Email")]
        public string ClientEmail { get; set; }
        
        [Required]
        [Display(Name = "Transaction Date")]
        [DataType(DataType.Date)]
        public DateTime TransactionDate { get; set; }
        
        [Display(Name = "Payment Method")]
        public string PaymentMethod { get; set; }
        
        [Display(Name = "Reference Number")]
        public string ReferenceNumber { get; set; }
        
        [Display(Name = "Notes")]
        public string Notes { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
} 