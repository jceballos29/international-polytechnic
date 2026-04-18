// Parámetros que llegan en la URL cuando el IdP redirige al login
export interface OAuthParams {
  client_id: string;
  redirect_uri: string;
  code_challenge: string;
  code_challenge_method: string;
  state?: string;
}

// Respuesta del endpoint POST /auth/login
export interface LoginResponse {
  code: string;
  redirect_uri: string;
  state?: string;
}

// Respuesta de error del Identity API
export interface ApiError {
  error: string;
  error_description: string;
  status: number;
}