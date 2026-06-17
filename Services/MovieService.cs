using System.Net.Http.Json;
using System.Text.Json.Serialization;
using AnimeMovieTracker.Components.Models;

namespace AnimeMovieTracker.Services;

public class MovieService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public MovieService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    private string ApiKey => _configuration["TMDB:ApiKey"] ?? "";

    public async Task<TmdbMoviePageResult> GetMoviesAsync(string category = "popular", int page = 1, int? genreId = null)
    {
        try
        {
            string url;

            if (genreId.HasValue)
            {
                url = $"https://api.themoviedb.org/3/discover/movie?api_key={ApiKey}&page={page}&with_genres={genreId.Value}&sort_by=popularity.desc&include_adult=false";
            }
            else
            {
                url = $"https://api.themoviedb.org/3/movie/{category}?api_key={ApiKey}&page={page}&include_adult=false";
            }

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                return new TmdbMoviePageResult();
            }

            var result = await response.Content.ReadFromJsonAsync<TmdbResponse>();

            return new TmdbMoviePageResult
            {
                Items = result?.Results?
                    .Where(m => !m.Adult)
                    .ToList() ?? new(),
                CurrentPage = result?.Page ?? page,
                TotalPages = result?.TotalPages ?? 1
            };
        }
        catch
        {
            return new TmdbMoviePageResult();
        }
    }

    public async Task<TmdbMoviePageResult> SearchMoviesAsync(string search, int page = 1, int? genreId = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(search))
            {
                return await GetMoviesAsync("popular", page, genreId);
            }

            var url = $"https://api.themoviedb.org/3/search/movie?api_key={ApiKey}&query={Uri.EscapeDataString(search)}&page={page}&include_adult=false";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                return new TmdbMoviePageResult();
            }

            var result = await response.Content.ReadFromJsonAsync<TmdbResponse>();

            var items = result?.Results?
                .Where(m => !m.Adult)
                .ToList() ?? new();

            if (genreId.HasValue)
            {
                items = items.Where(m => m.GenreIds.Contains(genreId.Value)).ToList();
            }

            return new TmdbMoviePageResult
            {
                Items = items,
                CurrentPage = result?.Page ?? page,
                TotalPages = result?.TotalPages ?? 1
            };
        }
        catch
        {
            return new TmdbMoviePageResult();
        }
    }

    public async Task<List<TmdbGenre>> GetMovieGenresAsync()
    {
        try
        {
            var url = $"https://api.themoviedb.org/3/genre/movie/list?api_key={ApiKey}";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                return new List<TmdbGenre>();
            }

            var result = await response.Content.ReadFromJsonAsync<TmdbGenreResponse>();

            return result?.Genres?.OrderBy(g => g.Name).ToList() ?? new List<TmdbGenre>();
        }
        catch
        {
            return new List<TmdbGenre>();
        }
    }

    public async Task<List<TmdbCastMember>> GetMovieCastAsync(int movieId)
    {
        try
        {
            var url = $"https://api.themoviedb.org/3/movie/{movieId}/credits?api_key={ApiKey}";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                return new List<TmdbCastMember>();
            }

            var result = await response.Content.ReadFromJsonAsync<TmdbCreditsResponse>();

            return result?.Cast?.Take(12).ToList() ?? new List<TmdbCastMember>();
        }
        catch
        {
            return new List<TmdbCastMember>();
        }
    }
    public async Task<TmdbMovie?> GetRandomMovieAsync()
    {
        try
        {
            var randomPage = Random.Shared.Next(1, 500);
            var result = await GetMoviesAsync("popular", randomPage);

            if (result.Items.Count == 0)
            {
                return null;
            }

            return result.Items[Random.Shared.Next(result.Items.Count)];
        }
        catch
        {
            return null;
        }
    }
}

public class TmdbMoviePageResult
{
    public List<TmdbMovie> Items { get; set; } = new();
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; } = 1;
}

public class TmdbResponse
{
    [JsonPropertyName("page")]
    public int Page { get; set; }

    [JsonPropertyName("results")]
    public List<TmdbMovie> Results { get; set; } = new();

    [JsonPropertyName("total_pages")]
    public int TotalPages { get; set; }
}

public class TmdbCreditsResponse
{
    [JsonPropertyName("cast")]
    public List<TmdbCastMember>? Cast { get; set; }
}

public class TmdbCastMember
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("character")]
    public string Character { get; set; } = "";

    [JsonPropertyName("profile_path")]
    public string? ProfilePath { get; set; }

    public string ImageUrl =>
        string.IsNullOrEmpty(ProfilePath)
            ? "https://placehold.co/100x150?text=No+Image"
            : $"https://image.tmdb.org/t/p/w185{ProfilePath}";
}

public class TmdbGenreResponse
{
    [JsonPropertyName("genres")]
    public List<TmdbGenre>? Genres { get; set; }
}

public class TmdbGenre
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";
}