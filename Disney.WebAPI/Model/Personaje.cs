using FluentValidation;
using System.ComponentModel.DataAnnotations;

namespace Disney.WebAPI.Model
{
    public class Personaje
    {
        [Key]
        public int Id { get; set; }
        [StringLength(150)]
        public string Nombre { get; set; }
        public Int16 Edad { get; set; }
        public Decimal Peso { get; set; }
        public string Historia { get; set; }
        public byte[] Imagen { get; set; }

        public ICollection<Pelicula> Peliculas { get; set; }
    }

    public class PersonajeValidator : AbstractValidator<Personaje>
    {
        public PersonajeValidator()
        {
            RuleSet("Id", () =>
            {
                RuleFor(x => x.Id).NotNull().GreaterThan(0);
            });

            RuleFor(x => x.Nombre).NotEmpty().Length(2, 150);
            RuleFor(x => (int)x.Edad).InclusiveBetween(1, Int16.MaxValue);
            RuleFor(x => x.Peso).GreaterThanOrEqualTo(0.1M);
            RuleFor(x => x.Historia).NotEmpty();
            RuleFor(x => x.Imagen).NotNull();
        }
    }
}
