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

namespace EstateEase.Areas.Agent.Controllers
{
    [Area("Agent")]
    [Authorize(Roles = "Agent")]
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

        public async Task<int> GetUnreadInquiriesCount()
        {
            var currentUserId = _userManager.GetUserId(User);
            
            // Get the agent record to get the agent ID
            var agent = await _context.Agents
                .FirstOrDefaultAsync(a => a.UserId == currentUserId);
                
            var agentId = agent?.Id;
            
            return await _context.Inquiries
                .Where(i => (i.AgentId == agentId || i.AgentId == currentUserId) && i.Status == "New")
                .CountAsync();
        }

        public async Task<IActionResult> Index(string propertyId = null)
        {
            var currentUserId = _userManager.GetUserId(User);
            
            // Get the agent record to get the agent ID
            var agent = await _context.Agents
                .FirstOrDefaultAsync(a => a.UserId == currentUserId);
                
            var agentId = agent?.Id;
            
            // Build query for inquiries
            var query = _context.Inquiries
                .Include(i => i.User)
                .Include(i => i.Property)
                .Where(i => i.AgentId == agentId || i.AgentId == currentUserId);
            
            // Apply property filter if provided
            if (!string.IsNullOrEmpty(propertyId))
            {
                query = query.Where(i => i.PropertyId == propertyId);
            }
            
            // Get inquiries for this agent's properties
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

            // Get properties for filter dropdown
            var properties = await _context.Properties
                .Where(p => p.AgentId == currentUserId || p.AgentId == agentId)
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
            
            // Get the agent record to get the agent ID
            var agent = await _context.Agents
                .FirstOrDefaultAsync(a => a.UserId == currentUserId);
                
            var agentId = agent?.Id;
            
            // Get the inquiry with details
            var inquiry = await _context.Inquiries
                .Include(i => i.User)
                .Include(i => i.Property)
                .Where(i => (i.AgentId == agentId || i.AgentId == currentUserId) && i.Id == id)
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
            
            // Get the agent record to get the agent ID
            var agent = await _context.Agents
                .FirstOrDefaultAsync(a => a.UserId == currentUserId);
                
            var agentId = agent?.Id;
            
            // Get the inquiry
            var inquiry = await _context.Inquiries
                .Include(i => i.User)
                .Where(i => (i.AgentId == agentId || i.AgentId == currentUserId) && i.Id == id)
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
            
            // Get the agent record to get the agent ID
            var agent = await _context.Agents
                .FirstOrDefaultAsync(a => a.UserId == currentUserId);
                
            var agentId = agent?.Id;
            
            // Get the inquiry
            var inquiry = await _context.Inquiries
                .Where(i => (i.AgentId == agentId || i.AgentId == currentUserId) && i.Id == id)
                .FirstOrDefaultAsync();

            if (inquiry == null)
            {
                return NotFound();
            }

            // Update inquiry status
            inquiry.Status = "Resolved";
            inquiry.UpdatedAt = DateTime.Now;
            
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
            
            // Get the agent record to get the agent ID
            var agent = await _context.Agents
                .FirstOrDefaultAsync(a => a.UserId == currentUserId);
                
            var agentId = agent?.Id;
            
            // Get the inquiry
            var inquiry = await _context.Inquiries
                .Where(i => (i.AgentId == agentId || i.AgentId == currentUserId) && i.Id == id)
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
            
            // Get the agent record to get the agent ID
            var agent = await _context.Agents
                .FirstOrDefaultAsync(a => a.UserId == currentUserId);
                
            var agentId = agent?.Id;
            
            // Build query for new inquiries
            var query = _context.Inquiries
                .Where(i => (i.AgentId == agentId || i.AgentId == currentUserId) && i.Status == "New");
            
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
        [Route("/api/agent/inquiries/count")]
        public async Task<IActionResult> GetInquiryCounts()
        {
            var currentUserId = _userManager.GetUserId(User);
            
            // Get the agent record to get the agent ID
            var agent = await _context.Agents
                .FirstOrDefaultAsync(a => a.UserId == currentUserId);
                
            var agentId = agent?.Id;
            
            var totalCount = await _context.Inquiries
                .Where(i => i.AgentId == agentId || i.AgentId == currentUserId)
                .CountAsync();
                
            var newCount = await _context.Inquiries
                .Where(i => (i.AgentId == agentId || i.AgentId == currentUserId) && i.Status == "New")
                .CountAsync();
                
            var inProgressCount = await _context.Inquiries
                .Where(i => (i.AgentId == agentId || i.AgentId == currentUserId) && i.Status == "In Progress")
                .CountAsync();
                
            var resolvedCount = await _context.Inquiries
                .Where(i => (i.AgentId == agentId || i.AgentId == currentUserId) && i.Status == "Resolved")
                .CountAsync();
                
            return Json(new {
                total = totalCount,
                new_count = newCount,
                in_progress = inProgressCount,
                resolved = resolvedCount
            });
        }
    }
} 