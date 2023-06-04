using {{ROOT_NAMESPACE}}.DTO;

namespace {{ROOT_NAMESPACE}}.Abstractions;

/// <summary>
///     Provides methods for managing company information.
/// </summary>
public interface IWeatherService
{
    Task<List<WeatherDto>> GetAllAsync();
}