using EstateEase.Models.Entities;

namespace EstateEase.Models.ViewModels
{
    public class PropertyDetailsViewModel
    {
        public Property Property { get; set; }
        public bool IsUserLoggedIn { get; set; }
    }
} 