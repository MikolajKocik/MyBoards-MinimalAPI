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

// podczas serializacji zostanie pominiêta nieskoñczonoa referencja miêdzy zale¿noœciami, która jest zapêtlona

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

// informuje nas przed uruchomieniem aplikacji czy wystêpuj¹ jakiekolwiek migracje, które zosta³y
// niezaaplikowane na bazie danych w connection stringu, jeœli s¹ EF zaaplikuje to.

using var scope = app.Services.CreateScope(); // Tworzy zakres dla us³ug Dependency Injection, aby mo¿na by³o uzyskaæ kontekst bazy danych
var dbContext = scope.ServiceProvider.GetService<MyBoardsContext>(); // pobiera instancjê klasy DbContext (nasz¹ bazê)

var pendingMigrations = dbContext.Database.GetPendingMigrations(); // Sprawdza, czy istniej¹ migracje, które jeszcze nie zosta³y zastosowane w bazie danych

// Je¿eli istniej¹ niezastosowane migracje, s¹ one stosowane za pomoc¹ metody Migrate()

if (pendingMigrations.Any())
{
    dbContext.Database.Migrate();
}

DataGenerator.Seed(dbContext); // wygenerowanie danych za pomoc¹ paczki Bogus

app.MapGet("ViewModel_data", async (MyBoardsContext db) =>
{
    var topAuthors = db.ViewTopAuthors.ToList();
    return topAuthors;
});

app.MapGet("pagination", async (MyBoardsContext db) =>
{
    // Dane wejœciowe symuluj¹ce parametry u¿ytkownika
    var filter = "a"; // Wyszukiwanie po fragmencie tekstu w polach Email lub FullName
    string sortBy = "FullName"; // Kolumna do sortowania
    bool sortByDescending = false; // Kierunek sortowania (false = rosn¹co)
    int pageNumber = 1; // Numer strony (1 = pierwsza strona)
    int pageSize = 10; // Liczba rekordów na stronie

    // Filtrowanie wyników
    var query = db.Users
        .Where(u => filter == null || // Bez filtra pobieramy wszystkie dane
                    (u.Email.ToLower().Contains(filter.ToLower()) ||
                     u.FullName.ToLower().Contains(filter.ToLower())));

    // Liczenie wszystkich rekordów spe³niaj¹cych filtr (potrzebne do paginacji)
    var totalCount = query.Count();

    // Dodanie sortowania, jeœli okreœlono kolumnê
    if (sortBy != null)
    {
        var columnsSelector = new Dictionary<string, Expression<Func<User, object>>>
        {
            {nameof(User.Email), user => user.Email},
            {nameof(User.FullName), user => user.FullName }
        };

        // Wybór kolumny do sortowania
        var sortByExpression = columnsSelector[sortBy];

        // Sortowanie: rosn¹co lub malej¹co
        query = sortByDescending
            ? query.OrderByDescending(sortByExpression)
            : query.OrderBy(sortByExpression);
    }

    // Paginacja: pomijamy rekordy z poprzednich stron i pobieramy dane dla bie¿¹cej
    var result = query.Skip(pageSize * (pageNumber - 1))
                      .Take(pageSize)
                      .ToList();

    // Przygotowanie wyniku z danymi dla bie¿¹cej strony i metadanymi paginacji
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
// kaskadowe usuwanie bêdzie automatyczne jeœli mamy powi¹zane encje

app.MapDelete("delete", async (MyBoardsContext db) =>
{
    var user = await db.Users
        .Include(u => u.Comments)
        .FirstAsync(u => u.Id == Guid.Parse("F26E49C7-2342-4F62-CBCC-08DA10AB0E61"));

    db.Users.Remove(user);

    await db.SaveChangesAsync();

});

app.Run();


