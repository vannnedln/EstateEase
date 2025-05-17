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
using Microsoft.Extensions.Logging;

namespace EstateEase.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class InquiryController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<InquiryController> _logger;

        public InquiryController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            ILogger<InquiryController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<int> GetUnreadInquiriesCount()
        {
            var currentUserId = _userManager.GetUserId(User);
            _logger.LogInformation($"Getting unread inquiries count for admin with UserId: {currentUserId}");
            
            var count = await _context.Inquiries
                .Include(i => i.Property)
                .Where(i => (i.AgentId == currentUserId || i.AgentId == null || 
                          i.Property.AgentId == currentUserId || i.Property.AgentId == null) && 
                          i.Status == "New")
                .CountAsync();
                
            _logger.LogInformation($"Found {count} unread inquiries for admin with UserId: {currentUserId}");
            return count;
        }

        public async Task<IActionResult> Index(string propertyId = null)
        {
            var currentUserId = _userManager.GetUserId(User);
            _logger.LogInformation($"Looking up inquiries for Admin with UserId: {currentUserId}");
            
            // Build query for inquiries, including those with null AgentId
            var query = _context.Inquiries
                .Include(i => i.User)
                .Include(i => i.Property)
                .Where(i => i.AgentId == currentUserId || i.AgentId == null || 
                            i.Property.AgentId == currentUserId || i.Property.AgentId == null);
            
            // Apply property filter if provided
            if (!string.IsNullOrEmpty(propertyId))
            {
                query = query.Where(i => i.PropertyId == propertyId);
                _logger.LogInformation($"Filtering inquiries by PropertyId: {propertyId}");
            }
            
            // For debugging, check how many inquiries exist in total
            var totalInquiries = await _context.Inquiries.CountAsync();
            _logger.LogInformation($"Total inquiries in database: {totalInquiries}");
            
            // Get inquiries for admin-listed properties
            var inquiries = await query
                .OrderByDescending(i => i.CreatedAt)
                .Select(i => new InquiryViewModel
                {
                    Id = i.Id,
                    ClientName = i.User.UserName,
                    ClientEmail = i.User.Email,
                    PropertyId = i.PropertyId,
                    PropertyTitle = i.Property != null ? i.Property.Title : null,
                    PropertyAddress = i.Property != null ? i.Property.Address : null,
                    Subject = i.Subject,
                    Message = i.Message,
                    Status = i.Status,
                    CreatedAt = i.CreatedAt,
                    UpdatedAt = i.UpdatedAt
                })
                .ToListAsync();
                
            _logger.LogInformation($"Found {inquiries.Count} inquiries for admin with ID: {currentUserId}");
            
            // For debugging, log all inquiries with their properties to verify the relationships
            var debugInquiries = await _context.Inquiries
                .Include(i => i.Property)
                .ToListAsync();
                
            foreach (var inquiry in debugInquiries)
            {
                _logger.LogInformation($"Inquiry ID: {inquiry.Id}, AgentId: {inquiry.AgentId}, " +
                    $"PropertyId: {inquiry.PropertyId}, Property.AgentId: {inquiry.Property?.AgentId}");
            }

            // Get properties for filter dropdown
            var properties = await _context.Properties
                .Where(p => p.AgentId == currentUserId)
                .OrderBy(p => p.Title)
                .Select(p => new { Id = p.Id, Title = p.Title })
                .ToListAsync();
            
            ViewBag.Properties = properties;
            
            // Set the unread count in ViewBag for the layout to use
            ViewBag.UnreadInquiriesCount = inquiries.Count(i => i.Status == "New");
            
            // If property filter was applied, add to ViewBag for UI
            if (!string.IsNullOrEmpty(propertyId))
            {
                var property = await _context.Properties
                    .Where(p => p.Id == propertyId)
                    .FirstOrDefaultAsync();
                    
                if (property != null)
                {
                    ViewBag.FilteredProperty = property.Title;
                    ViewBag.SelectedPropertyId = propertyId;
                }
            }

            return View(inquiries);
        }

        public async Task<IActionResult> Details(int id)
        {
            var currentUserId = _userManager.GetUserId(User);
            
            // Get the inquiry with details, including those with null AgentId
            var inquiry = await _context.Inquiries
                .Include(i => i.User)
                .Include(i => i.Property)
                .Include(i => i.Messages)
                .Where(i => (i.AgentId == currentUserId || i.AgentId == null || 
                          i.Property.AgentId == currentUserId || i.Property.AgentId == null) && 
                          i.Id == id)
                .Select(i => new InquiryViewModel
                {
                    Id = i.Id,
                    ClientName = i.User.UserName,
                    ClientEmail = i.User.Email,
                    PropertyId = i.PropertyId,
                    PropertyTitle = i.Property != null ? i.Property.Title : null,
                    PropertyAddress = i.Property != null ? i.Property.Address : null,
                    Subject = i.Subject,
                    Message = i.Message,
                    ReplyMessage = i.ReplyMessage,
                    Status = i.Status,
                    CreatedAt = i.CreatedAt,
                    UpdatedAt = i.UpdatedAt,
                    Messages = i.Messages.Select(m => new InquiryMessageViewModel
                    {
                        Id = m.Id,
                        InquiryId = m.InquiryId,
                        SenderId = m.SenderId,
                        SenderType = m.SenderType,
                        SenderName = m.SenderType == "Admin" ? "You" : m.SenderType,
                        Message = m.Message,
                        IsRead = m.IsRead,
                        CreatedAt = m.CreatedAt,
                        IsFromCurrentUser = m.SenderType == "Admin"
                    }).ToList()
                })
                .FirstOrDefaultAsync();

            if (inquiry == null)
            {
                return NotFound();
            }

            // Mark as read if status is "New"
            if (inquiry.Status == "New")
            {
                var dbInquiry = await _context.Inquiries.FindAsync(id);
                if (dbInquiry != null)
                {
                    dbInquiry.Status = "In Progress";
                    dbInquiry.ReadAt = DateTime.Now;
                    dbInquiry.UpdatedAt = DateTime.Now;
                    await _context.SaveChangesAsync();
                    
                    // Update view model
                    inquiry.Status = "In Progress";
                    inquiry.UpdatedAt = DateTime.Now;
                }
            }

            return View(inquiry);
        }

        public async Task<IActionResult> Reply(int id)
        {
            var currentUserId = _userManager.GetUserId(User);
            
            // Get the inquiry, including those with null AgentId
            var inquiry = await _context.Inquiries
                .Include(i => i.User)
                .Include(i => i.Property)
                .Where(i => (i.AgentId == currentUserId || i.AgentId == null || 
                           i.Property.AgentId == currentUserId || i.Property.AgentId == null) && 
                           i.Id == id)
                .FirstOrDefaultAsync();

            if (inquiry == null)
            {
                return NotFound();
            }

            // Create reply view model
            var model = new InquiryReplyViewModel
            {
                InquiryId = inquiry.Id,
                InquirySubject = inquiry.Subject,
                ClientName = inquiry.User.UserName,
                ClientEmail = inquiry.User.Email
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reply(int id, InquiryReplyViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var currentUserId = _userManager.GetUserId(User);
            
            // Get the inquiry, including those with null AgentId
            var inquiry = await _context.Inquiries
                .Include(i => i.Property)
                .Where(i => (i.AgentId == currentUserId || i.AgentId == null || 
                           i.Property.AgentId == currentUserId || i.Property.AgentId == null) && 
                           i.Id == id)
                .FirstOrDefaultAsync();

            if (inquiry == null)
            {
                return NotFound();
            }

            // Update inquiry status to In Progress when replying so user can reply back
            inquiry.Status = "In Progress";
            inquiry.UpdatedAt = DateTime.Now;
            
            // Save the reply message in the ReplyMessage field
            inquiry.ReplyMessage = model.ReplyMessage;
            
            // Set ReadByUser to false so the user can see the reply
            inquiry.ReadByUser = false;
            
            await _context.SaveChangesAsync();

            // TODO: Send email reply to client
            // This would typically use an email service

            TempData["Success"] = "Reply sent successfully";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsResolved(int id)
        {
            var currentUserId = _userManager.GetUserId(User);
            
            // Get the inquiry, including those with null AgentId
            var inquiry = await _context.Inquiries
                .Include(i => i.Property)
                .Where(i => (i.AgentId == currentUserId || i.AgentId == null || 
                           i.Property.AgentId == currentUserId || i.Property.AgentId == null) && 
                           i.Id == id)
                .FirstOrDefaultAsync();

            if (inquiry == null)
            {
                return NotFound();
            }

            // Update status
            inquiry.Status = "Resolved";
            inquiry.UpdatedAt = DateTime.Now;
            
            await _context.SaveChangesAsync();

            TempData["Success"] = "Inquiry marked as resolved";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllAsRead(string propertyId = null)
        {
            var currentUserId = _userManager.GetUserId(User);
            
            // Build query for new inquiries, including those with null AgentId
            var query = _context.Inquiries
                .Include(i => i.Property)
                .Where(i => (i.AgentId == currentUserId || i.AgentId == null || 
                           i.Property.AgentId == currentUserId || i.Property.AgentId == null) && 
                           i.Status == "New");
            
            // If propertyId is provided, filter by property
            if (!string.IsNullOrEmpty(propertyId))
            {
                query = query.Where(i => i.PropertyId == propertyId);
            }
            
            // Get all new inquiries
            var inquiries = await query.ToListAsync();
            
            // Mark all as "In Progress"
            foreach (var inquiry in inquiries)
            {
                inquiry.Status = "In Progress";
                inquiry.ReadAt = DateTime.Now;
                inquiry.UpdatedAt = DateTime.Now;
            }
            
            await _context.SaveChangesAsync();
            
            // Set success message
            TempData["Success"] = "All inquiries marked as read";
            
            // Return to the same view
            return RedirectToAction(nameof(Index), new { propertyId = propertyId });
        }

        [HttpGet]
        [Route("/api/admin/inquiries/count")]
        public async Task<IActionResult> GetInquiryCounts()
        {
            var currentUserId = _userManager.GetUserId(User);
            
            var totalCount = await _context.Inquiries
                .Include(i => i.Property)
                .Where(i => i.AgentId == currentUserId || i.AgentId == null || 
                         i.Property.AgentId == currentUserId || i.Property.AgentId == null)
                .CountAsync();
                
            var newCount = await _context.Inquiries
                .Include(i => i.Property)
                .Where(i => (i.AgentId == currentUserId || i.AgentId == null || 
                           i.Property.AgentId == currentUserId || i.Property.AgentId == null) && 
                           i.Status == "New")
                .CountAsync();
                
            var inProgressCount = await _context.Inquiries
                .Include(i => i.Property)
                .Where(i => (i.AgentId == currentUserId || i.AgentId == null || 
                           i.Property.AgentId == currentUserId || i.Property.AgentId == null) && 
                           i.Status == "In Progress")
                .CountAsync();
                
            var resolvedCount = await _context.Inquiries
                .Include(i => i.Property)
                .Where(i => (i.AgentId == currentUserId || i.AgentId == null || 
                           i.Property.AgentId == currentUserId || i.Property.AgentId == null) && 
                           i.Status == "Resolved")
                .CountAsync();
                
            return Json(new {
                total = totalCount,
                new_count = newCount,
                in_progress = inProgressCount,
                resolved = resolvedCount
            });
        }

        [HttpGet]
        [Route("admin-properties")]
        public async Task<IActionResult> AdminPropertyInquiries()
        {
            var currentUserId = _userManager.GetUserId(User);
            _logger.LogInformation($"Looking up admin property inquiries for Admin with UserId: {currentUserId}");
            
            // First get all inquiries with the [Admin Property] prefix
            var adminPrefixQuery = _context.Inquiries
                .Include(i => i.User)
                .Include(i => i.Property)
                .Where(i => i.Subject.StartsWith("[Admin Property]"));
                
            var adminPrefixInquiries = await adminPrefixQuery.ToListAsync();
            _logger.LogInformation($"Found {adminPrefixInquiries.Count} inquiries with [Admin Property] prefix");
            
            // Get admin properties - consider properties with AgentId == null as admin-listed
            var adminProperties = await _context.Properties
                .Where(p => p.AgentId == null || p.AgentId == currentUserId)
                .Select(p => p.Id)
                .ToListAsync();
                
            _logger.LogInformation($"Found {adminProperties.Count} properties owned by this admin");
            
            var adminPropertyInquiries = await _context.Inquiries
                .Include(i => i.User)
                .Include(i => i.Property)
                .Where(i => adminProperties.Contains(i.PropertyId))
                .ToListAsync();
                
            _logger.LogInformation($"Found {adminPropertyInquiries.Count} inquiries for properties owned by this admin");
            
            // Combine the two lists and remove duplicates
            var combinedInquiries = adminPrefixInquiries
                .Union(adminPropertyInquiries)
                .DistinctBy(i => i.Id)
                .OrderByDescending(i => i.CreatedAt)
                .ToList();
                
            _logger.LogInformation($"Combined list has {combinedInquiries.Count} inquiries");
            
            // Map to view models
            var inquiryViewModels = combinedInquiries
                .Select(i => new InquiryViewModel
                {
                    Id = i.Id,
                    ClientName = i.User?.UserName ?? "Unknown",
                    ClientEmail = i.User?.Email ?? "Unknown", 
                    PropertyId = i.PropertyId,
                    PropertyTitle = i.Property?.Title ?? "Unknown Property",
                    PropertyAddress = i.Property?.Address ?? "Unknown Address",
                    Subject = i.Subject,
                    Message = i.Message,
                    Status = i.Status,
                    CreatedAt = i.CreatedAt,
                    UpdatedAt = i.UpdatedAt
                })
                .ToList();
                
            // Get properties for filter dropdown
            var properties = await _context.Properties
                .Where(p => p.AgentId == currentUserId)
                .OrderBy(p => p.Title)
                .Select(p => new { Id = p.Id, Title = p.Title })
                .ToListAsync();
                
            ViewBag.Properties = properties;
            ViewBag.UnreadInquiriesCount = inquiryViewModels.Count(i => i.Status == "New");
            ViewBag.Title = "Admin Property Inquiries";
            
            return View("Index", inquiryViewModels);
        }

        // Quick reply from details page
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuickReply(int id, string replyMessage)
        {
            if (string.IsNullOrEmpty(replyMessage))
            {
                TempData["Error"] = "Reply message cannot be empty.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var currentUserId = _userManager.GetUserId(User);
            
            // Get the inquiry, including those with null AgentId
            var inquiry = await _context.Inquiries
                .Where(i => (i.AgentId == currentUserId || i.AgentId == null || 
                           i.Property.AgentId == currentUserId || i.Property.AgentId == null) && 
                           i.Id == id)
                .FirstOrDefaultAsync();

            if (inquiry == null)
            {
                return NotFound();
            }

            // Create a new message for the chat
            var message = new InquiryMessage
            {
                InquiryId = id,
                SenderId = currentUserId,
                SenderType = "Admin",
                Message = replyMessage,
                CreatedAt = DateTime.Now,
                IsRead = false // Not read by user yet
            };
            
            _context.InquiryMessages.Add(message);

            // Update inquiry status to In Progress when replying so user can reply back
            inquiry.Status = "In Progress";
            inquiry.UpdatedAt = DateTime.Now;
            
            // For backward compatibility, also save to ReplyMessage field
            inquiry.ReplyMessage = replyMessage;
            
            // Set ReadByUser to false so the user can see the reply
            inquiry.ReadByUser = false;
            
            await _context.SaveChangesAsync();

          
            return RedirectToAction(nameof(Details), new { id });
        }

        // Delete an inquiry
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteInquiry(int id)
        {
            var currentUserId = _userManager.GetUserId(User);
            
            // Get the inquiry, including those with null AgentId
            var inquiry = await _context.Inquiries
                .Include(i => i.Messages)
                .Where(i => (i.AgentId == currentUserId || i.AgentId == null || 
                           i.Property.AgentId == currentUserId || i.Property.AgentId == null) && 
                           i.Id == id)
                .FirstOrDefaultAsync();

            if (inquiry == null)
            {
                return NotFound();
            }

            // First remove all messages associated with this inquiry
            if (inquiry.Messages != null && inquiry.Messages.Any())
            {
                _context.InquiryMessages.RemoveRange(inquiry.Messages);
            }

            // Remove the inquiry
            _context.Inquiries.Remove(inquiry);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Inquiry deleted successfully";
            return RedirectToAction(nameof(Index));
        }
    }
} 