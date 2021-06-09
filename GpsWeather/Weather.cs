using System;

namespace GpsWeather
{
    public class Weather
    {
        public DateTime Time { get; set; } 
        public double Temperature { get; set; } 
        public double Wind { get; set; } 
        public double Direction { get; set; } 
        public double Rain { get; set; }
    }
}
