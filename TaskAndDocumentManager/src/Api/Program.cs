using TaskAndDocumentManager.Application.Auth.Interfaces;
using TaskAndDocumentManager.Application.Auth.UseCases;
using TaskAndDocumentManager.Application.Tasks.Interfaces;
using TaskAndDocumentManager.Application.Tasks.UseCases;
using TaskAndDocumentManager.Infrastructure.Persistence;
using TaskAndDocumentManager.Infrastructure.Auth.Services;
using TaskAndDocumentManager.Infrastructure.Auth;
using TaskAndDocumentManager.Infrastructure.Persistence.Repositories;
using TaskAndDocumentManager.Infrastructure.Auth.Token;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;



var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' is missing. Configure it before starting the API.");
//register service here IUserRepository, IPasswordHasher, IEmailValidator
builder.Services.AddDbContext<TaskDbContext>(options =>
    options.UseNpgsql(connectionString));
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ITaskRepository, TaskRepository>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IEmailValidator, EmailValidator>();
builder.Services.AddScoped<AuthenticateUser>();
builder.Services.AddScoped<GetCurrentUser>();
builder.Services.AddScoped<RegisterUser>();
builder.Services.AddScoped<GetCurrentUser>();
builder.Services.AddScoped<CreateTask>();
builder.Services.AddScoped<AssignTask>();
builder.Services.AddScoped<ListTasks>();
builder.Services.AddScoped<IPasswordValidator, PasswordValidator>();
builder.Services.AddScoped<ITokenService, JwtTokenService>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddAuthorization();
var jwtSection = builder.Configuration.GetSection("jwt");

var key = jwtSection["Key"];
var issuer = jwtSection["Issuer"];
var audience = jwtSection["Audience"];

var app = builder.Build();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    // app.UseHsts();
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseAuthentication();
app.UseAuthorization();



app.MapControllers();

app.Run();
