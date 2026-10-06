using CitasMedicas.Web.Modules.CatalogoMedico.Features.ListSpecialties;
using CitasMedicas.Web.Modules.CatalogoMedico.Features.RegisterDoctor;
using Microsoft.AspNetCore.Mvc;

namespace CitasMedicas.Web.Controllers;

public class DoctorsController(
    ListSpecialtiesQuery listSpecialties,
    RegisterDoctorCommand registerDoctor) : Controller
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
}
