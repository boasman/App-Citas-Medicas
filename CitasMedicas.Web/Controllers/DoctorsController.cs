using CitasMedicas.Web.Modules.CatalogoMedico.Features.ListSpecialties;
using CitasMedicas.Web.Modules.CatalogoMedico.Features.RegisterDoctor;
using CitasMedicas.Web.Modules.CatalogoMedico.Features.AssignDoctorSpecialty;
using Microsoft.AspNetCore.Mvc;

namespace CitasMedicas.Web.Controllers;

public class DoctorsController(
    ListSpecialtiesQuery listSpecialties,
    RegisterDoctorCommand registerDoctor,
    AssignDoctorSpecialtyCommand assignDoctorSpecialty,
    GetDoctorSpecialtyAssignmentOptionsQuery assignmentOptions) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        return View(new RegisterDoctorViewModel
        {
            Specialties = await listSpecialties.ExecuteAsync(cancellationToken)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RegisterDoctorViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return await ReturnCreateViewAsync(model, cancellationToken);
        }

        var created = await registerDoctor.ExecuteAsync(model.Name, model.SpecialtyId, cancellationToken);
        if (!created)
        {
            ModelState.AddModelError(nameof(model.SpecialtyId), "Selecciona una especialidad válida.");
            return await ReturnCreateViewAsync(model, cancellationToken);
        }

        TempData["DoctorSuccessMessage"] = $"El médico {model.Name.Trim()} se registró correctamente.";
        return RedirectToAction("Index", "Specialties");
    }

    private async Task<IActionResult> ReturnCreateViewAsync(RegisterDoctorViewModel model, CancellationToken cancellationToken)
    {
        model.Specialties = await listSpecialties.ExecuteAsync(cancellationToken);
        return View("Create", model);
    }

    [HttpGet]
    public async Task<IActionResult> AssignSpecialty(CancellationToken cancellationToken)
    {
        var options = await assignmentOptions.ExecuteAsync(cancellationToken);
        return View(new AssignDoctorSpecialtyViewModel
        {
            Doctors = options.Doctors,
            Specialties = options.Specialties
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignSpecialty(AssignDoctorSpecialtyViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return await ReturnAssignmentViewAsync(model, cancellationToken);
        }

        var assigned = await assignDoctorSpecialty.ExecuteAsync(model.DoctorId, model.SpecialtyId, cancellationToken);
        if (!assigned)
        {
            ModelState.AddModelError(string.Empty, "El médico o la especialidad seleccionados ya no están disponibles.");
            return await ReturnAssignmentViewAsync(model, cancellationToken);
        }

        TempData["DoctorSuccessMessage"] = "La especialidad del médico se actualizó correctamente.";
        return RedirectToAction("Index", "Specialties");
    }

    private async Task<IActionResult> ReturnAssignmentViewAsync(
        AssignDoctorSpecialtyViewModel model,
        CancellationToken cancellationToken)
    {
        var options = await assignmentOptions.ExecuteAsync(cancellationToken);
        model.Doctors = options.Doctors;
        model.Specialties = options.Specialties;
        return View("AssignSpecialty", model);
    }
}
