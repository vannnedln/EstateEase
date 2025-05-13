using EstateEase.Data;
using EstateEase.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

        public InquiryController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
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
                // Get the property
                var property = await _context.Properties
                    .Include(p => p.Agent)
                    .FirstOrDefaultAsync(p => p.Id == model.PropertyId);

                if (property == null)
                {
                    return NotFound("Property not found");
                }

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
                    // Check if AgentId is a User ID (happens with some properties)
                    var agent = await _context.Agents.FirstOrDefaultAsync(a => a.UserId == property.AgentId);
                    if (agent != null)
                    {
                        // If we found an agent record, use its ID 
                        inquiry.AgentId = agent.Id;
                    }
                    else
                    {
                        // Otherwise, use the property's AgentId directly
                        inquiry.AgentId = property.AgentId;
                    }
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