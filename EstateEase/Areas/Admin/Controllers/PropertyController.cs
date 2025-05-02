using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EstateEase.Data;
using EstateEase.Models.Entities;
using EstateEase.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EstateEase.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class PropertyController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public PropertyController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        public IActionResult Add()
        {
            return View();
        }

        public IActionResult ViewProperty()
        {
            return View();
        }

        [HttpGet]
        public IActionResult GetProperties(int draw, int start, int length, string search)
        {
            var query = _context.Properties.AsQueryable();

            // Apply search
            if (!string.IsNullOrEmpty(search))
            {
                var searchValue = search.ToLower();
                query = query.Where(p =>
                    p.Title.ToLower().Contains(searchValue) ||
                    p.PropertyType.ToLower().Contains(searchValue) ||
                    p.Address.ToLower().Contains(searchValue) ||
                    p.Status.ToLower().Contains(searchValue)
                );
            }

            var totalRecords = query.Count();

            // Apply sorting and pagination
            var data = query
                .Skip(start)
                .Take(length)
                .Select(p => new
                {
                    p.Id,
                    p.Title,
                    p.PropertyType,
                    p.BHK,
                    p.SellingType,
                    p.Size,
                    p.Price,
                    p.Address,
                    p.Status,
                    p.CreatedAt
                })
                .ToList();

            return Json(new
            {
                draw = draw,
                recordsTotal = totalRecords,
                recordsFiltered = totalRecords,
                data = data
            });
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var property = await _context.Properties.FindAsync(id);
                if (property == null)
                {
                    return Json(new { success = false });
                }

                _context.Properties.Remove(property);
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch
            {
                return Json(new { success = false });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(PropertyViewModel model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var property = new AddProperty
                    {
                        Title = model.Title,
                        Content = model.Content,
                        PropertyType = model.PropertyType,
                        BHK = model.BHK,
                        Bedrooms = model.Bedrooms,
                        Bathrooms = model.Bathrooms,
                        Balcony = model.Balcony,
                        Kitchen = model.Kitchen,
                        Hall = model.Hall,
                        TotalFloors = model.TotalFloors,
                        Size = model.Size,
                        Price = model.Price,
                        Address = model.Address,
                        Status = model.Status,
                        IsFeatured = model.IsFeatured,
                        SellingType = model.SellingType,
                        HasSwimmingPool = model.HasSwimmingPool,
                        HasParking = model.HasParking,
                        HasGym = model.HasGym,
                        HasSecurity = model.HasSecurity,
                        HasElevator = model.HasElevator,
                        HasCCTV = model.HasCCTV,
                        CreatedAt = DateTime.Now
                    };


                    try
                    {
                        // Handle MainImage
                        if (model.MainImage != null)
                        {
                            string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "properties", "main");
                            Directory.CreateDirectory(uploadsFolder);
                            string uniqueFileName = Guid.NewGuid().ToString() + "_" + model.MainImage.FileName;
                            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                            using (var fileStream = new FileStream(filePath, FileMode.Create))
                            {
                                await model.MainImage.CopyToAsync(fileStream);
                            }
                            property.MainImagePath = "images/properties/main/" + uniqueFileName;
                        }

                        _context.Properties.Add(property);
                        await _context.SaveChangesAsync();
                        TempData["Success"] = "Property added successfully!";
                        return RedirectToAction("ViewProperty");
                    }
                    catch (Exception ex)
                    {
                        TempData["Error"] = $"Error occurred while adding property: {ex.Message}";
                        return View(model);
                    }
                }
                return View(model);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error occurred while adding property: {ex.Message}";
                return View(model);
            }
        }

        private async Task<string> SaveImage(IFormFile image, string folder)
        {
            // Add file type validation
            var allowedTypes = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var extension = Path.GetExtension(image.FileName).ToLowerInvariant();
            if (!allowedTypes.Contains(extension))
            {
                throw new InvalidOperationException($"File type {extension} is not allowed");
            }

            string uniqueFileName = Guid.NewGuid().ToString() + "_" + image.FileName;
            string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", folder);
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await image.CopyToAsync(fileStream);
            }

            return $"/images/{folder}/{uniqueFileName}";
        }

        public async Task<IActionResult> Details(int id)
        {
            var property = await _context.Properties.FindAsync(id);
            if (property == null)
            {
                return NotFound();
            }

            // Split the additional image paths here
            string[] additionalImages = property.AdditionalImagePaths?.Split('|', StringSplitOptions.RemoveEmptyEntries);
            ViewBag.AdditionalImages = additionalImages;

            return View(property);
        }
    }
}