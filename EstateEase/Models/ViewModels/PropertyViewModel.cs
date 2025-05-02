using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace EstateEase.Models.ViewModels
{
    public class PropertyViewModel
    {
        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        [Required]
        public string Content { get; set; }

        [Required]
        [Display(Name = "Property Type")]
        public string PropertyType { get; set; }

        [Required]
        [Display(Name = "BHK")]
        public string BHK { get; set; }

        [Required]
        public int Bedrooms { get; set; }

        [Required]
        public int Bathrooms { get; set; }

        [Required]
        public int Balcony { get; set; }

        [Required]
        public int Kitchen { get; set; }

        [Required]
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
        [Display(Name = "Total Floors")]
        public int TotalFloors { get; set; }

        [Display(Name = "Featured Property")]
        public bool IsFeatured { get; set; }

        [Display(Name = "Main Image")]
        public IFormFile? MainImage { get; set; }

        [Display(Name = "Additional Images")]
        public List<IFormFile>? AdditionalImages { get; set; }

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

      
    }
}
