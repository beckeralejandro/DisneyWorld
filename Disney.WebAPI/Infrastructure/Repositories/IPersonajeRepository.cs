using Disney.WebAPI.Model;

namespace Disney.WebAPI.Infrastructure.Repositories
{
    public interface IPersonajeRepository
    {
        Task<List<Personaje>> GetAll();
        Task<List<Personaje>> GetFiltered(string? name, int? age, string? movies);
        Personaje GetDetailById(int id);
        Task Add(Personaje personaje);
        Task Remove(int personajeId);
        Task Update(Personaje personaje);
    }
}
