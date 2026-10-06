using System.Net;
using CitasMedicas.Web.Controllers;
using CitasMedicas.Web.Modules.AgendaMedica.Features.GetDoctorAvailability;
using CitasMedicas.Web.Modules.AgendaMedica.Persistence;
using CitasMedicas.Web.Modules.CatalogoMedico.Features.AssignDoctorSpecialty;
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

public sealed class AssignDoctorSpecialtyTests
{
    [Fact]
    public async Task Assignment_moves_doctor_to_selected_specialty()
    {
        await using var connection = await CreateDatabaseAsync();
        var doctor = await AddDoctorAsync(connection, "Dra. Laura Gómez", "Cardiología");
        await using var app = CreateTestApplication(connection);
        await app.StartAsync();
        using var client = app.GetTestClient();

        var response = await PostAssignmentAsync(client, doctor.Id, 2);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Specialties", response.Headers.Location?.OriginalString);
        await using var dbContext = CreateDbContext(connection);
        var savedDoctor = await dbContext.Doctors.AsNoTracking().SingleAsync(item => item.Id == doctor.Id);
        Assert.Equal("Dermatología", savedDoctor.SpecialtyName);
        Assert.Contains(await new GetDoctorAvailabilityQuery(dbContext).ListDoctorsAsync("Dermatología"),
            item => item.Id == doctor.Id);
        Assert.DoesNotContain(await new GetDoctorAvailabilityQuery(dbContext).ListDoctorsAsync("Cardiología"),
            item => item.Id == doctor.Id);
    }

    [Fact]
    public async Task Missing_selections_show_validation_errors_without_changing_doctor()
    {
        await using var connection = await CreateDatabaseAsync();
        var doctor = await AddDoctorAsync(connection, "Dra. Laura Gómez", "Cardiología");
        await using var app = CreateTestApplication(connection);
        await app.StartAsync();
        using var client = app.GetTestClient();

        var response = await PostAssignmentAsync(client, 0, 0);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        Assert.Contains("Selecciona un médico.", html);
        Assert.Contains("Selecciona una especialidad.", html);
        await AssertDoctorSpecialtyAsync(connection, doctor.Id, "Cardiología");
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task Unknown_ids_show_error_without_changing_doctor(bool unknownDoctor, bool unknownSpecialty)
    {
        await using var connection = await CreateDatabaseAsync();
        var doctor = await AddDoctorAsync(connection, "Dra. Laura Gómez", "Cardiología");
        await using var app = CreateTestApplication(connection);
        await app.StartAsync();
        using var client = app.GetTestClient();

        var doctorId = unknownDoctor ? doctor.Id + 999 : doctor.Id;
        var specialtyId = unknownSpecialty ? 999 : 2;
        var response = await PostAssignmentAsync(client, doctorId, specialtyId);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        Assert.Contains("El médico o la especialidad seleccionados ya no están disponibles.", html);
        await AssertDoctorSpecialtyAsync(connection, doctor.Id, "Cardiología");
    }

    [Fact]
    public async Task Assignment_page_lists_doctors_specialties_and_is_linked_from_specialties()
    {
        await using var connection = await CreateDatabaseAsync();
        await AddDoctorAsync(connection, "Dra. Laura Gómez", "Cardiología");
        await using var app = CreateTestApplication(connection);
        await app.StartAsync();
        using var client = app.GetTestClient();

        var indexResponse = await client.GetAsync("/Specialties");
        indexResponse.EnsureSuccessStatusCode();
        Assert.Contains("/Doctors/AssignSpecialty", await indexResponse.Content.ReadAsStringAsync());

        var formResponse = await client.GetAsync("/Doctors/AssignSpecialty");
        formResponse.EnsureSuccessStatusCode();
        var html = WebUtility.HtmlDecode(await formResponse.Content.ReadAsStringAsync());
        Assert.Contains("Dra. Laura Gómez", html);
        Assert.Contains("Especialidad actual: Cardiología", html);
        Assert.Contains("Dermatología", html);
        Assert.Contains("Guardar asignación", html);
    }

    private static async Task<HttpResponseMessage> PostAssignmentAsync(HttpClient client, int doctorId, int specialtyId)
    {
        var tokenResponse = await client.GetAsync("/test/antiforgery");
        tokenResponse.EnsureSuccessStatusCode();
        using var tokenJson = System.Text.Json.JsonDocument.Parse(await tokenResponse.Content.ReadAsStringAsync());
        var requestToken = tokenJson.RootElement.GetProperty("requestToken").GetString();
        var cookie = tokenResponse.Headers.GetValues("Set-Cookie").Single().Split(';', 2)[0];
        using var request = new HttpRequestMessage(HttpMethod.Post, "/Doctors/AssignSpecialty")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["DoctorId"] = doctorId.ToString(),
                ["SpecialtyId"] = specialtyId.ToString(),
                ["__RequestVerificationToken"] = requestToken!
            })
        };
        request.Headers.Add("Cookie", cookie);
        return await client.SendAsync(request);
    }

    private static async Task<Doctor> AddDoctorAsync(SqliteConnection connection, string name, string specialtyName)
    {
        await using var dbContext = CreateDbContext(connection);
        var doctor = new Doctor { Name = name, SpecialtyName = specialtyName };
        dbContext.Doctors.Add(doctor);
        await dbContext.SaveChangesAsync();
        return doctor;
    }

    private static async Task AssertDoctorSpecialtyAsync(SqliteConnection connection, int doctorId, string expectedSpecialty)
    {
        await using var dbContext = CreateDbContext(connection);
        var doctor = await dbContext.Doctors.AsNoTracking().SingleAsync(item => item.Id == doctorId);
        Assert.Equal(expectedSpecialty, doctor.SpecialtyName);
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
        builder.Services.AddScoped<AssignDoctorSpecialtyCommand>();
        builder.Services.AddScoped<GetDoctorSpecialtyAssignmentOptionsQuery>();

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
