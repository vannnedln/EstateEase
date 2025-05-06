using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace EstateEase.Models.ViewModels
{
    public class PropertyViewModel
    {
        public int Id { get; set; }

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

        [Display(Name = "Floor Plan")]
        public IFormFile? FloorPlanImage { get; set; }

        [Display(Name = "Basement Plan")]
        public IFormFile? BasementPlanImage { get; set; }

        [Display(Name = "Ground Floor Plan")]
        public IFormFile? GroundFloorPlanImage { get; set; }

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

        [Display(Name = "Property Images")]
        public List<IFormFile>? PropertyImages { get; set; }

 
        public string? PropertyImagePaths { get; set; }
        public string? FloorPlanImagePath { get; set; }
        public string? BasementPlanImagePath { get; set; }
        public string? GroundFloorPlanImagePath { get; set; }
    }
}
