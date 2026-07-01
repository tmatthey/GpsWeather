using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Math.Gps;

namespace GpsWeather;

public static class Program
{
    private static void Main(string[] args)
    {
        if (args.Length < 4)
        {
            Console.WriteLine("Usage: gpsweather <route tcx, gpx or kml> <number of points> <velocity km/h> <start local date time> [-md]");
            return;
        }

        var filename = args[0];
        var n = System.Math.Min(System.Math.Max(int.Parse(args[1], CultureInfo.InvariantCulture), 2), 100);
        var vel = int.Parse(args[2], CultureInfo.InvariantCulture) / 3.6; // km/h -> m/s
        var start = DateTime.Parse(args[3]);

        var track = Math.Tools.TrackReaders.Deserializer.File(filename);
        var gps = track.GpsPoints().ToList();
        var dist = Geodesy.Distance.HaversineAccumulated(gps);
        var total = dist.Last();
        var elevationGain = new List<double> { 0 };
        for (var i = 1; i < gps.Count; i++)
            elevationGain.Add(elevationGain.Last() + System.Math.Max(gps[i].Elevation - gps[i - 1].Elevation, 0));

        var index = new List<int> { 0 };
        for (var i = 1; i < n; i++)
            index.Add(dist.FindIndex(index.Last(), x => x >= total * i / n));

        index.Add(dist.Count - 1);

        var list = index.Select(i => new Station
        {
            Index = i,
            Distance = dist[i],
            Time = dist[i] / vel,
            Latitude = gps[i].Latitude,
            Longitude = gps[i].Longitude,
            Elevation = gps[i].Elevation,
            ElevationGain = elevationGain[i]
        }).ToList();

        var tempDir = Environment.ExpandEnvironmentVariables("%TEMP%");

        for (var i = 0; i < list.Count; i++)
        {
            var contentFilename = Path.Combine(tempDir, $"{i:D2}.json");
            var content = Forecast.GetLocationForecastComplete(list[i].Latitude, list[i].Longitude);
            File.WriteAllText(contentFilename, content);
            //var content = File.ReadAllText(contentFilename);
            var (weather, _, _, _, updateTime) = Forecast.Parse(content);
            list[i].Weather = weather;
            list[i].WeatherTime = updateTime;
        }

        var md = args.Length > 4 && args[4] == "-md";
        var sep = md ? " | " : ";";
        var sep2 = md ? "| " : "";
        var sep3 = md ? " |" : "";

        Console.WriteLine($"{sep2}Time{sep}Distance [km]{sep}Elevation [m]{sep}Elevation gain [m]{sep}Update time{sep}Weather{sep}Temperature [C]{sep}Rain [mm/h]{sep}Rain min [mm/h]{sep}Rain max [mm/h]{sep}Rain probability{sep}Wind [m/s]{sep}Direction{sep}Direction [Deg]{sep3}");
        if (md)
            Console.WriteLine("| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |");
        foreach (var station in list)
        {
            var t = start + new TimeSpan(0, 0, (int)station.Time);
            var w = station.Weather.Last(p => p.Time.ToLocalTime() <= t);
            Console.WriteLine($"{sep2}{t}{sep}" +
                              $"{System.Math.Round(station.Distance * 1e-3,2)}{sep}" +
                              $"{System.Math.Round(station.Elevation)}{sep}" +
                              $"{System.Math.Round(station.ElevationGain)}{sep}" +
                              $"{station.WeatherTime.ToLocalTime()}{sep}" +
                              $"{w.Symbol}{sep}" +
                              $"{w.Temperature}{sep}" +
                              $"{System.Math.Round(w.Rain,3)}{sep}" +
                              $"{System.Math.Round(w.RainMin,3)}{sep}" +
                              $"{System.Math.Round(w.RainMax,3)}{sep}" +
                              $"{System.Math.Round(w.RainProbability * 100.0)}{sep}" +
                              $"{w.Wind}{sep}" +
                              $"{Forecast.DirectionName(w.Direction)}{sep}" +
                              $"{w.Direction}{sep3}");
        }
    }
}
