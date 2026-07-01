using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Json.Nodes;

namespace GpsWeather;

public static class Forecast
{
    private static readonly HttpClient HttpClient = CreateHttpClient();
    private static readonly List<string> DirectionNameTable = ["N", "NNE", "NE", "ENE", "E", "ESE", "SE", "SSE", "S", "SSW", "SW", "WSW", "W", "WNW", "NW", "NNW"];
    private static readonly int DirectionNameTableN = DirectionNameTable.Count;
    private static readonly double DirectionNameDelta = 360.0 / (2.0 * DirectionNameTableN);

    public static string GetLocationForecastComplete(double lat, double lon)
    {
        var url = $"https://api.met.no/weatherapi/locationforecast/2.0/complete?lat={ToString(lat)}&lon={ToString(lon)}";
        return HttpClient.GetStringAsync(url).GetAwaiter().GetResult();
    }

    public static (List<Weather> weather, double latitue, double longtiude, double elevation, DateTime updateTime) Parse(string json)
    {
        var list = new List<Weather>();
        var compact = JsonNode.Parse(json);
        var timeSeries = (compact?["properties"]?["timeseries"]?.AsArray() ?? []).Where(a => a is not null);
        foreach (var point in timeSeries)
        {
            var time = point["time"]?.GetValue<DateTime>() ?? new DateTime(1970, 1, 1);
            var data = point["data"];
            var details = data?["instant"]?["details"];
            var temperature = details?["air_temperature"]?.GetValue<double>() ?? double.NaN;
            var wind = details?["wind_speed"]?.GetValue<double>() ?? double.NaN;
            var direction = details?["wind_from_direction"]?.GetValue<double>() ?? double.NaN;
            var rain = double.NaN;
            var rainMin = double.NaN;
            var rainMax = double.NaN;
            var rainProbability = double.NaN;
            var symbol = "";
            var ok = GetTimeData(data, 1, ref rain, ref rainMin, ref rainMax, ref rainProbability, ref symbol);
            if (!ok) 
                ok = GetTimeData(data, 6, ref rain, ref rainMin, ref rainMax, ref rainProbability, ref symbol);

            if (!ok) 
                GetTimeData(data, 12, ref rain, ref rainMin, ref rainMax, ref rainProbability, ref symbol);

            list.Add(new Weather
            {
                Time = time,
                Temperature = temperature,
                Wind = wind,
                Direction = direction,
                Rain = rain,
                RainMin = rainMin,
                RainMax = rainMax,
                RainProbability = rainProbability,
                Symbol = symbol
            });
        }

        var coordinates = compact?["geometry"]?["coordinates"]?.AsArray() ?? [double.NaN, double.NaN, double.NaN];

        return (list, coordinates[1]!.GetValue<double>(), coordinates[0]!.GetValue<double>(), coordinates[2]!.GetValue<double>(), compact?["properties"]?["meta"]?["updated_at"]?.GetValue<DateTime>() ?? new DateTime(1970, 1, 1));
    }

    public static string DirectionName(double d)
    {
        return DirectionNameTable[(int)((d + DirectionNameDelta) / 360.0 * DirectionNameTableN) % DirectionNameTableN];
    }

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("GpsWeather/1.0 (https://github.com/tmatthey/GpsWeather)");
        return client;
    }

    private static bool GetTimeData(JsonNode data, int hours, ref double rain, ref double rainMin, ref double rainMax, ref double rainProbability, ref string symbol)
    {
        if (data?[$"next_{hours}_hours"]?["details"]?["precipitation_amount"] == null)
            return false;

        rain = (data[$"next_{hours}_hours"]!["details"]!["precipitation_amount"]?.GetValue<double>() ?? double.NaN) / hours;
        rainMin = (data[$"next_{hours}_hours"]!["details"]!["precipitation_amount_min"]?.GetValue<double>() ?? double.NaN) / hours;
        rainMax = (data[$"next_{hours}_hours"]!["details"]!["precipitation_amount_max"]?.GetValue<double>() ?? double.NaN) / hours;
        rainProbability = (data[$"next_{hours}_hours"]!["details"]?["probability_of_precipitation"]?.GetValue<double>() ?? double.NaN) / 100.0;
        symbol = data[$"next_{hours}_hours"]!["summary"]!["symbol_code"]?.GetValue<string>() ?? "";
        
        return true;
    }

    private static string ToString(double x)
    {
        return x.ToString(CultureInfo.InvariantCulture);
    }
}