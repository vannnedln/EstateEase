using System;
using System.Collections.Generic;

namespace EstateEase.Models.ViewModels
{
    public class UserDashboardViewModel
    {
        public int OwnedPropertiesCount { get; set; }
        public int RentedPropertiesCount { get; set; }
        public int FavoritesCount { get; set; }
        public List<PropertySummaryViewModel> RecentProperties { get; set; } = new List<PropertySummaryViewModel>();
        public List<AppointmentSummaryViewModel> UpcomingAppointments { get; set; } = new List<AppointmentSummaryViewModel>();
        public List<OfferSummaryViewModel> RecentOffers { get; set; } = new List<OfferSummaryViewModel>();
    }

    public class PropertySummaryViewModel
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Address { get; set; }
        public decimal Price { get; set; }
        public string Type { get; set; }
        public string OwnershipType { get; set; }  // "Bought" or "Rented"
        public DateTime AcquisitionDate { get; set; }
        public string ImageUrl { get; set; }
    }

    public class AppointmentSummaryViewModel
    {
        public string Id { get; set; }
        public string PropertyId { get; set; }
        public string PropertyTitle { get; set; }
        public string PropertyAddress { get; set; }
        public DateTime AppointmentDate { get; set; }
        public string Status { get; set; }  // "Confirmed", "Pending", "Cancelled"
    }

    public class OfferSummaryViewModel
    {
        public string Id { get; set; }
        public string PropertyId { get; set; }
        public string PropertyTitle { get; set; }
        public decimal OfferAmount { get; set; }
        public DateTime OfferDate { get; set; }
        public string Status { get; set; }  // "Accepted", "Pending", "Rejected"
    }

    public class UserPropertiesViewModel
    {
        public List<UserPropertyViewModel> Properties { get; set; } = new List<UserPropertyViewModel>();
        public string OwnershipFilter { get; set; } = "All";
        public int TotalCount { get; set; }
        public int OwnedCount { get; set; }
        public int RentedCount { get; set; }
    }

    public class UserPropertyViewModel
    {
        public string Id { get; set; }
        public string PropertyId { get; set; }
        public string Title { get; set; }
        public string Address { get; set; }
        public decimal Price { get; set; }
        public string Type { get; set; }
        public int Bedrooms { get; set; }
        public int Bathrooms { get; set; }
        public decimal Size { get; set; }
        public string OwnershipType { get; set; }  // "Bought" or "Rented"
        public DateTime AcquisitionDate { get; set; }
        public DateTime? ExpiryDate { get; set; }  // For rentals
        public string Status { get; set; }
        public string ImageUrl { get; set; }
    }

    public class UserPropertyDetailViewModel : UserPropertyViewModel
    {
        public string Content { get; set; }
        public int Kitchen { get; set; }
        public int Balcony { get; set; }
        public int Hall { get; set; }
        public int TotalFloors { get; set; }
        public bool HasSwimmingPool { get; set; }
        public bool HasParking { get; set; }
        public bool HasGym { get; set; }
        public bool HasSecurity { get; set; }
        public bool HasElevator { get; set; }
        public bool HasCCTV { get; set; }
        public List<UserPropertyImageViewModel> Images { get; set; } = new List<UserPropertyImageViewModel>();
    }

    public class UserPropertyImageViewModel
    {
        public string Id { get; set; }
        public string ImagePath { get; set; }
        public string ImageType { get; set; }
        public bool IsMain { get; set; }
    }

    public class AppointmentsViewModel
    {
        public List<UserAppointmentViewModel> Appointments { get; set; } = new List<UserAppointmentViewModel>();
        public string StatusFilter { get; set; } = "All";
        public int TotalCount { get; set; }
        public int UpcomingCount { get; set; }
        public int PendingCount { get; set; }
        public int CompletedCount { get; set; }
        public int CancelledCount { get; set; }
    }

    public class UserAppointmentViewModel
    {
        public string Id { get; set; }
        public string PropertyId { get; set; }
        public string PropertyTitle { get; set; }
        public string PropertyAddress { get; set; }
        public string PropertyImage { get; set; }
        public decimal PropertyPrice { get; set; }
        public string PropertyType { get; set; }
        public int PropertyBedrooms { get; set; }
        public int PropertyBathrooms { get; set; }
        public decimal PropertySize { get; set; }
        public string AgentId { get; set; }
        public string AgentName { get; set; }
        public string AgentEmail { get; set; }
        public string AgentPhone { get; set; }
        public DateTime AppointmentDate { get; set; }
        public string Status { get; set; }  // "Confirmed", "Pending", "Completed", "Cancelled"
        public string Notes { get; set; }
        public DateTime CreatedAt { get; set; }
    }
} 