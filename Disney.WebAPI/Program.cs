using Disney.WebAPI;
using Disney.WebAPI.Infrastructure.Email;
using Disney.WebAPI.Infrastructure.Repositories;
using Disney.WebAPI.Model;
using Disney.WebAPI.ViewModel.Auth;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("AppDb");
builder.Services.AddDbContext<DisneyDbContext>(x => x.UseSqlServer(connectionString));

IdentityBuilder ib = builder.Services.AddIdentityCore<IdentityUser>(opt =>
{
    opt.Password.RequireDigit = false;
    opt.Password.RequiredLength = 3;
    opt.Password.RequireNonAlphanumeric = false;
    opt.Password.RequireUppercase = false;
    opt.Password.RequireLowercase = false;
});

ib = new IdentityBuilder(ib.UserType, typeof(IdentityRole), builder.Services);
ib.AddEntityFrameworkStores<DisneyDbContext>();

ib.AddRoleValidator<RoleValidator<IdentityRole>>();
ib.AddRoleManager<RoleManager<IdentityRole>>();
ib.AddSignInManager<SignInManager<IdentityUser>>();

ValidatorOptions.Global.LanguageManager.Culture = new CultureInfo("es");
builder.Services.AddFluentValidation(fv => fv.RegisterValidatorsFromAssemblyContaining<PersonajeValidator>());
builder.Services.AddFluentValidation(fv => fv.RegisterValidatorsFromAssemblyContaining<PeliculaValidator>());

builder.Services.AddAuthorization();
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = "Blah.Blah.Bearer",
        ValidAudience = "Blah.Blah.Bearer",
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("RANDOM_KEY_MUST_NOT_BE_SHARED"))
    };
});

builder.Services.AddScoped<IPersonajeRepository, PersonajeRepository>();
builder.Services.AddScoped<IPeliculaRepository, PeliculaRepository>();
builder.Services.AddScoped<ISendEmail, SendEmailMailGun>();

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Api", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: {token}\"",
        Name = "Authorization",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement()
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new List<string>()
        }});
});
builder.Services.AddCors();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
        c.DocumentTitle = "Todo APIs";
    });
}

app.UseHttpsRedirection();

#region Auth
app.MapPost("/auth/register", async (UserManager<IdentityUser> userManager, ISendEmail sendEmail, RegisterViewModel user) =>
{
    var newUser = new IdentityUser { UserName = user.UserName, Email = user.Email };

    var result = await userManager.CreateAsync(newUser, user.Password);
    if (!result.Succeeded)
    {
        var errors = result.Errors.Select(x => x.Description);
        return Results.BadRequest(errors);
    }

    await sendEmail.SendMail(user.Email, "Registración", "Gracias por registrarte");

    return Results.Ok();
});
app.MapPost("/auth/login", async (UserManager <IdentityUser> userManager, SignInManager <IdentityUser> signInManager, LoginViewModel user) =>
{
    var u = await userManager.FindByNameAsync(user.UserName);

    if (u == null)
        return Results.BadRequest("Usuario y clave incorrectos");

    var result = await signInManager.CheckPasswordSignInAsync(u, user.Password, false);

    if (!result.Succeeded) return Results.BadRequest("Usuario y clave incorrectos");

    var claims = new[]
    {
        new Claim(ClaimTypes.Name, user.UserName)
    };

    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("RANDOM_KEY_MUST_NOT_BE_SHARED"));
    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    var expiry = DateTime.Now.AddDays(3);

    var token = new JwtSecurityToken(
        //null,null,
        "Blah.Blah.Bearer", "Blah.Blah.Bearer",
        claims,
        expires: expiry,
        signingCredentials: creds
    );

    return Results.Ok(new JwtSecurityTokenHandler().WriteToken(token));
});
#endregion

#region Personajes
//app.MapGet("/characters", async ([FromServices] IPersonajeRepository personajeRepository) =>
//{
//    var personajes = await personajeRepository.GetAll();
//    return Results.Ok(personajes.Select(x => new
//    {
//        x.Imagen,
//        x.Nombre
//    }));
//});

app.MapGet("/characters", async (IPersonajeRepository personajeRepository, string? name, int? age, string? movies) =>
{
    var personajes = await personajeRepository.GetFiltered(name, age, movies);
    return Results.Ok(personajes.Select(x => new
    {
        x.Imagen,
        x.Nombre
    }));
}).RequireAuthorization();

app.MapPost("/character", async (IPersonajeRepository personajeRepository, IValidator<Personaje> PersonajeValidator, HttpRequest req) =>
{
    try
    {
        Personaje personaje = new();

        if (!req.HasFormContentType)
        {
            return Results.BadRequest();
        }

        var form = await req.ReadFormAsync();
        var file = form.Files["Imagen"];

        if (file is null)
        {
            return Results.BadRequest();
        }

        await using var uploadStream = file.OpenReadStream();

        uploadStream.Seek(0, SeekOrigin.Begin);
        using var memoryStream = new MemoryStream();
        {
            uploadStream.CopyTo(memoryStream);
            personaje.Imagen = memoryStream.ToArray();
        }

        if (req.Form["Nombre"].Count() != 0)
            personaje.Nombre = req.Form["Nombre"].ToString();
        if (req.Form["Edad"].Count() != 0)
            personaje.Edad = Int16.Parse(req.Form["Edad"].ToString());
        if (req.Form["Peso"].Count() != 0)
            personaje.Peso = Decimal.Parse(req.Form["Peso"].ToString());
        if (req.Form["Historia"].Count() != 0)
            personaje.Historia = req.Form["Historia"].ToString();

        var validationResult = PersonajeValidator.Validate(personaje);

        if (!validationResult.IsValid)
        {
            var errors = new { Errors = validationResult.Errors.Select(x => x.ErrorMessage) };
            return Results.BadRequest(errors);
        }

        await personajeRepository.Add(personaje);

        return Results.Ok();
    }
    catch
    {
        return Results.BadRequest();
    }
}).Accepts<IFormFile>("multipart/form-data").RequireAuthorization();

app.MapPut("/character", async (IPersonajeRepository personajeRepository, IValidator<Personaje> PersonajeValidator, HttpRequest req) =>
{
    try
    {
        Personaje personaje = new();

        if (!req.HasFormContentType)
        {
            return Results.BadRequest();
        }

        var form = await req.ReadFormAsync();
        var file = form.Files["Imagen"];

        if (file is null)
        {
            return Results.BadRequest();
        }

        await using var uploadStream = file.OpenReadStream();

        uploadStream.Seek(0, SeekOrigin.Begin);
        using var memoryStream = new MemoryStream();
        {
            uploadStream.CopyTo(memoryStream);
            personaje.Imagen = memoryStream.ToArray();
        }

        if (req.Form["Id"].Count() != 0)
            personaje.Id = int.Parse(req.Form["Id"].ToString());
        if (req.Form["Nombre"].Count() != 0)
            personaje.Nombre = req.Form["Nombre"].ToString();
        if (req.Form["Edad"].Count() != 0)
            personaje.Edad = Int16.Parse(req.Form["Edad"].ToString());
        if (req.Form["Peso"].Count() != 0)
            personaje.Peso = Decimal.Parse(req.Form["Peso"].ToString());
        if (req.Form["Historia"].Count() != 0)
            personaje.Historia = req.Form["Historia"].ToString();

        var validationResult = PersonajeValidator.Validate(personaje, options =>
        {
            options.IncludeRuleSets("Id").IncludeAllRuleSets();
        });

        if (!validationResult.IsValid)
        {
            var errors = new { Errors = validationResult.Errors.Select(x => x.ErrorMessage) };
            return Results.BadRequest(errors);
        }

        await personajeRepository.Update(personaje);

        return Results.Ok();
    }
    catch
    {
        return Results.BadRequest();
    }
}).Accepts<IFormFile>("multipart/form-data").RequireAuthorization();

app.MapDelete("/character/{id}", async (IPersonajeRepository personajeRepository, int id) =>
{
    await personajeRepository.Remove(id);

    return Results.Ok();
}).RequireAuthorization();

app.MapGet("/character{id}", (IPersonajeRepository personajeRepository, int id) =>
{
    var personaje = personajeRepository.GetDetailById(id);

    if (personaje == null)
        return Results.BadRequest("Personaje no existe");

    return Results.Ok(
        new
        {
            personaje.Imagen,
            personaje.Peso,
            personaje.Edad,
            personaje.Nombre,
            personaje.Historia,
            Peliculas = personaje.Peliculas.Select(x => new
            {
                x.Titulo,
                x.Imagen,
                x.Clasificacion,
                x.FechaCreacion,
                Genero = new
                {
                    x?.Genero?.Nombre,
                    x?.Genero?.Imagen
                }
            }).ToList()
        });
}).RequireAuthorization();

#endregion

#region Peliculas
//app.MapGet("/movies", async ([FromServices] IPeliculaRepository peliculaRepository) =>
//{
//    var peliculas = await peliculaRepository.GetAll();
//    return Results.Ok(peliculas.Select(x => new
//    {
//        x.Imagen,
//        x.Titulo,
//        x.FechaCreacion
//    }));
//});

app.MapGet("/movies", async (IPeliculaRepository peliculaRepository, string? name, int? genre, string order) =>
{
    var peliculas = await peliculaRepository.GetFiltered(name, genre, order);
    return Results.Ok(peliculas.Select(x => new
    {
        x.Imagen,
        x.Titulo,
        x.FechaCreacion
    }));
}).RequireAuthorization();

app.MapPost("/movie", async (IPeliculaRepository peliculaRepository, IValidator<Pelicula> PeliculaValidator, HttpRequest req) =>
{
    try
    {
        Pelicula pelicula = new();

        if (!req.HasFormContentType)
        {
            return Results.BadRequest();
        }

        var form = await req.ReadFormAsync();
        var file = form.Files["Imagen"];

        if (file is null)
        {
            return Results.BadRequest();
        }

        await using var uploadStream = file.OpenReadStream();

        uploadStream.Seek(0, SeekOrigin.Begin);
        using var memoryStream = new MemoryStream();
        {
            uploadStream.CopyTo(memoryStream);
            pelicula.Imagen = memoryStream.ToArray();
        }

        if (req.Form["Titulo"].Count() != 0)
            pelicula.Titulo = req.Form["Titulo"].ToString();
        if (req.Form["Clasificacion"].Count() != 0)
            pelicula.Clasificacion = Int16.Parse(req.Form["Clasificacion"].ToString());
        if (req.Form["FechaCreacion"].Count() != 0)
            pelicula.FechaCreacion = DateTime.Parse(req.Form["FechaCreacion"].ToString());

        var validationResult = PeliculaValidator.Validate(pelicula);

        if (!validationResult.IsValid)
        {
            var errors = new { Errors = validationResult.Errors.Select(x => x.ErrorMessage) };
            return Results.BadRequest(errors);
        }

        await peliculaRepository.Add(pelicula);

        return Results.Ok();
    }
    catch
    {
        return Results.BadRequest();
    }
}).RequireAuthorization();

app.MapPut("/movie", async (IPeliculaRepository peliculaRepository, IValidator<Pelicula> PeliculaValidator, HttpRequest req) =>
{
    Pelicula pelicula = new();

    if (!req.HasFormContentType)
    {
        return Results.BadRequest();
    }

    var form = await req.ReadFormAsync();
    var file = form.Files["Imagen"];

    if (file is null)
    {
        return Results.BadRequest();
    }

    await using var uploadStream = file.OpenReadStream();

    uploadStream.Seek(0, SeekOrigin.Begin);
    using var memoryStream = new MemoryStream();
    {
        uploadStream.CopyTo(memoryStream);
        pelicula.Imagen = memoryStream.ToArray();
    }

    if (req.Form["Id"].Count() != 0)
        pelicula.Id = int.Parse(req.Form["Id"].ToString());
    if (req.Form["Titulo"].Count() != 0)
        pelicula.Titulo = req.Form["Titulo"].ToString();
    if (req.Form["Clasificacion"].Count() != 0)
        pelicula.Clasificacion = Int16.Parse(req.Form["Clasificacion"].ToString());
    if (req.Form["FechaCreacion"].Count() != 0)
        pelicula.FechaCreacion = DateTime.Parse(req.Form["FechaCreacion"].ToString());

    var validationResult = PeliculaValidator.Validate(pelicula, options =>
    {
        options.IncludeRuleSets("Id").IncludeAllRuleSets();
    });

    if (!validationResult.IsValid)
    {
        var errors = new { Errors = validationResult.Errors.Select(x => x.ErrorMessage) };
        return Results.BadRequest(errors);
    }

    await peliculaRepository.Update(pelicula);

    return Results.Ok();
}).RequireAuthorization();

app.MapDelete("/movie/{id}", async (IPeliculaRepository peliculaRepository, int id) =>
{
    await peliculaRepository.Remove(id);

    return Results.Ok();
}).RequireAuthorization();
#endregion

app.Run();