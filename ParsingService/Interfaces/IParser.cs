using ParsingService.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParsingService.Interfaces
{
    public interface IParser
    {
        string ParserName { get; }
        Task<ParsingResult> ParseAsync(ParsingOptions options);
        bool CanHandle(string source);
    }
}
