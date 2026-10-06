using CitasMedicas.Web.Modules.Reservas.Features.GetPatientAppointments;
using Microsoft.AspNetCore.Mvc;

namespace CitasMedicas.Web.Controllers;

public sealed class AppointmentsController(GetPatientAppointmentsQuery appointments) : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        return View(new PatientAppointmentsViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(PatientAppointmentsViewModel model, CancellationToken cancellationToken)
    {
        model.Email = model.Email?.Trim() ?? string.Empty;
        model.HasSearched = true;
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        model.Appointments = await appointments.ExecuteAsync(model.Email, cancellationToken);
        return View(model);
    }
}
