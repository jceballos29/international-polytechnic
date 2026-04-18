namespace Identity.Domain.Enums;

/// <summary>
/// Estado de una aplicación cliente registrada en el IdP.
///
/// Active → funciona normalmente, puede obtener tokens.
///
/// Inactive → desactivada voluntariamente (ej: mantenimiento).
///   Los tokens activos siguen siendo válidos hasta expirar.
///   No hay revocación automática.
///
/// Suspended → suspendida por violación de políticas.
///   Más severo que Inactive.
///   Sus Refresh Tokens se revocan inmediatamente.
///   Los Access Tokens vigentes expiran solos (máx 15 min).
///
/// ¿Por qué no un simple booleano IsActive?
/// Un booleano no distingue entre "pausada voluntariamente"
/// y "suspendida por incidente de seguridad".
/// El enum permite lógica diferente para cada caso.
/// </summary>
public enum ApplicationStatus
{
    Active,
    Inactive,
    Suspended,
}
