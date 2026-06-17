using System.Net.Http.Json;
using System.Text.Json.Serialization;
using AnimeMovieTracker.Components.Models;

namespace AnimeMovieTracker.Services;

public class MovieService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    private static readonly HashSet<string> BlockedKeywordNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "softcore",
        "pink film",
        "erotic movie",
        "pornography",
        "adult film",
        "bdsm",
        "bondage",
        "sadomasochism",
        "masochism",
        "sexual pleasure",
        "sexually aggressive woman",
        "nymphomaniac",
        "masturbation"
    };

    private static readonly string[] StrongBlockedText =
    {
        "porn",
        "xxx",
        "softcore",
        "hardcore",
        "adult film",
        "pink film",
        "erotic movie"
    };

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
            var sortBy = category == "top_rated"
                ? "vote_average.desc"
                : "popularity.desc";

            var url =
                $"https://api.themoviedb.org/3/discover/movie" +
                $"?api_key={ApiKey}" +
                $"&page={page}" +
                $"&sort_by={sortBy}" +
                $"&include_adult=false" +
                $"&include_video=false" +
                $"&language=en-US" +
                $"&region=US" +
                $"&certification_country=US" +
                $"&certification.lte=R" +
                $"&vote_count.gte=50";

            if (category == "top_rated")
            {
                url += "&vote_average.gte=6";
            }

            if (genreId.HasValue)
            {
                url += $"&with_genres={genreId.Value}";
            }

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                return new TmdbMoviePageResult();
            }

            var result = await response.Content.ReadFromJsonAsync<TmdbResponse>();

            return new TmdbMoviePageResult
            {
                Items = await FilterSafeMoviesAsync(result?.Results),
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

            var url =
                $"https://api.themoviedb.org/3/search/movie" +
                $"?api_key={ApiKey}" +
                $"&query={Uri.EscapeDataString(search)}" +
                $"&page={page}" +
                $"&include_adult=false" +
                $"&language=en-US";

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                return new TmdbMoviePageResult();
            }

            var result = await response.Content.ReadFromJsonAsync<TmdbResponse>();

            var items = await FilterSafeMoviesAsync(result?.Results);

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
            for (var attempt = 0; attempt < 15; attempt++)
            {
                var randomPage = Random.Shared.Next(1, 150);
                var result = await GetMoviesAsync("popular", randomPage);

                if (result.Items.Count > 0)
                {
                    return result.Items[Random.Shared.Next(result.Items.Count)];
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    private async Task<List<TmdbMovie>> FilterSafeMoviesAsync(List<TmdbMovie>? movies)
    {
        if (movies is null)
        {
            return new List<TmdbMovie>();
        }

        var safeMovies = new List<TmdbMovie>();

        foreach (var movie in movies)
        {
            if (movie.Adult)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(movie.Title) || string.IsNullOrWhiteSpace(movie.PosterPath))
            {
                continue;
            }

            if (movie.VoteCount < 25)
            {
                continue;
            }

            if (HasStrongBlockedText(movie))
            {
                continue;
            }

            if (await HasBlockedKeywordsAsync(movie.Id))
            {
                continue;
            }

            safeMovies.Add(movie);
        }

        return safeMovies;
    }

    private static bool HasStrongBlockedText(TmdbMovie movie)
    {
        var text = $"{movie.Title} {movie.OriginalTitle} {movie.Overview}".ToLowerInvariant();

        return StrongBlockedText.Any(blocked =>
            text.Contains(blocked, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<bool> HasBlockedKeywordsAsync(int movieId)
    {
        try
        {
            var url = $"https://api.themoviedb.org/3/movie/{movieId}/keywords?api_key={ApiKey}";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            var result = await response.Content.ReadFromJsonAsync<TmdbKeywordResponse>();

            return result?.Keywords.Any(k => BlockedKeywordNames.Contains(k.Name)) ?? false;
        }
        catch
        {
            return false;
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

public class TmdbKeywordResponse
{
    [JsonPropertyName("keywords")]
    public List<TmdbKeyword> Keywords { get; set; } = new();
}

public class TmdbKeyword
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";
}