using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EstateEase.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EstateEase.Areas.Agent.Controllers
{
    [Area("Agent")]
    [Authorize(Roles = "Agent")]
    public class InquiryController : Controller
    {
        public IActionResult Index()
        {
            // Create sample inquiry data until database integration is complete
            var inquiries = new List<InquiryViewModel>
            {
                new InquiryViewModel
                {
                    Id = 1,
                    ClientName = "John Doe",
                    ClientEmail = "john.doe@example.com",
                    ClientPhone = "123-456-7890",
                    PropertyId = 1,
                    PropertyTitle = "Modern Apartment in Makati",
                    PropertyAddress = "123 Ayala Ave, Makati City",
                    Subject = "Inquiry about availability",
                    Message = "I'm interested in this property and would like to schedule a viewing.",
                    Status = "New",
                    CreatedAt = DateTime.Now.AddDays(-2)
                },
                new InquiryViewModel
                {
                    Id = 2,
                    ClientName = "Jane Smith",
                    ClientEmail = "jane.smith@example.com",
                    ClientPhone = "987-654-3210",
                    PropertyId = 2,
                    PropertyTitle = "Luxury Condo in BGC",
                    PropertyAddress = "456 Bonifacio High Street, Taguig",
                    Subject = "Price negotiation",
                    Message = "Is there any flexibility on the price? I'm very interested but my budget is slightly lower.",
                    Status = "In Progress",
                    CreatedAt = DateTime.Now.AddDays(-1)
                },
                new InquiryViewModel
                {
                    Id = 3,
                    ClientName = "Mark Johnson",
                    ClientEmail = "mark.johnson@example.com",
                    ClientPhone = "555-123-4567",
                    PropertyId = 3,
                    PropertyTitle = "Family Home in Quezon City",
                    PropertyAddress = "789 Commonwealth Ave, Quezon City",
                    Subject = "Payment options",
                    Message = "Do you accept installment payments? What are the terms?",
                    Status = "Resolved",
                    CreatedAt = DateTime.Now.AddDays(-3)
                }
            };

            return View(inquiries);
        }

        public IActionResult Details(int id)
        {
            // Fetch inquiry details and return view
            // For now, return a sample inquiry
            var inquiry = new InquiryViewModel
            {
                Id = id,
                ClientName = "John Doe",
                ClientEmail = "john.doe@example.com",
                ClientPhone = "123-456-7890",
                PropertyId = 1,
                PropertyTitle = "Modern Apartment in Makati",
                PropertyAddress = "123 Ayala Ave, Makati City",
                Subject = "Inquiry about availability",
                Message = "I'm interested in this property and would like to schedule a viewing.",
                Status = "New",
                CreatedAt = DateTime.Now.AddDays(-2)
            };

            return View(inquiry);
        }

        public IActionResult Reply(int id)
        {
            // Create a new reply view model with data from the inquiry
            var model = new InquiryReplyViewModel
            {
                InquiryId = id,
                InquirySubject = "Inquiry about availability",
                ClientName = "John Doe",
                ClientEmail = "john.doe@example.com"
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Reply(int id, InquiryReplyViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Send reply logic here

            TempData["Success"] = "Reply sent successfully";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult MarkAsResolved(int id)
        {
            // Mark inquiry as resolved logic here

            TempData["Success"] = "Inquiry marked as resolved";
            return RedirectToAction(nameof(Index));
        }
    }
} 