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
using EstateEase.Services;
using Microsoft.Extensions.Logging;

namespace EstateEase.Areas.User.Controllers
{
    [Area("User")]
    [Authorize(Roles = "User")]
    public class PaymentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IPaymentService _paymentService;
        private readonly ILogger<PaymentsController> _logger;

        public PaymentsController(
            ApplicationDbContext context,
            IPaymentService paymentService,
            ILogger<PaymentsController> logger)
        {
            _context = context;
            _paymentService = paymentService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                _logger.LogInformation($"Fetching payment history for user: {userId}");

                // Get the user's transactions
                var transactions = await _paymentService.GetUserTransactions(userId);
                _logger.LogInformation($"Found {transactions.Count} transactions for user {userId}");

                // Convert to view models
                var viewModels = new List<PaymentViewModel>();
                foreach (var transaction in transactions)
                {
                    try
                    {
                        // Get property details
                        var property = await _context.Properties
                            .FirstOrDefaultAsync(p => p.Id == transaction.PropertyId);

                        // Try to get agent name if available through the property
                        string agentName = null;
                        if (property != null && !string.IsNullOrEmpty(property.AgentId))
                        {
                            var agent = await _context.Users
                                .FirstOrDefaultAsync(u => u.Id == property.AgentId);
                            agentName = agent?.UserName;
                        }

                        // Get the UserProperty for expiry date if rental
                        DateTime? expiryDate = null;
                        if (transaction.TransactionType == "Rental" || transaction.TransactionType == "Rent")
                        {
                            var userProperty = await _context.UserProperties
                                .FirstOrDefaultAsync(up => up.UserId == userId && 
                                                    up.PropertyId == transaction.PropertyId);
                            
                            if (userProperty != null && userProperty.ExpiryDate.HasValue)
                            {
                                expiryDate = userProperty.ExpiryDate.Value;
                            }
                            else if (transaction.EndDate.HasValue)
                            {
                                // Fallback to transaction end date
                                expiryDate = transaction.EndDate;
                            }
                        }

                        // Create view model with correct ID type (string to int)
                        int transactionId;
                        if (!int.TryParse(transaction.Id, out transactionId))
                        {
                            transactionId = 0; // Default ID if parse fails
                        }

                        var model = new PaymentViewModel
                        {
                            Id = transactionId,
                            PropertyId = transaction.PropertyId,
                            PropertyTitle = property?.Title ?? "Unknown Property",
                            PropertyAddress = property?.Address ?? "Address unavailable",
                            PropertyImageUrl = GetPropertyImageUrl(property),
                            TransactionType = transaction.TransactionType,
                            Amount = transaction.Amount,
                            Status = transaction.Status ?? "Completed",
                            TransactionDate = transaction.CreatedAt,
                            PaymentMethod = transaction.PaymentMethod ?? "Online Payment",
                            ReferenceNumber = transaction.ReferenceNumber,
                            Notes = transaction.Notes,
                            RentalDuration = transaction.RentalDuration,
                            ExpiryDate = expiryDate,
                            AgentName = agentName
                        };

                        viewModels.Add(model);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error processing transaction {transaction.Id}: {ex.Message}");
                    }
                }

                return View(viewModels.OrderByDescending(t => t.TransactionDate).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching payment history: {ex.Message}");
                TempData["Error"] = "There was an error loading your payment history. Please try again later.";
                return View(new List<PaymentViewModel>());
            }
        }

        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                
                // Get the transaction - convert int to string for the query
                var transaction = await _context.Transactions
                    .FirstOrDefaultAsync(t => t.Id == id.ToString() && t.UserId == userId);

                if (transaction == null)
                {
                    _logger.LogWarning($"Transaction {id} not found for user {userId}");
                    TempData["Error"] = "Transaction not found or you don't have permission to view it.";
                    return RedirectToAction(nameof(Index));
                }

                // Get property details
                var property = await _context.Properties
                    .FirstOrDefaultAsync(p => p.Id == transaction.PropertyId);

                // Try to get agent name if available through the property
                string agentName = null;
                if (property != null && !string.IsNullOrEmpty(property.AgentId))
                {
                    var agent = await _context.Users
                        .FirstOrDefaultAsync(u => u.Id == property.AgentId);
                    agentName = agent?.UserName;
                }

                // Get the UserProperty for expiry date if rental
                DateTime? expiryDate = null;
                if (transaction.TransactionType == "Rental" || transaction.TransactionType == "Rent")
                {
                    var userProperty = await _context.UserProperties
                        .FirstOrDefaultAsync(up => up.UserId == userId && 
                                           up.PropertyId == transaction.PropertyId);
                    
                    if (userProperty != null && userProperty.ExpiryDate.HasValue)
                    {
                        expiryDate = userProperty.ExpiryDate.Value;
                    }
                    else if (transaction.EndDate.HasValue)
                    {
                        // Fallback to transaction end date
                        expiryDate = transaction.EndDate;
                    }
                }

                // Create view model
                var model = new PaymentViewModel
                {
                    Id = id,
                    PropertyId = transaction.PropertyId,
                    PropertyTitle = property?.Title ?? "Unknown Property",
                    PropertyAddress = property?.Address ?? "Address unavailable",
                    PropertyImageUrl = GetPropertyImageUrl(property),
                    TransactionType = transaction.TransactionType,
                    Amount = transaction.Amount,
                    Status = transaction.Status ?? "Completed",
                    TransactionDate = transaction.CreatedAt,
                    PaymentMethod = transaction.PaymentMethod ?? "Online Payment",
                    ReferenceNumber = transaction.ReferenceNumber,
                    Notes = transaction.Notes,
                    RentalDuration = transaction.RentalDuration,
                    ExpiryDate = expiryDate,
                    AgentName = agentName
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching transaction details: {ex.Message}");
                TempData["Error"] = "There was an error loading the transaction details. Please try again later.";
                return RedirectToAction(nameof(Index));
            }
        }

        public async Task<IActionResult> Download(int id)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                
                // Get the transaction - convert int to string for the query
                var transaction = await _context.Transactions
                    .FirstOrDefaultAsync(t => t.Id == id.ToString() && t.UserId == userId);

                if (transaction == null)
                {
                    TempData["Error"] = "Transaction not found or you don't have permission to download it.";
                    return RedirectToAction(nameof(Index));
                }

                if (transaction.Status != "Completed")
                {
                    TempData["Warning"] = "You can only download receipts for completed transactions.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                // Generate a PDF receipt using the payment service (implementation required)
                // This is a stub - you would implement actual PDF generation in the PaymentService
                
                TempData["Info"] = "Receipt download feature is under development. Please check back later.";
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error downloading receipt: {ex.Message}");
                TempData["Error"] = "There was an error generating the receipt. Please try again later.";
                return RedirectToAction(nameof(Index));
            }
        }

        // Helper method to get property image URL
        private string GetPropertyImageUrl(Property property)
        {
            if (property == null)
            {
                return "/images/property-placeholder.jpg";
            }

            try
            {
                // First try to get from PropertyImages collection
                if (property.PropertyImages != null && property.PropertyImages.Any())
                {
                    var firstImage = property.PropertyImages.FirstOrDefault();
                    if (firstImage != null && !string.IsNullOrEmpty(firstImage.ImagePath))
                    {
                        // Ensure path starts with /
                        string path = firstImage.ImagePath;
                        if (!path.StartsWith("/") && !path.StartsWith("http"))
                        {
                            path = "/" + path;
                        }
                        return path;
                    }
                }

                // Try to fetch image directly from database if not loaded with property
                var propertyImage = _context.Set<PropertyImage>()
                    .FirstOrDefault(pi => pi.PropertyId == property.Id);
                
                if (propertyImage != null && !string.IsNullOrEmpty(propertyImage.ImagePath))
                {
                    string path = propertyImage.ImagePath;
                    if (!path.StartsWith("/") && !path.StartsWith("http"))
                    {
                        path = "/" + path;
                    }
                    return path;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching property image: {ex.Message}");
            }

            // Fallback to placeholder
            return "/images/property-placeholder.jpg";
        }
    }
} 