using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EstateEase.Models.ViewModels;
using EstateEase.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using EstateEase.Models.Entities;

namespace EstateEase.Areas.Agent.Controllers
{
    [Area("Agent")]
    [Authorize(Roles = "Agent")]
    public class PaymentController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PaymentController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                // Get current user ID
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                
                // Get agent ID for the current user
                var agent = await _context.Agents.FirstOrDefaultAsync(a => a.UserId == userId);
                
                if (agent == null)
                {
                    TempData["Error"] = "Agent profile not found.";
                    return View(new List<PaymentViewModel>());
                }

                // Get all properties belonging to this agent
                var agentPropertyIds = await _context.Properties
                    .Where(p => p.AgentId == agent.Id)
                    .Select(p => p.Id)
                    .ToListAsync();

                // Get all transactions for these properties
                var transactions = await _context.Transactions
                    .Include(t => t.Property)
                    .Where(t => agentPropertyIds.Contains(t.PropertyId) && t.Status == "Completed")
                    .OrderByDescending(t => t.CompletedAt ?? t.CreatedAt)
                    .ToListAsync();

                // Map transactions to view models
                var payments = new List<PaymentViewModel>();
                
                foreach (var transaction in transactions)
                {
                    // Get user info
                    var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == transaction.UserId);
                    var userProfile = await _context.UserProfiles.FirstOrDefaultAsync(up => up.UserId == transaction.UserId);
                    
                    // Get property image
                    var propertyImage = await _context.PropertyImages
                        .Where(pi => pi.PropertyId == transaction.PropertyId)
                        .FirstOrDefaultAsync();
                    
                    // Calculate commission (3% of transaction amount)
                    decimal commissionPercentage = 3.0m;
                    decimal commission = transaction.Amount * (commissionPercentage / 100);
                    
                    payments.Add(new PaymentViewModel
                    {
                        Id = int.Parse(transaction.Id.Substring(0, Math.Min(8, transaction.Id.Length)), System.Globalization.NumberStyles.HexNumber),
                        PropertyId = transaction.PropertyId,
                        PropertyTitle = transaction.Property?.Title ?? "Unknown Property",
                        PropertyAddress = transaction.Property?.Address ?? "Unknown Address",
                        PropertyImageUrl = propertyImage?.ImagePath ?? "/images/property-placeholder.jpg",
                        TransactionType = transaction.TransactionType,
                        Amount = transaction.Amount,
                        Commission = commission,
                        CommissionPercentage = commissionPercentage,
                        ClientName = userProfile != null ? $"{userProfile.FirstName} {userProfile.LastName}" : user?.UserName ?? "Unknown Client",
                        ClientEmail = user?.Email ?? "No email",
                        TransactionDate = transaction.CompletedAt ?? transaction.CreatedAt,
                        PaymentMethod = transaction.PaymentMethod ?? "Online Payment",
                        ReferenceNumber = transaction.ReferenceNumber ?? transaction.Id.Substring(0, 8),
                        Notes = transaction.Notes ?? "Transaction completed successfully",
                        CreatedAt = transaction.CreatedAt
                    });
                }

                return View(payments);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while loading payment data: " + ex.Message;
                return View(new List<PaymentViewModel>());
            }
        }

        public async Task<IActionResult> Details(int id)
        {
            try
            {
                // Get current user ID
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                
                // Get agent ID for the current user
                var agent = await _context.Agents.FirstOrDefaultAsync(a => a.UserId == userId);
                
                if (agent == null)
                {
                    TempData["Error"] = "Agent profile not found.";
                    return RedirectToAction("Index");
                }

                // Get all properties belonging to this agent
                var agentPropertyIds = await _context.Properties
                    .Where(p => p.AgentId == agent.Id)
                    .Select(p => p.Id)
                    .ToListAsync();

                // Try to find the transaction by converting ID format
                string transactionId = id.ToString("X"); // Convert to hex
                
                var transaction = await _context.Transactions
                    .Include(t => t.Property)
                    .Where(t => t.Id.StartsWith(transactionId) && agentPropertyIds.Contains(t.PropertyId))
                    .FirstOrDefaultAsync();

                if (transaction == null)
                {
                    TempData["Error"] = "Transaction not found or you don't have permission to view it.";
                    return RedirectToAction("Index");
                }

                // Get user info
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == transaction.UserId);
                var userProfile = await _context.UserProfiles.FirstOrDefaultAsync(up => up.UserId == transaction.UserId);
                
                // Get property image
                var propertyImage = await _context.PropertyImages
                    .Where(pi => pi.PropertyId == transaction.PropertyId)
                    .FirstOrDefaultAsync();
                
                // Calculate commission (3% of transaction amount)
                decimal commissionPercentage = 3.0m;
                decimal commission = transaction.Amount * (commissionPercentage / 100);
                
                var payment = new PaymentViewModel
                {
                    Id = id,
                    PropertyId = transaction.PropertyId,
                    PropertyTitle = transaction.Property?.Title ?? "Unknown Property",
                    PropertyAddress = transaction.Property?.Address ?? "Unknown Address",
                    PropertyImageUrl = propertyImage?.ImagePath ?? "/images/property-placeholder.jpg",
                    TransactionType = transaction.TransactionType,
                    Amount = transaction.Amount,
                    Commission = commission,
                    CommissionPercentage = commissionPercentage,
                    ClientName = userProfile != null ? $"{userProfile.FirstName} {userProfile.LastName}" : user?.UserName ?? "Unknown Client",
                    ClientEmail = user?.Email ?? "No email",
                    TransactionDate = transaction.CompletedAt ?? transaction.CreatedAt,
                    PaymentMethod = transaction.PaymentMethod ?? "Online Payment",
                    ReferenceNumber = transaction.ReferenceNumber ?? transaction.Id.Substring(0, 8),
                    Notes = transaction.Notes ?? "Transaction completed successfully",
                    CreatedAt = transaction.CreatedAt
                };

                return View(payment);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while loading payment details: " + ex.Message;
                return RedirectToAction("Index");
            }
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