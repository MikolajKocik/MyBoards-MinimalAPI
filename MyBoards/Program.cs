using Microsoft.AspNetCore.Http.Json;
using Microsoft.EntityFrameworkCore;
using MyBoards.Entities;
using System;
using System.Text.Json.Serialization;

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

/* Lazy loading (paczka Nuget Proxies)

app.MapGet("LazyLoading_data", async (MyBoardsContext db) =>
{
    var withAddress = true;

    var user = db.Users
        .First(u => u.Id == Guid.Parse("8B49FE0E-AC8F-4521-CBC8-08DA10AB0E61"));

    if (withAddress)
    {
        var result = new { FullName = user.FullName, Address =  $"{user.Address.Street} {user.Address.City}"};
        return result;
    }

    return new { FullName = user.FullName, Address = "-"};
});
*/

// przy u�yciu 'FromSqlRaw' z parametrem minWorkItemsCount, kod by�by podatny na ataki sql injection
// RawSql u�ywamy gdy nie da si� napisa� polecenia w LINQ 
// metody musz� zwraca� dok�adnie taki model jaki mam w DbSet (nie moze zabrakn�� danej kolumny (pola))

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


