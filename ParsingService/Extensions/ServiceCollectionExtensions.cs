using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ParsingService.Interfaces;
using ParsingService.Parsers;
using ParsingService.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParsingService.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddParsers(this IServiceCollection services)
        {
            services.AddSingleton<IParserFactory, ParserFactory>();
            services.AddSingleton<IExportService, ExportService>();

            services.AddSingleton<IParser, DomclickParser>(sp => new DomclickParser(sp.GetService<ILogger<DomclickParser>>()));          
            //services.AddSingleton<IParser, CianParser>();
            //services.AddSingleton<IParser, AvitoParser>();

            return services;
        }
    }
}
