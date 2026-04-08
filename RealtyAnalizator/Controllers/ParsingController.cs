using Infrastructure.Sync;
using Microsoft.AspNetCore.Mvc;
using ParsingService;
using ParsingService.Models;
using ParsingService.Services;

namespace RealtyAnalizator.Controllers
{
    // Api/Controllers/ParsingController.cs
    [ApiController]
    [Route("api/[controller]")]
    public class ParsingController : ControllerBase
    {
        private readonly IParserFactory _parserFactory;
        private readonly ISyncService _syncService;
        private readonly IExportService _exportService;

        public ParsingController(
            IParserFactory parserFactory,
            ISyncService syncService,
            IExportService exportService)
        {
            _parserFactory = parserFactory;
            _exportService = exportService;
            _syncService = syncService;
        }

        [HttpGet("parse/{source}")]
        public async Task<IActionResult> Parse(
            string source,
            [FromQuery] int maxResults = 1000000)
        {
            try
            {
                var parser = _parserFactory.GetParser(source);

                var options = new ParsingOptions
                {
                    Query = "",
                    MaxResults = maxResults
                };

                var result = await parser.ParseAsync(options);

                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest($"Parsing error: {ex.Message}");
            }
        }

        [HttpGet("parse/{source}/json")]
        public async Task<IActionResult> ParseToJson(
            string source,
            [FromQuery] int maxResults = 1000000)
        {
            try
            {
                var parser = _parserFactory.GetParser(source);

                var options = new ParsingOptions
                {
                    Query = "",
                    MaxResults = maxResults
                };

                var result = await parser.ParseAsync(options);
                var json = await _exportService.ExportToJsonAsync(result);

                return Content(json, "application/json");
            }
            catch (Exception ex)
            {
                return BadRequest($"Error: {ex.Message}");
            }
        }

        [HttpGet("parse/{source}/csv")]
        public async Task<IActionResult> ParseToCsv(
            string source,
            [FromQuery] int maxResults = 1000000)
        {
            try
            {
                var parser = _parserFactory.GetParser(source);

                var options = new ParsingOptions
                {
                    Query = "",
                    MaxResults = maxResults
                };

                var result = await parser.ParseAsync(options);
                var csvData = await _exportService.ExportToCsvAsync(result);

                return File(
                    csvData,
                    "text/csv",
                    $"{source}_properties_{DateTime.Now:yyyyMMddHHmmss}.csv"
                );
            }
            catch (Exception ex)
            {
                return BadRequest($"Error: {ex.Message}");
            }
        }

        [HttpGet("sources")]
        public IActionResult GetAvailableSources()
        {
            var sources = _parserFactory.GetAllParsers()
                .Select(p => p.ParserName);

            return Ok(sources);
        }

        [HttpPost("sync/{source}")]
        public async Task<IActionResult> SyncSource(
            string source,
            [FromQuery] int maxResults = 1000)
        {
            try
            {

                var result = await _syncService.SyncAsync(source, maxResults);

                return Ok(new
                {
                    success = true,
                    message = $"Synchronization for {source} completed successfully",
                    data = new
                    {
                        result.Source,
                        result.StartTime,
                        result.EndTime,
                        result.Duration,
                        result.NewFlats,
                        result.UpdatedFlats,
                        result.NewBuildings,
                        result.UpdatedBuildings,
                        result.DeactivatedFlats,
                        result.SkippedFlats,
                        result.TotalProcessed,
                        result.Errors
                    }
                });
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { success = false, error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        [HttpPost("sync/all")]
        public async Task<IActionResult> SyncAllSources([FromQuery] int maxResultsPerSource = 1000)
        {
            try
            {

                var result = await _syncService.SyncAllAsync(maxResultsPerSource);

                return Ok(new
                {
                    success = true,
                    message = "Synchronization for all sources completed",
                    data = new
                    {
                        result.StartTime,
                        result.EndTime,
                        result.Duration,
                        result.NewFlats,
                        result.UpdatedFlats,
                        result.NewBuildings,
                        result.UpdatedBuildings,
                        result.DeactivatedFlats,
                        result.SkippedFlats,
                        result.TotalProcessed,
                        result.Errors,
                        result.Metadata
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }


        [HttpGet("sync/status")]
        public IActionResult GetSyncStatus()
        {
            return Ok(new
            {
                available_sources = _parserFactory.GetAllParsers().Select(p => p.ParserName),
                sync_endpoints = new
                {
                    single_source = "POST /api/parsing/sync/{source}?maxResults=1000",
                    all_sources = "POST /api/parsing/sync/all?maxResultsPerSource=1000"
                }
            });
        }
    }
}
