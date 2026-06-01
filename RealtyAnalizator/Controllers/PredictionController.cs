using Application.DTOs;
using Application.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace RealtyAnalizator.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class PredictionController : ControllerBase
    {
        private readonly PredictionService _predictionService;

        private readonly ILogger<PredictionController> _logger;

        public PredictionController(
            PredictionService predictionService,
            ILogger<PredictionController> logger)
        {
            _predictionService = predictionService;
            _logger = logger;
        }

        [HttpGet("flat/{flatId}")]
        [ProducesResponseType(typeof(PredictionResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> PredictPrice(Guid flatId)
        {
            try
            {
                _logger.LogInformation($"Запрос прогноза для квартиры {flatId}");

                var result = await _predictionService.PredictPriceAsync(flatId);

                if (result.Status == "ERROR")
                {
                    return StatusCode(500, result);
                }

                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex.Message);
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при обработке запроса для квартиры {flatId}");
                return StatusCode(500, new { error = "Внутренняя ошибка сервера" });
            }
        }

        [HttpGet("flat/{flatId}/analogs")]
        public async Task<IActionResult> GetAnalogs(Guid flatId)
        {
            try
            {
                var result = await _predictionService.GetTopAnalogsAsync(flatId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка получения аналогов");

                return StatusCode(500, new
                {
                    error = ex.Message
                });
            }
        }

        [HttpGet("health")]
        public async Task<IActionResult> Health()
        {
            await _predictionService.RefreshMlServiceStatus();
            return Ok(new { status = "ok", timestamp = DateTime.UtcNow });
        }

        [HttpPost("flats/batch")]
        [ProducesResponseType(typeof(BatchPredictionResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> PredictPricesBatch([FromBody] BatchPredictionRequest request)
        {
            try
            {
                if (request?.FlatIds == null || request.FlatIds.Count == 0)
                {
                    return BadRequest(new { error = "Не указаны идентификаторы квартир" });
                }

                if (request.FlatIds.Count > 100)
                {
                    return BadRequest(new { error = "Максимальное количество квартир для одновременного прогнозирования - 100" });
                }

                _logger.LogInformation($"Запрос пакетного прогноза для {request.FlatIds.Count} квартир");

                var results = await _predictionService.PredictPricesBatchAsync(request.FlatIds);

                var batchResult = new BatchPredictionResult
                {
                    TotalProcessed = results.Count,
                    Successful = results.Count(r => r.Status != "ERROR"),
                    Failed = results.Count(r => r.Status == "ERROR"),
                    Results = results,
                    ProcessingTime = DateTime.UtcNow
                };

                return Ok(batchResult);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex.Message);
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при пакетном прогнозировании");
                return StatusCode(500, new { error = "Внутренняя ошибка сервера" });
            }
        }

        [HttpPost("flats/analogs/batch")]
        [ProducesResponseType(typeof(BatchAnalogsResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAnalogsBatch([FromBody] BatchAnalogsRequest request)
        {
            try
            {
                if (request?.FlatIds == null || request.FlatIds.Count == 0)
                {
                    return BadRequest(new { error = "Не указаны идентификаторы квартир" });
                }

                if (request.FlatIds.Count > 50)
                {
                    return BadRequest(new { error = "Максимальное количество квартир для получения аналогов - 50" });
                }

                _logger.LogInformation($"Запрос пакетного получения аналогов для {request.FlatIds.Count} квартир");

                var results = await _predictionService.GetTopAnalogsBatchAsync(request.FlatIds, request.TopCount ?? 10);

                var batchResult = new BatchAnalogsResult
                {
                    TotalProcessed = results.Count,
                    Successful = results.Count(r => r.Analogs != null),
                    Failed = results.Count(r => r.Analogs == null),
                    Results = results,
                    ProcessingTime = DateTime.UtcNow
                };

                return Ok(batchResult);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex.Message);
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при пакетном получении аналогов");
                return StatusCode(500, new { error = "Внутренняя ошибка сервера" });
            }
        }
    }
}
