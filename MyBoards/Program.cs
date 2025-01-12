using Microsoft.AspNetCore.Http.Json;
using Microsoft.EntityFrameworkCore;
using MyBoards.Entities;
using System;
using System.Linq.Expressions;
using System.Text.Json.Serialization;
using MyBoards.Dto;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// podczas serializacji zostanie pomini�ta niesko�czonoa referencja mi�dzy zale�no�ciami, kt�ra jest zap�tlona

builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
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

// customowa logika seedowania

var users = dbContext.Users.ToList();
if (!users.Any())
{
    var user1 = new User()
    {
        Email = "user1@test.com",
        FullName = "User One",
        Address = new Address()
        {
            City = "Warszawa",
            Street = "Szeroka"
        }
    };

    var user2 = new User()
    {
        Email = "user2@test.com",
        FullName = "User Two",
        Address = new Address()
        {
            City = "Krak�w",
            Street = "D�uga"
        }
    };

    dbContext.Users.AddRange(user1, user2);
    dbContext.SaveChanges();
}

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


app.MapGet("RawSQL_data", async (MyBoardsContext db) =>
{
    var minWorkItemsCount = "85";

    var states = db.WorkItemStates
    .FromSqlInterpolated($@"        
    SELECT wis.Id, wis.Value
    FROM WorkItemStates wis
    JOIN WorkItems wi on wi.StateId = wis.Id
    GROUP BY wis.Id, wis.Value
    HAVING COUNT(*) > {minWorkItemsCount}"
    )
    .ToList();

    // w zale�no�ci czy korzystamy z parametru czy nie, wybieramy bezpo�redni� opcj� sql (dla bezppo�r. zapyta�)

    db.Database.ExecuteSqlRaw(@"
    UPDATE Comments
    SET UpdatedDate = GETDATE()
    WHERE AuthorId = '888FF7E0-E791-4EFE-CC0A-08DA10AB0E61'");

    var entries = db.ChangeTracker.Entries();

    return states;
});

// je�eli mamy dane, kt�re nie s� modyfikowane mo�emy zoptymalizacowa� dane przy u�yciu EF
// poniewa� nie b�d� �ledzone przez cz�� Trackera

app.MapGet("ChangeTracker_NoTracking", async (MyBoardsContext db) =>
{
    var states = db.WorkItemStates
    .AsNoTracking()
    .ToList();

    var entries1 = db.ChangeTracker.Entries();

    return states;
});

app.MapGet("data_ChangeTracker", async (MyBoardsContext db) =>
{
    var workItem = new Epic()
    {
        Id = 2
    };

    var entry = db.Attach(workItem); //dbcontext �ledzi workItem o konkretnym Id
    entry.State = EntityState.Deleted; // ustawiony stan na delete (musi wykona� polecenie delete z tabeli workItems)

    db.SaveChanges();

    return workItem;
});

// Include zawiera w sobie Join dlatego jest lepszy ni� tworzenie osobnego zapytania
// ThenInclude - je�li chcemy co� do��czy� do do��czonej encji 
// Metody te pozwalaj� na �adowanie w�a�ciwo�ci powi�zanych w naszych encjach.

app.MapGet("data", async (MyBoardsContext db) =>
{
    var user = await db.Users
    .Include(u => u.Comments).ThenInclude(w => w.WorkItem)
    .Include(a => a.Address)
    .FirstAsync(u => u.Id == Guid.Parse("68366DBE-0809-490F-CC1D-08DA10AB0E61"));


    // var userComments = await db.Comments.Where(c => c.AuthorId == user.Id).ToListAsync();

    return user;

});

app.MapPost("update", async (MyBoardsContext db) =>
{
    Epic epic = await db.Epics.FirstAsync(epic => epic.Id == 1);

    var rejectedState = await db.WorkItemStates.FirstAsync(a => a.Value == "Rejected");

    epic.State = rejectedState;

    await db.SaveChangesAsync();

    return epic;

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

    await db.Tags.AddRangeAsync(mvcTag, aspTag); //AddRange w por�wnianiu do Add pozwala na dodanie wielu warto�ci encji jednorazowo
    await db.SaveChangesAsync();

    return tags;
});

app.MapPost("createWithDependency", async (MyBoardsContext db) =>
{
    var address = new Address()
    {
        Id = Guid.NewGuid(),
        City = "Krak�w",
        Country = "Poland",
        Street = "D�uga"
    };

    var user = new User()
    {
        Email = "user@test.com",
        FullName = "Test User",
        Address = address,
    };

    db.Users.Add(user);
    await db.SaveChangesAsync();

    // Tworzenie DTO
    var result = new
    {
        user.Email,
        user.FullName,
        Address = new
        {
            address.City,
            address.Country,
            address.Street
        }
    };

    return result;
});

// po zmianie w dbcontext z 'DeleteBehavior.NoAction' na 'DeleteBehavior.ClientCascade'
// kaskadowe usuwanie b�dzie automatyczne je�li mamy powi�zane encje (z u�yciem 'Include')

app.MapDelete("delete", async (MyBoardsContext db) =>
{
    var user = await db.Users
        .Include(u => u.Comments)
        .FirstAsync(u => u.Id == Guid.Parse("F26E49C7-2342-4F62-CBCC-08DA10AB0E61"));

    db.Users.Remove(user);

    await db.SaveChangesAsync();

});


app.Run();


