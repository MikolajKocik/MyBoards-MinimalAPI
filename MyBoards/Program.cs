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

// podczas serializacji zostanie pominiêta nieskoñczonoa referencja miêdzy zale¿noœciami, która jest zapêtlona

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
            City = "Kraków",
            Street = "D³uga"
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

    // w zale¿noœci czy korzystamy z parametru czy nie, wybieramy bezpoœredni¹ opcjê sql (dla bezppoœr. zapytañ)

    db.Database.ExecuteSqlRaw(@"
    UPDATE Comments
    SET UpdatedDate = GETDATE()
    WHERE AuthorId = '888FF7E0-E791-4EFE-CC0A-08DA10AB0E61'");

    var entries = db.ChangeTracker.Entries();

    return states;
});

// je¿eli mamy dane, które nie s¹ modyfikowane mo¿emy zoptymalizacowaæ dane przy u¿yciu EF
// poniewa¿ nie bêd¹ œledzone przez czêœæ Trackera

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

    var entry = db.Attach(workItem); //dbcontext œledzi workItem o konkretnym Id
    entry.State = EntityState.Deleted; // ustawiony stan na delete (musi wykonaæ polecenie delete z tabeli workItems)

    db.SaveChanges();

    return workItem;
});

// Include zawiera w sobie Join dlatego jest lepszy ni¿ tworzenie osobnego zapytania
// ThenInclude - jeœli chcemy coœ do³¹czyæ do do³¹czonej encji 
// Metody te pozwalaj¹ na ³adowanie w³aœciwoœci powi¹zanych w naszych encjach.

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

    await db.Tags.AddRangeAsync(mvcTag, aspTag); //AddRange w porównianiu do Add pozwala na dodanie wielu wartoœci encji jednorazowo
    await db.SaveChangesAsync();

    return tags;
});

app.MapPost("createWithDependency", async (MyBoardsContext db) =>
{
    var address = new Address()
    {
        Id = Guid.NewGuid(),
        City = "Kraków",
        Country = "Poland",
        Street = "D³uga"
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
// kaskadowe usuwanie bêdzie automatyczne jeœli mamy powi¹zane encje (z u¿yciem 'Include')

app.MapDelete("delete", async (MyBoardsContext db) =>
{
    var user = await db.Users
        .Include(u => u.Comments)
        .FirstAsync(u => u.Id == Guid.Parse("F26E49C7-2342-4F62-CBCC-08DA10AB0E61"));

    db.Users.Remove(user);

    await db.SaveChangesAsync();

});


app.Run();


