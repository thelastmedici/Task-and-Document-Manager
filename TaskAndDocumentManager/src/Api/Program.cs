using TaskAndDocumentManager.Application.Auth.Interfaces;
using TaskAndDocumentManager.Api.Extensions;
using TaskAndDocumentManager.Application.Auth.UseCases;
using TaskAndDocumentManager.Application.Audit.Interfaces;
using TaskAndDocumentManager.Application.Documents.Interfaces;
using TaskAndDocumentManager.Application.Documents.UseCases;
using TaskAndDocumentManager.Application.Tasks.Interfaces;
using TaskAndDocumentManager.Application.Tasks.UseCases;
using TaskAndDocumentManager.Application.Workspaces.Interfaces;
using TaskAndDocumentManager.Infrastructure.Audit;
using TaskAndDocumentManager.Infrastructure.Auth.Services;
using TaskAndDocumentManager.Infrastructure.Auth;
using TaskAndDocumentManager.Infrastructure.Auth.Token;
using TaskAndDocumentManager.Infrastructure.Documents;
using TaskAndDocumentManager.Infrastructure.Persistence;
using TaskAndDocumentManager.Infrastructure.Storage;
using TaskAndDocumentManager.Infrastructure.Tasks;
using TaskAndDocumentManager.Infrastructure.Workspaces;
using TaskAndDocumentManager.Api.Authorization;
using TaskAndDocumentManager.Application.Documents.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;



var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' is missing. Configure it before starting the API.");
var jwtSection = builder.Configuration.GetSection("Jwt");
var key = jwtSection["Key"] ?? throw new InvalidOperationException("JWT signing key is missing. Configure Jwt:Key.");
var issuer = jwtSection["Issuer"] ?? "TaskAndDocumentManager";
var audience = jwtSection["Audience"] ?? "TaskAndDocumentManager.Client";
//register service here IUserRepository, IPasswordHasher, IEmailValidator
builder.Services.AddDbContext<TaskDbContext>(options =>
    options.UseNpgsql(connectionString));
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();
builder.Services.AddScoped<ITaskRepository, TaskRepository>();
builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
builder.Services.AddScoped<IDocumentAccessRepository, DocumentAccessRepository>();
builder.Services.AddScoped<IFileStorageService, FileStorageService>();
builder.Services.AddSingleton<IWorkspaceRepository, InMemoryWorkspaceRepository>();
builder.Services.AddSingleton<IWorkspaceMemberRepository, InMemoryWorkspaceMemberRepository>();
builder.Services.AddSingleton<IRoleCatalog, RoleCatalog>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IEmailValidator, EmailValidator>();
builder.Services.AddScoped<AuthenticateUser>();
builder.Services.AddScoped<GetCurrentUser>();
builder.Services.AddScoped<RegisterUser>();
builder.Services.AddScoped<DeactivateUser>();
builder.Services.AddScoped<GetCurrentUser>();
builder.Services.AddScoped<CreateTask>();
builder.Services.AddScoped<AssignTask>();
builder.Services.AddScoped<ListTasks>();
builder.Services.AddScoped<UpdateTask>();
builder.Services.AddScoped<DeleteTask>();
builder.Services.AddScoped<UploadDocument>();
builder.Services.AddScoped<DownloadDocument>();
builder.Services.AddScoped<DeleteDocument>();
builder.Services.AddScoped<ShareDocument>();
builder.Services.AddScoped<ShareTaskLinkedDocument>();
builder.Services.AddScoped<RevokeDocumentAccess>();
builder.Services.AddScoped<GetSharedDocuments>();
builder.Services.AddScoped<LinkDocumentToTask>();
builder.Services.AddScoped<GetDocumentMetadata>();
builder.Services.AddScoped<DocumentAccessEvaluator>();
builder.Services.AddScoped<ListAccessibleDocuments>();
builder.Services.AddScoped<IPasswordValidator, PasswordValidator>();
builder.Services.AddScoped<ITokenService, JwtTokenService>();
builder.Services.AddScoped<ListUsers>();
builder.Services.AddScoped<CreateUserAsAdmin>();
builder.Services.AddScoped<ChangeUserRole>();
builder.Services.AddScoped<DeleteUser>();
// Realtime presence and connection tracking
builder.Services.AddSingleton<TaskAndDocumentManager.Api.Realtime.IUserConnectionTracker, TaskAndDocumentManager.Api.Realtime.InMemoryUserConnectionTracker>();
builder.Services.AddSingleton<TaskAndDocumentManager.Application.Presence.Interfaces.IPresenceService, TaskAndDocumentManager.Api.Realtime.InMemoryPresenceService>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AppPolicies.Authenticated, policy =>
         policy.RequireRole(
            BuiltInRoles.AdminName,
            BuiltInRoles.ManagerName,
            BuiltInRoles.UserName
         )
    );

    options.AddPolicy(AppPolicies.AdminOnly, policy =>
        policy.RequireRole(BuiltInRoles.AdminName));

    
    options.AddPolicy(AppPolicies.ManagerOrAdmin, policy =>
         policy.RequireRole(BuiltInRoles.AdminName, BuiltInRoles.ManagerName));
});

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
// Set the tenant before authorization and endpoint code can query the DbContext.
app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated == true)
    {
        try
        {
            var db = context.RequestServices.GetRequiredService<TaskDbContext>();
            db.CurrentWorkspaceId = context.User.GetWorkspaceId();
        }
        catch (UnauthorizedAccessException)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }
    }

    await next();
});
app.UseAuthorization();
app.MapControllers();
app.Run();
