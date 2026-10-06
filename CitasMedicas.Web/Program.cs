using CitasMedicas.Web.Modules.CatalogoMedico.Persistence;
using Microsoft.EntityFrameworkCore;
using CitasMedicas.Web.Modules.AgendaMedica.Persistence;
using CitasMedicas.Web.Modules.Reservas.Features.BookAppointment;
using CitasMedicas.Web.Modules.Reservas.Features.GetPatientAppointments;
using CitasMedicas.Web.Modules.CatalogoMedico.Features.RegisterDoctor;
using CitasMedicas.Web.Modules.CatalogoMedico.Features.AssignDoctorSpecialty;
using CitasMedicas.Web.Modules.AgendaMedica.Features.RegisterAvailability;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<CatalogoMedicoDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("CitasMedicas")));
builder.Services.AddScoped<CitasMedicas.Web.Modules.CatalogoMedico.Features.ListSpecialties.ListSpecialtiesQuery>();
builder.Services.AddScoped<CitasMedicas.Web.Modules.CatalogoMedico.Features.CreateSpecialty.CreateSpecialtyCommand>();
builder.Services.AddScoped<RegisterDoctorCommand>();
builder.Services.AddScoped<AssignDoctorSpecialtyCommand>();
builder.Services.AddScoped<GetDoctorSpecialtyAssignmentOptionsQuery>();
builder.Services.AddScoped<CitasMedicas.Web.Modules.AgendaMedica.Features.GetDoctorAvailability.GetDoctorAvailabilityQuery>();
builder.Services.AddScoped<GetAvailabilityRegistrationOptionsQuery>();
builder.Services.AddScoped<RegisterAvailabilityCommand>();
builder.Services.AddScoped<BookAppointmentCommand>();
builder.Services.AddScoped<GetAppointmentConfirmationQuery>();
builder.Services.AddScoped<GetPatientAppointmentsQuery>();

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<CatalogoMedicoDbContext>();
    await dbContext.Database.EnsureCreatedAsync();
    await dbContext.Database.ExecuteSqlRawAsync("""
        IF SCHEMA_ID(N'AgendaMedica') IS NULL EXEC(N'CREATE SCHEMA [AgendaMedica]');
        IF OBJECT_ID(N'[AgendaMedica].[Doctors]', N'U') IS NULL
        BEGIN
            CREATE TABLE [AgendaMedica].[Doctors] (
                [Id] int IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Doctors] PRIMARY KEY,
                [Name] nvarchar(150) NOT NULL,
                [SpecialtyName] nvarchar(100) NOT NULL
            );
            CREATE INDEX [IX_Doctors_SpecialtyName] ON [AgendaMedica].[Doctors] ([SpecialtyName]);
        END;
        IF OBJECT_ID(N'[AgendaMedica].[AppointmentSlots]', N'U') IS NULL
        BEGIN
            CREATE TABLE [AgendaMedica].[AppointmentSlots] (
                [Id] int IDENTITY(1,1) NOT NULL CONSTRAINT [PK_AppointmentSlots] PRIMARY KEY,
                [DoctorId] int NOT NULL,
                [StartsAt] datetime2 NOT NULL,
                [IsOccupied] bit NOT NULL,
                CONSTRAINT [FK_AppointmentSlots_Doctors_DoctorId] FOREIGN KEY ([DoctorId]) REFERENCES [AgendaMedica].[Doctors] ([Id]) ON DELETE CASCADE
            );
            CREATE UNIQUE INDEX [IX_AppointmentSlots_DoctorId_StartsAt] ON [AgendaMedica].[AppointmentSlots] ([DoctorId], [StartsAt]);
        END;
        IF SCHEMA_ID(N'Reservas') IS NULL EXEC(N'CREATE SCHEMA [Reservas]');
        IF OBJECT_ID(N'[Reservas].[AppointmentReservations]', N'U') IS NULL
        BEGIN
            CREATE TABLE [Reservas].[AppointmentReservations] (
                [Id] int IDENTITY(1,1) NOT NULL CONSTRAINT [PK_AppointmentReservations] PRIMARY KEY,
                [AppointmentSlotId] int NOT NULL,
                [PatientName] nvarchar(150) NOT NULL,
                [PatientEmail] nvarchar(254) NOT NULL,
                [BookedAt] datetime2 NOT NULL,
                CONSTRAINT [FK_AppointmentReservations_AppointmentSlots_AppointmentSlotId]
                    FOREIGN KEY ([AppointmentSlotId]) REFERENCES [AgendaMedica].[AppointmentSlots] ([Id]),
                CONSTRAINT [UQ_AppointmentReservations_AppointmentSlotId] UNIQUE ([AppointmentSlotId])
            );
        END;
        """);
    if (!await dbContext.Doctors.AnyAsync())
    {
        var doctors = new[]
        {
            new Doctor { Name = "Dra. Ana Martínez", SpecialtyName = "Cardiología" },
            new Doctor { Name = "Dr. Luis Fernández", SpecialtyName = "Dermatología" },
            new Doctor { Name = "Dra. Sofía Ramírez", SpecialtyName = "Medicina general" },
            new Doctor { Name = "Dr. Pablo Torres", SpecialtyName = "Pediatría" },
            new Doctor { Name = "Dra. Elena Ruiz", SpecialtyName = "Traumatología" }
        };
        dbContext.Doctors.AddRange(doctors);
        await dbContext.SaveChangesAsync();

        var firstDay = DateTime.Today.AddDays(1);
        var slots = new List<AppointmentSlot>();
        foreach (var (doctor, index) in doctors.Select((doctor, index) => (doctor, index)))
        {
            for (var day = 0; day < 21; day++)
            {
                var date = firstDay.AddDays(day);
                if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                {
                    continue;
                }

                foreach (var time in new[] { new TimeSpan(9, 0, 0), new TimeSpan(10, 0, 0), new TimeSpan(11, 30, 0), new TimeSpan(14, 0, 0), new TimeSpan(15, 0, 0) })
                {
                    slots.Add(new AppointmentSlot
                    {
                        DoctorId = doctor.Id,
                        StartsAt = date.Add(time),
                        IsOccupied = day % 4 == 0 && time.Hours == 10 || day == index && time.Hours == 14
                    });
                }
            }
        }

        dbContext.AppointmentSlots.AddRange(slots);
        await dbContext.SaveChangesAsync();
    }
}

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


app.Run();
