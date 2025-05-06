using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EstateEase.Data;
using EstateEase.Models.Entities;
using EstateEase.Models.ViewModels;
using EstateEase.Services;
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
        private readonly IFileService _fileService;

        public PropertyController(
            ApplicationDbContext context,
            IWebHostEnvironment webHostEnvironment,
            IFileService fileService)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _fileService = fileService;
        }

        public IActionResult Add()
        {
            return View();
        }

        public IActionResult View(int id)
        {
            var property = _context.Properties
                .Select(p => new PropertyViewModel
                {
                    Id = p.Id,
                    Title = p.Title,
                    Content = p.Content,
                    PropertyType = p.PropertyType,
                    Bedrooms = p.Bedrooms,
                    Bathrooms = p.Bathrooms,
                    Balcony = p.Balcony,
                    Kitchen = p.Kitchen,
                    Hall = p.Hall,
                    TotalFloors = p.TotalFloors,
                    Size = p.Size,
                    Price = p.Price,
                    Address = p.Address,
                    Status = p.Status,
                    IsFeatured = p.IsFeatured,
                    SellingType = p.SellingType,
                    HasSwimmingPool = p.HasSwimmingPool,
                    HasParking = p.HasParking,
                    HasGym = p.HasGym,
                    HasSecurity = p.HasSecurity,
                    HasElevator = p.HasElevator,
                    HasCCTV = p.HasCCTV,
                    PropertyImagePaths = p.PropertyImagePaths,
                    FloorPlanImagePath = p.FloorPlanImagePath,
                    BasementPlanImagePath = p.BasementPlanImagePath,
                    GroundFloorPlanImagePath = p.GroundFloorPlanImagePath,
                })
                .FirstOrDefault(p => p.Id == id);

            if (property == null)
            {
                return NotFound();
            }

            return View(property);
        }
        public IActionResult ViewProperty()
        {
            var properties = _context.Properties
                .Select(p => new PropertyViewModel
                {
                    Id = p.Id,
                    Title = p.Title,
                    Content = p.Content,
                    PropertyType = p.PropertyType,
                    Bedrooms = p.Bedrooms,
                    Bathrooms = p.Bathrooms,
                    Balcony = p.Balcony,
                    Kitchen = p.Kitchen,
                    Hall = p.Hall,
                    TotalFloors = p.TotalFloors,
                    Size = p.Size,
                    Price = p.Price,
                    Address = p.Address,
                    Status = p.Status,
                    IsFeatured = p.IsFeatured,
                    SellingType = p.SellingType,
                    HasSwimmingPool = p.HasSwimmingPool,
                    HasParking = p.HasParking,
                    HasGym = p.HasGym,
                    HasSecurity = p.HasSecurity,
                    HasElevator = p.HasElevator,
                    HasCCTV = p.HasCCTV
                })
                .ToList();
            return View(properties);
        }

        [HttpGet]
        public IActionResult GetProperties(int draw, int start, int length, string search)
        {
            try
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
            catch (Exception ex)
            {
                return Json(new
                {
                    draw = draw,
                    recordsTotal = 0,
                    recordsFiltered = 0,
                    data = new object[] { },
                    error = $"Error retrieving properties: {ex.Message}"
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var property = await _context.Properties.FindAsync(id);
                if (property == null)
                {
                    return Json(new { success = false, message = "Property not found" });
                }

                // Delete all associated images
                var imagePaths = new List<string>();

                // Property images
                if (!string.IsNullOrEmpty(property.PropertyImagePaths))
                {
                    imagePaths.AddRange(property.PropertyImagePaths.Split(','));
                }

                // Floor plan images
                if (!string.IsNullOrEmpty(property.FloorPlanImagePath))
                {
                    imagePaths.Add(property.FloorPlanImagePath);
                }
                if (!string.IsNullOrEmpty(property.BasementPlanImagePath))
                {
                    imagePaths.Add(property.BasementPlanImagePath);
                }
                if (!string.IsNullOrEmpty(property.GroundFloorPlanImagePath))
                {
                    imagePaths.Add(property.GroundFloorPlanImagePath);
                }

                // Delete all image files
                foreach (var imagePath in imagePaths)
                {
                    var fullPath = Path.Combine(_webHostEnvironment.WebRootPath, imagePath);
                    if (System.IO.File.Exists(fullPath))
                    {
                        System.IO.File.Delete(fullPath);
                    }
                }

                _context.Properties.Remove(property);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Property deleted successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error deleting property: {ex.Message}" });
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

                        Bedrooms = model.Bedrooms,
                        Bathrooms = model.Bathrooms,
                        Balcony = model.Balcony,
                        Kitchen = model.Kitchen,
                        Hall = model.Hall,
                        Size = model.Size,
                        Price = model.Price,
                        Address = model.Address,
                        Status = model.Status,
                        TotalFloors = model.TotalFloors,
                        IsFeatured = model.IsFeatured,
                        SellingType = model.SellingType,
                        HasSwimmingPool = model.HasSwimmingPool,
                        HasParking = model.HasParking,
                        HasGym = model.HasGym,
                        HasSecurity = model.HasSecurity,
                        HasElevator = model.HasElevator,
                        HasCCTV = model.HasCCTV
                    };

                    // Handle property images
                    if (model.PropertyImages != null && model.PropertyImages.Count > 0)
                    {
                        var propertyImagePaths = new List<string>();
                        foreach (var image in model.PropertyImages)
                        {
                            var imagePath = await SaveImage(image, "property-images");
                            propertyImagePaths.Add(imagePath);
                        }
                        property.PropertyImagePaths = string.Join(",", propertyImagePaths);
                    }

                    // Handle floor plan image
                    if (model.FloorPlanImage != null)
                    {
                        property.FloorPlanImagePath = await SaveImage(model.FloorPlanImage, "floor-plans");
                    }

                    // Handle basement plan image
                    if (model.BasementPlanImage != null)
                    {
                        property.BasementPlanImagePath = await SaveImage(model.BasementPlanImage, "basement-plans");
                    }

                    // Handle ground floor plan image
                    if (model.GroundFloorPlanImage != null)
                    {
                        property.GroundFloorPlanImagePath = await SaveImage(model.GroundFloorPlanImage, "ground-floor-plans");
                    }

                    _context.Properties.Add(property);
                    await _context.SaveChangesAsync();

                    // Add detailed logging
                    Console.WriteLine($"Property {property.Id} saved successfully");
                    TempData["Success"] = $"Property '{property.Title}' added successfully!";
                    return RedirectToAction("ViewProperty");
                }

                // Log validation errors
                var errors = ModelState.Values.SelectMany(v => v.Errors);
                Console.WriteLine($"Validation errors: {string.Join(", ", errors.Select(e => e.ErrorMessage))}");
                return View(model);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding property: {ex}");
                TempData["Error"] = $"Error adding property: {ex.Message}";
                return View(model);
            }
        }

        private async Task<string> SaveImage(IFormFile image, string folderName)
        {
            var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", folderName);
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(image.FileName);
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await image.CopyToAsync(fileStream);
            }

            return Path.Combine("uploads", folderName, uniqueFileName).Replace("\\", "/");
        }

        public async Task<IActionResult> Details(int id)
        {
            var property = await _context.Properties.FindAsync(id);
            if (property == null)
            {
                return NotFound();
            }

            return View(property);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var property = await _context.Properties.FindAsync(id);
            if (property == null)
            {
                return NotFound();
            }

            var viewModel = new PropertyViewModel
            {
                Id = property.Id,
                Title = property.Title,
                Content = property.Content,
                PropertyType = property.PropertyType,
                Bedrooms = property.Bedrooms,
                Bathrooms = property.Bathrooms,
                Balcony = property.Balcony,
                Kitchen = property.Kitchen,
                Hall = property.Hall,
                TotalFloors = property.TotalFloors,
                Size = property.Size,
                Price = property.Price,
                Address = property.Address,
                Status = property.Status,
                IsFeatured = property.IsFeatured,
                SellingType = property.SellingType,
                HasSwimmingPool = property.HasSwimmingPool,
                HasParking = property.HasParking,
                HasGym = property.HasGym,
                HasSecurity = property.HasSecurity,
                HasElevator = property.HasElevator,
                HasCCTV = property.HasCCTV
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, PropertyViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var property = await _context.Properties.FindAsync(id);
                    if (property == null)
                    {
                        return NotFound();
                    }

                    // Update basic properties
                    property.Title = model.Title;
                    property.Content = model.Content;
                    property.PropertyType = model.PropertyType;
                    property.Bedrooms = model.Bedrooms;
                    property.Bathrooms = model.Bathrooms;
                    property.Balcony = model.Balcony;
                    property.Kitchen = model.Kitchen;
                    property.Hall = model.Hall;
                    property.TotalFloors = model.TotalFloors;
                    property.Size = model.Size;
                    property.Price = model.Price;
                    property.Address = model.Address;
                    property.Status = model.Status;
                    property.IsFeatured = model.IsFeatured;
                    property.SellingType = model.SellingType;
                    property.HasSwimmingPool = model.HasSwimmingPool;
                    property.HasParking = model.HasParking;
                    property.HasGym = model.HasGym;
                    property.HasSecurity = model.HasSecurity;
                    property.HasElevator = model.HasElevator;
                    property.HasCCTV = model.HasCCTV;
                    property.UpdatedAt = DateTime.Now;

                    // Handle property images - preserve existing if no new images uploaded
                    if (model.PropertyImages != null && model.PropertyImages.Count > 0)
                    {
                        // Delete existing property images if they exist
                        if (!string.IsNullOrEmpty(property.PropertyImagePaths))
                        {
                            var oldImagePaths = property.PropertyImagePaths.Split(',');
                            foreach (var imagePath in oldImagePaths)
                            {
                                var fullPath = Path.Combine(_webHostEnvironment.WebRootPath, imagePath);
                                if (System.IO.File.Exists(fullPath))
                                {
                                    System.IO.File.Delete(fullPath);
                                }
                            }
                        }

                        // Save new property images
                        var propertyImagePaths = new List<string>();
                        foreach (var image in model.PropertyImages)
                        {
                            if (image.Length > 0)
                            {
                                var imagePath = await SaveImage(image, "property-images");
                                propertyImagePaths.Add(imagePath);
                            }
                        }
                        property.PropertyImagePaths = string.Join(",", propertyImagePaths);
                    }

                    // Handle floor plan image - preserve existing if no new image uploaded
                    if (model.FloorPlanImage != null && model.FloorPlanImage.Length > 0)
                    {
                        // Delete existing floor plan image if it exists
                        if (!string.IsNullOrEmpty(property.FloorPlanImagePath))
                        {
                            var fullPath = Path.Combine(_webHostEnvironment.WebRootPath, property.FloorPlanImagePath);
                            if (System.IO.File.Exists(fullPath))
                            {
                                System.IO.File.Delete(fullPath);
                            }
                        }
                        property.FloorPlanImagePath = await SaveImage(model.FloorPlanImage, "floor-plans");
                    }

                    // Handle basement plan image - preserve existing if no new image uploaded
                    if (model.BasementPlanImage != null && model.BasementPlanImage.Length > 0)
                    {
                        // Delete existing basement plan image if it exists
                        if (!string.IsNullOrEmpty(property.BasementPlanImagePath))
                        {
                            var fullPath = Path.Combine(_webHostEnvironment.WebRootPath, property.BasementPlanImagePath);
                            if (System.IO.File.Exists(fullPath))
                            {
                                System.IO.File.Delete(fullPath);
                            }
                        }
                        property.BasementPlanImagePath = await SaveImage(model.BasementPlanImage, "basement-plans");
                    }

                    // Handle ground floor plan image - preserve existing if no new image uploaded
                    if (model.GroundFloorPlanImage != null && model.GroundFloorPlanImage.Length > 0)
                    {
                        // Delete existing ground floor plan image if it exists
                        if (!string.IsNullOrEmpty(property.GroundFloorPlanImagePath))
                        {
                            var fullPath = Path.Combine(_webHostEnvironment.WebRootPath, property.GroundFloorPlanImagePath);
                            if (System.IO.File.Exists(fullPath))
                            {
                                System.IO.File.Delete(fullPath);
                            }
                        }
                        property.GroundFloorPlanImagePath = await SaveImage(model.GroundFloorPlanImage, "ground-floor-plans");
                    }

                    _context.Update(property);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Property updated successfully!";
                    return RedirectToAction(nameof(ViewProperty));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PropertyExists(id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            // If we got this far, something failed, redisplay form
            TempData["Error"] = "Please check the form for errors.";
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteImage(int propertyId, string imagePath)
        {
            try
            {
                var property = await _context.Properties.FindAsync(propertyId);
                if (property == null)
                {
                    return Json(new { success = false, message = "Property not found" });
                }

                // Delete the physical file
                var fullPath = Path.Combine(_webHostEnvironment.WebRootPath, imagePath);
                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                }

                // Update the database
                if (property.PropertyImagePaths?.Contains(imagePath) == true)
                {
                    var paths = property.PropertyImagePaths.Split(',').ToList();
                    paths.Remove(imagePath);
                    property.PropertyImagePaths = string.Join(",", paths);
                }
                else if (property.FloorPlanImagePath == imagePath)
                {
                    property.FloorPlanImagePath = null;
                }
                else if (property.BasementPlanImagePath == imagePath)
                {
                    property.BasementPlanImagePath = null;
                }
                else if (property.GroundFloorPlanImagePath == imagePath)
                {
                    property.GroundFloorPlanImagePath = null;
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        private bool PropertyExists(int id)
        {
            return _context.Properties.Any(e => e.Id == id);
        }
    }
}



