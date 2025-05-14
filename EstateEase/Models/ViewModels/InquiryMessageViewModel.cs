using System;

namespace EstateEase.Models.ViewModels
{
    public class InquiryMessageViewModel
    {
        public int Id { get; set; }
        public int InquiryId { get; set; }
        public string SenderId { get; set; } = null!;
        public string SenderType { get; set; } = null!; // "User", "Agent", "Admin"
        public string SenderName { get; set; } = null!;
        public string Message { get; set; } = null!;
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
        
        // Helper property to determine if the message is from the current user
        public bool IsFromCurrentUser { get; set; }
    }
} 