using CinemaProject.Data;
using CinemaProject.Models;
using Microsoft.EntityFrameworkCore;

namespace CinemaProject.Services;

public sealed class CategoryService(ApplicationDbContext dbContext) : ICategoryService
{
    public async Task<PagedResult<Category>> SearchAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Categories.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchTerm = search.Trim();
            query = query.Where(category => category.Name.Contains(searchTerm));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        page = totalPages > 0 ? Math.Min(Math.Max(page, 1), totalPages) : 1;
        var items = await query
            .OrderBy(category => category.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Category>(items, totalCount, page, pageSize);
    }

    public Task<Category?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        dbContext.Categories.FirstOrDefaultAsync(category => category.Id == id, cancellationToken);

    public async Task CreateAsync(Category category, CancellationToken cancellationToken = default)
    {
        dbContext.Categories.Add(category);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> UpdateAsync(
        int id,
        Category category,
        CancellationToken cancellationToken = default)
    {
        var existingCategory = await dbContext.Categories.FindAsync([id], cancellationToken);
        if (existingCategory is null)
        {
            return false;
        }

        existingCategory.Name = category.Name;
        existingCategory.Description = category.Description;
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<CategoryDeleteResult> DeleteAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var category = await dbContext.Categories
            .Include(item => item.Movies)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (category is null)
        {
            return CategoryDeleteResult.NotFound;
        }

        if (category.Movies.Count > 0)
        {
            return CategoryDeleteResult.HasMovies;
        }

        dbContext.Categories.Remove(category);
        await dbContext.SaveChangesAsync(cancellationToken);
        return CategoryDeleteResult.Deleted;
    }
}
