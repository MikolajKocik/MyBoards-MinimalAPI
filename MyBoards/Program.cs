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
            City = "Krak�w",
            Street = "D�uga"
        }
    };

    dbContext.Users.AddRange(user1, user2);
    dbContext.SaveChanges();
}


app.Run();


