using System.ComponentModel.DataAnnotations;

namespace Disney.WebAPI.Model
{
    public class Genero
    {
        [Key]
        public int Id {  get; set; }
        [StringLength(50)]
        public string Nombre { get; set; }
        public byte[] Imagen { get; set; }

        public ICollection<Pelicula> Peliculas { get; set; }
    }
}
