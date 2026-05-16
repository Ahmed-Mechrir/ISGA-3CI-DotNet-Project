using AsvsSecurityAuditor.Data;
using AsvsSecurityAuditor.Models.Identity;
using AsvsSecurityAuditor.Repositories;
using AsvsSecurityAuditor.Repositories.Interfaces;
using AsvsSecurityAuditor.Services;
using AsvsSecurityAuditor.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var mvc = builder.Services.AddControllersWithViews();
if (builder.Environment.IsDevelopment())
    mvc.AddRazorRuntimeCompilation();
builder.Services.AddHttpClient();

var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' not found. Configure SQL Server in appsettings.json.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequiredLength = 10;
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.SlidingExpiration = true;
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", p => p.RequireRole(AsvsSecurityAuditor.Security.Roles.Admin));
    options.AddPolicy("Auditor", p => p.RequireRole(AsvsSecurityAuditor.Security.Roles.Admin, AsvsSecurityAuditor.Security.Roles.Auditor));
});

builder.Services.AddScoped<IRequirementRepository, RequirementRepository>();
builder.Services.AddScoped<IAssessmentRepository, AssessmentRepository>();
builder.Services.AddScoped<ICsvImportService, CsvImportService>();
builder.Services.AddScoped<IAssessmentService, AssessmentService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IPdfAssessmentReportService, PdfAssessmentReportService>();
builder.Services.AddScoped<IAiExplanationService, AiExplanationService>();

var app = builder.Build();

await DbInitializer.SeedAsync(app.Services, builder.Configuration,
    app.Logger);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(name: "areas", pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
