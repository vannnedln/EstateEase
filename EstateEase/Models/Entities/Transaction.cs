using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EstateEase.Models.Entities
{
    public class Transaction
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string UserId { get; set; }

        [Required]
        public string PropertyId { get; set; }

        [ForeignKey("PropertyId")]
        public virtual Property Property { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        public string TransactionType { get; set; } // Rent, Purchase, Reservation

        [Required]
        public string Status { get; set; } // Pending, Completed, Failed, Refunded

        public string PaymentMethod { get; set; } // Card, Bank Transfer, etc.

        public string PaymentProvider { get; set; } = "PayMongo";

        public string PaymentId { get; set; } // Payment ID from the payment provider
        
        public string CheckoutSessionId { get; set; } // Checkout session ID from PayMongo

        public string ReferenceNumber { get; set; } // Reference number for the transaction
        
        public string Notes { get; set; } // Additional notes for the transaction

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        public DateTime? CompletedAt { get; set; }

        // For rental transactions
        public DateTime? StartDate { get; set; }
        
        public DateTime? EndDate { get; set; }
        
        // For purchase transactions
        public bool IsReservation { get; set; } = false; // Whether this is a reservation payment for purchase
        
        public decimal ReservationAmount { get; set; } // Amount paid for reservation (for Purchase)
    }
} 