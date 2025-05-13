using EstateEase.Data;
using EstateEase.Models.Entities;
using EstateEase.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.IO;
using System.Security.Claims;
using System.Collections.Generic;

namespace EstateEase.Controllers
{
    [Authorize]
    public class PaymentController : Controller
    {
        private readonly IPaymentService _paymentService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(
            IPaymentService paymentService,
            ApplicationDbContext context,
            ILogger<PaymentController> logger)
        {
            _paymentService = paymentService;
            _context = context;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> CreateRentCheckout(string propertyId, string notes)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                
                // Get property details
                var property = await _context.Properties
                    .FirstOrDefaultAsync(p => p.Id == propertyId);
                
                if (property == null)
                {
                    return NotFound("Property not found");
                }
                
                // Create a transaction record
                var transaction = new Transaction
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = userId,
                    PropertyId = propertyId,
                    Amount = property.Price, // First month's rent
                    TransactionType = "Rent",
                    Status = "Initiated",
                    PaymentMethod = "Card", // Default to card, can be updated based on selection
                    ReferenceNumber = $"RENT-{DateTime.Now.ToString("yyyyMMddHHmmss")}",
                    Notes = notes,
                    CreatedAt = DateTime.Now,
                    StartDate = DateTime.Now,
                    EndDate = DateTime.Now.AddMonths(1)
                };
                
                _context.Transactions.Add(transaction);
                await _context.SaveChangesAsync();
                
                // Create a checkout session with PayMongo
                var successUrl = Url.Action("PaymentSuccess", "Payment", new { transactionId = transaction.Id }, Request.Scheme);
                var cancelUrl = Url.Action("PaymentCancel", "Payment", new { transactionId = transaction.Id }, Request.Scheme);
                
                var checkoutUrl = await _paymentService.CreateCheckoutSession(transaction, successUrl, cancelUrl);
                
                return Redirect(checkoutUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating rent checkout");
                return RedirectToAction("Error", "Home", new { message = "Failed to process payment." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreatePurchaseCheckout(string propertyId, string notes, bool isReservation = false)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                
                // Get property details
                var property = await _context.Properties
                    .FirstOrDefaultAsync(p => p.Id == propertyId);
                
                if (property == null)
                {
                    return NotFound("Property not found");
                }
                
                // Create a transaction record for full purchase
                var transaction = new Transaction
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = userId,
                    PropertyId = propertyId,
                    Amount = property.Price,
                    TransactionType = "Purchase",
                    Status = "Initiated",
                    PaymentMethod = "Card", // Default to card, can be updated based on selection
                    ReferenceNumber = $"PURCHASE-{DateTime.Now.ToString("yyyyMMddHHmmss")}",
                    Notes = notes,
                    CreatedAt = DateTime.Now,
                    IsReservation = false,
                    ReservationAmount = 0
                };
                
                _context.Transactions.Add(transaction);
                await _context.SaveChangesAsync();
                
                // Create a checkout session with PayMongo
                var successUrl = Url.Action("PaymentSuccess", "Payment", new { transactionId = transaction.Id }, Request.Scheme);
                var cancelUrl = Url.Action("PaymentCancel", "Payment", new { transactionId = transaction.Id }, Request.Scheme);
                
                var checkoutUrl = await _paymentService.CreateCheckoutSession(transaction, successUrl, cancelUrl);
                
                return Redirect(checkoutUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating purchase checkout");
                return RedirectToAction("Error", "Home", new { message = "Failed to process payment." });
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> PaymentSuccess(string transactionId)
        {
            try
            {
                // Ensure the transaction exists
                var transaction = await _context.Transactions
                    .Include(t => t.Property)
                    .FirstOrDefaultAsync(t => t.Id == transactionId);
                    
                if (transaction == null)
                {
                    return NotFound("Transaction not found");
                }
                
                // Update transaction status if it's not already completed
                if (transaction.Status != "Completed")
                {
                    transaction.Status = "Completed";
                    transaction.CompletedAt = DateTime.Now;
                    await _context.SaveChangesAsync();
                    
                    // Update property status
                    var property = await _context.Properties.FindAsync(transaction.PropertyId);
                    if (property != null)
                    {
                        if (transaction.TransactionType == "Rent")
                        {
                            property.Status = "Rented";
                        }
                        else if (transaction.TransactionType == "Purchase")
                        {
                            property.Status = "Sold";
                        }
                        await _context.SaveChangesAsync();
                    }
                }
                
                // Set success message
                TempData["SuccessMessage"] = "Payment completed successfully!";
                
                // Redirect to transaction details instead of showing a separate view
                return RedirectToAction("TransactionDetails", new { id = transactionId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payment success");
                return RedirectToAction("Error", "Home", new { message = "Error processing payment confirmation: " + ex.Message });
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> PaymentCancel(string transactionId)
        {
            // Update transaction status
            await _paymentService.UpdateTransactionStatus(transactionId, "Cancelled");
            
            // Add cancel message
            TempData["WarningMessage"] = "Payment was cancelled.";
            
            // Redirect to transaction details
            return RedirectToAction("TransactionDetails", new { id = transactionId });
        }

        [HttpPost]
        public async Task<IActionResult> WebhookHandler()
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var payload = await reader.ReadToEndAsync();
            
            // Get the PayMongo signature from the request headers
            Request.Headers.TryGetValue("PayMongo-Signature", out var signatureHeader);
            
            var isProcessed = await _paymentService.ProcessWebhookEvent(payload, signatureHeader);
            
            if (isProcessed)
            {
                return Ok(new { message = "Webhook processed successfully" });
            }
            else
            {
                return BadRequest(new { message = "Failed to process webhook" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Transactions()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var transactions = await _paymentService.GetUserTransactions(userId);
            
            return View(transactions);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> TransactionDetails(string id)
        {
            try
            {
                var transaction = await _context.Transactions
                    .Include(t => t.Property)
                    .ThenInclude(p => p.PropertyImages)
                    .FirstOrDefaultAsync(t => t.Id == id);
                    
                if (transaction == null)
                {
                    return NotFound();
                }
                
                // Ensure PropertyImages collection is never null
                if (transaction.Property.PropertyImages == null)
                {
                    transaction.Property.PropertyImages = new List<PropertyImage>();
                }
                
                return View(transaction);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error viewing transaction details");
                return RedirectToAction("Error", "Home", new { message = "Error viewing transaction details: " + ex.Message });
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> DownloadReceipt(string id)
        {
            var transaction = await _context.Transactions
                .Include(t => t.Property)
                .FirstOrDefaultAsync(t => t.Id == id);
                
            if (transaction == null)
            {
                return NotFound();
            }
            
            // For simplicity, we're just returning a text receipt for now
            // In a real implementation, you would generate a PDF using iTextSharp or a similar library
            
            var content = $"RECEIPT\n\n" +
                          $"Reference: {transaction.ReferenceNumber}\n" +
                          $"Date: {transaction.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")}\n" +
                          $"Property: {transaction.Property.Title}\n" +
                          $"Amount: PHP {transaction.Amount:N2}\n" +
                          $"Status: {transaction.Status}\n" +
                          $"Payment Method: {transaction.PaymentMethod}\n" +
                          $"Transaction Type: {transaction.TransactionType}";
                          
            return File(Encoding.UTF8.GetBytes(content), "text/plain", $"receipt_{transaction.ReferenceNumber}.txt");
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> CompletePayment(string id)
        {
            try
            {
                var transaction = await _context.Transactions
                    .Include(t => t.Property)
                    .FirstOrDefaultAsync(t => t.Id == id);
                    
                if (transaction == null)
                {
                    return NotFound();
                }
                
                if (transaction.Status == "Pending")
                {
                    // Update transaction status to completed
                    await _paymentService.UpdateTransactionStatus(id, "Completed");
                    
                    // Add success message
                    TempData["SuccessMessage"] = "Payment completed successfully!";
                }
                
                // Redirect back to transaction details
                return RedirectToAction("TransactionDetails", new { id = id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing payment manually");
                return RedirectToAction("Error", "Home", new { message = "Error completing payment: " + ex.Message });
            }
        }
    }
} 