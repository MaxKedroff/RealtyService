using ParsingService.Interfaces;
using ParsingService.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParsingService.Parsers
{
    public class AvitoParser : IParser
    {
        public string ParserName => "Avito";

        public bool CanHandle(string source) =>
            source.Equals("avito", StringComparison.OrdinalIgnoreCase);

        public async Task<ParsingResult> ParseAsync(ParsingOptions options)
        {
            var result = new ParsingResult
            {
                Source = ParserName,
                ParsedAt = DateTime.UtcNow
            };

            result.Properties = await FetchAvitoProperties(options);

            return result;
        }

        private Task<List<PropertyItem>> FetchAvitoProperties(ParsingOptions options)
        {
            throw new NotImplementedException();
        }
    }
}
