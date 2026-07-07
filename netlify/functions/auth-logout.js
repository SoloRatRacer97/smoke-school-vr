const {
  clearSessionCookie,
  getPool,
  getSessionToken,
  handleError,
  hashSessionToken,
  methodNotAllowed,
  response
} = require("./_auth");

exports.handler = async function authLogout(event) {
  if (event.httpMethod === "OPTIONS") {
    return response(204, {});
  }

  if (event.httpMethod !== "POST") {
    return methodNotAllowed(["POST"]);
  }

  try {
    const token = getSessionToken(event);
    if (token) {
      await getPool().query(
        `update sessions
            set revoked_at = now()
          where token_hash = $1
            and revoked_at is null`,
        [hashSessionToken(token)]
      );
    }

    return response(200, { ok: true }, {
      "Set-Cookie": clearSessionCookie()
    });
  } catch (error) {
    return handleError(error);
  }
};
