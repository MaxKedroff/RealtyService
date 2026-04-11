using Newtonsoft.Json.Linq;
using ParsingService.Interfaces;
using ParsingService.Models;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text.Json;

namespace ParsingService.Parsers
{
    public class DomclickParser : IParser
    {
        private readonly ILogger<DomclickParser>? _logger;
        private readonly HttpClient _httpClient;

        public string ParserName => "Domclick";

        public DomclickParser(ILogger<DomclickParser>? logger = null)
        {
            _logger = logger;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            _httpClient.DefaultRequestHeaders.Add("Accept",
                "application/json, text/plain, */*");
        }

        public bool CanHandle(string source) =>
            source.Equals("domclick", StringComparison.OrdinalIgnoreCase);

        public async Task<ParsingResult> ParseAsync(ParsingOptions options)
        {
            var result = new ParsingResult
            {
                Source = ParserName,
                ParsedAt = DateTime.UtcNow,
                Metadata = new Dictionary<string, object>()
            };

            try
            {
                _logger?.LogInformation("Starting parsing Domclick with query: {Query}, max results: {MaxResults}",
                    options.Query, options.MaxResults);

                result.Properties = await FetchDomclickProperties(options);
                result.Metadata["TotalParsed"] = result.Properties.Count;

                _logger?.LogInformation("Successfully parsed {Count} properties from Domclick", result.Properties.Count);
            }
            catch (HttpRequestException ex)
            {
                _logger?.LogError(ex, "HTTP error while parsing Domclick");
                result.Metadata["Error"] = $"HTTP error: {ex.Message}";
                result.Metadata["ErrorType"] = "HttpRequestException";
                throw new ParsingException($"Failed to fetch data from Domclick API: {ex.Message}", ex);
            }
            catch (JsonException ex)
            {
                _logger?.LogError(ex, "JSON parsing error while parsing Domclick");
                result.Metadata["Error"] = $"JSON parsing error: {ex.Message}";
                result.Metadata["ErrorType"] = "JsonException";
                throw new ParsingException($"Failed to parse Domclick API response: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Unexpected error while parsing Domclick");
                result.Metadata["Error"] = $"Unexpected error: {ex.Message}";
                result.Metadata["ErrorType"] = "UnexpectedException";
                throw new ParsingException($"Unexpected error while parsing Domclick: {ex.Message}", ex);
            }

            return result;
        }

        private async Task<List<PropertyItem>> FetchDomclickProperties(ParsingOptions options)
        {
            var items = new List<PropertyItem>();
            
            var errors = new List<string>();

            try
            {
                var cities = new List<string> { "1d1463ae-c80f-4d19-9331-a1b68a85b553 20561", "0d475b79-88de-4054-818c-37d8f9d0d440 31090", "da44671f-bc1d-4ac8-994e-bba66d806f26 2299" };
                foreach (var cityId in cities)
                {
                    var offset = 0;
                    const int limit = 30;
                    var currentCount = 0;
                    var totalCount = 0;

                    while (true)
                    {
                        if (currentCount >= options.MaxResults)
                        {
                            _logger?.LogDebug("Reached max results limit: {MaxResults}", options.MaxResults);
                            break;
                        }

                        var apiUrl = BuildApiUrl(offset, limit, cityId);

                        _logger?.LogDebug("Fetching Domclick data with offset: {Offset}, limit: {Limit}", offset, limit);

                        var response = await MakeRequestWithRetryAsync(apiUrl);
                        if (response.ToString().Contains("Bad Request"))
                            break;

                        var json = JObject.Parse(response);

                        if (json["error"] != null)
                        {
                            var errorMessage = json["error"]?.ToString() ?? "Unknown API error";
                            _logger?.LogWarning("API returned error: {Error}", errorMessage);
                            errors.Add($"Offset {offset}: {errorMessage}");
                            break;
                        }

                        if (totalCount == 0)
                        {
                            totalCount = json["result"]?["pagination"]?["total"]?.Value<int>() ?? 0;
                            _logger?.LogInformation("Total properties available: {TotalCount}", totalCount);

                            if (totalCount == 0)
                            {
                                _logger?.LogWarning("No properties found");
                                break;
                            }
                        }

                        var flats = json["result"]?["items"];
                        if (flats == null || !flats.HasValues)
                        {
                            _logger?.LogDebug("No more items at offset {Offset}", offset);
                            break;
                        }

                        foreach (var flat in flats)
                        {
                            if (currentCount >= options.MaxResults)
                            {
                                break;
                            }

                            try
                            {
                                var item = ParsePropertyItem(flat);
                                if (item != null)
                                {
                                    items.Add(item);
                                    currentCount++;
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger?.LogError(ex, "Error parsing individual property at offset {Offset}", offset);
                                errors.Add($"Failed to parse property at offset {offset}: {ex.Message}");
                            }
                        }

                        if (currentCount >= totalCount || flats.Count() < limit)
                        {
                            _logger?.LogDebug("Reached end of data. Total parsed: {CurrentCount}", currentCount);
                            break;
                        }

                        offset += limit;

                        await Task.Delay(250);
                    }
                    totalCount = 0;
                }
                return items;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error in FetchDomclickProperties");
                throw;
            }
            finally
            {
                _logger?.LogInformation("Parsing completed. Success: {SuccessCount}, Errors: {ErrorCount}",
                    items.Count, errors.Count);
            }

            return items;
        }

        private string BuildApiUrl(int offset, int limit, string cityId)
        {
            var cityInfo = cityId.Split(" ");
            var baseUrl = "https://bff-search-web.domclick.ru/api/offers/v1";
            var parameters = new List<string>
            {
                $"address={cityInfo[0]}",
                $"offset={offset}",
                $"limit={limit}",
                "sort=qi",
                "sort_dir=desc",
                "deal_type=sale",
                "category=living",
                "offer_type=flat",
                "offer_type=layout",
                $"aids={cityInfo[1]}",
                "enable_mixed_old_index=1"
            };

            return $"{baseUrl}?{string.Join("&", parameters)}";
        }

        private async Task<string> MakeRequestWithRetryAsync(string url, int maxRetries = 10)
        {
            for (int retry = 0; retry < maxRetries; retry++)
            {
                try
                {
                    using var request = new HttpRequestMessage(HttpMethod.Get, url);
                    var response = await _httpClient.SendAsync(request);

                    if (response.IsSuccessStatusCode)
                    {
                        return await response.Content.ReadAsStringAsync();
                    }

                    _logger?.LogWarning("Request failed with status {StatusCode}. Retry {Retry}/{MaxRetries}",
                        response.StatusCode, retry + 1, maxRetries);

                    return await response.Content.ReadAsStringAsync();
                }
                catch (HttpRequestException ex)
                {
                    _logger?.LogWarning(ex, "HTTP request failed. Retry {Retry}/{MaxRetries}", retry + 1, maxRetries);

                    if (retry == maxRetries - 1)
                    {
                        throw;
                    }

                    await Task.Delay(1000 * (retry + 1));
                }
            }

            throw new HttpRequestException($"Failed to fetch data after {maxRetries} attempts");
        }

        private PropertyItem? ParsePropertyItem(JToken flat)
        {
            try
            {
                var item = new PropertyItem
                {
                    Id = flat["id"]?.ToString(),
                    DealType = flat["dealType"]?.ToString(),
                    PropertyType = flat["offerType"]?.ToString(),
                    Status = flat["status"]?.ToString(),
                    Price = flat["price"]?.Value<double>() ?? 0,
                    Url = flat["path"]?.ToString()
                };

                if (flat["address"] != null)
                {
                    item.Address = flat["address"]["displayName"]?.ToString();
                    if (item.Address.Contains("Россия") || item.Address.Contains("область"))
                        item.City = item.Address.Split(", ")[1];
                    else
                    {
                        item.City = item.Address.Split(", ")[0];
                    }
                }

                if (flat["location"] != null)
                {
                    var lat = flat["location"]["lat"]?.ToString();
                    var lon = flat["location"]["lon"]?.ToString();
                    if (lat != null && lon != null)
                    {
                        item.coords = $"{lat} {lon}";
                    }
                }

                if (flat["photos"] != null && flat["photos"].HasValues)
                {
                    var firstPhoto = flat["photos"][0];
                    if (firstPhoto?["url"] != null)
                    {
                        item.planUrl = "https://img.dmclk.ru/s1920x1080q80" + firstPhoto["url"]?.ToString();
                    }
                }

                if (flat["description"] != null)
                {
                    item.Description = flat["description"]?.ToString();
                }

                if (flat["complex"] != null)
                {
                    if (flat["generalInfo"] != null)
                    {
                        item.Area = flat["generalInfo"]["area"]?.Value<double>() ?? 0;
                        item.Rooms = flat["generalInfo"]["rooms"].Value<int>();
                        item.Floor = flat["generalInfo"]["minFloor"].Value<int>();
                    }

                    item.zkName = flat["complex"]["name"]?.ToString();

                    if (flat["complex"]["building"] != null)
                    {
                        var year = flat["complex"]["building"]["endBuildYear"]?.ToString() ?? "неизвестно";
                        var quarter = flat["complex"]["building"]["endBuildQuarter"]?.ToString();
                        item.EndOfBuilding = quarter != null ? $"{year} год, {quarter} квартал" : $"{year} год";
                    }
                }
                else if (flat["house"] != null)
                {
                    if (flat["objectInfo"] != null)
                    {
                        item.Area = flat["objectInfo"]["area"]?.Value<double>() ?? 0;
                        item.Rooms = flat["objectInfo"]["rooms"].Value<int>();
                        item.Floor = flat["objectInfo"]["floor"].Value<int>();
                    }

                    item.EndOfBuilding = $"{flat["house"]["buildYear"]?.ToString()} год";
                }

                if (item.Area > 0 && item.Price > 0)
                {
                    item.PPM = (double)item.Price / item.Area;
                }

                return item;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error parsing property item with ID: {Id}", flat["id"]?.ToString());
                return null;
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
