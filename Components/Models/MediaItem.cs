namespace AnimeMovieTracker.Components.Models;

public class MediaItem
{
    public bool IsFavorite { get; set; }
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Type { get; set; } = ""; // Anime or Movie
    public string Genre { get; set; } = "";
    public int Year { get; set; }
    public string Status { get; set; } = "";
    public double Rating { get; set; }
    public string ImageUrl { get; set; } = "";
    public string Description { get; set; } = "";
}