namespace MapMe.Utils;

public static class Geo
{
    public static (double minLat, double minLng, double maxLat, double maxLng) Bbox(double lat, double lng,
        double radiusMeters)
    {
        const double earthRadius = 6378137.0; // meters
        var dLat = radiusMeters / earthRadius * (180.0 / Math.PI);
        var dLng = radiusMeters / (earthRadius * Math.Cos(lat * Math.PI / 180.0)) * (180.0 / Math.PI);
        return (lat - dLat, lng - dLng, lat + dLat, lng + dLng);
    }

    // Simplified tile key acting as a geohash prefix substitute for in-memory prototyping
    public static string TileKey(double lat, double lng, int precision)
    {
        var latR = Math.Round(lat, precision);
        var lngR = Math.Round(lng, precision);
        var latS = latR.ToString($"F{precision}");
        var lngS = lngR.ToString($"F{precision}");
        return $"{latS}|{lngS}";
    }

    public static IReadOnlyList<string> CoveringTiles((double minLat, double minLng, double maxLat, double maxLng) bbox,
        int precision, double stepDegrees)
    {
        var keys = new List<string>();
        for (var lat = bbox.minLat; lat <= bbox.maxLat; lat += stepDegrees)
        {
            for (var lng = bbox.minLng; lng <= bbox.maxLng; lng += stepDegrees)
            {
                keys.Add(TileKey(lat, lng, precision));
            }
        }

        return keys;
    }
}