using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using Newtonsoft.Json.Linq;

namespace GpsWeather
{
    public static class Forecast
    {
        public static string GetLocationForecastComplete(double lat, double lon)
        {
            var myHttpWebRequest = (HttpWebRequest)WebRequest.Create($"https://api.met.no/weatherapi/locationforecast/2.0/complete?lat={ToString(lat)}&lon={ToString(lon)}");
            myHttpWebRequest.UserAgent = "GpsWeather/1.0 https://github.com/tmatthey/GpsWeather";
            const int n = 256;
            var content = "";
            using (var myHttpWebResponse = (HttpWebResponse)myHttpWebRequest.GetResponse())
            {
                using (var streamResponse = myHttpWebResponse.GetResponseStream())
                {
                    using (var streamRead = new StreamReader(streamResponse))
                    {
                        var readBuff = new char[n];
                        var count = streamRead.Read(readBuff, 0, n);
                        while (count > 0)
                        {
                            content += new string(readBuff, 0, count);
                            count = streamRead.Read(readBuff, 0, n);
                        }
                    }
                }
            }

            return content;
        }

        public static (List<Weather> weather, double latitue, double longtiude, double elevation, DateTime updateTime) Parse(string json)
        {
            var list = new List<Weather>();
            dynamic compact = JObject.Parse(json);
            var timeseries = compact["properties"]["timeseries"];
            foreach (var point in timeseries)
            {
                var time = point["time"].Value;
                var data = point["data"];
                var details = data["instant"]["details"];
                double temperature = details["air_temperature"];
                double wind = details["wind_speed"];
                double direction = details["wind_from_direction"];
                var rain = double.NaN;
                var rainMin = double.NaN;
                var rainMax = double.NaN;
                var rainProbability = double.NaN;
                var symbole = "";
                var ok = GetTimeData(data, 1, ref rain, ref rainMin, ref rainMax, ref rainProbability, ref symbole);
                if (!ok)
                {
                    ok = GetTimeData(data, 6, ref rain, ref rainMin, ref rainMax, ref rainProbability, ref symbole);
                }

                if (!ok)
                {
                    GetTimeData(data, 12, ref rain, ref rainMin, ref rainMax, ref rainProbability, ref symbole);
                }

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
                    Symbol = symbole
                });
            }

            var coordinates = compact["geometry"]["coordinates"];

            return (list, coordinates[1], coordinates[0], coordinates[2], compact["properties"]["meta"]["updated_at"].Value);
        }

        public static string DirectionName(double d)
        {
            return DirectionNameTable[(int)((d + DirectionNameDelta) / 360.0 * DirectionNameTableN) % DirectionNameTableN];
        }
        private static readonly List<string> DirectionNameTable = new List<string> { "N", "NNE", "NE", "ENE", "E", "ESE", "SE", "SSE", "S", "SSW", "SW", "WSW", "W", "WNW", "NW", "NNW" };
        private static readonly int DirectionNameTableN = DirectionNameTable.Count;
        private static double DirectionNameDelta = 360.0 / (2.0 * DirectionNameTableN);

        private static bool GetTimeData(dynamic data, int hours, ref double rain, ref double rainMin, ref double rainMax, ref double rainProbability, ref string symbol)
        {
            if (data[$"next_{hours}_hours"] != null && data[$"next_{hours}_hours"]["details"] != null && data[$"next_{hours}_hours"]["details"]["precipitation_amount"] != null)
            {
                rain = data[$"next_{hours}_hours"]["details"]["precipitation_amount"] / (double)hours;
                rainMin = data[$"next_{hours}_hours"]["details"]["precipitation_amount_min"] / (double)hours;
                rainMax = data[$"next_{hours}_hours"]["details"]["precipitation_amount_max"] / (double)hours;
                rainProbability = data[$"next_{hours}_hours"]["details"]["probability_of_precipitation"] / 100.0;
                symbol = data[$"next_{hours}_hours"]["summary"]["symbol_code"];
                return true;
            }

            return false;
        }

        private static string ToString(double x)
        {
            return x.ToString(CultureInfo.InvariantCulture);
        }
    }
}