using EstateEase.Models.Entities;
using EstateEase.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EstateEase.Services
{
    public interface IPropertyService
    {
        Task<IEnumerable<Property>> GetPropertiesAsync();
        Task<Property> GetPropertyByIdAsync(int id);
        Task<IEnumerable<Property>> GetPropertiesByAgentIdAsync(string agentId);
        Task<IEnumerable<Property>> SearchPropertiesAsync(PropertySearchViewModel searchModel);
        Task<bool> AddPropertyAsync(Property property);
        Task<bool> UpdatePropertyAsync(Property property);
        Task<bool> DeletePropertyAsync(int id);
    }
} 