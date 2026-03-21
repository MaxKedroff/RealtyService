using ParsingService.Interfaces;
using ParsingService.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParsingService.Parsers
{
    public class CianParser : IParser
    {
        public string ParserName => "Cian";

        public bool CanHandle(string source) =>
            source.Equals("cian", StringComparison.OrdinalIgnoreCase);

        public async Task<ParsingResult> ParseAsync(ParsingOptions options)
        {
            var result = new ParsingResult
            {
                Source = ParserName,
                ParsedAt = DateTime.UtcNow
            };

            result.Properties = await FetchCianProperties(options);

            return result;
        }

        private Task<List<PropertyItem>> FetchCianProperties(ParsingOptions options)
        {
            throw new NotImplementedException();
        }
    }
}
