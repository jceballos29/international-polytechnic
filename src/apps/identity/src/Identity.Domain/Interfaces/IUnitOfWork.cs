namespace Identity.Domain.Interfaces;

/// <summary>
/// Unidad de Trabajo — coordina múltiples repositorios
/// en una sola transacción de base de datos.
///
/// Sin UoW — problema de consistencia:
///   1. Marcar authorization_code como usado ✅
///   2. Crear Refresh Token → falla ❌
///   → code marcado como usado pero sin token emitido
///   → el usuario no puede autenticarse y el code no sirve
///
/// Con UoW — todo o nada:
///   1. Marcar authorization_code como usado
///   2. Crear Refresh Token
///   3. SaveChangesAsync() → si algo falla, NADA se persiste
///
/// Patrón de uso en los Handlers:
///   await _authCodeRepo.MarkAsUsedAsync(codeId);
///   await _refreshTokenRepo.AddAsync(refreshToken);
///   await _auditLogRepo.AddAsync(auditLog);
///   await _unitOfWork.SaveChangesAsync(); ← confirma todo junto
/// </summary>
public interface IUnitOfWork : IDisposable
{
    /// <summary>
    /// Persiste todos los cambios pendientes en una sola transacción.
    /// Retorna el número de entidades afectadas.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken ct = default);

    /// <summary>
    /// Transacción explícita — para cuando necesitas múltiples
    /// SaveChangesAsync en la misma transacción lógica.
    /// En la mayoría de casos SaveChangesAsync solo es suficiente.
    /// </summary>
    Task BeginTransactionAsync(CancellationToken ct = default);
    Task CommitTransactionAsync(CancellationToken ct = default);
    Task RollbackTransactionAsync(CancellationToken ct = default);
}
