using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using ProductShared.Data;
using ProductShared.Services;

var builder = WebApplication.CreateBuilder(args);

//registers FastEndpoints services with the application's dependency injection container.
builder.Services.AddFastEndpoints();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IProductService, ProductService>();

var app = builder.Build();

//adds FastEndpoints to the HTTP request pipeline so that configured endpoint classes can receive and handle requests.
app.UseFastEndpoints();

app.Run();   