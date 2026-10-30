# TaskCinema

## SOLID refactor

The application now uses small service contracts between MVC controllers and persistence or file storage:

- `IPublicMovieCatalog` handles customer movie listing, details, and home page data.
- `IAdminMovieService` handles movie administration and paging.
- `ICategoryService` handles category administration and deletion rules.
- `IMovieImageStorage` validates, saves, and deletes movie poster files.

Controllers depend on these abstractions through dependency injection. EF Core work lives in the catalog and administration services, while poster file operations live in `LocalMovieImageStorage`. SQL Server configuration is read from `ConnectionStrings:CinemaProject` in `TaskCinema/appsettings.json`.

## Run locally

1. Update `ConnectionStrings:CinemaProject` for your SQL Server instance.
2. From this folder, run `dotnet restore`.
3. Run `dotnet run --project TaskCinema/TaskCinema.csproj`.

The database `CinemaProject` must exist, or be created by applying the included Entity Framework migrations.
