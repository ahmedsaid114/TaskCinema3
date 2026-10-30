using CinemaProject.Data;
using CinemaProject.Models;
using Microsoft.EntityFrameworkCore;

namespace CinemaProject.Services;

public sealed class AdminMovieService(ApplicationDbContext dbContext) : IAdminMovieService
{
    public async Task<PagedResult<Movie>> SearchAsync(
        string? search,
        int? categoryId,
        int? cinemaId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Movies
            .Include(movie => movie.Category)
            .Include(movie => movie.Cinema)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchTerm = search.Trim();
            query = query.Where(movie => movie.Name.Contains(searchTerm));
        }

        if (categoryId.HasValue)
        {
            query = query.Where(movie => movie.CategoryId == categoryId.Value);
        }

        if (cinemaId.HasValue)
        {
            query = query.Where(movie => movie.CinemaId == cinemaId.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        page = totalPages > 0 ? Math.Min(Math.Max(page, 1), totalPages) : 1;
        var items = await query
            .OrderBy(movie => movie.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Movie>(items, totalCount, page, pageSize);
    }

    public async Task<IReadOnlyList<Category>> GetCategoriesAsync(
        CancellationToken cancellationToken = default) =>
        await dbContext.Categories
            .AsNoTracking()
            .OrderBy(category => category.Name)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Cinema>> GetCinemasAsync(
        CancellationToken cancellationToken = default) =>
        await dbContext.Cinemas
            .AsNoTracking()
            .OrderBy(cinema => cinema.Name)
            .ToListAsync(cancellationToken);

    public Task<Movie?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        dbContext.Movies.FirstOrDefaultAsync(movie => movie.Id == id, cancellationToken);

    public async Task CreateAsync(Movie movie, CancellationToken cancellationToken = default)
    {
        dbContext.Movies.Add(movie);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> UpdateAsync(
        int id,
        Movie movie,
        string? mainImagePath,
        CancellationToken cancellationToken = default)
    {
        var existingMovie = await dbContext.Movies
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (existingMovie is null)
        {
            return false;
        }

        existingMovie.Name = movie.Name;
        existingMovie.Description = movie.Description;
        existingMovie.Price = movie.Price;
        existingMovie.Status = movie.Status;
        existingMovie.ReleaseDate = movie.ReleaseDate;
        existingMovie.CategoryId = movie.CategoryId;
        existingMovie.CinemaId = movie.CinemaId;

        if (mainImagePath is not null)
        {
            existingMovie.MainImg = mainImagePath;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<(bool Found, string? ImagePath)> DeleteAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var movie = await dbContext.Movies.FindAsync([id], cancellationToken);
        if (movie is null)
        {
            return (false, null);
        }

        var imagePath = movie.MainImg;
        dbContext.Movies.Remove(movie);
        await dbContext.SaveChangesAsync(cancellationToken);
        return (true, imagePath);
    }
}
