using System.Text.Json.Serialization;

namespace AnimeMovieTracker.Components.Models;

public class TmdbMovie
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("adult")]
    public bool Adult { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = "";

    [JsonPropertyName("original_title")]
    public string OriginalTitle { get; set; } = "";

    [JsonPropertyName("overview")]
    public string Overview { get; set; } = "";

    [JsonPropertyName("poster_path")]
    public string? PosterPath { get; set; }

    [JsonPropertyName("release_date")]
    public string? ReleaseDate { get; set; }

    [JsonPropertyName("vote_average")]
    public double VoteAverage { get; set; }

    [JsonPropertyName("vote_count")]
    public int VoteCount { get; set; }

    [JsonPropertyName("genre_ids")]
    public List<int> GenreIds { get; set; } = new();

    public bool IsFavorite { get; set; }

    public string Status { get; set; } = "";

    public string ImageUrl =>
        string.IsNullOrEmpty(PosterPath)
            ? "https://placehold.co/300x450?text=No+Poster"
            : $"https://image.tmdb.org/t/p/w500{PosterPath}";
}