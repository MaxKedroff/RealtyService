using Core.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public static class RayCastingService
    {
        /// <summary>
        /// Проверяет, находится ли точка внутри полигона
        /// Использует алгоритм Ray Casting (луча)
        /// </summary>
        public static bool IsPointInPolygon(string pointGeoString, List<GeoPoint> polygon)
        {
            if (string.IsNullOrWhiteSpace(pointGeoString))
                return false;

            // Парсим точку из строки
            var point = ParseSingleGeoPoint(pointGeoString);
            if (point == null)
                return false;

            return IsPointInPolygon(point, polygon);
        }

        public static List<GeoPoint> ParseGeoPointString(string geoPointString)
        {
            var points = new List<GeoPoint>();

            var pointStrings = geoPointString.Split(',', StringSplitOptions.RemoveEmptyEntries);
            foreach (var pointStr in pointStrings)
            {
                var coordinates = pointStr.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

                if (coordinates.Length == 2)
                {
                    if (double.TryParse(coordinates[0], System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out double lat) &&
                        double.TryParse(coordinates[1], System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out double lon))
                    {
                        points.Add(new GeoPoint
                        {
                            Latitude = lat,
                            Longitude = lon
                        });
                    }
                }
            }
            return points;
        }

        /// <summary>
        /// Проверяет, находится ли точка внутри полигона
        /// </summary>
        public static bool IsPointInPolygon(GeoPoint point, List<GeoPoint> polygon)
        {
            if (polygon == null || polygon.Count < 3)
                return false;

            bool inside = false;

            for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
            {
                // Проверяем, пересекает ли луч от точки ребро полигона
                bool intersect = ((polygon[i].Latitude > point.Latitude) != (polygon[j].Latitude > point.Latitude)) &&
                    (point.Longitude < (polygon[j].Longitude - polygon[i].Longitude) *
                    (point.Latitude - polygon[i].Latitude) /
                    (polygon[j].Latitude - polygon[i].Latitude) + polygon[i].Longitude);

                if (intersect)
                    inside = !inside;
            }

            return inside;
        }

        /// <summary>
        /// Парсит одиночную точку из строки формата "широта долгота"
        /// </summary>
        public static GeoPoint ParseSingleGeoPoint(string geoPointString)
        {
            if (string.IsNullOrWhiteSpace(geoPointString))
                return null;

            var normalizedString = geoPointString.Trim().Replace(',', '.');

            var coordinates = normalizedString.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (coordinates.Length == 2)
            {
                if (double.TryParse(coordinates[0], NumberStyles.Any, CultureInfo.InvariantCulture, out double lat) &&
                    double.TryParse(coordinates[1], NumberStyles.Any, CultureInfo.InvariantCulture, out double lon))
                {
                    return new GeoPoint { Latitude = lat, Longitude = lon };
                }
            }

            return null;
        }
    }
}
