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
        private readonly IExportService _exportService;

        public ParsingController(
            IParserFactory parserFactory,
            IExportService exportService)
        {
            _parserFactory = parserFactory;
            _exportService = exportService;
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
    }
}
