using System.Security.Claims;
using AnimeMovieTracker.Data;
using AnimeMovieTracker.Models;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;

namespace AnimeMovieTracker.Services;

public class SavedMediaService
{
    private readonly ApplicationDbContext _context;
    private readonly AuthenticationStateProvider _authProvider;

    public SavedMediaService(ApplicationDbContext context, AuthenticationStateProvider authProvider)
    {
        _context = context;
        _authProvider = authProvider;
    }

    private async Task<string?> GetUserIdAsync()
    {
        var authState = await _authProvider.GetAuthenticationStateAsync();
        return authState.User.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    public async Task<List<SavedMediaItem>> GetMyItemsAsync()
    {
        var userId = await GetUserIdAsync();

        if (string.IsNullOrWhiteSpace(userId))
            return new();

        return await _context.SavedMediaItems
            .Where(x => x.UserId == userId)
            .AsNoTracking()
            .OrderBy(x => x.Title)
            .ToListAsync();
    }

    public async Task DeleteItemAsync(int id)
    {
        var userId = await GetUserIdAsync();

        var item = await _context.SavedMediaItems
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);

        if (item is null)
            return;

        _context.SavedMediaItems.Remove(item);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateStatusAsync(int id, string status)
    {
        var userId = await GetUserIdAsync();

        var item = await _context.SavedMediaItems
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);

        if (item is null)
            return;

        item.Status = status;
        await _context.SaveChangesAsync();
    }

    public async Task ToggleFavoriteAsync(int id)
    {
        var userId = await GetUserIdAsync();

        var item = await _context.SavedMediaItems
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);

        if (item is null)
            return;

        item.IsFavorite = !item.IsFavorite;
        await _context.SaveChangesAsync();
    }

    public async Task SaveOrUpdateItemAsync(
    int mediaId,
    string mediaType,
    string title,
    string imageUrl,
    string status,
    bool isFavorite)
    {
        var userId = await GetUserIdAsync();

        if (string.IsNullOrWhiteSpace(userId))
            return;

        var item = await _context.SavedMediaItems
            .FirstOrDefaultAsync(x =>
                x.UserId == userId &&
                x.MediaId == mediaId &&
                x.MediaType == mediaType);

        if (item is null)
        {
            item = new SavedMediaItem
            {
                UserId = userId,
                MediaId = mediaId,
                MediaType = mediaType.Trim(),
                Title = title.Trim(),
                ImageUrl = imageUrl,
                Status = status.Trim(),
                IsFavorite = isFavorite
            };

            _context.SavedMediaItems.Add(item);
        }
        else
        {
            item.Title = title.Trim();
            item.ImageUrl = imageUrl;
            item.Status = status.Trim();
            item.IsFavorite = isFavorite;
        }

        await _context.SaveChangesAsync();
    }
}