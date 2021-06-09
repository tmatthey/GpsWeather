using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Net;
using Newtonsoft.Json.Linq;


namespace GpsWeather
{
    public static class Forecast
    {
        public static string GetLocationForecastCompact(double lat, double lon)
        {
            var myHttpWebRequest = (HttpWebRequest)WebRequest.Create($"https://api.met.no/weatherapi/locationforecast/2.0/compact?lat={ToString(lat)}&lon={ToString(lon)}");
            myHttpWebRequest.UserAgent = "GpsWeather/1.0 https://github.com/tmatthey/GpsWeather";
            const int N = 256;
            var content = "";
            using (var myHttpWebResponse = (HttpWebResponse)myHttpWebRequest.GetResponse())
            {
                using (var streamResponse = myHttpWebResponse.GetResponseStream())
                {
                    using (var streamRead = new StreamReader(streamResponse))
                    {
                        var readBuff = new char[N];
                        var count = streamRead.Read(readBuff, 0, N);
                        while (count > 0)
                        {
                            content += new string(readBuff, 0, count);
                            count = streamRead.Read(readBuff, 0, N);
                        }
                    }
                }
            }

            return content;
        }

        public static (List<Weather> weather, double latitue, double longtiude, double elevation) Parse(string json)
        {
            var list = new List<Weather>();
            dynamic compact = JObject.Parse(json);
            var timeseries = compact["properties"]["timeseries"];
            foreach (var point in timeseries)
            {
                var time = point["time"].Value.ToLocalTime();
                var data = point["data"];
                var details = data["instant"]["details"];
                double temperature = details["air_temperature"];
                double wind = details["wind_speed"];
                double direction = details["wind_from_direction"];
                var rain = double.NaN;

                var ok = GetRain(data, 1, ref rain);
                if (!ok) ok = GetRain(data, 6, ref rain);
                if (!ok) GetRain(data, 12, ref rain);
                list.Add(new Weather
                {
                    Time = time,
                    Temperature = temperature,
                    Wind = wind,
                    Direction = direction,
                    Rain = rain
                });
            }

            var coordinates = compact["geometry"]["coordinates"];

            return (list, coordinates[1], coordinates[0], coordinates[2]);
        }

        private static bool GetRain(dynamic data, int hours, ref double rain)
        {
            if (data[$"next_{hours}_hours"] != null && data[$"next_{hours}_hours"]["details"] != null && data[$"next_{hours}_hours"]["details"]["precipitation_amount"] != null)
            {
                rain = data[$"next_{hours}_hours"]["details"]["precipitation_amount"] / (double)hours;
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