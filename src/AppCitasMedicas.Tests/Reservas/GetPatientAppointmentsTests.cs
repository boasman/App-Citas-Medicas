using CitasMedicas.Web.Controllers;
using CitasMedicas.Web.Modules.AgendaMedica.Persistence;
using CitasMedicas.Web.Modules.CatalogoMedico.Persistence;
using CitasMedicas.Web.Modules.Reservas.Features.GetPatientAppointments;
using CitasMedicas.Web.Modules.Reservas.Persistence;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using Xunit;

namespace AppCitasMedicas.Tests.Reservas;

public sealed class GetPatientAppointmentsTests
{
    [Fact]
    public async Task Search_shows_only_future_appointments_for_email_in_date_order()
    {
        await using var connection = await CreateDatabaseAsync();
        await using var dbContext = CreateDbContext(connection);
        await AddReservationAsync(dbContext, "elena@example.com", "Dra. Ana Martínez", "Cardiología", DateTime.Now.AddDays(3));
        await AddReservationAsync(dbContext, "elena@example.com", "Dr. Luis Fernández", "Dermatología", DateTime.Now.AddDays(1));
        await AddReservationAsync(dbContext, "elena@example.com", "Dra. Ana Martínez", "Cardiología", DateTime.Now.AddDays(-1));
        await AddReservationAsync(dbContext, "otra@example.com", "Dr. Pablo Torres", "Pediatría", DateTime.Now.AddDays(2));
        await using var app = CreateTestApplication(connection);
        await app.StartAsync();
        using var client = app.GetTestClient();

        var response = await PostSearchAsync(client, "  ELENA@example.com  ");

        response.EnsureSuccessStatusCode();
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        Assert.Contains("Dra. Ana Martínez", html);
        Assert.Contains("Dr. Luis Fernández", html);
        Assert.Contains("Cardiología", html);
        Assert.Contains("Dermatología", html);
        Assert.DoesNotContain("Dr. Pablo Torres", html);
        Assert.DoesNotContain("Pediatría", html);
        Assert.DoesNotContain(DateTime.Now.AddDays(-1).ToString("dddd d 'de' MMMM 'de' yyyy"), html);
        Assert.True(html.IndexOf("Dr. Luis Fernández", StringComparison.Ordinal)
            < html.IndexOf("Dra. Ana Martínez", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Search_with_no_future_appointments_shows_empty_state()
    {
        await using var connection = await CreateDatabaseAsync();
        await using var app = CreateTestApplication(connection);
        await app.StartAsync();
        using var client = app.GetTestClient();

        var response = await PostSearchAsync(client, "nadie@example.com");

        response.EnsureSuccessStatusCode();
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        Assert.Contains("No tienes citas futuras", html);
        Assert.Contains("No encontramos citas próximas asociadas a ese email.", html);
    }

    private static async Task<HttpResponseMessage> PostSearchAsync(HttpClient client, string email)
    {
        var tokenResponse = await client.GetAsync("/test/antiforgery");
        tokenResponse.EnsureSuccessStatusCode();
        using var tokenJson = System.Text.Json.JsonDocument.Parse(await tokenResponse.Content.ReadAsStringAsync());
        var requestToken = tokenJson.RootElement.GetProperty("requestToken").GetString();
        var cookie = tokenResponse.Headers.GetValues("Set-Cookie").Single().Split(';', 2)[0];
        using var request = new HttpRequestMessage(HttpMethod.Post, "/Appointments/Index")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Email"] = email,
                ["__RequestVerificationToken"] = requestToken!
            })
        };
        request.Headers.Add("Cookie", cookie);
        return await client.SendAsync(request);
    }

    private static async Task AddReservationAsync(
        CatalogoMedicoDbContext dbContext,
        string email,
        string doctorName,
        string specialtyName,
        DateTime startsAt)
    {
        var slot = new AppointmentSlot
        {
            Doctor = new Doctor { Name = doctorName, SpecialtyName = specialtyName },
            StartsAt = startsAt,
            IsOccupied = true
        };
        dbContext.AppointmentReservations.Add(new AppointmentReservation
        {
            AppointmentSlot = slot,
            PatientName = "Paciente",
            PatientEmail = email,
            BookedAt = DateTime.Now
        });
        await dbContext.SaveChangesAsync();
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
            .AddApplicationPart(typeof(AppointmentsController).Assembly);
        builder.Services.AddAntiforgery();
        builder.Services.AddDbContext<CatalogoMedicoDbContext>(options => options.UseSqlite(connection));
        builder.Services.AddScoped<GetPatientAppointmentsQuery>();

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
