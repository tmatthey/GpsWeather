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
            var (list, lat, lon, elv) = Forecast.Parse(content);
            list.Count.ShouldBe(83);
        }
    }
}
