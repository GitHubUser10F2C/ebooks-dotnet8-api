using ebooks_dotnet8_api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<DataContext>(opt => opt.UseInMemoryDatabase("ebooks"));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
var app = builder.Build();

var ebooks = app.MapGroup("api/ebook");

// TODO: Add more routes
ebooks.MapPost("/", CreateEBook);
ebooks.MapGet("/", GetEbooks);
ebooks.MapPut("/{id}", UpdateEbook);
ebooks.MapPut("/{id}/change-availability", ChangeAvailability);
ebooks.MapPut("/{id}/increment-stock", IncrementStock);
ebooks.MapPost("/purchase", Purchase);
ebooks.MapDelete("/{id}", DeleteEbook);

app.Run();

// TODO: Add more methods
static IResult CreateEBook(DataContext context, EBook ebook)
{
    if (context.EBooks.Any(x => x.Title == ebook.Title))
    {
        if (context.EBooks.Any(x => x.Author == ebook.Author))
        {
            return TypedResults.BadRequest("Un ebook con el mismo título y autor ya existe");
        }
    }

    ebook.IsAvailable = true;
    ebook.Stock = 0;

    context.EBooks.Add(ebook);
    context.SaveChanges();

    return TypedResults.Created($"/ebook/{ebook.Id}", ebook);
}

static IResult GetEbooks(DataContext context, [FromQuery]string genre, [FromQuery]string author, [FromQuery]string format)
{

    return TypedResults.Ok(context.EBooks.ToArray());
}

static IResult UpdateEbook(int id, DataContext context, EBook inputEbook)
{
    var ebook = context.EBooks.Find(id);

    if (ebook is null)
        return TypedResults.NotFound();

    if (context.EBooks.Any(x => x.Title == inputEbook.Title))
    {
        if (context.EBooks.Any(x => x.Author == inputEbook.Author))
        {
            return TypedResults.BadRequest("Un ebook con el mismo título y autor ya existe, no se pudo actualizar");
        }
    }

    ebook.Title = inputEbook.Title;
    ebook.Author = inputEbook.Author;
    ebook.Genre = inputEbook.Genre;
    ebook.Format = inputEbook.Format;
    ebook.Price = inputEbook.Price;

    context.SaveChangesAsync();

    return TypedResults.NoContent();
}

static IResult ChangeAvailability(int id, DataContext context)
{
    var ebook = context.EBooks.Find(id);

    if (ebook is null)
        return TypedResults.NotFound();

    ebook.IsAvailable = !ebook.IsAvailable;

    context.SaveChangesAsync();

    return TypedResults.NoContent();
}

static IResult IncrementStock(int id, [FromBody]int stock, DataContext context)
{
    var ebook = context.EBooks.Find(id);

    if (ebook is null)
        return TypedResults.NotFound();

    ebook.Stock += stock;

    context.SaveChangesAsync();

    return TypedResults.NoContent();
}

static IResult Purchase(int id, int stockToBuy, int amountToPay, DataContext context)
{
    var ebook = context.EBooks.Find(id);

    if (ebook is null)
        return TypedResults.NotFound();

    if (ebook.Stock < stockToBuy)
    {
        return TypedResults.BadRequest("No hay suficiente stock para cubrir la compra");
    }

    if (ebook.Price * stockToBuy != amountToPay)
    {
        return TypedResults.BadRequest("El monto a pagar no corresponde con el calculado por el sistema");
    }

    ebook.Stock -= stockToBuy;

    context.SaveChangesAsync();

    return TypedResults.NoContent();
}

static IResult DeleteEbook(int id, DataContext context)
{
    if (context.EBooks.Find(id) is EBook eBook)
    {
        context.EBooks.Remove(eBook);
        context.SaveChangesAsync();
        return TypedResults.NoContent();
    }

    return TypedResults.NotFound();
}