using ParsingService.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParsingService
{
    public interface IParserFactory
    {
        IParser GetParser(string source);
        IEnumerable<IParser> GetAllParsers();
    }
}
