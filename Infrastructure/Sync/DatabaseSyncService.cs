using Core.Models;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ParsingService;
using ParsingService.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Infrastructure.Sync
{
    public class DatabaseSyncService : ISyncService
    {
        private readonly IParserFactory _parserFactory;
        private readonly AppDbContext _context;
        private readonly ILogger<DatabaseSyncService> _logger;
        private readonly Dictionary<string, City> _citiesCache;

        public DatabaseSyncService(
            IParserFactory parserFactory,
            AppDbContext context,
            ILogger<DatabaseSyncService> logger)
        {
            _parserFactory = parserFactory;
            _context = context;
            _logger = logger;
            _citiesCache = new Dictionary<string, City>();
        }

        private async void UploadDefaultCitiesIfNotExist()
        {
            var city = await _context.Cities.FirstOrDefaultAsync(c => c.CityName == "Москва" || c.CityName == "Омск");
            if (city == null)
            {
                var defaultCities = new List<City>
                {
                    new City
                    {
                        CityId = Guid.NewGuid(),
                        CityName = "Москва",
                        CreateDate = DateTime.UtcNow,
                        UpdateDate = DateTime.UtcNow
                    },
                    new City
                    {
                        CityId = Guid.NewGuid(),
                        CityName = "Омск",
                        CreateDate = DateTime.UtcNow,
                        UpdateDate = DateTime.UtcNow
                    },
                    new City
                    {
                        CityId = Guid.NewGuid(),
                        CityName = "Екатеринбург",
                        CreateDate = DateTime.UtcNow,
                        UpdateDate = DateTime.UtcNow
                    },
                };
                _context.Cities.AddRange(defaultCities);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<SyncResult> SyncAllAsync(int maxResultsPerSource = 1000)
        {
            var allSources = _parserFactory.GetAllParsers().Select(p => p.ParserName);
            var combinedResult = new SyncResult
            {
                Source = "ALL",
                StartTime = DateTime.UtcNow,
                TotalProcessed = 0
            };

            foreach (var source in allSources)
            {
                try
                {
                    var sourceResult = await SyncAsync(source, maxResultsPerSource);

                    combinedResult.NewFlats += sourceResult.NewFlats;
                    combinedResult.UpdatedFlats += sourceResult.UpdatedFlats;
                    combinedResult.NewBuildings += sourceResult.NewBuildings;
                    combinedResult.UpdatedBuildings += sourceResult.UpdatedBuildings;
                    combinedResult.DeactivatedFlats += sourceResult.DeactivatedFlats;
                    combinedResult.SkippedFlats += sourceResult.SkippedFlats;
                    combinedResult.TotalProcessed += sourceResult.TotalProcessed;
                    combinedResult.Errors.AddRange(sourceResult.Errors);

                    combinedResult.Metadata[$"{source}_duration"] = sourceResult.Duration;
                    combinedResult.Metadata[$"{source}_new_flats"] = sourceResult.NewFlats;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error syncing source {Source}", source);
                    combinedResult.Errors.Add($"Source {source}: {ex.Message}");
                }
            }
            combinedResult.EndTime = DateTime.UtcNow;
            combinedResult.Duration = combinedResult.EndTime - combinedResult.StartTime;
            return combinedResult;
        }

        public async Task<SyncResult> SyncAsync(string source, int maxResults = 1000)
        {
            var result = new SyncResult
            {
                Source = source,
                StartTime = DateTime.UtcNow,
                TotalProcessed = 0
            };

            try
            {
                _logger.LogInformation("Starting synchronization for source: {Source}", source);
                //UploadDefaultCitiesIfNotExist();
                var parser = _parserFactory.GetParser(source);

                var options = new ParsingOptions
                {
                    Query = "",
                    MaxResults = maxResults
                };

                var parsingResult = await parser.ParseAsync(options);

                if (parsingResult.Properties == null || !parsingResult.Properties.Any())
                {
                    _logger.LogWarning("No properties parsed from {Source}", source);
                    result.Errors.Add("No properties returned from parser");
                    return result;
                }

                _logger.LogInformation("Parsed {Count} properties from {Source}",
                    parsingResult.Properties.Count, source);


                var existingFlats = await _context.Flats
                    .Where(f => f.Source == $"{source}")
                    .ToDictionaryAsync(f => f.ExternalId, f => f);

                var existingBuildings = await _context.Buildings
                    .ToDictionaryAsync(b => b.Address, b => b);

                foreach (var property in parsingResult.Properties)
                {
                    try
                    {
                        await ProcessProperty(property, source, existingFlats, existingBuildings, result);
                        result.TotalProcessed++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing property with ID {PropertyId}", property.Id);
                        result.Errors.Add($"Property {property.Id}: {ex.Message}");
                    }
                }
                await DeactivateOldFlatsAsync(source, result);

                await _context.SaveChangesAsync();

                result.EndTime = DateTime.UtcNow;
                result.Duration = result.EndTime - result.StartTime;

                _logger.LogInformation(
                    "Synchronization for {Source} completed. New flats: {NewFlats}, " +
                    "Deactivated flats: {DeactivatedFlats}, New buildings: {NewBuildings}, " +
                    "Duration: {Duration}",
                    source, result.NewFlats, result.DeactivatedFlats,
                    result.NewBuildings, result.Duration);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fatal error during synchronization for {Source}", source);
                result.Errors.Add($"Fatal error: {ex.Message}");
                result.EndTime = DateTime.UtcNow;
                throw;
            }
        }

        private async Task ProcessProperty(
            PropertyItem property,
            string source,
            Dictionary<string, Flat> existingFlats,
            Dictionary<string, Building> existingBuildings,
            SyncResult result)
        {
            if (string.IsNullOrEmpty(property.Id))
            {
                _logger.LogWarning("Property has no ID, skipping");
                result.SkippedFlats++;
                return;
            }

            var building = await ProcessBuilding(property, existingBuildings, result);
            if (building == null)
            {
                _logger.LogWarning("Could not process building for property {PropertyId}", property.Id);
                result.SkippedFlats++;
                return;
            }

            if (existingFlats.TryGetValue(property.Id, out var existingFlat))
            {
                var wasUpdated = UpdateExistingFlat(existingFlat, property, building);
                if (wasUpdated)
                {
                    result.UpdatedFlats++;
                    _logger.LogDebug("Updated flat with external ID {ExternalId}", property.Id);
                }
                else
                {
                    result.SkippedFlats++;
                }
            }
            else
            {
                var flat = CreateFlatFromProperty(property, source, building);
                await _context.Flats.AddAsync(flat);
                result.NewFlats++;
                _logger.LogDebug("Added new flat with external ID {ExternalId}", property.Id);
            }
        }

        private async Task<Building?> ProcessBuilding(
            PropertyItem property,
            Dictionary<string,Building> existingBuildings,
            SyncResult result)
        {
            if (string.IsNullOrEmpty(property.Address))
            {
                _logger.LogWarning("Property {PropertyId} has no address", property.Id);
                return null;
            }
            if (existingBuildings.TryGetValue(property.Address, out var existingBuilding))
            {
                var wasUpdated = UpdateExistingBuilding(existingBuilding, property);
                if (wasUpdated)
                {
                    result.UpdatedBuildings++;
                }
                return existingBuilding;
            }

            var building = CreateBuildingFromProperty(property);
            await _context.Buildings.AddAsync(building);
            existingBuildings[property.Address] = building;
            result.NewBuildings++;
            _logger.LogDebug("Added new building at address {Address}", property.Address);
            return building;
        }

        private City GetCityByName(string city)
        {
            return _context.Cities
                .FirstOrDefault(el => el.CityName == city);
        }

        private Flat CreateFlatFromProperty(
            PropertyItem property,
            string source,
            Building building)
        {
            var publishedDate = property.PublishedDate ?? DateTime.UtcNow;
            var isActive = (DateTime.UtcNow - publishedDate).TotalDays <= 40;

            var city = GetCityByName(property.City);
            return new Flat
            {
                FlatId = Guid.NewGuid(),
                FlatArea = property.Area,
                FlatAreaLiving = 0,
                FlatAreaKitchen = 0,
                FlatRooms = property.Rooms,
                FlatFloor = property.Floor,
                FlatBalcony = false,
                FlatLoggia = false,
                Renovation = TypesRenovation.Черновая,
                FlatPrice = property.Price,
                FlatPriceSQM = property.PPM,
                FlatStatus = Status.Продается,
                FlatPublished = publishedDate,
                FlatUnpublished = isActive ? null : DateTime.UtcNow,
                FlatFurniture = false,
                PictureUrl = property.planUrl,
                Source = source,
                ExternalId = property.Id,
                IsActive = isActive,
                BuildingId = building.BuildingId,
                Building = building,
                CityId = city.CityId,
                City = city
            };

        }

        private Building CreateBuildingFromProperty(PropertyItem property)
        {
            var yearBuild = ParseYearFromString(property.EndOfBuilding);
            var isNew = yearBuild.Year >= DateTime.UtcNow.Year - 5;

            return new Building
            {
                BuildingId = Guid.NewGuid(),
                Address = property.Address ?? "Unknown Address",
                GeoPoint = property.coords ?? "0 0",
                YearBuild = yearBuild,
                WallMaterial = WallMaterial.Кирпично_монолитный,
                TotalFloors = 0,
                IsNew = isNew,
                MetroDistance = null,
                ExternalId = property.Id,
            };
        }

        private bool UpdateExistingFlat(Flat existingFlat, PropertyItem property, Building building)
        {
            bool updated = false;

            if (property.Price != existingFlat.FlatPrice)
            {
                existingFlat.FlatPrice = property.Price;
                updated = true;
            }

            if (property.PPM != existingFlat.FlatPriceSQM)
            {
                existingFlat.FlatPriceSQM = property.PPM;
                updated = true;
            }

            var shouldBeActive = (DateTime.UtcNow - existingFlat.FlatPublished).TotalDays <= 40;
            if (existingFlat.IsActive != shouldBeActive)
            {
                existingFlat.IsActive = shouldBeActive;
                if (!shouldBeActive)
                {
                    existingFlat.FlatUnpublished = DateTime.UtcNow;
                }
                updated = true;
            }

            if (!string.IsNullOrEmpty(property.planUrl) &&
                existingFlat.PictureUrl != property.planUrl)
            {
                existingFlat.PictureUrl = property.planUrl;
                updated = true;
            }
            return updated;
        }

        private bool UpdateExistingBuilding(Building existingBuilding, PropertyItem property)
        {
            bool updated = false;
            if (!string.IsNullOrEmpty(property.coords) &&
                existingBuilding.GeoPoint != property.coords)
            {
                existingBuilding.GeoPoint = property.coords;
                updated = true;
            }

            if (!string.IsNullOrEmpty(property.EndOfBuilding))
            {
                var parsedYear = ParseYearFromString(property.EndOfBuilding);
                if (parsedYear.Year > 1900 && existingBuilding.YearBuild.Year != parsedYear.Year)
                {
                    existingBuilding.YearBuild = parsedYear;
                    existingBuilding.IsNew = parsedYear.Year >= DateTime.UtcNow.Year - 5;
                    updated = true;
                }
            }

            return updated;
        }

        private async Task DeactivateOldFlatsAsync(string source, SyncResult result)
        {
            var deactivationDate = DateTime.UtcNow.AddDays(-40);
            var flatsToDeactivate = await _context.Flats
                .Where(f => f.Source == source &&
                        f.IsActive &&
                        f.FlatPublished <= deactivationDate).ToListAsync();

            foreach (var flat in flatsToDeactivate)
            {
                flat.IsActive = false;
                flat.FlatUnpublished = DateTime.UtcNow;
                result.DeactivatedFlats++;
            }

            if (flatsToDeactivate.Any())
            {
                _logger.LogInformation("Deactivated {Count} old flats for source {Source}",
                    flatsToDeactivate.Count, source);
            }
        }

        private DateTime ParseYearFromString(string? yearString)
        {
            if (string.IsNullOrEmpty(yearString))
                return DateTime.UtcNow;

            var match = Regex.Match(yearString, @"(\d{4})");
            if (match.Success && int.TryParse(match.Groups[1].Value, out int year))
            {
                try
                {
                    return new DateTime(year, 1, 1);
                }
                catch
                {
                    return DateTime.UtcNow;
                }
            }

            return DateTime.UtcNow;
        }
    }
}
