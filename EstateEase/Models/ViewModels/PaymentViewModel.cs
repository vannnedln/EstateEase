using System;
using System.ComponentModel.DataAnnotations;

namespace EstateEase.Models.ViewModels
{
    public class PaymentViewModel
    {
        public int Id { get; set; }
        
        [Display(Name = "Property")]
        public string PropertyId { get; set; } = string.Empty;
        
        [Display(Name = "Property Title")]
        public string PropertyTitle { get; set; } = "Unknown Property";
        
        [Display(Name = "Property Address")]
        public string PropertyAddress { get; set; } = "No address available";
        
        public string PropertyImageUrl { get; set; } = "/images/property-placeholder.jpg";
        
        [Display(Name = "Transaction Type")]
        public string TransactionType { get; set; } = "Other";
        
        [Required]
        [Display(Name = "Amount")]
        [DataType(DataType.Currency)]
        public decimal Amount { get; set; }
        
        [Display(Name = "Commission")]
        [DataType(DataType.Currency)]
        public decimal Commission { get; set; }
        
        [Display(Name = "Commission Percentage")]
        public decimal CommissionPercentage { get; set; }
        
        [Display(Name = "Client Name")]
        public string ClientName { get; set; } = "Unknown Client";
        
        [Display(Name = "Client Email")]
        public string ClientEmail { get; set; } = string.Empty;
        
        [Display(Name = "Agent Name")]
        public string AgentName { get; set; } = "No Agent";
        
        [Required]
        [Display(Name = "Transaction Date")]
        [DataType(DataType.Date)]
        public DateTime TransactionDate { get; set; } = DateTime.Now;
        
        [Display(Name = "Payment Method")]
        public string PaymentMethod { get; set; } = "Online Payment";
        
        [Display(Name = "Reference Number")]
        public string ReferenceNumber { get; set; } = string.Empty;
        
        [Display(Name = "Notes")]
        public string Notes { get; set; } = string.Empty;
        
        [Display(Name = "Status")]
        public string Status { get; set; } = "Completed";
        
        [Display(Name = "Rental Duration (Months)")]
        public int? RentalDuration { get; set; } = 12; // Default to 12 months when not specified
        
        [Display(Name = "Rental Expiry Date")]
        [DataType(DataType.Date)]
        public DateTime? ExpiryDate { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        // Helper method to safely calculate monthly rate
        public decimal GetMonthlyRate()
        {
            try
            {
                if (RentalDuration.HasValue && RentalDuration.Value > 0)
                {
                    return Amount / RentalDuration.Value;
                }
                else if (ExpiryDate.HasValue && TransactionDate != default)
                {
                    // Calculate months between transaction date and expiry
                    var months = ((ExpiryDate.Value.Year - TransactionDate.Year) * 12) +
                              ExpiryDate.Value.Month - TransactionDate.Month;
                    if (months > 0)
                    {
                        return Amount / months;
                    }
                }
                
                // Default
                return Amount;
            }
            catch
            {
                return Amount;
            }
        }
    }
} 