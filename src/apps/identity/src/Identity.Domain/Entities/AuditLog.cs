namespace Identity.Domain.Entities;

/// <summary>
/// Registro inmutable de eventos de seguridad.
///
/// Es INMUTABLE — nunca se actualiza ni se borra.
/// Solo se crea. Por eso NO hereda de BaseEntity
/// (no tiene UpdatedAt — un log no se actualiza).
///
/// Útil para:
///   - Auditorías de seguridad
///   - Detectar actividad sospechosa
///   - Troubleshooting: "¿por qué no puede entrar este usuario?"
/// </summary>
public class AuditLog
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }

    /// <summary>Null para eventos donde el usuario no existe (ej: email inexistente).</summary>
    public Guid? UserId { get; private set; }

    /// <summary>Null para eventos del IdP directo.</summary>
    public Guid? ClientApplicationId { get; private set; }

    /// <summary>Ver AuditEventType para los valores posibles.</summary>
    public string EventType { get; private set; } = string.Empty;

    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }

    /// <summary>
    /// Datos adicionales en JSON.
    /// Ej: { "reason": "invalid_password", "attempts": 3 }
    /// </summary>
    public string? Metadata { get; private set; }

    public bool Success { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private AuditLog() { }

    public static AuditLog Create(
        Guid tenantId,
        string eventType,
        bool success,
        Guid? userId = null,
        Guid? clientApplicationId = null,
        string? ipAddress = null,
        string? userAgent = null,
        string? metadata = null
    )
    {
        if (string.IsNullOrWhiteSpace(eventType))
            throw new ArgumentException("El tipo de evento es requerido.", nameof(eventType));

        return new AuditLog
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = userId,
            ClientApplicationId = clientApplicationId,
            EventType = eventType,
            Success = success,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Metadata = metadata,
            CreatedAt = DateTime.UtcNow,
        };
    }
}
