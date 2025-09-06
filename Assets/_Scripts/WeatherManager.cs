using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using Newtonsoft.Json;
using System;
using TMPro;

public class WeatherManager : MonoBehaviour
{
    [Header("API Settings")]
    public string apiKey;
    private string city;

    [Header("UI Elements")]
    public TMP_InputField cityInputField;
    public TMP_Text statusText;
    public TMP_Text timeText;

    [Header("Scene Objects")]
    public Light sunLight;
    public ParticleSystem rainEffect;
    public ParticleSystem snowEffect;

    [Header("Audio")]
    public AudioSource dayAudio;
    public AudioSource nightAudio;
    public AudioSource rainAudio;
    public AudioSource snowAudio;
    [Tooltip("How long it takes to fade between audio tracks, in seconds.")]
    public float audioCrossfadeDuration = 2.0f;

    [Header("Light Settings")]
    public Gradient sunColor;
    public Gradient ambientColor;

    [Header("Weather Data")]
    public WeatherData currentWeatherData;

    private readonly string apiUrl = "https://api.openweathermap.org/data/2.5/weather";
    private AudioSource _currentAudio;
    private Coroutine _crossfadeCoroutine;

    void Start()
    {
        rainEffect.Stop();
        snowEffect.Stop();
        statusText.text = "";
        timeText.text = "";
        StopAllAudio();
    }

    public void OnSearchButtonPress()
    {
        city = cityInputField.text;
        GetWeather();
        timeText.text = "--:--";
    }



    private void GetWeather()
    {
        if (string.IsNullOrEmpty(city))
        {
            statusText.text = "Please enter a city name.";
            return;
        }
        statusText.text = $"Fetching weather for {city}...";
        StartCoroutine(FetchWeatherData());
    }

    private IEnumerator FetchWeatherData()
    {
        string fullUrl = $"{apiUrl}?q={city}&appid={apiKey}&units=metric";
        using (UnityWebRequest request = UnityWebRequest.Get(fullUrl))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                statusText.text = "City not found. Please try again.";
                timeText.text = "";
            }
            else
            {
                string jsonResponse = request.downloadHandler.text;
                currentWeatherData = JsonConvert.DeserializeObject<WeatherData>(jsonResponse);
                statusText.text = $"Displaying weather for: {currentWeatherData.name}";

                UpdateSun();
                UpdateWeatherEffects();
                UpdateWeatherAudio();
                UpdateTimeDisplay();
            }
        }
    }
    
    // UPDATED: This now correctly calculates the city's local time
    private void UpdateTimeDisplay()
    {
        DateTime utcTime = UnixTimeStampToDateTime(currentWeatherData.dt);
        DateTime localTime = utcTime.AddSeconds(currentWeatherData.timezone);
        timeText.text = localTime.ToString("HH:mm");
    }
    
    // UPDATED: This now correctly uses the city's local time for day/night audio
    private void UpdateWeatherAudio()
    {
        string weatherCondition = currentWeatherData.weather[0].main;
        AudioSource targetAudio = null;

        if (weatherCondition == "Rain" || weatherCondition == "Drizzle" || weatherCondition == "Thunderstorm")
        {
            targetAudio = rainAudio;
        }
        else if (weatherCondition == "Snow")
        {
            targetAudio = snowAudio;
        }
        else
        {
            DateTime utcTime = UnixTimeStampToDateTime(currentWeatherData.dt);
            DateTime localTime = utcTime.AddSeconds(currentWeatherData.timezone);
            DateTime sunriseTime = UnixTimeStampToDateTime(currentWeatherData.sys.sunrise).AddSeconds(currentWeatherData.timezone);
            DateTime sunsetTime = UnixTimeStampToDateTime(currentWeatherData.sys.sunset).AddSeconds(currentWeatherData.timezone);

            if (localTime > sunriseTime && localTime < sunsetTime)
            {
                targetAudio = dayAudio;
            }
            else
            {
                targetAudio = nightAudio;
            }
        }

        if (_currentAudio != targetAudio)
        {
            if (_crossfadeCoroutine != null)
            {
                StopCoroutine(_crossfadeCoroutine);
            }
            _crossfadeCoroutine = StartCoroutine(CrossfadeAudio(targetAudio));
        }
    }

    // UPDATED: This now correctly uses the city's local time for the sun position
    private void UpdateSun()
    {
        DateTime utcTime = UnixTimeStampToDateTime(currentWeatherData.dt);
        DateTime localTime = utcTime.AddSeconds(currentWeatherData.timezone);
        DateTime sunriseTime = UnixTimeStampToDateTime(currentWeatherData.sys.sunrise).AddSeconds(currentWeatherData.timezone);
        DateTime sunsetTime = UnixTimeStampToDateTime(currentWeatherData.sys.sunset).AddSeconds(currentWeatherData.timezone);

        double dayLength = (sunsetTime - sunriseTime).TotalSeconds;
        double timeSinceSunrise = (localTime - sunriseTime).TotalSeconds;
        float dayPercent = Mathf.Clamp01((float)(timeSinceSunrise / dayLength));
        sunLight.transform.rotation = Quaternion.Euler(dayPercent * 180f, -60f, 0);
        sunLight.color = sunColor.Evaluate(dayPercent);
        RenderSettings.ambientLight = ambientColor.Evaluate(dayPercent);
    }
    
    private void UpdateWeatherEffects()
    {
        string weatherCondition = currentWeatherData.weather[0].main;
        rainEffect.Stop();
        snowEffect.Stop();
        if (weatherCondition == "Rain" || weatherCondition == "Drizzle" || weatherCondition == "Thunderstorm") { rainEffect.Play(); }
        else if (weatherCondition == "Snow") { snowEffect.Play(); }
    }
    
    private void StopAllAudio()
    {
        dayAudio.Stop();
        nightAudio.Stop();
        rainAudio.Stop();
        snowAudio.Stop();

        dayAudio.volume = 0;
        nightAudio.volume = 0;
        rainAudio.volume = 0;
        snowAudio.volume = 0;
    }

    private IEnumerator CrossfadeAudio(AudioSource newAudio)
    {
        AudioSource oldAudio = _currentAudio;
        _currentAudio = newAudio;

        if (newAudio != null)
        {
            newAudio.Play();
        }

        float timer = 0f;
        while (timer < audioCrossfadeDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / audioCrossfadeDuration;

            if (oldAudio != null)
            {
                oldAudio.volume = Mathf.Lerp(0.2f, 0f, progress);
            }

            if (newAudio != null)
            {
                newAudio.volume = Mathf.Lerp(0f, 0.2f, progress);
            }

            yield return null;
        }

        if (oldAudio != null)
        {
            oldAudio.Stop();
        }
        if (newAudio != null)
        {
            newAudio.volume = 0.2f;
        }
    }

    // UPDATED: This function now ONLY converts to UTC. No more .ToLocalTime()!
    private DateTime UnixTimeStampToDateTime(long unixTimeStamp)
    {
        DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        dateTime = dateTime.AddSeconds(unixTimeStamp);
        return dateTime;
    }
}