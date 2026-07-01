using System;
using System.Collections.Generic;

namespace GpsWeather;

public class Station
{
    public int Index { get; set; }

    public double Distance { get; set; }

    public double Time { get; set; }

    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public double Elevation { get; set; }

    public double ElevationGain { get; set; }

    public List<Weather> Weather { get; set; } = null;

    public DateTime WeatherTime { get; set; }
}