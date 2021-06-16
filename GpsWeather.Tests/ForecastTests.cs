using System.Globalization;
using System.IO;
using NUnit.Framework;
using Shouldly;

namespace GpsWeather.Tests
{
    [TestFixture]
    public class ForecastTests
    {
        [Test]
        public void Parse()
        {
            var content = File.ReadAllText(@"..\..\..\..\TestFiles\compact.json");
            var (list, lat, lon, elv, update) = Forecast.Parse(content);
            list.Count.ShouldBe(83);
            lat.ShouldBe(60.3727);
            lon.ShouldBe(8.8857);
            elv.ShouldBe(781);
            update.ToString(CultureInfo.InvariantCulture).ShouldBe("06/15/2021 04:52:58");
        }

        [TestCase(0.0, "N")]
        [TestCase(360.0 - 31.0 / 32.0 + 0.01, "N")]
        [TestCase(31.0 / 32.0 - 0.01, "N")]
        [TestCase(180.0, "S")]
        [TestCase(180.0 - 31.0 / 32.0 + 0.01, "S")]
        [TestCase(180.0 + 31.0 / 32.0 - 0.01, "S")]
        public void DirectionName(double d, string name)
        {
            Forecast.DirectionName(d).ShouldBe(name);
        }
    }
}
