namespace AnimeMovieTracker.Models;

public class SavedMediaItem
{
    public int Id { get; set; }

    public string UserId { get; set; } = "";

    public int MediaId { get; set; }

    public string MediaType { get; set; } = "";

    public string Title { get; set; } = "";

    public string ImageUrl { get; set; } = "";

    public string Status { get; set; } = "";

    public bool IsFavorite { get; set; }

    public ApplicationUser? User { get; set; }
}