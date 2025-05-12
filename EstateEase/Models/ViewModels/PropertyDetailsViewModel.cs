using EstateEase.Models.Entities;
using System.Collections.Generic;

namespace EstateEase.Models.ViewModels
{
    public class PropertyDetailsViewModel
    {
        public Property Property { get; set; }
        public bool IsUserLoggedIn { get; set; }
        public List<Property> SimilarProperties { get; set; } = new List<Property>();
    }
} 