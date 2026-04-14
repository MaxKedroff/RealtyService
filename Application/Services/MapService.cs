using Application.DTOs;
using Application.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public class MapService : IMapService
    {

        private AppDbContext _context;

        public MapService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<GetBuildingsResultDTO> GetBuildingsAsync(Guid cityId, int page, int pageSize)
        {
            var query = _context.Buildings
            .Where(b => b.Flats.Any(f => f.CityId == cityId && f.IsActive));

            var totalCount = await query.CountAsync();

            var buildings = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(b => new BuildingDTO
                {
                    BuildingId = b.BuildingId,
                    GeoPoint = b.GeoPoint,
                    Address = b.Address,
                    YearBuild = b.YearBuild,
                    FlatsCount = b.Flats.Count()
                }).ToListAsync();

            return new GetBuildingsResultDTO
            {
                Amount = totalCount,
                Buildings = buildings
            };
        }

        public async Task<IEnumerable<FlatDTO>> GetFlatsInBuildingAsync(Guid cityId, Guid buildingsId)
        {
            var building = await _context.Buildings
                .Include(b => b.Flats)
                .FirstOrDefaultAsync(el => el.BuildingId == buildingsId);

            if (building == null)
                return Enumerable.Empty<FlatDTO>(); 

            if (building.Flats == null)
                return Enumerable.Empty<FlatDTO>();

            return building.Flats.Where(flat => flat.IsActive).Select(el => new FlatDTO
            {
                Id = el.FlatId,
                rooms = el.FlatRooms,
                area = el.FlatArea,
                floor = el.FlatFloor,
                Price = el.FlatPrice,
                SQM = el.FlatPriceSQM,
                PictureUrl = el.PictureUrl,
                Source = el.Source
            }).ToList();
        }
    }
}
