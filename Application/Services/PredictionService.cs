using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Core.Models;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Services
{
    public class PredictionService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<PredictionService> _logger;
        private readonly HttpClient _httpClient;

        private readonly string _mlServiceUrl;
        private bool _mlServiceAvailable;

        public PredictionService(
            AppDbContext context,
            ILogger<PredictionService> logger, IHttpClientFactory httpClientFactory = null)
        {
            _context = context;
            _logger = logger;
            _httpClient = httpClientFactory?.CreateClient() ?? new HttpClient();
            _mlServiceUrl = Environment.GetEnvironmentVariable("ML_SERVICE_URL") ?? "http://127.0.0.1:5000";
            _mlServiceAvailable = false;
            Task.Run(async () => await CheckMlServiceHealth());
        }

        private async Task CheckMlServiceHealth()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_mlServiceUrl}/health");
                if (response.IsSuccessStatusCode)
                {
                    _mlServiceAvailable = true;
                    _logger.LogInformation($"ML сервис доступен по адресу: {_mlServiceUrl}");
                }
                else
                {
                    _logger.LogWarning($"ML сервис недоступен. Будет использоваться упрощенный расчет.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Не удалось подключиться к ML сервису: {ex.Message}. Будет использоваться упрощенный расчет.");
                _mlServiceAvailable = false;
            }
        }

        public async Task<PredictionResult> PredictPriceAsync(Guid flatId)
        {
            var result = new PredictionResult
            {
                PredictionTime = DateTime.UtcNow
            };

            try
            {
                var flat = await _context.Flats
                    .Include(f => f.Building)
                    .Include(f => f.City)
                    .FirstOrDefaultAsync(f => f.FlatId == flatId);

                if (flat == null)
                {
                    throw new ArgumentException($"Квартира с ID {flatId} не найдена");
                }

                result.ActualPrice = flat.FlatPrice;
                var mlResponse = await PredictWithMlService(flat);
                if (mlResponse == null)
                {
                    _logger.LogError($"Ошибка при прогнозировании цены для квартиры {flatId}");
                    result.Status = "ERROR";
                    result.PredictedPrice = result.ActualPrice;
                    result.PredictedPriceMln = result.ActualPrice / 1000000;
                    return result;
                }
                double predictedPrice = mlResponse.PredictedPrice;
                

                result.PredictedPrice = predictedPrice;
                result.PredictedPriceMln = predictedPrice / 1000000;
                result.DeviationPercent = ((predictedPrice - flat.FlatPrice) / flat.FlatPrice) * 100;
                result.Status = GetPriceStatus(result.DeviationPercent);
                result.Recommendation = GetRecommendation(result.DeviationPercent);

                _logger.LogInformation($"Прогноз для квартиры {flatId}: предсказано {predictedPrice:F0}, " +
                                      $"реально {flat.FlatPrice:F0}, отклонение {result.DeviationPercent:F1}%, " +
                                      $"ML сервис: {(_mlServiceAvailable ? "доступен" : "недоступен")}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при прогнозировании цены для квартиры {flatId}");
                result.Status = "ERROR";
                result.PredictedPrice = result.ActualPrice;
                result.PredictedPriceMln = result.ActualPrice / 1000000;
            }
            return result;
        }

        private long GetGuidHash(Guid guid)
{
    var bytes = guid.ToByteArray();
    return BitConverter.ToInt64(bytes, 0) ^ BitConverter.ToInt64(bytes, 8);
}
        

        /// <summary>
        /// Прогнозирование с использованием модели
        /// </summary>
        private async Task<MLPredictionResponse> PredictWithMlService(Flat flat)
        {
            try
            {
                var requestData = new Dictionary<string, object>
                {
                    ["flat_id"] = flat.FlatId.ToString(),
                    ["actual_price"] = flat.FlatPrice,
                    ["FLAT_AREA"] = flat.FlatArea,
                    ["FLAT_ROOMS"] = flat.FlatRooms,
                    ["FLAT_FLOOR"] = flat.FlatFloor,
                    ["FLAT_PRICE"] = flat.FlatPrice,
                    ["FLAT_PRICE_SQM"] = flat.FlatPriceSQM,
                    ["total_area"] = flat.FlatArea,
                    ["kitchen_area"] = flat.FlatAreaKitchen > 0 ? flat.FlatAreaKitchen : flat.FlatArea * 0.15,
                    ["living_area"] = flat.FlatAreaLiving > 0 ? flat.FlatAreaLiving : flat.FlatArea * 0.7,
                    ["floor"] = flat.FlatFloor,
                    ["floors_total"] = flat.Building?.TotalFloors,
                    ["rooms"] = flat.FlatRooms,
                    ["Source"] = flat.Source,
                    ["CITY_ID"] = GetGuidHash(flat.CityId),
                    ["balcony"] = flat.FlatBalcony,
                    ["loggia"] = flat.FlatLoggia,
                    ["renovation"] = flat.Renovation.ToString(),
                    ["build_year"] = flat.Building?.YearBuild.Year,
                    ["city"] = flat.City?.CityName,
                    ["address"] = flat.Building?.Address,
                    ["is_first_floor"] = flat.FlatFloor == 1,
                    ["is_last_floor"] = flat.Building != null && flat.FlatFloor == flat.Building.TotalFloors
                };

                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = false
                };

                var json = JsonSerializer.Serialize(requestData, options);
                _logger.LogInformation($"Отправка запроса к ML сервису: {json}");

                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{_mlServiceUrl}/predict", content);
                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    var mlResponse = JsonSerializer.Deserialize<MLPredictionResponse>(responseJson, options);
                    _logger.LogInformation($"ML сервис вернул прогноз: {mlResponse?.PredictedPrice:F0} руб.");
                    return mlResponse;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning($"ML сервис вернул ошибку {response.StatusCode}: {errorContent}");
                    _mlServiceAvailable = false;
                    return null;
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Ошибка HTTP запроса к ML сервису");
                _mlServiceAvailable = false;
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обращении к ML сервису");
                return null;
            }
        }

        /// <summary>
        /// Определение статуса цены
        /// </summary>
        private string GetPriceStatus(double deviationPercent)
        {
            if (deviationPercent > 15) return "ЗАВЫШЕНА";
            if (deviationPercent > 5) return "НЕМНОГО ЗАВЫШЕНА";
            if (deviationPercent < -15) return "ЗАНИЖЕНА";
            if (deviationPercent < -5) return "НЕМНОГО ЗАНИЖЕНА";
            return "АДЕКВАТНАЯ";
        }

        /// <summary>
        /// Формирование рекомендации
        /// </summary>
        private string GetRecommendation(double deviationPercent)
        {
            if (deviationPercent > 15)
            {
                return $"Цена значительно выше рыночной (на {deviationPercent:F0}%). Рекомендуется снизить цену для ускорения продажи.";
            }
            if (deviationPercent > 5)
            {
                return $"Цена немного выше рыночной (на {deviationPercent:F0}%). Небольшая корректировка цены может привлечь больше покупателей.";
            }
            if (deviationPercent < -15)
            {
                return $"Отличное предложение! Цена ниже рыночной на {-deviationPercent:F0}%. Рекомендуется быстрая продажа.";
            }
            if (deviationPercent < -5)
            {
                return $"Хорошая цена! Ниже рынка на {-deviationPercent:F0}%. Быстрая продажа очень вероятна.";
            }
            return "Цена соответствует рынку. Хорошее предложение.";
        }

        /// <summary>
        /// Метод для ручного обновления статуса ML сервиса
        /// </summary>
        public async Task RefreshMlServiceStatus()
        {
            await CheckMlServiceHealth();
        }
    }
}

#region DTO для взаимодействия с ML сервисом

/// <summary>
/// Запрос к ML сервису
/// </summary>
public class MLPredictionRequest
{
    // Идентификатор
    public string FlatId { get; set; }
    public double? ActualPrice { get; set; }

    // Основные характеристики квартиры
    public double TotalArea { get; set; }
    public double LivingArea { get; set; }
    public double KitchenArea { get; set; }

    // Параметры этажа и комнат
    public int Floor { get; set; }
    public int FloorsTotal { get; set; }
    public int Rooms { get; set; }

    // Дополнительные параметры
    public int Balcony { get; set; }
    public int Loggia { get; set; }
    public string Renovation { get; set; }
    public int Furniture { get; set; }

    // Параметры дома
    public int BuildYear { get; set; }
    public string HouseType { get; set; }
    public string HouseMaterial { get; set; }
    public bool HasParking { get; set; }
    public bool HasElevator { get; set; }
    public bool HasGarbageChute { get; set; }

    // Локация
    public string City { get; set; }
    public string District { get; set; }
    public string Address { get; set; }

    // Вычисляемые параметры
    public bool IsFirstFloor { get; set; }
    public bool IsLastFloor { get; set; }
    public double FloorRatio { get; set; }

    // Дополнительная информация
    public string Source { get; set; }
    public bool IsActive { get; set; }
    public int PublishedDays { get; set; }
}

/// <summary>
/// Ответ от ML сервиса
/// </summary>
public class MLPredictionResponse
{
    [JsonPropertyName("predicted_price")]
    public double PredictedPrice { get; set; }

    [JsonPropertyName("predicted_price_mln")]
    public double PredictedPriceMln { get; set; }

    [JsonPropertyName("currency")]
    public string Currency { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }

    [JsonPropertyName("model_version")]
    public string ModelVersion { get; set; }

    [JsonPropertyName("model_name")]
    public string ModelName { get; set; }

    [JsonPropertyName("actual_price")]
    public double? ActualPrice { get; set; }

    [JsonPropertyName("deviation")]
    public double? Deviation { get; set; }

    [JsonPropertyName("deviation_percent")]
    public double? DeviationPercent { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; }

    [JsonPropertyName("recommendation")]
    public string Recommendation { get; set; }
}

#endregion

#region Результат предсказания

public class PredictionResult
{
    public DateTime PredictionTime { get; set; }
    public double ActualPrice { get; set; }
    public double PredictedPrice { get; set; }
    public double PredictedPriceMln { get; set; }
    public double DeviationPercent { get; set; }
    public string Status { get; set; }
    public string Recommendation { get; set; }
    public double Confidence { get; set; }
    public string ModelVersion { get; set; }
}

#endregion