using System;
using System.ComponentModel.DataAnnotations;

namespace EstateEase.Models.ViewModels
{
    public class PropertySearchViewModel
    {
        public string PropertyType { get; set; }
        public string SellingType { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public int? Bedrooms { get; set; }
        public int? Bathrooms { get; set; }
        public string Location { get; set; }
        public string SortBy { get; set; } = "date_desc"; // Default sort by newest
        
        // For pagination
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 12;
    }
} 