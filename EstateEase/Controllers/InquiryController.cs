using EstateEase.Data;
using EstateEase.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace EstateEase.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InquiryController : ControllerBase
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

        [HttpGet("test")]
        public IActionResult Test()
        {
            return Ok(new { success = true, message = "API is working correctly" });
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create([FromBody] InquiryCreateModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                return Unauthorized();
            }

            try
            {
                _logger.LogInformation($"Creating inquiry from user {userId} for property {model.PropertyId}");
                
                // Get the property
                var property = await _context.Properties
                    .Include(p => p.Agent)
                    .FirstOrDefaultAsync(p => p.Id == model.PropertyId);

                if (property == null)
                {
                    _logger.LogWarning($"Property not found: {model.PropertyId}");
                    return NotFound("Property not found");
                }

                _logger.LogInformation($"Found property: {property.Id}, Title: {property.Title}, AgentId: {property.AgentId}");

                // Create a simple inquiry without navigation properties
                var inquiry = new Inquiry
                {
                    UserId = userId,
                    PropertyId = model.PropertyId,
                    Subject = model.Subject,
                    Message = model.Message,
                    Status = "New",
                    CreatedAt = DateTime.Now
                };

                // Only set AgentId if there is an agent
                if (property.AgentId != null)
                {
                    _logger.LogInformation($"Property has AgentId: {property.AgentId}");
                    
                    // Check if AgentId is a User ID (happens with some properties)
                    var agent = await _context.Agents.FirstOrDefaultAsync(a => a.UserId == property.AgentId);
                    if (agent != null)
                    {
                        // If we found an agent record, use its ID 
                        inquiry.AgentId = agent.Id;
                        _logger.LogInformation($"Found Agent record with ID: {agent.Id} for UserId: {property.AgentId}");
                    }
                    else
                    {
                        // Otherwise, use the property's AgentId directly (admin user ID)
                        inquiry.AgentId = property.AgentId;
                        _logger.LogInformation($"No Agent found, using property's AgentId directly: {property.AgentId}");
                        
                        // Check if the property owner is an admin
                        var propertyOwner = await _userManager.FindByIdAsync(property.AgentId);
                        if (propertyOwner != null)
                        {
                            var isAdmin = await _userManager.IsInRoleAsync(propertyOwner, "Admin");
                            _logger.LogInformation($"Property owner is admin: {isAdmin}");
                            
                            if (isAdmin)
                            {
                                // Add a special note in the subject to indicate this is for an admin-listed property
                                inquiry.Subject = "[Admin Property] " + inquiry.Subject;
                                _logger.LogInformation("Marked inquiry as admin property");
                            }
                        }
                    }
                }
                else
                {
                    // If AgentId is null, it's an admin property
                    _logger.LogInformation($"Property {property.Id} has no AgentId set - treating as admin property");
                    
                    // Mark as admin property but don't set AgentId to avoid foreign key constraints
                    // We'll just set inquiry.AgentId = null here
                    inquiry.AgentId = null;
                    
                    // Mark as admin property
                    inquiry.Subject = "[Admin Property] " + inquiry.Subject;
                    _logger.LogInformation("Marked inquiry as admin property with null AgentId");
                }

                _context.Inquiries.Add(inquiry);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Inquiry sent successfully" });
            }
            catch (DbUpdateException dbEx)
            {
                var innerException = dbEx.InnerException != null ? dbEx.InnerException.Message : "No inner exception";
                return StatusCode(500, new { message = $"Database error: {dbEx.Message}, Inner: {innerException}" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"An error occurred: {ex.Message}" });
            }
        }
    }

    public class InquiryCreateModel
    {
        public string PropertyId { get; set; }
        public string Subject { get; set; }
        public string Message { get; set; }
    }
} 