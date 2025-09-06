// This attribute tells Unity that this class can be serialized, 
// which is necessary for converting it to/from formats like JSON.
[System.Serializable]
public class WeatherData
{
    public WeatherInfo[] weather; // The API sends an array, so we need an array
    public MainInfo main;
    public SysInfo sys;
    public long dt; // 'dt' is the timestamp (time of day)
    public int timezone;
    public string name; // The name of the city
}

[System.Serializable]
public class WeatherInfo
{
    public string main; // "Clouds", "Rain", "Clear", etc.
}

[System.Serializable]
public class MainInfo
{
    public float temp; // Temperature
}

[System.Serializable]
public class SysInfo
{
    public long sunrise;
    public long sunset;
}