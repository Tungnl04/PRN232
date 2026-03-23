using FoodStoreAPI.Interfaces;
using FoodStoreRepository.Models;
using Microsoft.EntityFrameworkCore;
using TFC.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var hash = BCrypt.Net.BCrypt.HashPassword("123456");
Console.WriteLine(hash);
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<FoodStoreDbContext>(options =>
    options.UseSqlServer(builder.Configuration
        .GetConnectionString("Default")));
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IStaffService, StaffService>();
builder.Services.AddScoped<IAdminService, AdminService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
