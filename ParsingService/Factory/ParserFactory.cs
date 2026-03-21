using ParsingService.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParsingService
{
    public class ParserFactory : IParserFactory
    {

        private readonly IEnumerable<IParser> _parsers;
        public ParserFactory(IEnumerable<IParser> parsers)
        {
            _parsers = parsers;
        }


        public IEnumerable<IParser> GetAllParsers()
        {
            return _parsers;
        }

        public IParser GetParser(string source)
        {
            var parser = _parsers.FirstOrDefault(p => p.CanHandle(source));

            if (parser == null)
            {
                throw new ArgumentException($"Parser for source '{source}' not found");
            }
            return parser;
        }
    }
}
