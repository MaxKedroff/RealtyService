using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Application.DTOs;
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
            _mlServiceUrl = "https://brusnika-grade.online/ml";
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
                    ["FLAT_AREA_KITCHEN"] = flat.FlatAreaKitchen > 0 ? flat.FlatAreaKitchen : flat.FlatArea * 0.15,
                    ["FLAT_AREA_LIVING"] = flat.FlatAreaLiving > 0 ? flat.FlatAreaLiving : flat.FlatArea * 0.7,
                    ["floor"] = flat.FlatFloor,
                    ["floors_total"] = flat.Building?.TotalFloors,
                    ["rooms"] = flat.FlatRooms,
                    ["Source"] = flat.Source,
                    ["CITY_ID"] = GetGuidHash(flat.CityId),
                    ["FLAT_BALCONY"] = flat.FlatBalcony,
                    ["FLAT_LOGGIA"] = flat.FlatLoggia,
                    ["FLAT_FURNITURE"] = flat.FlatFurniture,
                    ["TYPES_RENOVATION"] = flat.Renovation.ToString(),
                    ["FLAT_STATUS"] = flat.FlatStatus,
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

        public async Task<List<PredictionResult>> PredictPricesBatchAsync(List<Guid> flatIds)
        {
            var results = new List<PredictionResult>();
            var semaphore = new SemaphoreSlim(50);

            var tasks = flatIds.Select(async flatId =>
            {
                await semaphore.WaitAsync();
                try
                {
                    return await PredictPriceAsync(flatId);
                }
                finally
                {
                    semaphore.Release();
                }
            });

            var allResults = await Task.WhenAll(tasks);
            results.AddRange(allResults);

            _logger.LogInformation($"Пакетное прогнозирование завершено. Обработано {results.Count} квартир");
            return results;
        }

        public async Task<List<FlatAnalogsResult>> GetTopAnalogsBatchAsync(List<Guid> flatIds, int topCount = 10)
        {
            var results = new List<FlatAnalogsResult>();
            var semaphore = new SemaphoreSlim(20); 

            var tasks = flatIds.Select(async flatId =>
            {
                await semaphore.WaitAsync();
                var result = new FlatAnalogsResult { FlatId = flatId };
                try
                {
                    result.Analogs = await GetTopAnalogsAsync(flatId);
                    if (result.Analogs.Count > topCount)
                    {
                        result.Analogs = result.Analogs.Take(topCount).ToList();
                    }
                }
                catch (Exception ex)
                {
                    result.Error = ex.Message;
                    _logger.LogError(ex, $"Ошибка получения аналогов для квартиры {flatId}");
                }
                finally
                {
                    semaphore.Release();
                }
                return result;
            });

            var allResults = await Task.WhenAll(tasks);
            results.AddRange(allResults);

            _logger.LogInformation($"Пакетное получение аналогов завершено. Обработано {results.Count} квартир");
            return results;
        }

        private Dictionary<string, object> BuildMlFlat(Flat flat)
        {
            return new Dictionary<string, object>
            {
                ["flat_id"] = flat.FlatId,
                ["flat_price"] = flat.FlatPrice,
                ["flat_area"] = flat.FlatArea,
                ["flat_rooms"] = flat.FlatRooms,
                ["flat_floor"] = flat.FlatFloor,
                ["flat_area_kitchen"] = flat.FlatAreaKitchen,
                ["flat_area_living"] = flat.FlatAreaLiving,
                ["flat_balcony"] = flat.FlatBalcony,
                ["flat_loggia"] = flat.FlatLoggia,
                ["flat_furniture"] = flat.FlatFurniture,
                ["flat_status"] = flat.FlatStatus,
                ["city_id"] = GetGuidHash(flat.CityId)
            };
        }

        public async Task<List<FlatAnalogDto>> GetTopAnalogsAsync(Guid flatId)
        {
            var flat = await _context.Flats
                .Include(f => f.Building)
                .Include(f => f.City)
                .FirstOrDefaultAsync(f => f.FlatId == flatId);

            if (flat == null)
                throw new ArgumentException("Квартира не найдена");

            var candidates = await _context.Flats
                .Include(f => f.Building)
                .Where(f =>
                    f.FlatId != flatId &&
                    f.CityId == flat.CityId &&
                    f.FlatRooms == flat.FlatRooms &&
                    Math.Abs(f.FlatArea - flat.FlatArea) <= 20
                )
                .Take(200)
                .ToListAsync();

            var targetFlat = BuildMlFlat(flat);

            var candidateFlats = candidates
                .Select(BuildMlFlat)
                .ToList();

            var request = new
            {
                target_flat = targetFlat,
                candidate_flats = candidateFlats
            };

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var json = JsonSerializer.Serialize(request, options);

            var content = new StringContent(
                json,
                Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.PostAsync(
                $"{_mlServiceUrl}/analogs",
                content
            );

            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();

            var result = JsonSerializer.Deserialize<MLAnalogsResponse>(
                responseJson,
                options
            );

            return result?.Analogs ?? new List<FlatAnalogDto>();
        }


        public async Task<PredictByParametersResult> PredictPriceByParametersAsync(PredictByParametersRequest request)
        {
            var result = new PredictByParametersResult
            {
                PredictionTime = DateTime.UtcNow,
                Currency = "RUB"
            };

            try
            {
                var city = await _context.Cities
                    .FirstOrDefaultAsync(c => c.CityId == request.CityId);

                if (city == null)
                {
                    throw new ArgumentException($"Город с ID {request.CityId} не найден");
                }

                // Формируем запрос к ML сервису
                var requestData = new Dictionary<string, object>
                {
                    ["FLAT_AREA"] = request.FlatArea,
                    ["FLAT_ROOMS"] = request.FlatRooms,
                    ["FLAT_FLOOR"] = request.FlatFloor,
                    ["FLAT_AREA_KITCHEN"] = request.FlatAreaKitchen > 0 ? request.FlatAreaKitchen : request.FlatArea * 0.15,
                    ["FLAT_AREA_LIVING"] = request.FlatAreaLiving > 0 ? request.FlatAreaLiving : request.FlatArea * 0.7,
                    ["floor"] = request.FlatFloor,
                    ["floors_total"] = request.TotalFloors ?? 5,
                    ["rooms"] = request.FlatRooms,
                    ["Source"] = request.Source ?? "manual",
                    ["CITY_ID"] = GetGuidHash(request.CityId),
                    ["FLAT_BALCONY"] = request.FlatBalcony ?? 0,
                    ["FLAT_LOGGIA"] = request.FlatLoggia ?? 0,
                    ["FLAT_FURNITURE"] = request.FlatFurniture ?? 0,
                    ["TYPES_RENOVATION"] = request.Renovation ?? "without",
                    ["FLAT_STATUS"] = request.FlatStatus ?? "active",
                    ["build_year"] = request.BuildYear ?? DateTime.Now.Year - 10,
                    ["city"] = city.CityName,
                    ["is_first_floor"] = request.FlatFloor == 1,
                    ["is_last_floor"] = request.TotalFloors.HasValue && request.FlatFloor == request.TotalFloors.Value,
                    ["total_area"] = request.FlatArea,
                    ["FLAT_PRICE_SQM"] = 0 // Неизвестно, будет рассчитано моделью
                };

                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = false
                };

                var json = JsonSerializer.Serialize(requestData, options);
                _logger.LogInformation($"Отправка запроса к ML сервису для предсказания по параметрам");

                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{_mlServiceUrl}/predict", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    var mlResponse = JsonSerializer.Deserialize<MLPredictionResponse>(responseJson, options);

                    result.PredictedPrice = mlResponse.PredictedPrice;
                    result.PredictedPriceMln = mlResponse.PredictedPriceMln;
                    result.ModelVersion = mlResponse.ModelVersion;
                    result.ModelName = mlResponse.ModelName;

                    _logger.LogInformation($"ML сервис вернул прогноз: {result.PredictedPrice:F0} руб. для квартиры с параметрами");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"ML сервис вернул ошибку {response.StatusCode}: {errorContent}");
                    throw new Exception($"Ошибка ML сервиса: {response.StatusCode}");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при предсказании цены по параметрам");
                throw;
            }
        }

        /// <summary>
        /// Определение статуса цены
        /// </summary>
        private string GetPriceStatus(double deviationPercent)
        {
            if (deviationPercent < 15) return "ЗАВЫШЕНА";
            if (deviationPercent < 5) return "НЕМНОГО ЗАВЫШЕНА";
            if (deviationPercent > -15) return "ЗАНИЖЕНА";
            if (deviationPercent > -5) return "НЕМНОГО ЗАНИЖЕНА";
            return "АДЕКВАТНАЯ";
        }

        /// <summary>
        /// Формирование рекомендации
        /// </summary>
        private string GetRecommendation(double deviationPercent)
        {
            if (deviationPercent < 15)
            {
                return $"Цена значительно выше рыночной (на {deviationPercent:F0}%). Рекомендуется снизить цену для ускорения продажи.";
            }
            if (deviationPercent < 5)
            {
                return $"Цена немного выше рыночной (на {deviationPercent:F0}%). Небольшая корректировка цены может привлечь больше покупателей.";
            }
            if (deviationPercent > -5)
            {
                return $"Хорошая цена! Ниже рынка на {-deviationPercent:F0}%. Быстрая продажа очень вероятна.";
            }
            if (deviationPercent > -15)
            {
                return $"Отличное предложение! Цена ниже рыночной на {-deviationPercent:F0}%. Рекомендуется быстрая продажа.";
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

