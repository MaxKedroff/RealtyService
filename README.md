# RealtyService
для теста данных:
1) git clone https://github.com/MaxKedroff/RealtyService.git
2) cd RealtyService
3) dotnet restore
4) dotnet run
5) переходим на http://localhost:5031/swagger/index.html
6) /api/Parsing/sources - посмотреть доступные парсеры на данный момент
7) /api/Parsing/parse/{source}/json - вывод данных в json формате, source берется из запроса выше, maxResults опционально, но желательно часто полный сбор не запускать, максимум раз в час
8) /api/Parsing/parse/{source}/csv - вывод данных в csv формате, source берется из запроса выше, maxResults опционально, но желательно часто полный сбор не запускать, максимум раз в час
