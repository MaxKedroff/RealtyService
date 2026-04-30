using Application.DTOs;
using Application.Interfaces;
using Core.Models;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public class PolygonService : IPolygonService
    {
        private AppDbContext _context;

        public PolygonService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<PolygonDto> CreateNewPolygonAsync(CreateUpdatePolygonDto createDto)
        {
            var favourite = new Favourite
            {
                FavouriteId = Guid.NewGuid(),
                Color = createDto.color,
                GeoPoints = createDto.GeoPoints,
                CreateDate = DateTime.UtcNow,
                ParametersJson = null
            };

            await _context.Favourites.AddAsync(favourite);
            await _context.SaveChangesAsync();

            return new PolygonDto
            {
                id = favourite.FavouriteId,
                color = favourite.Color,
                GeoPoints = favourite.GeoPoints,
                Parameters = null 
            };
        }

        public async Task<bool> DeletePolygonAsync(Guid id)
        {
            var favourite = await _context.Favourites.FindAsync(id);

            if (favourite == null)
            {
                return false;
            }

            _context.Favourites.Remove(favourite);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<SavedPolygonsDto> GetAllSavedPolygonsAsync()
        {
            var polygons = await _context.Favourites
                .OrderByDescending(f => f.CreateDate)
                .ToListAsync();

            var polygonDtos = polygons.Select(f => new PolygonDto
            {
                id = f.FavouriteId,
                color = f.Color,
                GeoPoints = f.GeoPoints,
                Parameters = null
            }).ToList();

            return new SavedPolygonsDto
            {
                Amount = polygonDtos.Count,
                SavedPolygons = polygonDtos
            };
        }

        public async Task<PolygonDto> UpdatePolygonAsync(Guid id, CreateUpdatePolygonDto updateDto)
        {
            var favourite = await _context.Favourites.FindAsync(id);

            if (favourite == null)
            {
                throw new ArgumentException($"Polygon with id {id} not found");
            }

            // Update properties
            if (!string.IsNullOrEmpty(updateDto.color))
            {
                favourite.Color = updateDto.color;
            }

            if (!string.IsNullOrEmpty(updateDto.GeoPoints))
            {
                favourite.GeoPoints = updateDto.GeoPoints;
            }

            await _context.SaveChangesAsync();

            return new PolygonDto
            {
                id = favourite.FavouriteId,
                color = favourite.Color,
                GeoPoints = favourite.GeoPoints,
                Parameters = null
            };
        }
    }
}
