[System.Serializable]
public class WeatherData
{
    public WeatherInfo[] weather; 
    public MainInfo main;
    public SysInfo sys;
    public long dt; //time of day
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
    public float temp; 
}

[System.Serializable]
public class SysInfo
{
    public long sunrise;
    public long sunset;
}