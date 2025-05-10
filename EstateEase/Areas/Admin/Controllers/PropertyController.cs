using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EstateEase.Models;
using EstateEase.Models.ViewModels;
using EstateEase.Models.Entities;
using EstateEase.Data;
using System.IO;
using Microsoft.AspNetCore.Hosting;

namespace EstateEase.Controllers.Admin
{
    [Area("Admin")]
    public class PropertyController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public PropertyController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            
            // Create upload directories if they don't exist
            EnsureUploadDirectoriesExist();
        }

        private void EnsureUploadDirectoriesExist()
        {
            try
            {
                if (_webHostEnvironment.WebRootPath != null)
                {
                    // Create base uploads directory
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }
                    
                    // Create properties directory
                    string propertiesFolder = Path.Combine(uploadsFolder, "properties");
                    if (!Directory.Exists(propertiesFolder))
                    {
                        Directory.CreateDirectory(propertiesFolder);
                    }
                    
                    // Create floorplans directory
                    string floorplansFolder = Path.Combine(uploadsFolder, "floorplans");
                    if (!Directory.Exists(floorplansFolder))
                    {
                        Directory.CreateDirectory(floorplansFolder);
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the error but don't throw it in the constructor
                System.Diagnostics.Debug.WriteLine($"Error creating upload directories: {ex.Message}");
            }
        }

        public IActionResult Add()
        {
            return View();
        }

        public async Task<IActionResult> ViewProperty(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var property = await _context.Properties
                .Include(p => p.PropertyImages)
                .FirstOrDefaultAsync(p => p.Id == id);

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
                SellingType = property.SellingType,
                Bedrooms = property.Bedrooms,
                Bathrooms = property.Bathrooms,
                Kitchen = property.Kitchen,
                Balcony = property.Balcony,
                Hall = property.Hall,
                TotalFloors = property.TotalFloors,
                Size = property.Size,
                Address = property.Address,
                Price = property.Price,
                Status = property.Status,
                HasSwimmingPool = property.HasSwimmingPool,
                HasParking = property.HasParking,
                HasGym = property.HasGym,
                HasSecurity = property.HasSecurity,
                HasElevator = property.HasElevator,
                HasCCTV = property.HasCCTV,
                IsFeatured = property.IsFeatured,
                CreatedAt = property.CreatedAt,
                UpdatedAt = property.UpdatedAt,
                AgentId = property.AgentId,
                ExistingImages = property.PropertyImages.Select(pi => new PropertyImageViewModel
                {
                    Id = pi.Id,
                    ImagePath = pi.ImagePath,
                    IsMain = pi.IsMain,
                    ImageType = pi.ImageType
                }).ToList()
            };

            return View(viewModel);
        }

        public async Task<IActionResult> ViewPropertyList()
        {
            var properties = await _context.Properties
                .Include(p => p.PropertyImages)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            var viewModels = properties.Select(p => new PropertyViewModel
            {
                Id = p.Id,
                Title = p.Title,
                Content = p.Content,
                PropertyType = p.PropertyType,
                SellingType = p.SellingType,
                Bedrooms = p.Bedrooms,
                Bathrooms = p.Bathrooms,
                Kitchen = p.Kitchen,
                Balcony = p.Balcony,
                Hall = p.Hall,
                TotalFloors = p.TotalFloors,
                Size = p.Size,
                Address = p.Address,
                Price = p.Price,
                Status = p.Status,
                HasSwimmingPool = p.HasSwimmingPool,
                HasParking = p.HasParking,
                HasGym = p.HasGym,
                HasSecurity = p.HasSecurity,
                HasElevator = p.HasElevator,
                HasCCTV = p.HasCCTV,
                IsFeatured = p.IsFeatured,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
                AgentId = p.AgentId,
                ExistingImages = p.PropertyImages.Select(pi => new PropertyImageViewModel
                {
                    Id = pi.Id,
                    ImagePath = pi.ImagePath,
                    IsMain = pi.IsMain,
                    ImageType = pi.ImageType
                }).ToList()
            });

            return View(viewModels);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestFormLimits(MultipartBodyLengthLimit = 52428800)] // 50MB
        [RequestSizeLimit(52428800)] // 50MB
        public async Task<IActionResult> Add([FromForm] PropertyViewModel model)
        {
            // Log the model state for debugging
            foreach (var key in ModelState.Keys)
            {
                var state = ModelState[key];
                if (state.Errors.Count > 0)
                {
                    foreach (var error in state.Errors)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error in {key}: {error.ErrorMessage}");
                    }
                }
            }

            // Detailed validation debugging
            if (!ModelState.IsValid)
            {
                // Log all validation errors for debugging
                foreach (var error in ModelState)
                {
                    if (error.Value.Errors.Count > 0)
                    {
                        string errorMessage = string.Join(", ", error.Value.Errors.Select(e => e.ErrorMessage));
                        TempData[$"Error_{error.Key}"] = $"{error.Key}: {errorMessage}"; 
                    }
                }
                
                TempData["Error"] = "Please fix the validation errors and try again.";
                return View(model);
            }

            try
            {
                // Make sure required properties have values
                if (string.IsNullOrEmpty(model.Title) || string.IsNullOrEmpty(model.Content))
                {
                    TempData["Error"] = "Title and Description are required.";
                    return View(model);
                }

                // Create new property
                var property = new Property
                {
                    Id = Guid.NewGuid().ToString(),
                    Title = model.Title,
                    Content = model.Content,
                    PropertyType = model.PropertyType,
                    Bedrooms = model.Bedrooms,
                    Bathrooms = model.Bathrooms,
                    Balcony = model.Balcony,
                    Kitchen = model.Kitchen,
                    Hall = model.Hall,
                    Price = model.Price,
                    Size = model.Size,
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
                    HasCCTV = model.HasCCTV,
                    AgentId = model.AgentId,
                    CreatedAt = DateTime.Now
                };

                _context.Properties.Add(property);
                await _context.SaveChangesAsync();

                // Handle property images
                if (model.PropertyImages != null && model.PropertyImages.Any())
                {
                    bool isFirstImage = true; // Track if it's the first image
                    foreach (var image in model.PropertyImages)
                    {
                        if (image != null && image.Length > 0)
                        {
                            try
                            {
                                var imagePath = await SaveImage(image, "property");
                                if (!string.IsNullOrEmpty(imagePath))
                                {
                                    var propertyImage = new PropertyImage
                                    {
                                        Id = Guid.NewGuid().ToString(),
                                        PropertyId = property.Id,
                                        ImagePath = imagePath,
                                        ImageType = "Property",
                                        IsMain = isFirstImage, // Only first image is main
                                        CreatedAt = DateTime.Now
                                    };
                                    _context.PropertyImages.Add(propertyImage);
                                    isFirstImage = false; // Subsequent images aren't main
                                }
                            }
                            catch (Exception ex)
                            {
                                // Log image error but continue with other images
                                TempData["ImageError"] = $"Error saving an image: {ex.Message}";
                            }
                        }
                    }
                }
                else
                {
                    // For testing purposes, create a placeholder image
                    System.Diagnostics.Debug.WriteLine("No images uploaded, creating a placeholder");
                    var placeholderImagePath = "/uploads/properties/placeholder.jpg";
                    
                    // First check if the placeholder actually exists in the filesystem
                    string webRootPath = _webHostEnvironment.WebRootPath;
                    string fullPath = Path.Combine(webRootPath, "uploads", "properties", "placeholder.jpg");
                    
                    // If placeholder doesn't exist, create a directory for it (we won't create the actual image)
                    if (!System.IO.File.Exists(fullPath))
                    {
                        string uploadsFolder = Path.Combine(webRootPath, "uploads", "properties");
                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }
                    }
                    
                    // Add a record for the placeholder (even if the file doesn't exist, this will help debugging)
                    var propertyImage = new PropertyImage
                    {
                        Id = Guid.NewGuid().ToString(),
                        PropertyId = property.Id,
                        ImagePath = placeholderImagePath,
                        ImageType = "Property",
                        IsMain = true,
                        CreatedAt = DateTime.Now
                    };
                    _context.PropertyImages.Add(propertyImage);
                }

                // Handle floor plan image
                try
                {
                    if (model.FloorPlanImage != null)
                    {
                        var imagePath = await SaveImage(model.FloorPlanImage, "floorplan");
                        if (!string.IsNullOrEmpty(imagePath))
                        {
                            var floorPlanImage = new PropertyImage
                            {
                                Id = Guid.NewGuid().ToString(),
                                PropertyId = property.Id,
                                ImagePath = imagePath,
                                ImageType = "FloorPlan",
                                IsMain = false, // Floor plans are not main images
                                CreatedAt = DateTime.Now
                            };
                            _context.PropertyImages.Add(floorPlanImage);
                        }
                    }
                }
                catch (Exception ex)
                {
                    TempData["FloorPlanError"] = $"Error saving floor plan: {ex.Message}";
                }

                // Handle ground floor plan image
                try
                {
                    if (model.GroundFloorPlanImage != null)
                    {
                        var imagePath = await SaveImage(model.GroundFloorPlanImage, "groundfloor");
                        if (!string.IsNullOrEmpty(imagePath))
                        {
                            var groundFloorImage = new PropertyImage
                            {
                                Id = Guid.NewGuid().ToString(),
                                PropertyId = property.Id,
                                ImagePath = imagePath,
                                ImageType = "GroundFloor",
                                IsMain = false, // Floor plans are not main images
                                CreatedAt = DateTime.Now
                            };
                            _context.PropertyImages.Add(groundFloorImage);
                        }
                    }
                }
                catch (Exception ex)
                {
                    TempData["GroundFloorError"] = $"Error saving ground floor plan: {ex.Message}";
                }

                // Handle basement plan image
                try
                {
                    if (model.BasementPlanImage != null)
                    {
                        var imagePath = await SaveImage(model.BasementPlanImage, "basement");
                        if (!string.IsNullOrEmpty(imagePath))
                        {
                            var basementImage = new PropertyImage
                            {
                                Id = Guid.NewGuid().ToString(),
                                PropertyId = property.Id,
                                ImagePath = imagePath,
                                ImageType = "Basement",
                                IsMain = false, // Floor plans are not main images
                                CreatedAt = DateTime.Now
                            };
                            _context.PropertyImages.Add(basementImage);
                        }
                    }
                }
                catch (Exception ex)
                {
                    TempData["BasementError"] = $"Error saving basement plan: {ex.Message}";
                }

                await _context.SaveChangesAsync();

                TempData["Success"] = "Property added successfully!";
                return RedirectToAction("ViewPropertyList");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while adding the property: " + ex.Message;

                if (ex.InnerException != null)
                {
                    TempData["InnerError"] = ex.InnerException.Message;
                }
                return View(model);
            }
        }

        private async Task<string?> SaveImage(IFormFile image, string imageType)
        {
            if (image == null || image.Length == 0)
                return null;

            try
            {
                string uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(image.FileName)}";
              
                string folder = imageType.ToLower() switch
                {
                    "property" => "properties",
                    "floorplan" => "floorplans",
                    "basement" => "floorplans",
                    "groundfloor" => "floorplans",
                    _ => "properties"
                };
               
                if (_webHostEnvironment.WebRootPath == null)
                {
                    throw new DirectoryNotFoundException("Web root path is null. Cannot save images.");
                }
                
                string uploadBase = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
                if (!Directory.Exists(uploadBase))
                {
                    Directory.CreateDirectory(uploadBase);
                }
          
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", folder);
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await image.CopyToAsync(fileStream);
                }

                return $"/uploads/{folder}/{uniqueFileName}";
            }
            catch (Exception ex)
            {
                // Log the error and rethrow
                System.Diagnostics.Debug.WriteLine($"Error saving image: {ex.Message}");
                throw;
            }
        }

        // GET: Admin/Property/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var property = await _context.Properties
                .Include(p => p.PropertyImages)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (property == null)
            {
                TempData["Error"] = "Property not found";
                return RedirectToAction(nameof(ViewPropertyList));
            }

            // Enhanced debugging for image loading
            System.Diagnostics.Debug.WriteLine($"=== DEBUG: Property Edit ===");
            System.Diagnostics.Debug.WriteLine($"Property ID: {property.Id}");
            System.Diagnostics.Debug.WriteLine($"PropertyImages count: {property.PropertyImages?.Count ?? 0}");
            
            // Add debug information to TempData
            TempData["DebugPropertyId"] = property.Id;
            TempData["DebugImagesCount"] = property.PropertyImages?.Count.ToString() ?? "0";
            
            if (property.PropertyImages != null)
            {
                foreach (var img in property.PropertyImages)
                {
                    System.Diagnostics.Debug.WriteLine($"Image ID: {img.Id}, Type: {img.ImageType}, Path: {img.ImagePath}, IsMain: {img.IsMain}");
                    
                    // Verify if the image file exists
                    if (!string.IsNullOrEmpty(img.ImagePath))
                    {
                        string webRootPath = _webHostEnvironment.WebRootPath;
                        string imagePath = img.ImagePath.TrimStart('/');
                        string fullPath = Path.Combine(webRootPath, imagePath);
                        
                        bool fileExists = System.IO.File.Exists(fullPath);
                        System.Diagnostics.Debug.WriteLine($"Image file exists: {fileExists}, Full path: {fullPath}");
                    }
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("PropertyImages collection is NULL");
            }
            
            // Create the view model and map the properties
            var model = new PropertyViewModel
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
                Price = property.Price,
                Size = property.Size,
                Address = property.Address,
                Status = property.Status,
                TotalFloors = property.TotalFloors,
                IsFeatured = property.IsFeatured,
                SellingType = property.SellingType,
                HasSwimmingPool = property.HasSwimmingPool,
                HasParking = property.HasParking,
                HasGym = property.HasGym,
                HasSecurity = property.HasSecurity,
                HasElevator = property.HasElevator,
                HasCCTV = property.HasCCTV,
                AgentId = property.AgentId,
                CreatedAt = property.CreatedAt,
                UpdatedAt = property.UpdatedAt,
                // Map existing images to view model
                ExistingImages = property.PropertyImages?.Select(pi => new PropertyImageViewModel
                {
                    Id = pi.Id,
                    ImagePath = pi.ImagePath.StartsWith("http") ? pi.ImagePath : pi.ImagePath,
                    IsMain = pi.IsMain,
                    ImageType = pi.ImageType
                }).ToList()
            };

            // Debug the view model's images
            System.Diagnostics.Debug.WriteLine($"ViewModel ExistingImages count: {model.ExistingImages?.Count ?? 0}");
            if (model.ExistingImages != null)
            {
                foreach (var img in model.ExistingImages)
                {
                    System.Diagnostics.Debug.WriteLine($"ViewModel Image: ID: {img.Id}, Type: {img.ImageType}, Path: {img.ImagePath}, IsMain: {img.IsMain}");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("ViewModel ExistingImages is NULL");
            }

            return View(model);
        }

        // POST: Admin/Property/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestFormLimits(MultipartBodyLengthLimit = 52428800)] // 50MB
        [RequestSizeLimit(52428800)] // 50MB
        public async Task<IActionResult> Edit(string id, [FromForm] PropertyViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                // Log validation errors
                foreach (var error in ModelState)
                {
                    if (error.Value.Errors.Count > 0)
                    {
                        string errorMessage = string.Join(", ", error.Value.Errors.Select(e => e.ErrorMessage));
                        TempData[$"Error_{error.Key}"] = $"{error.Key}: {errorMessage}";
                    }
                }
                
                // Get the existing images again so they show up in the form
                var existingImages = await _context.PropertyImages
                    .Where(pi => pi.PropertyId == id)
                    .Select(pi => new PropertyImageViewModel
                    {
                        Id = pi.Id,
                        ImagePath = pi.ImagePath,
                        IsMain = pi.IsMain,
                        ImageType = pi.ImageType
                    })
                    .ToListAsync();
                
                model.ExistingImages = existingImages;
                
                TempData["Error"] = "Please fix the validation errors and try again.";
                return View(model);
            }

            try
            {
                var property = await _context.Properties.FindAsync(id);
                
                if (property == null)
                {
                    TempData["Error"] = "Property not found";
                    return RedirectToAction(nameof(ViewPropertyList));
                }

                // Update the property
                property.Title = model.Title;
                property.Content = model.Content;
                property.PropertyType = model.PropertyType;
                property.Bedrooms = model.Bedrooms;
                property.Bathrooms = model.Bathrooms;
                property.Balcony = model.Balcony;
                property.Kitchen = model.Kitchen;
                property.Hall = model.Hall;
                property.Price = model.Price;
                property.Size = model.Size;
                property.Address = model.Address;
                property.Status = model.Status;
                property.TotalFloors = model.TotalFloors;
                property.IsFeatured = model.IsFeatured;
                property.SellingType = model.SellingType;
                property.HasSwimmingPool = model.HasSwimmingPool;
                property.HasParking = model.HasParking;
                property.HasGym = model.HasGym;
                property.HasSecurity = model.HasSecurity;
                property.HasElevator = model.HasElevator;
                property.HasCCTV = model.HasCCTV;
                property.AgentId = model.AgentId;
                property.UpdatedAt = DateTime.Now;

                _context.Update(property);
                
                // Handle new property images - replace existing ones if provided
                if (model.PropertyImages != null && model.PropertyImages.Any(img => img != null && img.Length > 0))
                {
                    // Delete existing property images
                    var existingPropertyImages = await _context.PropertyImages
                        .Where(pi => pi.PropertyId == id && pi.ImageType == "Property")
                        .ToListAsync();
                    
                    if (existingPropertyImages.Any())
                    {
                        _context.PropertyImages.RemoveRange(existingPropertyImages);
                    }
                    
                    // Add new property images
                    bool isFirstImage = true; // Track if it's the first image
                    foreach (var image in model.PropertyImages)
                    {
                        if (image != null && image.Length > 0)
                        {
                            try
                            {
                                var imagePath = await SaveImage(image, "property");
                                if (!string.IsNullOrEmpty(imagePath))
                                {
                                    var propertyImage = new PropertyImage
                                    {
                                        Id = Guid.NewGuid().ToString(),
                                        PropertyId = property.Id,
                                        ImagePath = imagePath,
                                        ImageType = "Property",
                                        IsMain = isFirstImage, // First image is main
                                        CreatedAt = DateTime.Now
                                    };
                                    _context.PropertyImages.Add(propertyImage);
                                    isFirstImage = false; // Subsequent images aren't main
                                }
                            }
                            catch (Exception ex)
                            {
                                TempData["ImageError"] = $"Error saving an image: {ex.Message}";
                            }
                        }
                    }
                }

                // Handle floor plan image - replace if provided
                if (model.FloorPlanImage != null && model.FloorPlanImage.Length > 0)
                {
                    // Delete existing floor plan image
                    var existingFloorPlan = await _context.PropertyImages
                        .Where(pi => pi.PropertyId == id && pi.ImageType == "FloorPlan")
                        .ToListAsync();
                    
                    if (existingFloorPlan.Any())
                    {
                        _context.PropertyImages.RemoveRange(existingFloorPlan);
                    }
                    
                    // Add new floor plan image
                    try
                    {
                        var imagePath = await SaveImage(model.FloorPlanImage, "floorplan");
                        if (!string.IsNullOrEmpty(imagePath))
                        {
                            var floorPlanImage = new PropertyImage
                            {
                                Id = Guid.NewGuid().ToString(),
                                PropertyId = property.Id,
                                ImagePath = imagePath,
                                ImageType = "FloorPlan",
                                IsMain = false, // Floor plans are not main images
                                CreatedAt = DateTime.Now
                            };
                            _context.PropertyImages.Add(floorPlanImage);
                        }
                    }
                    catch (Exception ex)
                    {
                        TempData["FloorPlanError"] = $"Error saving floor plan: {ex.Message}";
                    }
                }

                // Handle ground floor plan image - replace if provided
                if (model.GroundFloorPlanImage != null && model.GroundFloorPlanImage.Length > 0)
                {
                    // Delete existing ground floor plan
                    var existingGroundFloorPlan = await _context.PropertyImages
                        .Where(pi => pi.PropertyId == id && pi.ImageType == "GroundFloor")
                        .ToListAsync();
                    
                    if (existingGroundFloorPlan.Any())
                    {
                        _context.PropertyImages.RemoveRange(existingGroundFloorPlan);
                    }
                    
                    // Add new ground floor plan
                    try
                    {
                        var imagePath = await SaveImage(model.GroundFloorPlanImage, "groundfloor");
                        if (!string.IsNullOrEmpty(imagePath))
                        {
                            var groundFloorImage = new PropertyImage
                            {
                                Id = Guid.NewGuid().ToString(),
                                PropertyId = property.Id,
                                ImagePath = imagePath,
                                ImageType = "GroundFloor",
                                IsMain = false, // Floor plans are not main images
                                CreatedAt = DateTime.Now
                            };
                            _context.PropertyImages.Add(groundFloorImage);
                        }
                    }
                    catch (Exception ex)
                    {
                        TempData["GroundFloorError"] = $"Error saving ground floor plan: {ex.Message}";
                    }
                }

                // Handle basement plan image - replace if provided
                if (model.BasementPlanImage != null && model.BasementPlanImage.Length > 0)
                {
                    // Delete existing basement plan
                    var existingBasementPlan = await _context.PropertyImages
                        .Where(pi => pi.PropertyId == id && pi.ImageType == "Basement")
                        .ToListAsync();
                    
                    if (existingBasementPlan.Any())
                    {
                        _context.PropertyImages.RemoveRange(existingBasementPlan);
                    }
                    
                    // Add new basement plan
                    try
                    {
                        var imagePath = await SaveImage(model.BasementPlanImage, "basement");
                        if (!string.IsNullOrEmpty(imagePath))
                        {
                            var basementImage = new PropertyImage
                            {
                                Id = Guid.NewGuid().ToString(),
                                PropertyId = property.Id,
                                ImagePath = imagePath,
                                ImageType = "Basement",
                                IsMain = false, // Floor plans are not main images
                                CreatedAt = DateTime.Now
                            };
                            _context.PropertyImages.Add(basementImage);
                        }
                    }
                    catch (Exception ex)
                    {
                        TempData["BasementError"] = $"Error saving basement plan: {ex.Message}";
                    }
                }

                await _context.SaveChangesAsync();

                TempData["Success"] = "Property updated successfully!";
                return RedirectToAction(nameof(ViewPropertyList));
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while updating the property: " + ex.Message;
                if (ex.InnerException != null)
                {
                    TempData["InnerError"] = ex.InnerException.Message;
                }
                
                // Get the existing images again
                var existingImages = await _context.PropertyImages
                    .Where(pi => pi.PropertyId == id)
                    .Select(pi => new PropertyImageViewModel
                    {
                        Id = pi.Id,
                        ImagePath = pi.ImagePath,
                        IsMain = pi.IsMain,
                        ImageType = pi.ImageType
                    })
                    .ToListAsync();
                
                model.ExistingImages = existingImages;
                
                return View(model);
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteImage(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return Json(new { success = false, message = "Invalid image ID" });
            }

            try
            {
                // Find the image
                var image = await _context.PropertyImages.FindAsync(id);
                if (image == null)
                {
                    return Json(new { success = false, message = "Image not found" });
                }

                // Get the physical file path
                string webRootPath = _webHostEnvironment.WebRootPath;
                string imagePath = image.ImagePath.TrimStart('/');
                string fullPath = Path.Combine(webRootPath, imagePath);

                // Delete the file if it exists
                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                }

                // Remove from database
                _context.PropertyImages.Remove(image);
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SetMainImage(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return Json(new { success = false, message = "Invalid image ID" });
            }

            try
            {
                // Find the image
                var image = await _context.PropertyImages.FindAsync(id);
                if (image == null)
                {
                    return Json(new { success = false, message = "Image not found" });
                }

                // Make sure it's a property image
                if (image.ImageType != "Property")
                {
                    return Json(new { success = false, message = "Only property images can be set as main" });
                }

                // Reset main status for all other images of this property
                var otherImages = await _context.PropertyImages
                    .Where(p => p.PropertyId == image.PropertyId && p.Id != id)
                    .ToListAsync();

                foreach (var otherImage in otherImages)
                {
                    otherImage.IsMain = false;
                }

                // Set this image as main
                image.IsMain = true;

                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
} 