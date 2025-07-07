using System.Text.Json;

namespace AzureDocGen.Web;

public class WeatherApiClient(HttpClient httpClient, ILogger<WeatherApiClient> logger)
{
    public async Task<WeatherForecast[]> GetWeatherAsync(int maxItems = 10, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("Attempting to fetch weather data from {BaseAddress}/weatherforecast", httpClient.BaseAddress);
            
            List<WeatherForecast>? forecasts = null;

            await foreach (var forecast in httpClient.GetFromJsonAsAsyncEnumerable<WeatherForecast>("/weatherforecast", cancellationToken))
            {
                if (forecasts?.Count >= maxItems)
                {
                    break;
                }
                if (forecast is not null)
                {
                    forecasts ??= [];
                    forecasts.Add(forecast);
                }
            }

            logger.LogInformation("Successfully fetched {Count} weather forecasts", forecasts?.Count ?? 0);
            return forecasts?.ToArray() ?? [];
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "HTTP request failed when fetching weather data from {BaseAddress}", httpClient.BaseAddress);
            throw;
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to deserialize weather data response");
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error when fetching weather data");
            throw;
        }
    }
}

public record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
