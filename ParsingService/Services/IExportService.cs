using ParsingService.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParsingService.Services
{
    public interface IExportService
    {
        Task<string> ExportToJsonAsync(ParsingResult result);
        Task<byte[]> ExportToCsvAsync(ParsingResult result);
    }
}
