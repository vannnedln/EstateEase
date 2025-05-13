using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EstateEase.Models.ViewModels;
using System;
using System.Collections.Generic;

namespace EstateEase.Areas.Agent.Controllers
{
    [Area("Agent")]
    [Authorize(Roles = "Agent")]
    public class PaymentController : Controller
    {
        public IActionResult Index()
        {
            // Create sample payment data until database integration is complete
            var payments = new List<PaymentViewModel>
            {
                new PaymentViewModel
                {
                    Id = 1,
                    PropertyId = 1,
                    PropertyTitle = "Modern Apartment in Makati",
                    PropertyAddress = "123 Ayala Ave, Makati City",
                    PropertyImageUrl = "/images/property-placeholder.jpg",
                    TransactionType = "Sale",
                    Amount = 5500000M,
                    Commission = 137500M,
                    CommissionPercentage = 2.5M,
                    ClientName = "John Doe",
                    ClientEmail = "john.doe@example.com",
                    TransactionDate = DateTime.Now.AddDays(-15),
                    PaymentMethod = "Bank Transfer",
                    ReferenceNumber = "TRX-12345",
                    Notes = "Property sold above asking price"
                },
                new PaymentViewModel
                {
                    Id = 2,
                    PropertyId = 2,
                    PropertyTitle = "Luxury Condo in BGC",
                    PropertyAddress = "456 Bonifacio High Street, Taguig",
                    PropertyImageUrl = "/images/property-placeholder.jpg",
                    TransactionType = "Rental",
                    Amount = 45000M,
                    Commission = 1125M,
                    CommissionPercentage = 2.5M,
                    ClientName = "Jane Smith",
                    ClientEmail = "jane.smith@example.com",
                    TransactionDate = DateTime.Now.AddDays(-7),
                    PaymentMethod = "Credit Card",
                    ReferenceNumber = "TRX-67890",
                    Notes = "1-year lease contract"
                },
                new PaymentViewModel
                {
                    Id = 3,
                    PropertyId = 3,
                    PropertyTitle = "Family Home in Quezon City",
                    PropertyAddress = "789 Commonwealth Ave, Quezon City",
                    PropertyImageUrl = "/images/property-placeholder.jpg",
                    TransactionType = "Sale",
                    Amount = 7800000M,
                    Commission = 195000M,
                    CommissionPercentage = 2.5M,
                    ClientName = "Mark Johnson",
                    ClientEmail = "mark.johnson@example.com",
                    TransactionDate = DateTime.Now.AddDays(-30),
                    PaymentMethod = "Bank Transfer",
                    ReferenceNumber = "TRX-24680",
                    Notes = "Negotiated sale"
                }
            };

            return View(payments);
        }

        public IActionResult Details(int id)
        {
            // For demo purposes, create a sample payment based on the id
            var payment = new PaymentViewModel
            {
                Id = id,
                PropertyId = id,
                PropertyTitle = "Property #" + id,
                PropertyAddress = id + " Sample Street, Metro Manila",
                PropertyImageUrl = "/images/property-placeholder.jpg",
                TransactionType = id % 2 == 0 ? "Rental" : "Sale",
                Amount = id % 2 == 0 ? 45000M : 5000000M,
                Commission = id % 2 == 0 ? 1125M : 125000M,
                CommissionPercentage = 2.5M,
                ClientName = "Client #" + id,
                ClientEmail = "client" + id + "@example.com",
                TransactionDate = DateTime.Now.AddDays(-id),
                PaymentMethod = "Bank Transfer",
                ReferenceNumber = "TRX-" + id.ToString("D5"),
                Notes = "Sample transaction notes for transaction #" + id
            };

            return View(payment);
        }

        public IActionResult Download(int id)
        {
            // In a real application, generate an invoice PDF
            // For now, just redirect back with a message
            TempData["Info"] = "Invoice download functionality will be implemented soon.";
            return RedirectToAction(nameof(Details), new { id });
        }
    }
} 