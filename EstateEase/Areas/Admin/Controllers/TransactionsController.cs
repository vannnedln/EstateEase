using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EstateEase.Data;
using EstateEase.Models.ViewModels;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;

namespace EstateEase.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class TransactionsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TransactionsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                // Get all transactions
                var transactions = await _context.Transactions
                    .Include(t => t.Property)
                    .Where(t => t.Status == "Completed")
                    .OrderByDescending(t => t.CompletedAt ?? t.CreatedAt)
                    .ToListAsync();

                // Map transactions to view models
                var payments = new List<PaymentViewModel>();
                
                foreach (var transaction in transactions)
                {
                    // Get user info
                    var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == transaction.UserId);
                    var userProfile = await _context.UserProfiles.FirstOrDefaultAsync(up => up.UserId == transaction.UserId);
                    
                    // Get agent info if exists
                    var agent = transaction.Property?.AgentId != null 
                        ? await _context.Agents.FirstOrDefaultAsync(a => a.Id == transaction.Property.AgentId)
                        : null;
                    
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
                        AgentName = agent != null ? $"{agent.FirstName} {agent.LastName}" : "No Agent",
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
                TempData["Error"] = "An error occurred while loading transaction data: " + ex.Message;
                return View(new List<PaymentViewModel>());
            }
        }

        public async Task<IActionResult> Details(int id)
        {
            try
            {
                // Try to find the transaction by converting ID format
                string transactionId = id.ToString("X"); // Convert to hex
                
                var transaction = await _context.Transactions
                    .Include(t => t.Property)
                    .Where(t => t.Id.StartsWith(transactionId))
                    .FirstOrDefaultAsync();

                if (transaction == null)
                {
                    TempData["Error"] = "Transaction not found.";
                    return RedirectToAction("Index");
                }

                // Get user info
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == transaction.UserId);
                var userProfile = await _context.UserProfiles.FirstOrDefaultAsync(up => up.UserId == transaction.UserId);
                
                // Get agent info if exists
                var agent = transaction.Property?.AgentId != null 
                    ? await _context.Agents.FirstOrDefaultAsync(a => a.Id == transaction.Property.AgentId)
                    : null;
                
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
                    AgentName = agent != null ? $"{agent.FirstName} {agent.LastName}" : "No Agent",
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
                TempData["Error"] = "An error occurred while loading transaction details: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        public async Task<IActionResult> Download(int id)
        {
            try
            {
                // Try to find the transaction by converting ID format
                string transactionId = id.ToString("X"); // Convert to hex
                
                var transaction = await _context.Transactions
                    .Include(t => t.Property)
                    .Where(t => t.Id.StartsWith(transactionId))
                    .FirstOrDefaultAsync();

                if (transaction == null)
                {
                    TempData["Error"] = "Transaction not found.";
                    return RedirectToAction("Index");
                }

                // Get user info
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == transaction.UserId);
                var userProfile = await _context.UserProfiles.FirstOrDefaultAsync(up => up.UserId == transaction.UserId);
                
                // Get agent info if exists
                var agent = transaction.Property?.AgentId != null 
                    ? await _context.Agents.FirstOrDefaultAsync(a => a.Id == transaction.Property.AgentId)
                    : null;
                
                // Calculate commission
                decimal commissionPercentage = 3.0m;
                decimal commission = transaction.Amount * (commissionPercentage / 100);
                
                var refNumber = transaction.ReferenceNumber ?? transaction.Id.Substring(0, 8);
                var invoiceFileName = $"Invoice_{refNumber}_{DateTime.Now.ToString("yyyyMMdd")}.pdf";
                
                // In a real application, you would use a PDF library here like iTextSharp, DinkToPdf, etc.
                // For this simplified implementation, we'll generate a text file instead
                var tempFile = Path.GetTempFileName();
                using (var writer = new StreamWriter(tempFile))
                {
                    writer.WriteLine("EstateEase - Transaction Invoice");
                    writer.WriteLine("==============================");
                    writer.WriteLine();
                    writer.WriteLine($"Reference #: {refNumber}");
                    writer.WriteLine($"Date: {(transaction.CompletedAt ?? transaction.CreatedAt).ToString("MMM dd, yyyy hh:mm tt")}");
                    writer.WriteLine($"Status: Completed");
                    writer.WriteLine();
                    writer.WriteLine("Property Information");
                    writer.WriteLine("-------------------");
                    writer.WriteLine($"Title: {transaction.Property?.Title ?? "Unknown Property"}");
                    writer.WriteLine($"Address: {transaction.Property?.Address ?? "Unknown Address"}");
                    writer.WriteLine();
                    writer.WriteLine("Client Information");
                    writer.WriteLine("-----------------");
                    writer.WriteLine($"Name: {(userProfile != null ? $"{userProfile.FirstName} {userProfile.LastName}" : user?.UserName ?? "Unknown Client")}");
                    writer.WriteLine($"Email: {user?.Email ?? "No email"}");
                    writer.WriteLine();
                    writer.WriteLine("Agent Information");
                    writer.WriteLine("----------------");
                    writer.WriteLine($"Name: {(agent != null ? $"{agent.FirstName} {agent.LastName}" : "No Agent")}");
                    writer.WriteLine();
                    writer.WriteLine("Payment Details");
                    writer.WriteLine("---------------");
                    writer.WriteLine($"Transaction Type: {transaction.TransactionType}");
                    writer.WriteLine($"Payment Method: {transaction.PaymentMethod ?? "Online Payment"}");
                    writer.WriteLine($"Transaction Amount: ₱{transaction.Amount.ToString("N2")}");
                    writer.WriteLine($"Commission ({commissionPercentage}%): ₱{commission.ToString("N2")}");
                    writer.WriteLine($"Total Amount: ₱{transaction.Amount.ToString("N2")}");
                    writer.WriteLine();
                    writer.WriteLine("Notes");
                    writer.WriteLine("-----");
                    writer.WriteLine(transaction.Notes ?? "Transaction completed successfully");
                }
                
                // Return the text file as a downloadable PDF (for demo purposes)
                byte[] fileBytes = System.IO.File.ReadAllBytes(tempFile);
                System.IO.File.Delete(tempFile);  // Delete the temp file
                
                return File(fileBytes, "application/pdf", invoiceFileName);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while generating the invoice: " + ex.Message;
                return RedirectToAction("Details", new { id });
            }
        }
    }
} 