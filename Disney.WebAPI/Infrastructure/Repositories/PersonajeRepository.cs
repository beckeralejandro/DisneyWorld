using Disney.WebAPI.Model;
using Microsoft.EntityFrameworkCore;

namespace Disney.WebAPI.Infrastructure.Repositories
{
    public class PersonajeRepository : IPersonajeRepository
    {
        private readonly DisneyDbContext db;

        public PersonajeRepository(DisneyDbContext db)
        {
            this.db = db;
        }

        public async Task<List<Personaje>> GetAll()
        {
            return await db.Personaje.ToListAsync();
        }

        public async Task<List<Personaje>> GetFiltered(string? name, int? age, string? movies)
        {
            var result = db.Personaje.AsQueryable();
            if (!string.IsNullOrWhiteSpace(name))
                result = result.Where(x => x.Nombre == name);
            if (age.HasValue)
                result = result.Where(x => x.Edad == age);
            if (!string.IsNullOrWhiteSpace(movies)) {
                var arrMoviesIds = movies.Split(',').Select(x => Int32.Parse(x)).ToArray();
                result = result.Where(x => x.Peliculas.Any(p => arrMoviesIds.Contains(p.Id)));
            }

            return await result.ToListAsync();
        }

        public Personaje? GetDetailById(int id)
        {
            return db.Personaje.Where(x => x.Id == id).Include(x => x.Peliculas).ThenInclude(x => x.Genero).FirstOrDefault();
        }

        public async Task Add(Personaje personaje)
        {
            db.Personaje.Add(personaje);
            await db.SaveChangesAsync();
        }

        public async Task Remove(int personajeId)
        {
            var personaje = db.Personaje.Where(x => x.Id == personajeId).First();
            db.Personaje.Remove(personaje);
            await db.SaveChangesAsync();
        }

        public async Task Update(Personaje personaje)
        {
            var p = db.Personaje.Where(x => x.Id == personaje.Id).FirstOrDefault();
            p.Peso = personaje.Peso;
            p.Imagen = personaje.Imagen;
            p.Historia = personaje.Historia;
            p.Edad = personaje.Edad;
            p.Nombre = personaje.Nombre;

            await db.SaveChangesAsync();
        }
    }
}
