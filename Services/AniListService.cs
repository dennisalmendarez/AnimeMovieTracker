using System.Net.Http.Json;
using System.Text.Json.Serialization;
using AnimeMovieTracker.Components.Models;

namespace AnimeMovieTracker.Services;

public class AniListService
{
    private readonly HttpClient _httpClient;

    public AniListService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<AnimePageResult> GetAnimePageAsync(
        int page = 1,
        int perPage = 30,
        string sort = "TITLE_ROMAJI",
        string? search = null,
        List<string>? genres = null)
    {
        var request = new
        {
            query = """
            query ($page: Int, $perPage: Int, $sort: [MediaSort], $search: String, $genre_in: [String], $genre_not_in: [String]) {
              Page(page: $page, perPage: $perPage) {
                pageInfo {
                  currentPage
                  hasNextPage
                }
                media(
                  type: ANIME,
                  search: $search,
                  sort: $sort,
                  genre_in: $genre_in,
                  genre_not_in: $genre_not_in
                ) {
                  id
                  title {
                    romaji
                    english
                  }
                  coverImage {
                    large
                  }
                  startDate {
                    year
                  }
                  genres
                  averageScore
                  episodes
                  status
                  description(asHtml: false)
                }
              }
            }
            """,
            variables = new
            {
                page,
                perPage,
                sort = new[] { sort },
                search = string.IsNullOrWhiteSpace(search) ? null : search,
                genre_in = genres is { Count: > 0 } ? genres : null,
                genre_not_in = new[] { "Hentai" }
            }
        };

        var response = await _httpClient.PostAsJsonAsync("https://graphql.anilist.co", request);

        if (!response.IsSuccessStatusCode)
        {
            return new AnimePageResult();
        }

        var result = await response.Content.ReadFromJsonAsync<AniListResponse>();

        var items = result?.Data?.Page?.Media?
            .Where(a => !a.Genres.Contains("Hentai"))
            .Select(a => new MediaItem
            {
                Id = a.Id,
                Title = a.Title.Romaji ?? a.Title.English ?? "Unknown Title",
                Type = "Anime",
                Genre = a.Genres.FirstOrDefault() ?? "Unknown",
                Year = a.StartDate?.Year ?? 0,
                Rating = (a.AverageScore ?? 0) / 10.0,
                ImageUrl = a.CoverImage?.Large ?? "",
                Description = a.Description ?? "",
                Status = ""
            }).ToList() ?? new List<MediaItem>();

        return new AnimePageResult
        {
            Items = items,
            CurrentPage = result?.Data?.Page?.PageInfo?.CurrentPage ?? page,
            HasNextPage = result?.Data?.Page?.PageInfo?.HasNextPage ?? false
        };
    }

    public async Task<List<string>> GetGenresAsync()
    {
        var request = new
        {
            query = """
            query {
              GenreCollection
            }
            """
        };

        var response = await _httpClient.PostAsJsonAsync("https://graphql.anilist.co", request);

        if (!response.IsSuccessStatusCode)
        {
            return new List<string>();
        }

        var result = await response.Content.ReadFromJsonAsync<AniListGenreResponse>();

        return result?.Data?.GenreCollection?
            .Where(g => g != "Hentai")
            .OrderBy(g => g)
            .ToList() ?? new List<string>();
    }

    public async Task<List<AnimeCharacter>> GetAnimeCharactersAsync(int animeId)
    {
        var request = new
        {
            query = """
        query ($id: Int) {
          Media(id: $id, type: ANIME) {
            characters(page: 1, perPage: 12, sort: ROLE) {
              edges {
                role
                node {
                  name {
                    full
                  }
                  image {
                    medium
                  }
                }
                voiceActors(language: JAPANESE, sort: RELEVANCE) {
                  name {
                    full
                  }
                }
              }
            }
          }
        }
        """,
            variables = new
            {
                id = animeId
            }
        };

        var response = await _httpClient.PostAsJsonAsync("https://graphql.anilist.co", request);

        if (!response.IsSuccessStatusCode)
        {
            return new List<AnimeCharacter>();
        }

        var result = await response.Content.ReadFromJsonAsync<AniListCharacterResponse>();

        return result?.Data?.Media?.Characters?.Edges?
            .Where(e => e.Role == "MAIN" || e.Role == "Main")
            .Take(12)
            .Select(e => new AnimeCharacter
            {
                Name = e.Node?.Name?.Full ?? "Unknown",
                ImageUrl = e.Node?.Image?.Medium ?? "",
                VoiceActor = e.VoiceActors?.FirstOrDefault()?.Name?.Full ?? ""
            })
            .ToList() ?? new List<AnimeCharacter>();
    }
}

public class AnimePageResult
{
    public List<MediaItem> Items { get; set; } = new();
    public int CurrentPage { get; set; }
    public bool HasNextPage { get; set; }
}

public class AniListResponse
{
    [JsonPropertyName("data")]
    public AniListData? Data { get; set; }
}

public class AniListData
{
    [JsonPropertyName("Page")]
    public AniListPage? Page { get; set; }
}

public class AniListPage
{
    [JsonPropertyName("pageInfo")]
    public AniListPageInfo? PageInfo { get; set; }

    [JsonPropertyName("media")]
    public List<AniListAnime>? Media { get; set; }
}

public class AniListPageInfo
{
    [JsonPropertyName("currentPage")]
    public int? CurrentPage { get; set; }

    [JsonPropertyName("hasNextPage")]
    public bool HasNextPage { get; set; }
}

public class AniListAnime
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("title")]
    public AniListTitle Title { get; set; } = new();

    [JsonPropertyName("coverImage")]
    public AniListCoverImage? CoverImage { get; set; }

    [JsonPropertyName("startDate")]
    public AniListStartDate? StartDate { get; set; }

    [JsonPropertyName("genres")]
    public List<string> Genres { get; set; } = new();

    [JsonPropertyName("averageScore")]
    public int? AverageScore { get; set; }

    [JsonPropertyName("episodes")]
    public int? Episodes { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

public class AniListTitle
{
    [JsonPropertyName("romaji")]
    public string? Romaji { get; set; }

    [JsonPropertyName("english")]
    public string? English { get; set; }
}

public class AniListCoverImage
{
    [JsonPropertyName("large")]
    public string? Large { get; set; }
}

public class AniListStartDate
{
    [JsonPropertyName("year")]
    public int? Year { get; set; }
}

public class AniListGenreResponse
{
    [JsonPropertyName("data")]
    public AniListGenreData? Data { get; set; }
}

public class AniListGenreData
{
    [JsonPropertyName("GenreCollection")]
    public List<string>? GenreCollection { get; set; }
}

public class AnimeCharacter
{
    public string Name { get; set; } = "";
    public string ImageUrl { get; set; } = "";
    public string VoiceActor { get; set; } = "";
}

public class AniListCharacterResponse
{
    [JsonPropertyName("data")]
    public AniListCharacterData? Data { get; set; }
}

public class AniListCharacterData
{
    [JsonPropertyName("Media")]
    public AniListCharacterMedia? Media { get; set; }
}

public class AniListCharacterMedia
{
    [JsonPropertyName("characters")]
    public AniListCharactersConnection? Characters { get; set; }
}

public class AniListCharactersConnection
{
    [JsonPropertyName("edges")]
    public List<AniListCharacterEdge>? Edges { get; set; }
}

public class AniListCharacterEdge
{
    [JsonPropertyName("role")]
    public string? Role { get; set; }

    [JsonPropertyName("node")]
    public AniListCharacterNode? Node { get; set; }

    [JsonPropertyName("voiceActors")]
    public List<AniListVoiceActor>? VoiceActors { get; set; }
}

public class AniListCharacterNode
{
    [JsonPropertyName("name")]
    public AniListCharacterName? Name { get; set; }

    [JsonPropertyName("image")]
    public AniListCharacterImage? Image { get; set; }
}

public class AniListCharacterName
{
    [JsonPropertyName("full")]
    public string? Full { get; set; }
}

public class AniListCharacterImage
{
    [JsonPropertyName("medium")]
    public string? Medium { get; set; }
}

public class AniListVoiceActor
{
    [JsonPropertyName("name")]
    public AniListCharacterName? Name { get; set; }
}