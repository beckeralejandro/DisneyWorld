using FluentValidation;
using System.ComponentModel.DataAnnotations;

namespace Disney.WebAPI.Model
{
    public class Pelicula
    {
        [Key]
        public int Id { get; set; }
        [StringLength(250)]
        public string Titulo { get; set; }
        public DateTime FechaCreacion { get; set; }
        [Range(1, 5)]
        public Int16 Clasificacion { get; set; }
        public byte[] Imagen { get; set; }

        public int? GeneroId { get; set; }
        

        public Genero? Genero { get; set; }
        public ICollection<Personaje> Personajes { get; set; }
    }

    public class PeliculaValidator : AbstractValidator<Pelicula>
    {
        public PeliculaValidator()
        {
            RuleSet("Id", () =>
            {
                RuleFor(x => x.Id).NotNull().GreaterThan(0);
            });

            RuleFor(x => x.Titulo).NotEmpty().Length(2, 150);
            RuleFor(x => (int)x.Clasificacion).InclusiveBetween(1, 5);
            RuleFor(x => x.Imagen).NotNull();
        }
    }
}
