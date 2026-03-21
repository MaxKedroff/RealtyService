using Newtonsoft.Json.Linq;
using ParsingService.Interfaces;
using ParsingService.Models;
using System.Drawing;

namespace ParsingService.Parsers
{
    public class DomclickParser : IParser
    {
        public string ParserName => "Domclick";

        public bool CanHandle(string source) =>
            source.Equals("domclick", StringComparison.OrdinalIgnoreCase);

        public async Task<ParsingResult> ParseAsync(ParsingOptions options)
        {
            var result = new ParsingResult
            {
                Source = ParserName,
                ParsedAt = DateTime.UtcNow
            };

            result.Properties = await FetchDomclickProperties(options);

            return result;
        }

        private async Task<List<PropertyItem>> FetchDomclickProperties(ParsingOptions options)
        {
            HttpClient client = new HttpClient();

            var offset = 0;
            var totalCount = 0;
            var currentCount = 0;
            var items = new List<PropertyItem>();
            while (true)
            {
                var apiUrl = $"https://bff-search-web.domclick.ru/api/offers/v1?address=0d475b79-88de-4054-818c-37d8f9d0d440&offset={offset}&limit=30&sort=qi&sort_dir=desc&deal_type=sale&category=living&offer_type=flat&offer_type=layout&aids=20561&enable_mixed_old_index=1";
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, apiUrl);
                HttpResponseMessage response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                var json = JObject.Parse(responseBody);
                totalCount = int.Parse(json["result"]["pagination"]["total"].ToString());
                if (currentCount == totalCount)
                {
                    return items;
                }
                var flats = json["result"]["items"];

                foreach (var flat in flats)
                {
                    if (currentCount > options.MaxResults)
                    {
                        return items;
                    }
                    var item = new PropertyItem
                    {
                        Id = flat["id"].ToString(),
                        DealType = flat["dealType"].ToString(),
                        PropertyType = flat["offerType"].ToString(),
                        Status = flat["status"].ToString(),
                        Price = decimal.Parse(flat["price"].ToString()),
                        Address = flat["address"]["displayName"].ToString(),
                        coords = $"{flat["location"]["lat"].ToString()} {flat["location"]["lon"].ToString()}",
                        planUrl = "https://img.dmclk.ru/s1920x1080q80" + flat["photos"][0]["url"].ToString(),
                        Url = flat["path"].ToString()
                    };

                    if (flat["description"] != null)
                    {
                        item.Description = flat["description"].ToString();
                    }

                    if (flat["complex"] != null)
                    {
                        item.Area = double.Parse(flat["generalInfo"]["area"].ToString());
                        item.Rooms = int.Parse(flat["generalInfo"]["rooms"].ToString());
                        // от застройщика
                        item.Floor = int.Parse(flat["generalInfo"]["minFloor"].ToString());

                        item.zkName = flat["complex"]["name"].ToString();
                        item.EndOfBuilding = $"{flat["complex"]["building"]["endBuildYear"]} год, {flat["complex"]["building"]["endBuildQuarter"]} квартал";
                    }
                    if (flat["house"] != null)
                    {
                        item.Area = double.Parse(flat["objectInfo"]["area"].ToString());
                        item.Rooms = int.Parse(flat["objectInfo"]["rooms"].ToString());
                        item.Floor = int.Parse(flat["objectInfo"]["floor"].ToString());
                        item.EndOfBuilding = $"{flat["house"]["buildYear"].ToString()} год";
                    }

                    item.PPM = decimal.ToDouble(item.Price) / item.Area;

                    items.Add(item);

                    currentCount++;
                }
                offset += 30;
            }

        }
    }
}
