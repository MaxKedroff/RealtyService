using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.DTOs;
using BenchmarkDotNet.Loggers;
using Core.Models;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.ML;
using Microsoft.ML.Data;
using Newtonsoft.Json;

namespace Application.Services
{
    public class PredictionService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<PredictionService> _logger;

        private MLContext _mlContext;
        private ITransformer _model;
        private PredictionEngine<FlatPredictionData, PricePrediction> _predictionEngine;

        private Dictionary<string, object> _modelConfig;
        private Dictionary<string, double> _featureImportance;

        private readonly AnalogSearchConfig _analogConfig;


        public PredictionService(
            AppDbContext context,
            ILogger<PredictionService> logger)
        {
            _context = context;
            _logger = logger;
            _mlContext = new MLContext(seed: 42);
            _analogConfig = new AnalogSearchConfig();

            InitializeAsync().GetAwaiter().GetResult();
        }

        private async Task InitializeAsync()
        {
            try
            {
                var basePath = AppDomain.CurrentDomain.BaseDirectory;

                var modelsPath = Path.GetFullPath(Path.Combine(
                    basePath,
                    "..", "..", "..",  
                    "..", "Application", "ml_models"
                ));

                if (!Directory.Exists(modelsPath))
                {
                    _logger.LogWarning($"Папка с моделями не найдена: {modelsPath}");
                    return;
                }

                var configPath = Path.Combine(modelsPath, "model_config.json");
                if (File.Exists(configPath))
                {
                    var configJson = await File.ReadAllTextAsync(configPath);
                    _modelConfig = JsonConvert.DeserializeObject<Dictionary<string, object>>(configJson);
                    _logger.LogInformation("Конфигурация модели загружена");
                }

                var importancePath = Path.Combine(modelsPath, "feature_importance.csv");
                if (File.Exists(importancePath))
                {
                    _featureImportance = await LoadFeatureImportance(importancePath);
                    _logger.LogInformation("Важность признаков загружена");
                }

                var modelPath = Path.Combine(modelsPath, "price_prediction_model.zip");
                if (File.Exists(modelPath))
                {
                    _model = _mlContext.Model.Load(modelPath, out _);
                    _predictionEngine = _mlContext.Model.CreatePredictionEngine<FlatPredictionData, PricePrediction>(_model);
                    _logger.LogInformation("ML модель загружена");
                }
                else
                {
                    _logger.LogWarning("ML модель не найдена, будет использоваться упрощенный метод");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при инициализации PredictionService");
            }
        }

        private string GetModelsPath()
        {
            var currentFileDirectory = Path.GetDirectoryName(typeof(PredictionService).Assembly.Location);

            var projectRoot = Path.Combine(currentFileDirectory, "..");

            // Путь к папке с моделями
            return Path.GetFullPath(Path.Combine(projectRoot, "ml_models"));
        }

        private async Task<Dictionary<string, double>> LoadFeatureImportance(string path)
        {
            var importance = new Dictionary<string, double>();

            try
            {
                using var reader = new StreamReader(path);
                string line;
                bool isFirst = true;

                while ((line = await reader.ReadLineAsync()) != null)
                {
                    if (isFirst)
                    {
                        isFirst = false;
                        continue;
                    }

                    var parts = line.Split(',');
                    if (parts.Length >= 2 && double.TryParse(parts[1], out double imp))
                    {
                        importance[parts[0]] = imp;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка загрузки важности признаков");
            }

            return importance;
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
                var input = PreparePredictionInput(flat);
                var predictedPrice = await PredictWithModel(input);
                result.PredictedPrice = predictedPrice;
                result.PredictedPriceMln = predictedPrice / 1000000;
                result.DeviationPercent = ((predictedPrice - flat.FlatPrice) / flat.FlatPrice) * 100;
                result.Status = GetPriceStatus(result.DeviationPercent);
                result.Recommendation = GetRecommendation(result.DeviationPercent);
                result.SimilarFlats = await FindSimilarFlatsInDatabase(flat, 5);
                if (_featureImportance != null)
                {
                    result.FeatureImportance = _featureImportance;
                }
                result.Confidence = CalculateConfidence(result.SimilarFlats, result.DeviationPercent);
                if (_modelConfig != null && _modelConfig.ContainsKey("version"))
                {
                    result.ModelVersion = _modelConfig["version"]?.ToString() ?? "1.0.0";
                }

                _logger.LogInformation($"Прогноз для квартиры {flatId}: предсказано {predictedPrice:F0}, " +
                                      $"реально {flat.FlatPrice:F0}, отклонение {result.DeviationPercent:F1}%, " +
                                      $"найдено аналогов: {result.SimilarFlats.Count}");
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

        /// <summary>
        /// Подготовка входных данных для модели
        /// </summary>
        private PredictionInput PreparePredictionInput(Flat flat)
        {
            var input = new PredictionInput
            {
                Area = flat.FlatArea,
                Rooms = flat.FlatRooms,
                Floor = flat.FlatFloor,
                BuildYear = flat.Building?.YearBuild,
                PropertyType = "flat",
                District = flat.City?.CityName ?? "unknown",
                DealType = "sale",
                Description = $"Квартира площадью {flat.FlatArea} кв.м, {flat.FlatRooms} комнат",
                Address = flat.Building?.Address ?? ""
            };

            return input;
        }

        /// <summary>
        /// Прогнозирование с использованием модели
        /// </summary>
        private async Task<double> PredictWithModel(PredictionInput input)
        {
            var predictionData = new FlatPredictionData
            {
                Area = (float)input.Area,
                Rooms = input.Rooms,
                Floor = input.Floor,
                BuildYear = input.BuildYear,
                PropertyType = input.PropertyType,
                HouseType = input.HouseType,
                District = input.District,
                DealType = input.DealType
            };

            var prediction = _predictionEngine.Predict(predictionData);
            return prediction.Price;
        }

        /// <summary>
        /// Определение статуса цены
        /// </summary>
        private string GetPriceStatus(double deviationPercent)
        {
            if (deviationPercent > 10) return "ЗАВЫШЕНА";
            if (deviationPercent < -10) return "ЗАНИЖЕНА";
            return "АДЕКВАТНАЯ";
        }

        /// <summary>
        /// Формирование рекомендации
        /// </summary>
        private string GetRecommendation(double deviationPercent)
        {
            if (deviationPercent > 10)
            {
                return "Цена выше рыночной. Рекомендуется снизить на " +
                       $"{deviationPercent:F0}% для ускорения продажи.";
            }
            if (deviationPercent < -10)
            {
                return $"Цена ниже рыночной на {-deviationPercent:F0}%. " +
                       "Отличное предложение для быстрой продажи!";
            }
            return "Цена соответствует рынку. Хорошее предложение.";
        }

        /// <summary>
        /// Вычисление уверенности прогноза
        /// </summary>
        private double CalculateConfidence(List<SimilarFlat> similarFlats, double deviationPercent)
        {
            double confidence = 0.7;

            if (similarFlats.Count >= 3)
            {
                confidence += 0.15;
            }

            if (Math.Abs(deviationPercent) < 10)
            {
                confidence += 0.1;
            }

            return Math.Min(0.95, confidence);
        }

        private async Task<List<SimilarFlat>> FindSimilarFlatsInDatabase(Flat targetFlat, int count)
        {
            var similarFlats = new List<SimilarFlat>();

            try
            {
                var query = _context.Flats
                    .Include(f => f.Building)
                    .Include(f => f.City)
                    .Where(f => f.FlatId != targetFlat.FlatId && f.IsActive)
                    .AsQueryable();

                var pricePerSqm = targetFlat.FlatPrice / targetFlat.FlatArea;
                var pricePerSqmMin = pricePerSqm * 0.7;
                var pricePerSqmMax = pricePerSqm * 1.3;

                query = query.Where(f =>
                    (f.FlatPrice / f.FlatArea) >= pricePerSqmMin &&
                    (f.FlatPrice / f.FlatArea) <= pricePerSqmMax);

                var areaMin = targetFlat.FlatArea * 0.7;
                var areaMax = targetFlat.FlatArea * 1.3;
                query = query.Where(f => f.FlatArea >= areaMin && f.FlatArea <= areaMax);

                var roomsMin = Math.Max(1, targetFlat.FlatRooms - 1);
                var roomsMax = targetFlat.FlatRooms + 1;
                query = query.Where(f => f.FlatRooms >= roomsMin && f.FlatRooms <= roomsMax);

                if (targetFlat.CityId != Guid.Empty)
                {
                    query = query.Where(f => f.CityId == targetFlat.CityId);
                }

                var candidates = await query
                    .Take(50)
                    .ToListAsync();

                _logger.LogDebug($"Найдено {candidates.Count} кандидатов для сравнения с квартирой {targetFlat.FlatId}");

                foreach (var candidate in candidates)
                {
                    double similarityScore = CalculateDetailedSimilarityScore(targetFlat, candidate);
                    if (similarityScore >= _analogConfig.MinSimilarityThreshold)
                    {
                        similarFlats.Add(new SimilarFlat
                        {
                            FlatId = candidate.FlatId,
                            Price = candidate.FlatPrice,
                            Area = candidate.FlatArea,
                            Rooms = candidate.FlatRooms,
                            Floor = candidate.FlatFloor,
                            SimilarityScore = similarityScore,
                            Address = candidate.Building?.Address ?? "",
                            PricePerSqm = candidate.FlatPrice / candidate.FlatArea,
                            BuildingYear = candidate.Building?.YearBuild,
                            DistanceToMetro = candidate.Building?.MetroDistance
                        });

                        similarFlats = similarFlats
                        .OrderByDescending(f => f.SimilarityScore)
                        .Take(count)
                        .ToList();

                        _logger.LogInformation($"Для квартиры {targetFlat.FlatId} найдено {similarFlats.Count} аналогов из {candidates.Count} кандидатов");

                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при поиске похожих квартир в БД");
            }
            return similarFlats;
        }

        /// <summary>
        /// Детальное вычисление score сходства между квартирами
        /// </summary>
        private double CalculateDetailedSimilarityScore(Flat target, Flat candidate)
        {
            double totalScore = 0;
            double totalWeight = 0;

            double areaDiff = 1 - Math.Min(1, Math.Abs(target.FlatArea - candidate.FlatArea) / Math.Max(target.FlatArea, 1));
            totalScore += areaDiff * 0.25;
            totalWeight += 0.25;

            double roomsDiff = 1 - Math.Min(1, Math.Abs(target.FlatRooms - candidate.FlatRooms) / 5.0);
            totalScore += roomsDiff * 0.15;
            totalWeight += 0.15;

            double floorScore = CalculateFloorSimilarity(target, candidate);
            totalScore += floorScore * 0.10;
            totalWeight += 0.10;

            double targetPriceSqm = target.FlatPrice / target.FlatArea;
            double candidatePriceSqm = candidate.FlatPrice / candidate.FlatArea;
            double priceDiff = 1 - Math.Min(1, Math.Abs(targetPriceSqm - candidatePriceSqm) / Math.Max(targetPriceSqm, 1));
            totalScore += priceDiff * 0.30;
            totalWeight += 0.30;

            

            double balconyScore = (target.FlatBalcony == candidate.FlatBalcony) ? 1.0 : 0.5;
            totalScore += balconyScore * 0.05;
            totalWeight += 0.05;

            double renovationScore = (target.Renovation == candidate.Renovation) ? 1.0 : 0.6;
            totalScore += renovationScore * 0.05;
            totalWeight += 0.05;

            return totalWeight > 0 ? totalScore / totalWeight : 0;
        }

        /// <summary>
        /// Вычисление сходства по этажу
        /// </summary>
        private double CalculateFloorSimilarity(Flat target, Flat candidate)
        {
            if (target.FlatFloor == candidate.FlatFloor)
                return 1.0;

            int targetFloorType = GetFloorType(target.FlatFloor, target.Building?.TotalFloors ?? 10);
            int candidateFloorType = GetFloorType(candidate.FlatFloor, candidate.Building?.TotalFloors ?? 10);

            if (targetFloorType == candidateFloorType)
                return 0.8;

            return 0.4;
        }

        /// <summary>
        /// Определение типа этажа
        /// </summary>
        private int GetFloorType(int floor, int totalFloors)
        {
            if (floor == 1) return 1;           
            if (floor == totalFloors) return 2; 
            return 3;                        
        }

    }
}

#region Вспомогательные классы

public class FlatPredictionData
{
    public float Area { get; set; }
    public int Rooms { get; set; }
    public int Floor { get; set; }
    public DateTime? BuildYear { get; set; }
    public string PropertyType { get; set; }
    public string HouseType { get; set; }
    public string District { get; set; }
    public string DealType { get; set; }
}

public class PricePrediction
{
    [ColumnName("Score")]
    public double Price { get; set; }
}

public class AnalogSearchConfig
{
    public double MinSimilarityThreshold { get; set; } = 0.5;
    public int MaxCandidatesCount { get; set; } = 50;
    public double AreaTolerancePercent { get; set; } = 0.3;
    public double PricePerSqmTolerancePercent { get; set; } = 0.3;
}



public class FlatDeal
{
    public Guid FlatId { get; set; }
    public double ActualPrice { get; set; }
    public double PredictedPrice { get; set; }
    public double DeviationPercent { get; set; }
    public string Address { get; set; } = "";
    public double Area { get; set; }
    public int Rooms { get; set; }
    public double SavingsAmount { get; set; }
}

#endregion
