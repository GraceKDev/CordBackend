using CordBackend.Models;
using Microsoft.EntityFrameworkCore;
using Route = CordBackend.Models.Route;

namespace CordBackend.Data;

public sealed class CordDbContext(DbContextOptions<CordDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Coordinate> Coordinates => Set<Coordinate>();
    public DbSet<Route> Routes => Set<Route>();
}
