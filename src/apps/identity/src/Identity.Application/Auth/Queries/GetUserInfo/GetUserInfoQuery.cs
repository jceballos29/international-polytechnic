using Identity.Application.Common.Models;
using MediatR;

namespace Identity.Application.Auth.Queries.GetUserInfo;

/// <summary>
/// Retorna información del usuario autenticado.
///
/// UserId y ClientApplicationId vienen del Access Token
/// que el middleware JwtBearer valida automáticamente.
/// El controller extrae los claims del token y los pasa aquí.
/// </summary>
public record GetUserInfoQuery(Guid UserId, string ClientId) : IRequest<Result<UserInfoResult>>;
