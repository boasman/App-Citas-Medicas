namespace CitasMedicas.Web.Modules.CatalogoMedico.Persistence;

public sealed class Specialty
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
