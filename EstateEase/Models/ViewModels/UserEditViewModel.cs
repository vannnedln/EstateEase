using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace EstateEase.Models.ViewModels
{
    public class UserEditViewModel
    {
        public string Id { get; set; }
        
        [StringLength(50, ErrorMessage = "First name cannot be longer than 50 characters")]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }
        
        [StringLength(50, ErrorMessage = "Last name cannot be longer than 50 characters")]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }
        
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [Display(Name = "Email")]
        public string Email { get; set; }
        
        [Phone(ErrorMessage = "Invalid phone number")]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }
        
        [Display(Name = "Date of Birth")]
        [DataType(DataType.Date)]
        public DateTime? Birthday { get; set; }
        
        [StringLength(100, ErrorMessage = "Address cannot be longer than 100 characters")]
        [Display(Name = "Address")]
        public string Address { get; set; }
        
        [StringLength(50, ErrorMessage = "Barangay cannot be longer than 50 characters")]
        [Display(Name = "Barangay")]
        public string Barangay { get; set; }
        
        [StringLength(50, ErrorMessage = "City cannot be longer than 50 characters")]
        [Display(Name = "City")]
        public string City { get; set; }
        
        [StringLength(20, ErrorMessage = "Postal code cannot be longer than 20 characters")]
        [Display(Name = "Postal Code")]
        public string PostalCode { get; set; }
        
        [StringLength(50, ErrorMessage = "Country cannot be longer than 50 characters")]
        [Display(Name = "Country")]
        public string Country { get; set; }
        
        // This is intentionally NOT required - users can keep their existing profile picture
        [Display(Name = "Profile Picture")]
        public IFormFile ProfilePicture { get; set; }
        
        public string CurrentProfilePictureUrl { get; set; }
        
        // Computed property for full name
        public string FullName => $"{FirstName} {LastName}";
    }
} 