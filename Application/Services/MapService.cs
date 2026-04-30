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

        public async Task<IEnumerable<FlatDTO>> GetFlatsByFilter(FlatFilterDTO filterDTO)
        {
            var allExistingFlats = _context.Flats.Include(el => el.Building).Where(flat => flat.CityId == filterDTO.CityId);
            var query = allExistingFlats.AsQueryable();

            if (filterDTO.minArea > 0 || filterDTO.maxArea > 0)
            {
                query = query.Where(flat => flat.FlatArea >= filterDTO.minArea &&
                                            flat.FlatArea <= filterDTO.maxArea);
            }

            if (filterDTO.minRooms > 0 || filterDTO.maxRooms > 0)
            {
                query = query.Where(flat => flat.FlatRooms >= filterDTO.minRooms &&
                                            flat.FlatRooms <= filterDTO.maxRooms);
            }

            if (filterDTO.minFloor > 0 || filterDTO.maxFloor > 0)
            {
                query = query.Where(flat => flat.FlatFloor >= filterDTO.minFloor &&
                                            flat.FlatFloor <= filterDTO.maxFloor);
            }

            if (filterDTO.minPrice > 0 || filterDTO.maxPrice > 0)
            {
                query = query.Where(flat => flat.FlatPrice >= filterDTO.minPrice &&
                                            flat.FlatPrice <= filterDTO.maxPrice);
            }

            if (filterDTO.minSQM > 0 || filterDTO.maxSQM > 0)
            {
                query = query.Where(flat => flat.FlatPriceSQM >= filterDTO.minSQM &&
                                            flat.FlatPriceSQM <= filterDTO.maxSQM);
            }
            var flats = await query.ToListAsync();

            if (!string.IsNullOrWhiteSpace(filterDTO.GeoPoint))
            {
                var polygon = RayCastingService.ParseGeoPointString(filterDTO.GeoPoint);
                if (polygon.Count >= 3)
                {
                    flats = flats.Where(flat => RayCastingService.IsPointInPolygon(flat.Building.GeoPoint, polygon)).ToList();
                }
            }

            var result = flats.Take(filterDTO.Limit).Select(flat => new FlatDTO
            {
                Id = flat.FlatId,
                area = flat.FlatArea,
                rooms = flat.FlatRooms,
                floor = flat.FlatFloor,
                Price = flat.FlatPrice,
                SQM = flat.FlatPriceSQM,
                coords = flat.Building.GeoPoint
            });

            return result;
            
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

        public async Task<HeatMapDto> GetHeatMapData(Guid cityId)
        {
            var buildings = await _context.Buildings
                .Include(b => b.Flats.Where(f => f.CityId == cityId && f.IsActive))
                .Where(b => b.Flats.Any(f => f.CityId == cityId && f.IsActive))
                .ToListAsync();

            var heatMapData = new List<HeatMapObject>();

            foreach (var building in buildings)
            {
                if (building.Flats == null || !building.Flats.Any())
                    continue;

                var flatsList = building.Flats.ToList();

                int apartmentsCount = flatsList.Count;
                var prices = flatsList.Select(f => (decimal)f.FlatPrice).OrderBy(p => p).ToList();
                decimal medianPrice = CalculateMedian(prices);



                var heatMapObject = new HeatMapObject
                {
                    coordinates = building.GeoPoint,
                    FlatsCount = apartmentsCount,
                    YearBuilt = building.YearBuild,
                    MedianActualPrice = medianPrice
                };
                heatMapData.Add(heatMapObject);
            }
            return new HeatMapDto
            {
                Amount = heatMapData.Count(),
                HeatMapObjects = heatMapData
            };
        }

        private decimal CalculateMedian(List<decimal> values)
        {
            if (values == null || values.Count == 0)
                return 0;

            int count = values.Count;
            int middleIndex = count / 2;

            if (count % 2 == 0)
            {
                return (values[middleIndex - 1] + values[middleIndex]) / 2;
            }
            else
            {
                return values[middleIndex];
            }
        }
    }
}
