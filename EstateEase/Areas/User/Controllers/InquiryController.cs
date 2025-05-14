using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EstateEase.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EstateEase.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using EstateEase.Models.Entities;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Http;

namespace EstateEase.Areas.User.Controllers
{
    [Area("User")]
    [Authorize(Roles = "User")]
    public class InquiryController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public InquiryController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<int> GetUnreadRepliesCount()
        {
            var currentUserId = _userManager.GetUserId(User);
            
            return await _context.Inquiries
                .Where(i => i.UserId == currentUserId && i.ReadByUser == false)
                .CountAsync();
        }

        public async Task<IActionResult> Index()
        {
            var currentUserId = _userManager.GetUserId(User);
            
            // Get all inquiries made by this user
            var inquiries = await _context.Inquiries
                .Include(i => i.Property)
                .Where(i => i.UserId == currentUserId)
                .OrderByDescending(i => i.CreatedAt)
                .Select(i => new InquiryViewModel
                {
                    Id = i.Id,
                    PropertyId = i.PropertyId,
                    PropertyTitle = i.Property != null ? i.Property.Title : null,
                    PropertyAddress = i.Property != null ? i.Property.Address : null,
                    Subject = i.Subject,
                    Message = i.Message,
                    Status = i.Status,
                    CreatedAt = i.CreatedAt,
                    UpdatedAt = i.UpdatedAt,
                    ReadByUser = i.ReadByUser
                })
                .ToListAsync();

            // Set the unread count in ViewBag for the layout to use
            ViewBag.UnreadRepliesCount = inquiries.Count(i => i.ReadByUser == false);
            
            return View(inquiries);
        }

        public async Task<IActionResult> Details(int id, string replyMessage = null)
        {
            var currentUserId = _userManager.GetUserId(User);
            
            // Check if we have a reply message - if so, process it before showing the details
            if (!string.IsNullOrEmpty(replyMessage) && Request.Method.Equals("POST", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"[DEBUG] Reply received directly in Details action: {replyMessage}");
                
                // Get the inquiry to update
                var inquiryToUpdate = await _context.Inquiries
                    .Where(i => i.UserId == currentUserId && i.Id == id)
                    .FirstOrDefaultAsync();
                    
                if (inquiryToUpdate != null)
                {
                    // Create new message
                    var message = new InquiryMessage
                    {
                        InquiryId = id,
                        SenderId = currentUserId,
                        SenderType = "User",
                        Message = replyMessage,
                        CreatedAt = DateTime.Now,
                        IsRead = false // Not read by agent/admin yet
                    };
                    
                    _context.InquiryMessages.Add(message);
                    
                    // Update inquiry status
                    inquiryToUpdate.Status = "In Progress";
                    inquiryToUpdate.UpdatedAt = DateTime.Now;
                    inquiryToUpdate.ReadByUser = true;
                    
                    try
                    {
                        await _context.SaveChangesAsync();
                        Console.WriteLine("[DEBUG] Reply saved from Details action");
                        TempData["Success"] = "Your reply has been sent successfully.";
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[DEBUG] Error saving reply: {ex.Message}");
                        TempData["Error"] = "An error occurred while sending your reply.";
                    }
                    
                    // Redirect to refresh the page and show the success message
                    return RedirectToAction(nameof(Details), new { id });
                }
            }
            
            // Get the inquiry with details
            var inquiry = await _context.Inquiries
                .Include(i => i.Property)
                .Include(i => i.Messages)
                .Where(i => i.UserId == currentUserId && i.Id == id)
                .Select(i => new InquiryViewModel
                {
                    Id = i.Id,
                    PropertyId = i.PropertyId,
                    PropertyTitle = i.Property != null ? i.Property.Title : null,
                    PropertyAddress = i.Property != null ? i.Property.Address : null,
                    Subject = i.Subject,
                    Message = i.Message,
                    ReplyMessage = i.ReplyMessage,
                    Status = i.Status,
                    CreatedAt = i.CreatedAt,
                    UpdatedAt = i.UpdatedAt,
                    ReadByUser = i.ReadByUser,
                    Messages = i.Messages.Select(m => new InquiryMessageViewModel
                    {
                        Id = m.Id,
                        InquiryId = m.InquiryId,
                        SenderId = m.SenderId,
                        SenderType = m.SenderType,
                        SenderName = m.SenderType == "User" ? "You" : m.SenderType,
                        Message = m.Message,
                        IsRead = m.IsRead,
                        CreatedAt = m.CreatedAt,
                        IsFromCurrentUser = m.SenderId == currentUserId
                    }).ToList()
                })
                .FirstOrDefaultAsync();

            if (inquiry == null)
            {
                return NotFound();
            }

            // Mark as read by user if it wasn't already
            if (inquiry.ReadByUser == false)
            {
                var dbInquiry = await _context.Inquiries.FindAsync(id);
                if (dbInquiry != null)
                {
                    dbInquiry.ReadByUser = true;
                    await _context.SaveChangesAsync();
                    
                    // Update view model
                    inquiry.ReadByUser = true;
                }
            }
            
            // Mark all messages as read by the user
            var unreadMessages = await _context.InquiryMessages
                .Where(m => m.InquiryId == id && !m.IsRead && m.SenderType != "User")
                .ToListAsync();
                
            if (unreadMessages.Any())
            {
                foreach (var message in unreadMessages)
                {
                    message.IsRead = true;
                }
                
                await _context.SaveChangesAsync();
            }

            return View(inquiry);
        }

        [HttpGet]
        [Route("/api/user/inquiries/count")]
        public async Task<IActionResult> GetInquiryCounts()
        {
            var currentUserId = _userManager.GetUserId(User);
            
            var totalCount = await _context.Inquiries
                .Where(i => i.UserId == currentUserId)
                .CountAsync();
                
            var newCount = await _context.Inquiries
                .Where(i => i.UserId == currentUserId && i.ReadByUser == false)
                .CountAsync();
                
            var inProgressCount = await _context.Inquiries
                .Where(i => i.UserId == currentUserId && i.Status == "In Progress")
                .CountAsync();
                
            var resolvedCount = await _context.Inquiries
                .Where(i => i.UserId == currentUserId && i.Status == "Resolved")
                .CountAsync();
                
            return Json(new {
                total = totalCount,
                new_count = newCount,
                in_progress = inProgressCount,
                resolved = resolvedCount
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reply(int id, string replyMessage = null, string ReplyMessage = null)
        {
            // Use whichever parameter is provided
            string message = replyMessage ?? ReplyMessage;
            
            // Debug output
            Console.WriteLine($"Reply method called with id={id}, message='{message}'");
            
            if (string.IsNullOrEmpty(message))
            {
                TempData["Error"] = "Reply message cannot be empty.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var currentUserId = _userManager.GetUserId(User);
            
            // Get the inquiry
            var inquiry = await _context.Inquiries
                .Where(i => i.UserId == currentUserId && i.Id == id)
                .FirstOrDefaultAsync();

            if (inquiry == null)
            {
                return NotFound();
            }

            // Create a new message from the user
            var inquiryMessage = new InquiryMessage
            {
                InquiryId = id,
                SenderId = currentUserId,
                SenderType = "User",
                Message = message,
                CreatedAt = DateTime.Now,
                IsRead = false // Not read by agent/admin yet
            };
            
            _context.InquiryMessages.Add(inquiryMessage);
            
            // Update inquiry status
            inquiry.Status = "In Progress";
            inquiry.UpdatedAt = DateTime.Now;
            inquiry.ReadByUser = true; // User has read their own message
            
            await _context.SaveChangesAsync();

            TempData["Success"] = "Your reply has been sent successfully.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // Quick reply from details page
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("User/Inquiry/QuickReply/{id}")]
        public async Task<IActionResult> QuickReply(int id, string replyMessage)
        {
            // Log all form data
            Console.WriteLine($"[DEBUG] QuickReply method called with id={id}, replyMessage='{replyMessage}'");
            foreach (var key in Request.Form.Keys)
            {
                Console.WriteLine($"[DEBUG] Form Key: {key}, Value: {Request.Form[key]}");
            }
            
            if (string.IsNullOrEmpty(replyMessage))
            {
                TempData["Error"] = "Reply message cannot be empty.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var currentUserId = _userManager.GetUserId(User);
            Console.WriteLine($"[DEBUG] CurrentUserId: {currentUserId}");
            
            // Get the inquiry
            var inquiry = await _context.Inquiries
                .Where(i => i.UserId == currentUserId && i.Id == id)
                .FirstOrDefaultAsync();

            if (inquiry == null)
            {
                Console.WriteLine($"[DEBUG] Inquiry with ID {id} not found for user {currentUserId}");
                return NotFound();
            }

            Console.WriteLine($"[DEBUG] Found inquiry with ID {id}, creating message");
            
            // Create a new message from the user
            var message = new InquiryMessage
            {
                InquiryId = id,
                SenderId = currentUserId,
                SenderType = "User",
                Message = replyMessage,
                CreatedAt = DateTime.Now,
                IsRead = false // Not read by agent/admin yet
            };
            
            _context.InquiryMessages.Add(message);
            
            // Update inquiry status
            inquiry.Status = "In Progress";
            inquiry.UpdatedAt = DateTime.Now;
            inquiry.ReadByUser = true; // User has read their own message
            
            try
            {
                await _context.SaveChangesAsync();
                Console.WriteLine($"[DEBUG] Successfully saved message");
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DEBUG] Error saving message: {ex.Message}");
                TempData["Error"] = "Error sending reply";
            }
            
            return RedirectToAction(nameof(Details), new { id });
        }

        // Direct reply method that doesn't rely on model binding
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("User/Inquiry/Reply/{id}")]
        public async Task<IActionResult> DirectReply(int id, string replyMessage)
        {
            // Log all form data
            Console.WriteLine($"[DEBUG] DirectReply method called with id={id}, replyMessage='{replyMessage}'");
            foreach (var key in Request.Form.Keys)
            {
                Console.WriteLine($"[DEBUG] Form Key: {key}, Value: {Request.Form[key]}");
            }
            
            if (string.IsNullOrEmpty(replyMessage))
            {
                TempData["Error"] = "Reply message cannot be empty.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var currentUserId = _userManager.GetUserId(User);
            Console.WriteLine($"[DEBUG] CurrentUserId: {currentUserId}");
            
            // Get the inquiry
            var inquiry = await _context.Inquiries
                .Where(i => i.UserId == currentUserId && i.Id == id)
                .FirstOrDefaultAsync();

            if (inquiry == null)
            {
                Console.WriteLine($"[DEBUG] Inquiry with ID {id} not found for user {currentUserId}");
                return NotFound();
            }

            Console.WriteLine($"[DEBUG] Found inquiry with ID {id}, creating message");
            
            // Create a new message from the user
            var message = new InquiryMessage
            {
                InquiryId = id,
                SenderId = currentUserId,
                SenderType = "User",
                Message = replyMessage,
                CreatedAt = DateTime.Now,
                IsRead = false // Not read by agent/admin yet
            };
            
            _context.InquiryMessages.Add(message);
            
            // Update inquiry status
            inquiry.Status = "In Progress";
            inquiry.UpdatedAt = DateTime.Now;
            inquiry.ReadByUser = true; // User has read their own message
            
            try
            {
                await _context.SaveChangesAsync();
                Console.WriteLine($"[DEBUG] Successfully saved message");
                TempData["Success"] = "Your reply has been sent successfully.";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DEBUG] Error saving message: {ex.Message}");
                TempData["Error"] = "An error occurred while sending your reply.";
            }
            
            return RedirectToAction(nameof(Details), new { id });
        }
    }
} 