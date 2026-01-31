var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// Add services to the container.







app.Run();

public interface IPasswordHasher // interface for IPasswordHasher --> PasswordHasher
{
    
}

public interface ITokenService // interfcae for ITokenService--> JwtTokenService
{
    
}

public interface IUserRepository // interface 
{
    
}
