using EstateEase.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace EstateEase.Services
{
    public class AdminInquiryUpdateService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<AdminInquiryUpdateService> _logger;

        public AdminInquiryUpdateService(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            ILogger<AdminInquiryUpdateService> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task UpdateAdminInquiries()
        {
            try
            {
                _logger.LogInformation("Starting update of inquiries for admin-listed properties");
                
                // Find inquiries for properties listed by admins
                var inquiriesForAdminProperties = await _context.Inquiries
                    .Include(i => i.Property)
                    .Where(i => i.Property.AgentId != null && !i.Subject.StartsWith("[Admin Property]"))
                    .ToListAsync();
                
                _logger.LogInformation($"Found {inquiriesForAdminProperties.Count} inquiries to check for admin properties");
                
                int updatedCount = 0;
                foreach (var inquiry in inquiriesForAdminProperties)
                {
                    if (inquiry.Property?.AgentId != null)
                    {
                        var propertyOwner = await _userManager.FindByIdAsync(inquiry.Property.AgentId);
                        if (propertyOwner != null && await _userManager.IsInRoleAsync(propertyOwner, "Admin"))
                        {
                            // Mark this as an admin property inquiry
                            inquiry.Subject = "[Admin Property] " + inquiry.Subject;
                            updatedCount++;
                        }
                    }
                }
                
                if (updatedCount > 0)
                {
                    _logger.LogInformation($"Updated {updatedCount} inquiries for admin-listed properties");
                    await _context.SaveChangesAsync();
                }
                else
                {
                    _logger.LogInformation("No inquiries needed to be updated");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating admin inquiries");
            }
        }
    }
} 