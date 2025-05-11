using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EstateEase.Data;
using EstateEase.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace EstateEase.Areas.User.Controllers
{
    [Area("User")]
    [Authorize(Roles = "User")]
    public class AppointmentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public AppointmentsController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(string statusFilter = "All")
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var query = _context.Appointments
                .Include(a => a.Property)
                .ThenInclude(p => p.PropertyImages)
                .Include(a => a.Agent)
                .Where(a => a.UserId == userId);

            // Apply filter if needed
            if (statusFilter != "All")
            {
                query = query.Where(a => a.Status == statusFilter);
            }

            var appointments = await query
                .OrderByDescending(a => a.AppointmentDate)
                .Select(a => new UserAppointmentViewModel
                {
                    Id = a.Id,
                    PropertyId = a.PropertyId,
                    PropertyTitle = a.Property.Title,
                    PropertyAddress = a.Property.Address,
                    PropertyImage = a.Property.PropertyImages.Any()
                        ? (a.Property.PropertyImages.FirstOrDefault() != null 
                            ? a.Property.PropertyImages.FirstOrDefault().ImagePath 
                            : "/uploads/properties/placeholder.jpg")
                        : "/uploads/properties/placeholder.jpg",
                    AgentId = a.AgentId,
                    AgentName = a.Agent.FirstName + " " + a.Agent.LastName,
                    AgentEmail = a.Agent.Email,
                    AgentPhone = a.Agent.PhoneNumber,
                    AppointmentDate = a.AppointmentDate,
                    Status = a.Status,
                    Notes = a.Notes,
                    CreatedAt = a.CreatedAt
                })
                .ToListAsync();

            var viewModel = new AppointmentsViewModel
            {
                Appointments = appointments,
                StatusFilter = statusFilter,
                TotalCount = appointments.Count(),
                UpcomingCount = appointments.Count(a => a.Status == "Confirmed" && a.AppointmentDate > DateTime.Now),
                PendingCount = appointments.Count(a => a.Status == "Pending"),
                CompletedCount = appointments.Count(a => a.Status == "Completed"),
                CancelledCount = appointments.Count(a => a.Status == "Cancelled")
            };

            return View(viewModel);
        }

        public async Task<IActionResult> Details(string id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var appointment = await _context.Appointments
                .Include(a => a.Property)
                .ThenInclude(p => p.PropertyImages)
                .Include(a => a.Agent)
                .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

            if (appointment == null)
            {
                return NotFound();
            }

            var viewModel = new UserAppointmentViewModel
            {
                Id = appointment.Id,
                PropertyId = appointment.PropertyId,
                PropertyTitle = appointment.Property.Title,
                PropertyAddress = appointment.Property.Address,
                PropertyImage = appointment.Property.PropertyImages.Any()
                    ? (appointment.Property.PropertyImages.FirstOrDefault() != null 
                        ? appointment.Property.PropertyImages.FirstOrDefault().ImagePath 
                        : "/uploads/properties/placeholder.jpg")
                    : "/uploads/properties/placeholder.jpg",
                PropertyPrice = appointment.Property.Price,
                PropertyType = appointment.Property.PropertyType,
                PropertyBedrooms = appointment.Property.Bedrooms,
                PropertyBathrooms = appointment.Property.Bathrooms,
                PropertySize = appointment.Property.Size,
                AgentId = appointment.AgentId,
                AgentName = appointment.Agent.FirstName + " " + appointment.Agent.LastName,
                AgentEmail = appointment.Agent.Email,
                AgentPhone = appointment.Agent.PhoneNumber,
                AppointmentDate = appointment.AppointmentDate,
                Status = appointment.Status,
                Notes = appointment.Notes,
                CreatedAt = appointment.CreatedAt
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(string id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var appointment = await _context.Appointments
                .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

            if (appointment == null)
            {
                return NotFound();
            }

            // Only allow cancellation if the appointment is not already cancelled or completed
            if (appointment.Status != "Cancelled" && appointment.Status != "Completed")
            {
                appointment.Status = "Cancelled";
                appointment.UpdatedAt = DateTime.Now;

                _context.Update(appointment);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Appointment cancelled successfully.";
            }
            else
            {
                TempData["Error"] = "This appointment cannot be cancelled.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
} 