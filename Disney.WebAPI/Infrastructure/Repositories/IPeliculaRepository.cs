using Disney.WebAPI.Model;

namespace Disney.WebAPI.Infrastructure.Repositories
{
    public interface IPeliculaRepository
    {
        Task<List<Pelicula>> GetAll();
        Task<List<Pelicula>> GetFiltered(string? name, int? genre, string order);
        Pelicula? GetDetailById(int id);
        Task Add(Pelicula pelicula);
        Task Remove(int peliculaId);
        Task Update(Pelicula pelicula);
    }
}
