using Disney.WebAPI.Model;
using Microsoft.EntityFrameworkCore;

namespace Disney.WebAPI.Infrastructure.Repositories
{
    public class PeliculaRepository : IPeliculaRepository
    {
        private readonly DisneyDbContext db;

        public PeliculaRepository(DisneyDbContext db)
        {
            this.db = db;
        }

        public async Task<List<Pelicula>> GetAll()
        {
            return await db.Pelicula.ToListAsync();
        }

        public async Task<List<Pelicula>> GetFiltered(string? name, int? genre, string order)
        {
            var result = db.Pelicula.AsQueryable();
            if (!string.IsNullOrWhiteSpace(name))
                result = result.Where(x => x.Titulo == name);
            if (genre.HasValue)
                result = result.Where(x => x.GeneroId == genre);
            if (order == "ASC")
                result.OrderBy(x => x.Titulo);
            else
                result.OrderByDescending(x => x.Titulo);

            return await result.ToListAsync();
        }

        public async Task Add(Pelicula pelicula)
        {
            db.Pelicula.Add(pelicula);
            await db.SaveChangesAsync();
        }

        public async Task Remove(int peliculaId)
        {
            var pelicula = db.Pelicula.Where(x => x.Id == peliculaId).First();
            db.Pelicula.Remove(pelicula);
            await db.SaveChangesAsync();
        }

        public async Task Update(Pelicula pelicula)
        {
            var p = db.Pelicula.Where(x => x.Id == pelicula.Id).FirstOrDefault();
            p.Titulo = pelicula.Titulo;
            p.GeneroId = pelicula.GeneroId;
            p.FechaCreacion = pelicula.FechaCreacion;
            p.Clasificacion = pelicula.Clasificacion;
            p.Imagen = pelicula.Imagen;

            await db.SaveChangesAsync();
        }
    }
}
