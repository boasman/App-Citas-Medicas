using CitasMedicas.Web.Controllers;
using CitasMedicas.Web.Modules.AgendaMedica.Features.GetDoctorAvailability;
using CitasMedicas.Web.Modules.AgendaMedica.Persistence;
using CitasMedicas.Web.Modules.CatalogoMedico.Persistence;
using CitasMedicas.Web.Modules.Reservas.Features.BookAppointment;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using Xunit;

namespace AppCitasMedicas.Tests.Reservas;

public sealed class AppointmentConfirmationTests
{
    [Fact]
    public async Task Book_then_confirm_saves_appointment_and_returns_confirmation_details()
    {
        await using var connection = await CreateDatabaseAsync();
        await using var dbContext = CreateDbContext(connection);
        var (doctor, slot) = await AddAvailableSlotAsync(dbContext);
        var controller = CreateController(dbContext);

        var bookingResult = await controller.Book(new BookAppointmentViewModel
        {
            DoctorId = doctor.Id,
            SlotId = slot.Id,
            PatientName = "  Elena García  ",
            PatientEmail = "  elena@example.com  "
        }, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(bookingResult);
        Assert.Equal(nameof(AvailabilityController.Confirmation), redirect.ActionName);
        var reservationId = Assert.IsType<int>(redirect.RouteValues!["id"]);

        var reservation = await dbContext.AppointmentReservations.AsNoTracking().SingleAsync();
        Assert.Equal("Elena García", reservation.PatientName);
        Assert.Equal("elena@example.com", reservation.PatientEmail);
        Assert.Equal(slot.Id, reservation.AppointmentSlotId);
        Assert.True(await dbContext.AppointmentSlots.AsNoTracking()
            .Where(item => item.Id == slot.Id)
            .Select(item => item.IsOccupied)
            .SingleAsync());

        var confirmationResult = await controller.Confirmation(reservationId, CancellationToken.None);

        var view = Assert.IsType<ViewResult>(confirmationResult);
        Assert.Null(view.ViewName);
        var model = Assert.IsType<AppointmentConfirmationViewModel>(view.Model);
        Assert.Equal("Elena García", model.PatientName);
        Assert.Equal("elena@example.com", model.PatientEmail);
        Assert.Equal(doctor.Name, model.DoctorName);
        Assert.Equal(doctor.SpecialtyName, model.SpecialtyName);
        Assert.Equal(slot.StartsAt, model.StartsAt);

        await using var app = CreateTestApplication(connection);
        await app.StartAsync();
        using var client = app.GetTestClient();
        var response = await client.GetAsync($"/Availability/Confirmation?id={reservationId}");
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();
        var decodedHtml = WebUtility.HtmlDecode(html);

        Assert.Contains("Tu cita se ha reservado correctamente.", decodedHtml);
        Assert.Contains(doctor.Name, decodedHtml);
        Assert.Contains(doctor.SpecialtyName, decodedHtml);
        Assert.Contains($"{slot.StartsAt.Day}", decodedHtml);
        Assert.Contains(slot.StartsAt.ToString("HH:mm"), decodedHtml);
    }

    [Fact]
    public async Task Booking_same_slot_twice_does_not_create_a_second_reservation()
    {
        await using var connection = await CreateDatabaseAsync();
        await using var dbContext = CreateDbContext(connection);
        var (doctor, slot) = await AddAvailableSlotAsync(dbContext);
        var command = new BookAppointmentCommand(dbContext);

        var firstReservationId = await command.ExecuteAsync(
            doctor.Id,
            slot.Id,
            "Elena García",
            "elena@example.com");
        var secondReservationId = await command.ExecuteAsync(
            doctor.Id,
            slot.Id,
            "Otro paciente",
            "otro@example.com");

        Assert.NotNull(firstReservationId);
        Assert.Null(secondReservationId);
        Assert.Equal(1, await dbContext.AppointmentReservations.CountAsync());
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

    private static async Task<(Doctor Doctor, AppointmentSlot Slot)> AddAvailableSlotAsync(CatalogoMedicoDbContext dbContext)
    {
        var doctor = new Doctor { Name = "Dra. Ana Martínez", SpecialtyName = "Cardiología" };
        var slot = new AppointmentSlot
        {
            Doctor = doctor,
            StartsAt = DateTime.Now.AddDays(2),
            IsOccupied = false
        };

        dbContext.AppointmentSlots.Add(slot);
        await dbContext.SaveChangesAsync();
        return (doctor, slot);
    }

    private static AvailabilityController CreateController(CatalogoMedicoDbContext dbContext)
    {
        return new AvailabilityController(
            new GetDoctorAvailabilityQuery(dbContext),
            new BookAppointmentCommand(dbContext),
            new GetAppointmentConfirmationQuery(dbContext));
    }

    private static WebApplication CreateTestApplication(SqliteConnection connection)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddControllersWithViews()
            .AddApplicationPart(typeof(AvailabilityController).Assembly);
        builder.Services.AddDbContext<CatalogoMedicoDbContext>(options => options.UseSqlite(connection));
        builder.Services.AddScoped<GetDoctorAvailabilityQuery>();
        builder.Services.AddScoped<BookAppointmentCommand>();
        builder.Services.AddScoped<GetAppointmentConfirmationQuery>();

        var app = builder.Build();
        app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
        return app;
    }
}
