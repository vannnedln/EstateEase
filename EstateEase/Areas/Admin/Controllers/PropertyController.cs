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
        public async Task<IActionResult> Add(PropertyViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
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
                                    PropertyId = property.Id,
                                    ImagePath = imagePath,
                                    ImageType = "Property",
                                    IsMain = false,
                                    IsMainImage = false,
                                    CreatedAt = DateTime.Now
                                };
                                _context.PropertyImages.Add(propertyImage);
                            }
                        }
                    }
                }

                // Handle floor plan images
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
                            IsMain = true,
                            IsMainImage = false,
                            CreatedAt = DateTime.Now
                        };
                        _context.PropertyImages.Add(floorPlanImage);
                    }
                }

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
                            IsMain = false,
                            IsMainImage = false,
                            CreatedAt = DateTime.Now
                        };
                        _context.PropertyImages.Add(groundFloorImage);
                    }
                }

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
                            IsMain = false,
                            IsMainImage = false,
                            CreatedAt = DateTime.Now
                        };
                        _context.PropertyImages.Add(basementImage);
                    }
                }

                await _context.SaveChangesAsync();

                TempData["Success"] = "Property added successfully!";
                return RedirectToAction("ViewPropertyList");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while adding the property: " + ex.Message;
                return View(model);
            }
        }

        private async Task<string> SaveImage(IFormFile image, string imageType)
        {
            if (image == null || image.Length == 0)
                return null;

            // Create unique filename
            string uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(image.FileName)}";
            
            // Determine the correct folder based on image type
            string folder = imageType.ToLower() switch
            {
                "property" => "properties",
                "floorplan" => "floorplans",
                "basement" => "floorplans",
                "groundfloor" => "floorplans",
                _ => "properties"
            };

            // Create directory if it doesn't exist
            string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", folder);
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            // Save file
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await image.CopyToAsync(fileStream);
            }

            // Return relative path
            return $"/uploads/{folder}/{uniqueFileName}";
        }
    }
} 