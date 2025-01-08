using Microsoft.EntityFrameworkCore;
using MyBoards.Entities;
using System;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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
if(!users.Any())
{
    var user1 = new User()
    {
        Email = "user1@test.com",
        Fullname = "User One",
        Address = new Address()
        {
            City = "Warszawa",
            Street = "Szeroka"
        }
    };

    var user2 = new User()
    {
        Email = "user2@test.com",
        Fullname = "User Two",
        Address = new Address()
        {
            City = "Kraków",
            Street = "D³uga"
        }
    };

    dbContext.Users.AddRange(user1, user2);
    dbContext.SaveChanges();
}


app.Run();


