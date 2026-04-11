using Infrastructure.Sync;
using Microsoft.AspNetCore.Mvc;
using ParsingService;
using ParsingService.Models;
using ParsingService.Services;

namespace RealtyAnalizator.Controllers
{
    // Api/Controllers/ParsingController.cs
    [ApiController]
    [Route("api/v1/parsing")]
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
