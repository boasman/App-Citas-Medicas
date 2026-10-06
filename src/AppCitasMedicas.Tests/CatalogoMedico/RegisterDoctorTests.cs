using System.Net;
using CitasMedicas.Web.Controllers;
using CitasMedicas.Web.Modules.AgendaMedica.Features.GetDoctorAvailability;
using CitasMedicas.Web.Modules.CatalogoMedico.Features.CreateSpecialty;
using CitasMedicas.Web.Modules.CatalogoMedico.Features.ListSpecialties;
using CitasMedicas.Web.Modules.CatalogoMedico.Features.RegisterDoctor;
using CitasMedicas.Web.Modules.CatalogoMedico.Persistence;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AppCitasMedicas.Tests.CatalogoMedico;

public sealed class RegisterDoctorTests
{
    [Fact]
    public async Task Register_saves_doctor_and_makes_them_available_in_specialty_query()
    {
        await using var connection = await CreateDatabaseAsync();
        await using var app = CreateTestApplication(connection);
        await app.StartAsync();
        using var client = app.GetTestClient();

        var response = await PostCreateAsync(client, "  Dra. Laura Gómez  ", 1);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Specialties", response.Headers.Location?.OriginalString);

        await using var dbContext = CreateDbContext(connection);
        var doctor = await dbContext.Doctors.AsNoTracking().SingleAsync();
        Assert.Equal("Dra. Laura Gómez", doctor.Name);
        Assert.Equal("Cardiología", doctor.SpecialtyName);

        var doctors = await new GetDoctorAvailabilityQuery(dbContext).ListDoctorsAsync("Cardiología");
        Assert.Contains(doctors, item => item.Id == doctor.Id && item.Name == doctor.Name);
        Assert.Empty(await new GetDoctorAvailabilityQuery(dbContext).ListDoctorsAsync("Dermatología"));
    }

    [Fact]
    public async Task Missing_name_and_specialty_show_validation_errors_without_saving()
    {
        await using var connection = await CreateDatabaseAsync();
        await using var app = CreateTestApplication(connection);
        await app.StartAsync();
        using var client = app.GetTestClient();

        var response = await PostCreateAsync(client, "", 0);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        Assert.Contains("El nombre es obligatorio.", html);
        Assert.Contains("Selecciona una especialidad.", html);

        await using var dbContext = CreateDbContext(connection);
        Assert.Empty(await dbContext.Doctors.ToListAsync());
    }

    [Fact]
    public async Task Unknown_specialty_shows_error_without_saving()
    {
        await using var connection = await CreateDatabaseAsync();
        await using var app = CreateTestApplication(connection);
        await app.StartAsync();
        using var client = app.GetTestClient();

        var response = await PostCreateAsync(client, "Dr. Mario Díaz", 999);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        Assert.Contains("Selecciona una especialidad válida.", html);

        await using var dbContext = CreateDbContext(connection);
        Assert.Empty(await dbContext.Doctors.ToListAsync());
    }

    [Fact]
    public async Task Specialty_page_links_to_doctor_registration_and_form_lists_specialties()
    {
        await using var connection = await CreateDatabaseAsync();
        await using var app = CreateTestApplication(connection);
        await app.StartAsync();
        using var client = app.GetTestClient();

        var indexResponse = await client.GetAsync("/Specialties");
        indexResponse.EnsureSuccessStatusCode();
        var indexHtml = await indexResponse.Content.ReadAsStringAsync();
        Assert.Contains("/Doctors/Create", indexHtml);

        var formResponse = await client.GetAsync("/Doctors/Create");
        formResponse.EnsureSuccessStatusCode();
        var formHtml = WebUtility.HtmlDecode(await formResponse.Content.ReadAsStringAsync());
        Assert.Contains("Registrar médico", formHtml);
        Assert.Contains("Cardiología", formHtml);
    }

    private static async Task<HttpResponseMessage> PostCreateAsync(HttpClient client, string name, int specialtyId)
    {
        var tokenResponse = await client.GetAsync("/test/antiforgery");
        tokenResponse.EnsureSuccessStatusCode();
        using var tokenJson = System.Text.Json.JsonDocument.Parse(await tokenResponse.Content.ReadAsStringAsync());
        var requestToken = tokenJson.RootElement.GetProperty("requestToken").GetString();
        var cookie = tokenResponse.Headers.GetValues("Set-Cookie").Single().Split(';', 2)[0];
        using var request = new HttpRequestMessage(HttpMethod.Post, "/Doctors/Create")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Name"] = name,
                ["SpecialtyId"] = specialtyId.ToString(),
                ["__RequestVerificationToken"] = requestToken!
            })
        };
        request.Headers.Add("Cookie", cookie);
        return await client.SendAsync(request);
    }

    private static async Task<SqliteConnection> CreateDatabaseAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var dbContext = CreateDbContext(connection);
        await dbContext.Database.EnsureCreatedAsync();
        return connection;
    }

    private static CatalogoMedicoDbContext CreateDbContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<CatalogoMedicoDbContext>()
            .UseSqlite(connection)
            .Options;
        return new CatalogoMedicoDbContext(options);
    }

    private static WebApplication CreateTestApplication(SqliteConnection connection)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddControllersWithViews()
            .AddApplicationPart(typeof(DoctorsController).Assembly);
        builder.Services.AddAntiforgery();
        builder.Services.AddDbContext<CatalogoMedicoDbContext>(options => options.UseSqlite(connection));
        builder.Services.AddScoped<ListSpecialtiesQuery>();
        builder.Services.AddScoped<CreateSpecialtyCommand>();
        builder.Services.AddScoped<GetDoctorAvailabilityQuery>();
        builder.Services.AddScoped<RegisterDoctorCommand>();

        var app = builder.Build();
        app.MapGet("/test/antiforgery", (IAntiforgery antiforgery, HttpContext context) =>
        {
            var tokens = antiforgery.GetAndStoreTokens(context);
            return Results.Json(new { requestToken = tokens.RequestToken });
        });
        app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
        return app;
    }
}
