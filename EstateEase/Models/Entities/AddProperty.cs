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
        public string PropertyType { get; set; }

        [Required]
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
        [Column(TypeName = "decimal(18,2)")]
        public decimal Size { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [Required]
        public string Address { get; set; }

        [Required]
        public string Status { get; set; }

        [Required]
        public int TotalFloors { get; set; }

        public bool IsFeatured { get; set; }

        public string? MainImagePath { get; set; }

        public string? FloorPlanImagePath { get; set; }

        public string? BasementPlanImagePath { get; set; }

        public string? GroundFloorPlanImagePath { get; set; }

        public string? AdditionalImagePaths { get; set; }  // Change from List<string> to string

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        [Required]
        public string SellingType { get; set; }

        public bool HasSwimmingPool { get; set; }

        public bool HasParking { get; set; }

        public bool HasGym { get; set; }

        public bool HasSecurity { get; set; }

        public bool HasElevator { get; set; }

        public bool HasCCTV { get; set; }
    }
}