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
    option => option.UseSqlServer(builder.Configuration.GetConnectionString("MyBoardsConnectionString"))
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

// endpoint (zapytanie asynchroniczne)

app.MapGet("data", async (MyBoardsContext db) =>
{
    var authorsCommentCounts = await db.Comments
    .GroupBy(c => c.AuthorId)
    .Select(g => new { g.Key, Count = g.Count() })
    .ToListAsync();

    var topAuthor = authorsCommentCounts
    .First(a => a.Count == authorsCommentCounts.Max(acc => acc.Count));

    var userDetails = db.Users.First(u => u.Id == topAuthor.Key);

    return new { userDetails, commentCount = topAuthor.Count };
});

// Include zawiera w sobie Join dlatego jest lepszy ni� tworzenie osobnego zapytania
// ThenInclude - je�li chcemy co� do��czy� do do��czonej encji 
// Metody te pozwalaj� na �adowanie w�a�ciwo�ci powi�zanych w naszych encjach.

app.MapGet("data2", async (MyBoardsContext db) =>
{
    var user = await db.Users
    .Include(u => u.Comments).ThenInclude(w => w.WorkItem) 
    .Include(a => a.Address)
    .FirstAsync(u => u.Id == Guid.Parse("68366DBE-0809-490F-CC1D-08DA10AB0E61"));


    // var userComments = await db.Comments.Where(c => c.AuthorId == user.Id).ToListAsync();

    return user;

});

// endpoint post do update'u 

app.MapPost("update", async (MyBoardsContext db) =>
{
    Epic epic = await db.Epics.FirstAsync(epic => epic.Id == 1);

    var rejectedState = await db.WorkItemStates.FirstAsync(a => a.Value == "Rejected");

    epic.State = rejectedState;

    await db.SaveChangesAsync();

    return epic;

});

// endpoint create

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

app.MapDelete("delete", async (MyBoardsContext db) => 
{
    var user = await db.Users
    .FirstAsync(u => u.Id == Guid.Parse("78CF834E-7724-4995-CBC4-08DA10AB0E61")); // 'user' o konkretnym id

    var userComments = db.Comments.Where(c => c.AuthorId == user.Id).ToList();
    db.RemoveRange(userComments); // RemoveRange poniewa� operujemy na li�cie komentarzy nie jednym obiekcie jak User
    await db.SaveChangesAsync();

    db.Users.Remove(user); 

    await db.SaveChangesAsync();
});


app.Run();


