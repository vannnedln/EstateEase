using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EstateEase.Data;
using EstateEase.Models.ViewModels;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using EstateEase.Models;
using EstateEase.Models.Entities;
using System;
using System.Collections.Generic;

namespace EstateEase.Areas.Agent.Controllers
{
    [Area("Agent")]
    [Authorize(Roles = "Agent")]
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

        public async Task<IActionResult> PropertyList()
        {
            try
            {
                // Get current user ID
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                
                // Get agent ID for the current user
                var agent = await _context.Agents.FirstOrDefaultAsync(a => a.UserId == userId);
                
                if (agent == null)
                {
                    TempData["Error"] = "Agent profile not found.";
                    return View(new List<PropertyViewModel>());
                }
                
                // Get properties associated with this agent
                var properties = await _context.Properties
                    .Include(p => p.PropertyImages)
                    .Where(p => p.AgentId == agent.Id)
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
                        ImageType = pi.ImageType,
                        CreatedAt = pi.CreatedAt
                    }).ToList()
                }).ToList();

                return View(viewModels);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while loading properties: " + ex.Message;
                return View(new List<PropertyViewModel>());
            }
        }

        public IActionResult Index()
        {
            return RedirectToAction(nameof(PropertyList));
        }

        public IActionResult Add()
        {
            return View();
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

                // Get current agent ID
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var agent = await _context.Agents.FirstOrDefaultAsync(a => a.UserId == userId);
                
                if (agent == null)
                {
                    TempData["Error"] = "Agent profile not found. Please contact an administrator.";
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
                    AgentId = agent.Id,
                    CreatedAt = DateTime.Now
                };

                _context.Properties.Add(property);
                await _context.SaveChangesAsync();

                // Handle property images
                if (model.PropertyImages != null && model.PropertyImages.Any())
                {
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
                                        CreatedAt = DateTime.Now
                                    };
                                    _context.PropertyImages.Add(propertyImage);
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
                        CreatedAt = DateTime.Now
                    };
                    _context.PropertyImages.Add(propertyImage);
                }

                // Handle floor plan image
                try
                {
                    if (model.FloorPlanImage != null && model.FloorPlanImage.Count > 0)
                    {
                        foreach (var floorPlanImage in model.FloorPlanImage)
                        {
                            var imagePath = await SaveImage(floorPlanImage, "floorplan");
                            if (!string.IsNullOrEmpty(imagePath))
                            {
                                var propertyFloorPlan = new PropertyImage
                                {
                                    Id = Guid.NewGuid().ToString(),
                                    PropertyId = property.Id,
                                    ImagePath = imagePath,
                                    ImageType = "FloorPlan",
                                    CreatedAt = DateTime.Now
                                };
                                _context.PropertyImages.Add(propertyFloorPlan);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    TempData["FloorPlanError"] = $"Error saving floor plan: {ex.Message}";
                }

                await _context.SaveChangesAsync();

                TempData["Success"] = "Property added successfully!";
                return RedirectToAction("PropertyList");
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
            {
                System.Diagnostics.Debug.WriteLine("SaveImage: Image is null or empty");
                return null;
            }

            try
            {
                // Generate a unique filename
                string uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(image.FileName)}";
                System.Diagnostics.Debug.WriteLine($"SaveImage: Generated file name: {uniqueFileName}");
              
                // Determine the appropriate folder based on image type
                string folder = imageType.ToLower() switch
                {
                    "property" => "properties",
                    "floorplan" => "floorplans",
                    _ => "properties"  // Default to properties for any other type
                };
                System.Diagnostics.Debug.WriteLine($"SaveImage: Selected folder: {folder}");
               
                // Validate web root path
                if (string.IsNullOrEmpty(_webHostEnvironment.WebRootPath))
                {
                    System.Diagnostics.Debug.WriteLine("SaveImage: Web root path is null");
                    throw new DirectoryNotFoundException("Web root path is null. Cannot save images.");
                }
                
                // Ensure base uploads directory exists
                string uploadBase = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
                System.Diagnostics.Debug.WriteLine($"SaveImage: Upload base path: {uploadBase}");
                
                if (!Directory.Exists(uploadBase))
                {
                    System.Diagnostics.Debug.WriteLine($"SaveImage: Creating upload base directory: {uploadBase}");
                    Directory.CreateDirectory(uploadBase);
                }
          
                // Ensure specific upload folder exists
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", folder);
                System.Diagnostics.Debug.WriteLine($"SaveImage: Upload folder path: {uploadsFolder}");
                
                if (!Directory.Exists(uploadsFolder))
                {
                    System.Diagnostics.Debug.WriteLine($"SaveImage: Creating upload folder: {uploadsFolder}");
                    try {
                        Directory.CreateDirectory(uploadsFolder);
                    } 
                    catch (Exception ex) {
                        System.Diagnostics.Debug.WriteLine($"SaveImage: ERROR creating directory - {ex.Message}");
                        return null;
                    }
                }
                
                // Check write access to directory
                if (!HasWriteAccessToDirectory(uploadsFolder))
                {
                    System.Diagnostics.Debug.WriteLine($"SaveImage: No write access to directory: {uploadsFolder}");
                    return null;
                }

                // Determine full file path
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                System.Diagnostics.Debug.WriteLine($"SaveImage: Full file path: {filePath}");
                
                try
                {
                    // Use synchronous file operation for immediate effect and to avoid any potential task scheduling issues
                    using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        System.Diagnostics.Debug.WriteLine("SaveImage: Copying file to stream");
                        await image.CopyToAsync(fileStream);
                        await fileStream.FlushAsync(); // Ensure all data is written to disk
                    }
                    
                    // Verify file was actually saved with appropriate size
                    if (System.IO.File.Exists(filePath))
                    {
                        var fileInfo = new FileInfo(filePath);
                        if (fileInfo.Length > 0)
                        {
                            System.Diagnostics.Debug.WriteLine($"SaveImage: File successfully saved to {filePath}, size: {fileInfo.Length} bytes");
                        }
                            else
                        {
                            System.Diagnostics.Debug.WriteLine($"SaveImage: WARNING - File exists but has zero size: {filePath}");
                            // Try one more time with synchronous File.WriteAllBytes
                            try
                            {
                                using (var memoryStream = new MemoryStream())
                                {
                                    await image.CopyToAsync(memoryStream);
                                    System.IO.File.WriteAllBytes(filePath, memoryStream.ToArray());
                                    System.Diagnostics.Debug.WriteLine($"SaveImage: Retry save successful: {filePath}");
                                }
                            }
                            catch (Exception exRetry)
                            {
                                System.Diagnostics.Debug.WriteLine($"SaveImage: Retry failed - {exRetry.Message}");
                                return null;
                            }
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"SaveImage: ERROR - File not found after save attempt: {filePath}");
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"SaveImage: ERROR while writing file - {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"SaveImage: ERROR Stack trace - {ex.StackTrace}");
                    return null;
                }

                // Return the path relative to web root
                var relativePath = $"/uploads/{folder}/{uniqueFileName}";
                System.Diagnostics.Debug.WriteLine($"SaveImage: Returning relative path: {relativePath}");
                return relativePath;
            }
            catch (Exception ex)
            {
                // Log the error and return null
                System.Diagnostics.Debug.WriteLine($"SaveImage ERROR: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"SaveImage ERROR Stack Trace: {ex.StackTrace}");
                
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"SaveImage ERROR Inner: {ex.InnerException.Message}");
                }
                
                // Don't throw, just return null to indicate failure
                return null;
            }
        }

        private bool HasWriteAccessToDirectory(string directoryPath)
        {
            try
            {
                // First check if directory exists
                if (!Directory.Exists(directoryPath))
                {
                    System.Diagnostics.Debug.WriteLine($"Directory does not exist: {directoryPath}");
                    return false;
                }
                
                // Try to create a temporary file to test permissions
                string testFilePath = Path.Combine(directoryPath, $"temp_{Guid.NewGuid()}.tmp");
                using (FileStream fs = System.IO.File.Create(testFilePath, 1, FileOptions.DeleteOnClose))
                {
                    System.Diagnostics.Debug.WriteLine($"Successfully created test file: {testFilePath}");
                }
                
                // If we got here, we have write access
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Write permission check failed: {ex.Message}");
                return false;
            }
        }

        // GET: Agent/Property/Edit/5
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
                return RedirectToAction(nameof(PropertyList));
            }

            // Ensure the property belongs to the current agent
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var agent = await _context.Agents.FirstOrDefaultAsync(a => a.UserId == userId);
            
            if (agent == null || property.AgentId != agent.Id)
            {
                TempData["Error"] = "You do not have permission to edit this property.";
                return RedirectToAction(nameof(PropertyList));
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
                    System.Diagnostics.Debug.WriteLine($"Image ID: {img.Id}, Type: {img.ImageType}, Path: {img.ImagePath}");
                    
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
                    ImagePath = pi.ImagePath,
                    ImageType = pi.ImageType,
                    CreatedAt = pi.CreatedAt
                }).ToList()
            };

            return View(model);
        }

        // POST: Agent/Property/Edit/5
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

            // Ensure the property belongs to the current agent
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var agent = await _context.Agents.FirstOrDefaultAsync(a => a.UserId == userId);
            var property = await _context.Properties.FindAsync(id);
            
            if (agent == null || property == null || property.AgentId != agent.Id)
            {
                TempData["Error"] = "You do not have permission to edit this property.";
                return RedirectToAction(nameof(PropertyList));
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
                        ImageType = pi.ImageType,
                        CreatedAt = pi.CreatedAt
                    })
                    .ToListAsync();
                
                model.ExistingImages = existingImages;
                
                TempData["Error"] = "Please fix the validation errors and try again.";
                return View(model);
            }

            try
            {
                // Update property details from the model
                property.Title = model.Title;
                property.Content = model.Content;
                property.PropertyType = model.PropertyType;
                property.SellingType = model.SellingType;
                property.Bedrooms = model.Bedrooms;
                property.Bathrooms = model.Bathrooms;
                property.Kitchen = model.Kitchen;
                property.Balcony = model.Balcony;
                property.Hall = model.Hall;
                property.TotalFloors = model.TotalFloors;
                property.Size = model.Size;
                property.Address = model.Address;
                property.Price = model.Price;
                property.Status = model.Status;
                property.HasSwimmingPool = model.HasSwimmingPool;
                property.HasParking = model.HasParking;
                property.HasGym = model.HasGym;
                property.HasSecurity = model.HasSecurity;
                property.HasElevator = model.HasElevator;
                property.HasCCTV = model.HasCCTV;
                property.IsFeatured = model.IsFeatured;
                property.UpdatedAt = DateTime.Now;
                
                // Update the property in the database context
                _context.Properties.Update(property);
                System.Diagnostics.Debug.WriteLine($"Property details updated in context: {property.Id}");

                // Process deleted images
                if (Request.Form.ContainsKey("DeletedImageIds") && !string.IsNullOrWhiteSpace(Request.Form["DeletedImageIds"]))
                {
                    var deletedImageIdsString = Request.Form["DeletedImageIds"].ToString();
                    var deletedImageIds = deletedImageIdsString.Split(',', StringSplitOptions.RemoveEmptyEntries);
                    
                    foreach (var imageId in deletedImageIds)
                    {
                        if (string.IsNullOrWhiteSpace(imageId))
                            continue;
                            
                        var image = await _context.PropertyImages.FindAsync(imageId);
                        if (image != null && image.PropertyId == id)
                        {
                            string? imagePath = image.ImagePath;
                                    
                            // Delete from database
                            _context.PropertyImages.Remove(image);
                                        
                            // Delete file if it exists
                            if (!string.IsNullOrEmpty(imagePath))
                            {
                                try
                                {
                                    string fullPath = Path.Combine(_webHostEnvironment.WebRootPath, imagePath.TrimStart('/'));
                                    
                                    if (System.IO.File.Exists(fullPath))
                                    {
                                        System.IO.File.Delete(fullPath);
                                    }
                                }
                                catch (Exception fileEx)
                                {
                                    // Log error but continue with operation
                                    System.Diagnostics.Debug.WriteLine($"Error deleting image file: {fileEx.Message}");
                                }
                            }
                        }
                    }
                }
                
                // Process replacement images
                foreach (var file in Request.Form.Files)
                {
                    if (file.Name.StartsWith("ReplacementImages[") && file.Name.EndsWith("]"))
                    {
                        // Extract the image ID from the name format ReplacementImages[123]
                        int startIndex = "ReplacementImages[".Length;
                        int endIndex = file.Name.Length - 1; // exclude the closing bracket
                        
                        if (startIndex < endIndex)
                        {
                            var imageId = file.Name.Substring(startIndex, endIndex - startIndex);
                            
                            // Find the image to replace
                            var image = await _context.PropertyImages.FindAsync(imageId);
                            if (image != null && image.PropertyId == id)
                            {
                                // Store old path for later deletion
                                string oldImagePath = image.ImagePath;
                                
                                // Save the new image first to ensure it works
                                var newImagePath = await SaveImage(file, image.ImageType);
                                if (!string.IsNullOrEmpty(newImagePath))
                                {
                                    // Update database record with new path
                                    image.ImagePath = newImagePath;
                                    image.UpdatedAt = DateTime.Now;
                                    
                                    _context.PropertyImages.Update(image);
                                    
                                    // Now delete the old image file
                                    if (!string.IsNullOrEmpty(oldImagePath))
                                    {
                                        try
                                        {
                                            string fullPath = Path.Combine(_webHostEnvironment.WebRootPath, oldImagePath.TrimStart('/'));
                                                    
                                            if (System.IO.File.Exists(fullPath))
                                            {
                                                System.IO.File.Delete(fullPath);
                                            }
                                        }
                                        catch (Exception fileEx)
                                        {
                                            // Log error but continue with operation
                                            System.Diagnostics.Debug.WriteLine($"Error deleting old image file: {fileEx.Message}");
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                
                // Process new property images added via PropertyImages file input
                if (model.PropertyImages != null && model.PropertyImages.Any())
                {
                    foreach (var image in model.PropertyImages)
                    {
                        if (image != null && image.Length > 0)
                        {
                            var imagePath = await SaveImage(image, "property");
                            if (!string.IsNullOrEmpty(imagePath))
                            {
                                var propertyImage = new PropertyImage
                                {
                                    Id = Guid.NewGuid().ToString(),
                                    PropertyId = id,
                                    ImagePath = imagePath,
                                    ImageType = "Property",
                                    CreatedAt = DateTime.Now
                                };
                                
                                _context.PropertyImages.Add(propertyImage);
                            }
                        }
                    }
                }
                else
                {
                    // Check for property images in the form directly
                    var propertyImagesFiles = Request.Form.Files.Where(f => f.Name == "PropertyImages").ToList();
                    if (propertyImagesFiles.Any())
                    {
                        foreach (var file in propertyImagesFiles)
                        {
                            if (file.Length > 0)
                            {
                                var imagePath = await SaveImage(file, "property");
                                if (!string.IsNullOrEmpty(imagePath))
                                {
                                    var propertyImage = new PropertyImage
                                    {
                                        Id = Guid.NewGuid().ToString(),
                                        PropertyId = id,
                                        ImagePath = imagePath,
                                        ImageType = "Property",
                                        CreatedAt = DateTime.Now
                                    };
                                    
                                    _context.PropertyImages.Add(propertyImage);
                                }
                            }
                        }
                    }
                }
                
                // Process new floor plan images
                if (model.FloorPlanImage != null && model.FloorPlanImage.Any())
                {
                    foreach (var image in model.FloorPlanImage)
                    {
                        if (image != null && image.Length > 0)
                        {
                            var imagePath = await SaveImage(image, "floorplan");
                            if (!string.IsNullOrEmpty(imagePath))
                            {
                                var propertyImage = new PropertyImage
                                {
                                    Id = Guid.NewGuid().ToString(),
                                    PropertyId = id,
                                    ImagePath = imagePath,
                                    ImageType = "FloorPlan",
                                    CreatedAt = DateTime.Now
                                };
                                        
                                _context.PropertyImages.Add(propertyImage);
                            }
                        }
                    }
                }
                else
                {
                    // Check for floor plan images in the form directly
                    var floorPlanImagesFiles = Request.Form.Files.Where(f => f.Name == "FloorPlanImages").ToList();
                    if (floorPlanImagesFiles.Any())
                    {
                        foreach (var file in floorPlanImagesFiles)
                        {
                            if (file.Length > 0)
                            {
                                var imagePath = await SaveImage(file, "floorplan");
                                if (!string.IsNullOrEmpty(imagePath))
                                {
                                    var propertyImage = new PropertyImage
                                    {
                                        Id = Guid.NewGuid().ToString(),
                                        PropertyId = id,
                                        ImagePath = imagePath,
                                        ImageType = "FloorPlan", 
                                        CreatedAt = DateTime.Now
                                    };
                                    
                                    _context.PropertyImages.Add(propertyImage);
                                }
                            }
                        }
                    }
                }

                // Final save of all changes
                await _context.SaveChangesAsync();

                TempData["Success"] = "Property updated successfully!";
                return RedirectToAction(nameof(PropertyList));
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
                        ImageType = pi.ImageType,
                        CreatedAt = pi.CreatedAt
                    })
                    .ToListAsync();
                
                model.ExistingImages = existingImages;
                
                return View(model);
            }
        }

        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var property = await _context.Properties
                .Include(p => p.PropertyImages)
                .Include(p => p.Agent)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (property == null)
            {
                return NotFound();
            }

            // Ensure the property belongs to the current agent
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var agent = await _context.Agents
                .Include(a => a.User) // Include the Identity User to get email and phone
                .FirstOrDefaultAsync(a => a.UserId == userId);
            
            if (agent == null || property.AgentId != agent.Id)
            {
                TempData["Error"] = "You do not have permission to view this property.";
                return RedirectToAction(nameof(PropertyList));
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
                    ImageType = pi.ImageType,
                    CreatedAt = pi.CreatedAt
                }).ToList()
            };

            // Pass agent information to the view using ViewBag
            // Get contact info from the ASP.NET Identity User
            ViewBag.Agent = new
            {
                FirstName = agent.FirstName,
                LastName = agent.LastName,
                PhoneNumber = agent.User?.PhoneNumber, // Get from Identity User
                Email = agent.User?.Email, // Get from Identity User
                ProfilePictureUrl = agent.ProfilePictureUrl
            };

            return View("ViewProperty", viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            try
            {
                var property = await _context.Properties
                    .Include(p => p.PropertyImages)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (property == null)
                {
                    TempData["Error"] = "Property not found";
                    return RedirectToAction(nameof(PropertyList));
                }

                // Ensure the property belongs to the current agent
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var agent = await _context.Agents.FirstOrDefaultAsync(a => a.UserId == userId);
                
                if (agent == null || property.AgentId != agent.Id)
                {
                    TempData["Error"] = "You do not have permission to delete this property.";
                    return RedirectToAction(nameof(PropertyList));
                }

                // Delete all associated images from file system
                int imagesDeleted = 0;
                int filesDeletionFailed = 0;

                if (property.PropertyImages != null && property.PropertyImages.Any())
                {
                    foreach (var image in property.PropertyImages)
                    {
                        if (!string.IsNullOrEmpty(image.ImagePath))
                        {
                            try
                            {
                                string webRootPath = _webHostEnvironment.WebRootPath;
                                string imagePath = image.ImagePath.TrimStart('/');
                                string fullPath = Path.Combine(webRootPath, imagePath);
                                
                                if (System.IO.File.Exists(fullPath))
                                {
                                    System.IO.File.Delete(fullPath);
                                    imagesDeleted++;
                                }
                            }
                            catch (Exception ex)
                            {
                                filesDeletionFailed++;
                                System.Diagnostics.Debug.WriteLine($"Error deleting image file: {ex.Message}");
                            }
                        }
                    }
                }

                // Remove the property from the database (cascade delete will remove associated images)
                _context.Properties.Remove(property);
                await _context.SaveChangesAsync();

                if (filesDeletionFailed > 0)
                {
                    TempData["Warning"] = $"Property deleted but {filesDeletionFailed} image files could not be deleted";
                }
                else
                {
                    TempData["Success"] = $"Property and all associated images ({imagesDeleted}) deleted successfully";
                }

                return RedirectToAction(nameof(PropertyList));
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while deleting the property: " + ex.Message;
                return RedirectToAction(nameof(PropertyList));
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteImage([FromBody] DeleteImageRequest request)
        {
            if (string.IsNullOrEmpty(request.ImageId))
            {
                return Json(new { success = false, message = "Invalid image ID" });
            }

            try
            {
                // Find the image
                var image = await _context.PropertyImages.FindAsync(request.ImageId);
                if (image == null)
                {
                    return Json(new { success = false, message = "Image not found" });
                }

                // Verify the image belongs to the property if propertyId is provided
                if (!string.IsNullOrEmpty(request.PropertyId) && image.PropertyId != request.PropertyId)
                {
                    return Json(new { success = false, message = "Image does not belong to this property" });
                }

                // Verify the property belongs to the current agent
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var agent = await _context.Agents.FirstOrDefaultAsync(a => a.UserId == userId);
                var property = await _context.Properties.FindAsync(image.PropertyId);
                
                if (agent == null || property == null || property.AgentId != agent.Id)
                {
                    return Json(new { success = false, message = "You do not have permission to delete this image." });
                }

                // Get the physical file path
                string webRootPath = _webHostEnvironment.WebRootPath;
                string imagePath = image.ImagePath.TrimStart('/');
                string fullPath = Path.Combine(webRootPath, imagePath);
                
                bool fileDeleted = false;
                
                // Delete the file if it exists
                if (System.IO.File.Exists(fullPath))
                {
                    try
                    {
                        System.IO.File.Delete(fullPath);
                        fileDeleted = true;
                    }
                    catch (Exception ex)
                    {
                        // Log but continue with DB deletion
                        System.Diagnostics.Debug.WriteLine($"Error deleting image file: {ex.Message}");
                    }
                }
                else
                {
                    // File doesn't exist, consider it "deleted"
                    fileDeleted = true;
                }

                // Remove from database
                _context.PropertyImages.Remove(image);
                await _context.SaveChangesAsync();

                return Json(new { 
                    success = true, 
                    message = fileDeleted ? 
                        "Image deleted successfully from database and file system" : 
                        "Image deleted from database but the file may still exist on the server" 
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }

        public class DeleteImageRequest
        {
            public string ImageId { get; set; }
            public string PropertyId { get; set; }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestFormLimits(MultipartBodyLengthLimit = 52428800)] // 50MB
        [RequestSizeLimit(52428800)] // 50MB
        public async Task<IActionResult> AddImage(string propertyId, string imageType, IFormFile newImage)
        {
            if (string.IsNullOrEmpty(propertyId) || string.IsNullOrEmpty(imageType) || newImage == null)
            {
                return Json(new { success = false, message = "Invalid request. Property ID, image type and new image are required." });
            }

            try
            {
                // Find the property
                var property = await _context.Properties.FindAsync(propertyId);
                if (property == null)
                {
                    return Json(new { success = false, message = "Property not found." });
                }

                // Verify the property belongs to the current agent
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var agent = await _context.Agents.FirstOrDefaultAsync(a => a.UserId == userId);
                
                if (agent == null || property.AgentId != agent.Id)
                {
                    return Json(new { success = false, message = "You do not have permission to modify this property." });
                }

                // Standardize the image type
                string standardizedImageType = imageType.ToLower().Contains("floor") ? "FloorPlan" : "Property";

                // Save the new image
                string? newImagePath = await SaveImage(newImage, standardizedImageType);
                if (string.IsNullOrEmpty(newImagePath))
                {
                    return Json(new { success = false, message = "Failed to save the new image." });
                }

                // Create a new property image
                var propertyImage = new PropertyImage
                {
                    Id = Guid.NewGuid().ToString(),
                    PropertyId = propertyId,
                    ImagePath = newImagePath,
                    ImageType = standardizedImageType,
                    CreatedAt = DateTime.Now
                };

                // Add the new image to the database
                _context.PropertyImages.Add(propertyImage);
                await _context.SaveChangesAsync();

                return Json(new { 
                    success = true, 
                    message = "Image added successfully.", 
                    newImagePath = newImagePath,
                    imageId = propertyImage.Id
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }
    }
} 