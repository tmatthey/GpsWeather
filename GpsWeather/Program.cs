using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Math.Gps;

namespace GpsWeather
{
    public static class Program
    {
        private static void Main(string[] args)
        {
            if (args.Length < 4)
            {
                Console.WriteLine("Usage: gpsweather <route tcx, gpx or kml> <number of points> <velocity km/h> <start date time>");
                return;
            }
            var filename = args[0];
            var n = System.Math.Max(int.Parse(args[1], CultureInfo.InvariantCulture), 2);
            var vel = int.Parse(args[2], CultureInfo.InvariantCulture) / 3.6; // km/h -> m/s
            var start = DateTime.Parse(args[3]);

            var track = Math.Tools.TrackReaders.Deserializer.File(filename);
            var gps = track.GpsPoints().ToList();
            var dist = Geodesy.Distance.HaversineAccumulated(gps);
            var total = dist.Last();

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
                Elevation = gps[i].Elevation
            }).ToList();

            for (var i = 0; i < list.Count; i++)
            {
                var content = Forecast.GetLocationForecastCompact(list[i].Latitude, list[i].Longitude);
                File.WriteAllText($"{i}.json", content);
                //var content = File.ReadAllText($"{i}.json");
                var (weather, latitude, longitude, elevation) = Forecast.Parse(content);
                list[i].Weather = weather;
            }

            Console.WriteLine($"Time {DateTime.Now}\tDistance [km]\tElevation [m]\tTemperature [C]\tRain [mm/h]\tWind [m/s]\tDirection [Deg]");
            foreach (var station in list)
            {
                var t = start + new TimeSpan(0, 0, (int)station.Time);
                var w = station.Weather.First(p => p.Time >= t);
                Console.WriteLine($"{t}\t{station.Distance * 1e-3}\t{station.Elevation}\t{w.Temperature}\t{w.Rain}\t{w.Wind}\t{w.Direction}");
            }
        }
    }
}
