using System.ComponentModel.DataAnnotations;

namespace EstateEase.Models.ViewModels
{
    public class DeleteAgentViewModel
    {
        public string Id { get; set; }
        
        public string UserId { get; set; }
        
        [Display(Name = "Full Name")]
        public string FullName { get; set; }
        
        [Display(Name = "Email")]
        public string Email { get; set; }
        
        public string ProfilePictureUrl { get; set; }
        
        [Display(Name = "Confirm Deletion")]
        public bool ConfirmDeletion { get; set; }
    }
} 