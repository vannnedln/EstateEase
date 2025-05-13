using EstateEase.Data;
using EstateEase.Models.Entities;
using EstateEase.Models.ViewModels;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EstateEase.Services
{
    public class PropertyService : IPropertyService
    {
        private readonly ApplicationDbContext _context;

        public PropertyService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Property>> GetPropertiesAsync()
        {
            return await _context.Properties
                .Include(p => p.Agent)
                .Include(p => p.PropertyImages)
                .ToListAsync();
        }

        public async Task<Property> GetPropertyByIdAsync(int id)
        {
            string stringId = id.ToString();
            return await _context.Properties
                .Include(p => p.Agent)
                .Include(p => p.PropertyImages)
                .FirstOrDefaultAsync(p => p.Id == stringId);
        }

        public async Task<IEnumerable<Property>> GetPropertiesByAgentIdAsync(string agentId)
        {
            return await _context.Properties
                .Include(p => p.PropertyImages)
                .Where(p => p.AgentId == agentId)
                .ToListAsync();
        }

        public async Task<IEnumerable<Property>> SearchPropertiesAsync(PropertySearchViewModel searchModel)
        {
            var query = _context.Properties
                .Include(p => p.Agent)
                .Include(p => p.PropertyImages)
                .AsQueryable();

            // Apply filters based on search model
            if (!string.IsNullOrEmpty(searchModel.PropertyType))
            {
                query = query.Where(p => p.PropertyType == searchModel.PropertyType);
            }

            if (!string.IsNullOrEmpty(searchModel.SellingType))
            {
                query = query.Where(p => p.SellingType == searchModel.SellingType);
            }

            if (searchModel.MinPrice.HasValue)
            {
                query = query.Where(p => p.Price >= searchModel.MinPrice.Value);
            }

            if (searchModel.MaxPrice.HasValue)
            {
                query = query.Where(p => p.Price <= searchModel.MaxPrice.Value);
            }

            if (searchModel.Bedrooms.HasValue)
            {
                query = query.Where(p => p.Bedrooms >= searchModel.Bedrooms.Value);
            }

            if (searchModel.Bathrooms.HasValue)
            {
                query = query.Where(p => p.Bathrooms >= searchModel.Bathrooms.Value);
            }

            if (!string.IsNullOrEmpty(searchModel.Location))
            {
                query = query.Where(p => p.Address.Contains(searchModel.Location));
            }

            // Apply sorting
            switch (searchModel.SortBy)
            {
                case "price_asc":
                    query = query.OrderBy(p => p.Price);
                    break;
                case "price_desc":
                    query = query.OrderByDescending(p => p.Price);
                    break;
                case "date_desc":
                    query = query.OrderByDescending(p => p.CreatedAt);
                    break;
                case "date_asc":
                    query = query.OrderBy(p => p.CreatedAt);
                    break;
                default:
                    query = query.OrderByDescending(p => p.CreatedAt);
                    break;
            }

            return await query.ToListAsync();
        }

        public async Task<bool> AddPropertyAsync(Property property)
        {
            try
            {
                _context.Properties.Add(property);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> UpdatePropertyAsync(Property property)
        {
            try
            {
                _context.Properties.Update(property);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> DeletePropertyAsync(int id)
        {
            try
            {
                string stringId = id.ToString();
                var property = await _context.Properties.FindAsync(stringId);
                if (property == null)
                    return false;

                _context.Properties.Remove(property);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
} 