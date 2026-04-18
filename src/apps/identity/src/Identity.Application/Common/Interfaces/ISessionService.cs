using Identity.Application.Common.Models;

namespace Identity.Application.Common.Interfaces;

/// <summary>
/// Contrato para gestión de sesiones SSO en Redis.
///
/// La sesión SSO es lo que hace posible el Single Sign-On:
/// el usuario se autentica una vez y todas las apps del ecosistema
/// lo reconocen sin pedir credenciales de nuevo.
///
/// La sesión vive en Redis (no en la DB) porque:
///   - Acceso muy rápido (cada request OAuth la consulta)
///   - TTL nativo (Redis expira automáticamente)
///   - No necesita persistencia permanente
/// </summary>
public interface ISessionService
{
    /// <summary>
    /// Crea una sesión SSO para el usuario autenticado.
    /// Retorna el sessionId — valor que va en la cookie HttpOnly.
    /// </summary>
    Task<string> CreateSessionAsync(
        Guid userId,
        Guid tenantId,
        string email,
        CancellationToken ct = default
    );

    /// <summary>
    /// Recupera la sesión por su ID.
    /// Retorna null si no existe o expiró.
    /// </summary>
    Task<SessionInfo?> GetSessionAsync(string sessionId, CancellationToken ct = default);

    /// <summary>
    /// Elimina la sesión — cierre de sesión en el IdP.
    /// Después de esto el usuario necesita autenticarse de nuevo
    /// para obtener nuevos tokens en cualquier app.
    /// </summary>
    Task DestroySessionAsync(string sessionId, CancellationToken ct = default);
}
