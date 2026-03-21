using BenchmarkDotNet.Exporters.Csv;
using CsvHelper;
using CsvHelper.Configuration;
using ParsingService.Models;
using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ParsingService.Services
{
    public class ExportService : IExportService
    {
        public async Task<byte[]> ExportToCsvAsync(ParsingResult result)
        {
            using var memoryStream = new MemoryStream();
            var encoding = new UTF8Encoding(true);
            using var writer = new StreamWriter(memoryStream, encoding, leaveOpen: true);

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = ",",
                Encoding = encoding
            };

            using var csv = new CsvWriter(writer, config);

            await csv.WriteRecordsAsync(result.Properties);
            await writer.FlushAsync();

            return memoryStream.ToArray();
        }

        public Task<string> ExportToJsonAsync(ParsingResult result)
        {
            var json = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
            return Task.FromResult(json);
        }
    }
}
