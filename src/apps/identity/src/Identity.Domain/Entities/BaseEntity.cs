namespace Identity.Domain.Entities;

/// <summary>
/// Clase base para todas las entidades del dominio.
///
/// ¿Por qué Guid y no int?
///   - El Guid se genera en la app ANTES del INSERT a la DB.
///     Con int autoincremental, tienes que hacer el INSERT primero
///     y luego consultar el ID — dos viajes a la DB.
///   - Los Guid no exponen volumen de datos
///     (un id=1000 te dice que hay ~1000 registros).
///   - Seguros para sistemas distribuidos — sin colisiones
///     aunque haya múltiples instancias del servicio.
///
/// ¿Por qué setters protected y no private?
///   - EF Core necesita poder asignar las propiedades
///     cuando lee de la base de datos.
///   - Con private EF Core no puede materializarlas.
///   - Con protected solo esta clase y sus hijos pueden asignarlas.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; protected set; }

    /// <summary>
    /// Siempre UTC — nunca hora local.
    /// Si el servidor está en Colombia (UTC-5) y el usuario en España (UTC+1),
    /// necesitamos una referencia común para comparar fechas correctamente.
    /// </summary>
    public DateTime CreatedAt { get; protected set; }

    /// <summary>
    /// Null cuando la entidad acaba de crearse — aún no ha sido actualizada.
    /// </summary>
    public DateTime? UpdatedAt { get; protected set; }

    /// <summary>
    /// Constructor protegido para EF Core.
    /// Sin este constructor, EF Core no puede instanciar
    /// la entidad cuando la lee de la base de datos.
    /// </summary>
    protected BaseEntity() { }

    /// <summary>
    /// Registra la fecha de actualización.
    /// Se llama desde los métodos de modificación de cada entidad.
    /// </summary>
    protected void MarkAsUpdated() => UpdatedAt = DateTime.UtcNow;
}
