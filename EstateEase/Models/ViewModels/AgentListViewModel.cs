using System;

namespace EstateEase.Models.ViewModels
{
    public class AgentListViewModel
    {
        public string Id { get; set; }
        public string UserId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string ProfilePictureUrl { get; set; }
        public string LicenseNumber { get; set; }
        public int PropertyCount { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string FullName => $"{FirstName} {LastName}";
    }
} 