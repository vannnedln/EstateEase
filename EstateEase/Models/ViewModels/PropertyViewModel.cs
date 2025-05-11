using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace EstateEase.Models.ViewModels
{
    public class PropertyViewModel
    {
        public string? Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        [Required]
        public string Content { get; set; }

        [Required]
        [Display(Name = "Property Type")]
        public string PropertyType { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Number of bedrooms must be at least 1")]
        public int Bedrooms { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Number of bathrooms must be at least 1")]
        public int Bathrooms { get; set; }

        [Required]
        [Range(0, int.MaxValue, ErrorMessage = "Number of balconies cannot be negative")]
        public int Balcony { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Number of kitchens must be at least 1")]
        public int Kitchen { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Number of halls must be at least 1")]
        public int Hall { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
        public decimal Price { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Size must be greater than 0")]
        [Display(Name = "Area Size (sq m)")]
        public decimal Size { get; set; }

        [Required]
        [Display(Name = "Address")]
        public string Address { get; set; }

        [Required]
        public string Status { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Total floors must be at least 1")]
        [Display(Name = "Total Floors")]
        public int TotalFloors { get; set; }

        [Display(Name = "Featured Property")]
        public bool IsFeatured { get; set; }

        [Required]
        [Display(Name = "Selling Type")]
        public string SellingType { get; set; }

        [Display(Name = "Swimming Pool")]
        public bool HasSwimmingPool { get; set; }

        [Display(Name = "Parking")]
        public bool HasParking { get; set; }

        [Display(Name = "Gym")]
        public bool HasGym { get; set; }

        [Display(Name = "Security")]
        public bool HasSecurity { get; set; }

        [Display(Name = "Elevator")]
        public bool HasElevator { get; set; }

        [Display(Name = "CCTV")]
        public bool HasCCTV { get; set; }

        // Image upload properties
        [Display(Name = "Property Images")]
        public List<IFormFile>? PropertyImages { get; set; }

        [Display(Name = "Floor Plan")]
        public List<IFormFile>? FloorPlanImage { get; set; }
        
        // Properties for pending image actions
        public List<string>? DeletedImageIds { get; set; }
        public string? MainImageId { get; set; }
        
        // Dictionary structure isn't easily bindable, so we'll use form handling in the controller
        // public Dictionary<string, IFormFile>? ReplacementImages { get; set; }
        
        // Complex nested structures aren't easily bindable, so we'll use form handling in the controller
        // public List<NewImageViewModel>? NewImages { get; set; }

        // Navigation properties
        public string? AgentId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        // Collection of existing images
        public ICollection<PropertyImageViewModel>? ExistingImages { get; set; }
    }

    public class PropertyImageViewModel
    {
        public string? Id { get; set; }
        public string ImagePath { get; set; }
        public string ImageType { get; set; }
        public DateTime CreatedAt { get; set; }
    }
    
    // This class is just for documentation purposes, as we'll handle this in the controller
    public class NewImageViewModel
    {
        public string Type { get; set; }
        public IFormFile File { get; set; }
    }
}