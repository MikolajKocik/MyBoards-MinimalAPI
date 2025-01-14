using Microsoft.AspNetCore.Http.Json;
using Microsoft.EntityFrameworkCore;
using MyBoards.Entities;
using System;
using System.Linq.Expressions;
using System.Text.Json.Serialization;
using MyBoards.Dto;
using MyBoards;
using Sieve.Services;
using MyBoards.Sieve;
using Sieve.Models;
using Microsoft.AspNetCore.Mvc;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<ISieveProcessor, ApplicationSieveProcessor>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// podczas serializacji zostanie pomini�ta niesko�czonoa referencja mi�dzy zale�no�ciami, kt�ra jest zap�tlona

builder.Services.Configure<Microsoft.AspNetCore.Mvc.JsonOptions>(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
});


builder.Services.AddDbContext<MyBoardsContext>(
    option => option
    //.UseLazyLoadingProxies()
    .UseSqlServer(builder.Configuration.GetConnectionString("MyBoardsConnectionString"))
    );

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// informuje nas przed uruchomieniem aplikacji czy wyst�puj� jakiekolwiek migracje, kt�re zosta�y
// niezaaplikowane na bazie danych w connection stringu, je�li s� EF zaaplikuje to.

using var scope = app.Services.CreateScope(); // Tworzy zakres dla us�ug Dependency Injection, aby mo�na by�o uzyska� kontekst bazy danych
var dbContext = scope.ServiceProvider.GetService<MyBoardsContext>(); // pobiera instancj� klasy DbContext (nasz� baz�)

var pendingMigrations = dbContext.Database.GetPendingMigrations(); // Sprawdza, czy istniej� migracje, kt�re jeszcze nie zosta�y zastosowane w bazie danych

// Je�eli istniej� niezastosowane migracje, s� one stosowane za pomoc� metody Migrate()

if (pendingMigrations.Any())
{
    dbContext.Database.Migrate();
}

DataGenerator.Seed(dbContext); // wygenerowanie danych za pomoc� paczki Bogus

app.MapGet("ViewModel_data", async (MyBoardsContext db) =>
{
    var topAuthors = db.ViewTopAuthors.ToList();
    return topAuthors;
});

app.MapGet("pagination", async (MyBoardsContext db) =>
{
    // Dane wej�ciowe symuluj�ce parametry u�ytkownika
    var filter = "a"; // Wyszukiwanie po fragmencie tekstu w polach Email lub FullName
    string sortBy = "FullName"; // Kolumna do sortowania
    bool sortByDescending = false; // Kierunek sortowania (false = rosn�co)
    int pageNumber = 1; // Numer strony (1 = pierwsza strona)
    int pageSize = 10; // Liczba rekord�w na stronie

    // Filtrowanie wynik�w
    var query = db.Users
        .Where(u => filter == null || // Bez filtra pobieramy wszystkie dane
                    (u.Email.ToLower().Contains(filter.ToLower()) ||
                     u.FullName.ToLower().Contains(filter.ToLower())));

    // Liczenie wszystkich rekord�w spe�niaj�cych filtr (potrzebne do paginacji)
    var totalCount = query.Count();

    // Dodanie sortowania, je�li okre�lono kolumn�
    if (sortBy != null)
    {
        var columnsSelector = new Dictionary<string, Expression<Func<User, object>>>
        {
            {nameof(User.Email), user => user.Email},
            {nameof(User.FullName), user => user.FullName }
        };

        // Wyb�r kolumny do sortowania
        var sortByExpression = columnsSelector[sortBy];

        // Sortowanie: rosn�co lub malej�co
        query = sortByDescending
            ? query.OrderByDescending(sortByExpression)
            : query.OrderBy(sortByExpression);
    }

    // Paginacja: pomijamy rekordy z poprzednich stron i pobieramy dane dla bie��cej
    var result = query.Skip(pageSize * (pageNumber - 1))
                      .Take(pageSize)
                      .ToList();

    // Przygotowanie wyniku z danymi dla bie��cej strony i metadanymi paginacji
    var pageResult = new PageResult<User>(result, totalCount, pageSize, pageNumber);

    return pageResult;
});

app.MapGet("data", async (MyBoardsContext db) =>
{
    var userComments = await db.Users
    .Include(u => u.Comments)
    .Include(u => u.Address)
    .Where(u => u.Address.Country == "Albania")
    .SelectMany(u => u.Comments)
    .Select(c => c.Message)
    .ToListAsync();

    return userComments;

});

app.MapPut("update", async (MyBoardsContext db) =>
{
    Epic epic = await db.Epics.FirstAsync(epic => epic.Id == 1);

    var rejectedState = await db.WorkItemStates.FirstAsync(a => a.Value == "Rejected");

    epic.State = rejectedState;

    await db.SaveChangesAsync();

    return epic;

});

app.MapPost("sieve", async ([FromBody]SieveModel query, ISieveProcessor sieveProcessor, MyBoardsContext db) =>
{
    var epics = db.Epics
    .Include(e => e.Author)
    .AsQueryable();

    var dtos = await sieveProcessor
    .Apply(query, epics)
    .Select(e => new EpicDto()
    {
        Id = e.Id,
        Area = e.Area,
        Priority = e.Priority,
        StarDate = e.StartDate,
        AuthorFullName = e.Author.FullName
    })
    .ToListAsync();

    var totalCount = await sieveProcessor
    .Apply(query, epics, applyPagination: false, applySorting: false)
    .CountAsync();

    var result = new PageResult<EpicDto>(dtos, totalCount, query.PageSize.Value, query.Page.Value);

    return result;
});

app.MapPost("create", async (MyBoardsContext db) =>
{
    Tag mvcTag = new Tag()
    {
        Value = "MVC"
    };

    Tag aspTag = new Tag()
    {
        Value = "ASP"
    };

    var tags = new List<Tag>() { mvcTag, aspTag };

    await db.Tags.AddRangeAsync(mvcTag, aspTag); 
    await db.SaveChangesAsync();

    return tags;
});

// po zmianie w dbcontext z 'DeleteBehavior.NoAction' na 'DeleteBehavior.ClientCascade'
// kaskadowe usuwanie b�dzie automatyczne je�li mamy powi�zane encje

app.MapDelete("delete", async (MyBoardsContext db) =>
{
    var user = await db.Users
        .Include(u => u.Comments)
        .FirstAsync(u => u.Id == Guid.Parse("F26E49C7-2342-4F62-CBCC-08DA10AB0E61"));

    db.Users.Remove(user);

    await db.SaveChangesAsync();

});

app.Run();


