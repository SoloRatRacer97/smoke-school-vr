const crypto = require("crypto");
const {
  createSessionCookie,
  getPool,
  handleError,
  hashSessionToken,
  methodNotAllowed,
  readJsonBody,
  response,
  sessionExpiryDate,
  userPayload,
  verifyPassword
} = require("./_auth");

exports.handler = async function authLogin(event) {
  if (event.httpMethod === "OPTIONS") {
    return response(204, {});
  }

  if (event.httpMethod !== "POST") {
    return methodNotAllowed(["POST"]);
  }

  try {
    const body = readJsonBody(event);
    const email = String(body.email || "").trim().toLowerCase();
    const password = String(body.password || "");

    if (!email || !password) {
      return response(400, { ok: false, error: "missing_credentials" });
    }

    const userResult = await getPool().query(
      `select id::text as id, email, display_name, student_id, password_hash, active
         from users
        where lower(email) = lower($1)
        limit 1`,
      [email]
    );

    const user = userResult.rows[0];
    if (!user || user.active !== true || !verifyPassword(password, user.password_hash)) {
      return response(401, { ok: false, error: "invalid_credentials" });
    }

    const token = crypto.randomBytes(32).toString("base64url");
    const tokenHash = hashSessionToken(token);
    const expiresAt = sessionExpiryDate();

    await getPool().query(
      `insert into sessions (user_id, token_hash, expires_at)
       values ($1, $2, $3)`,
      [user.id, tokenHash, expiresAt]
    );

    return response(200, { ok: true, user: userPayload(user) }, {
      "Set-Cookie": createSessionCookie(token, expiresAt)
    });
  } catch (error) {
    return handleError(error);
  }
};
