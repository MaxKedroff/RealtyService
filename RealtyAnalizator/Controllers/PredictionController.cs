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
    }
}
