using System.Collections.Generic;

namespace ControlUnitFredrik.Data;

public class WeatherInfo
{
    public string Name { get; set; }
    public MainInfo Main { get; set; }
    public List<WeatherDescription> Weather { get; set; }
}

public class MainInfo
{
    public double Temp { get; set; }
}

public class WeatherDescription
{
    public string Description { get; set; }
    public string Icon { get; set; }
}