using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EstateEase.Models.Entities
{
    public class AddProperty
    {
        [Key]
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
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
        public decimal Size { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Size must be greater than 0")]
        [Display(Name = "Area Size (sq m)")]
        public decimal Price { get; set; }

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

        public string? PropertyImagePaths { get; set; }  // Changed from AdditionalImagePaths
        public string? FloorPlanImagePath { get; set; }
        public string? BasementPlanImagePath { get; set; }
        public string? GroundFloorPlanImagePath { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

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
    }
}