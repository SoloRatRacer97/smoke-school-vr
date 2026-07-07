const crypto = require("crypto");
const { Pool } = require("pg");

const COOKIE_NAME = process.env.AUTH_COOKIE_NAME || "smoke_school_session";
const CONFIGURED_SESSION_DAYS = Number.parseInt(process.env.AUTH_SESSION_DAYS || "30", 10);
const SESSION_DAYS = Number.isFinite(CONFIGURED_SESSION_DAYS) && CONFIGURED_SESSION_DAYS > 0
  ? CONFIGURED_SESSION_DAYS
  : 30;
const COOKIE_SECURE = process.env.AUTH_COOKIE_SECURE !== "false";

let pool;

class HttpError extends Error {
  constructor(statusCode, message) {
    super(message);
    this.statusCode = statusCode;
  }
}

function getPool() {
  if (!process.env.DATABASE_URL) {
    throw new HttpError(500, "DATABASE_URL is not configured.");
  }

  if (!pool) {
    pool = new Pool({
      connectionString: process.env.DATABASE_URL,
      ssl: process.env.PGSSLMODE === "disable" ? false : { rejectUnauthorized: false }
    });
  }

  return pool;
}

function getSessionSecret() {
  const secret = process.env.AUTH_SESSION_SECRET;
  if (!secret || secret.length < 32) {
    throw new HttpError(500, "AUTH_SESSION_SECRET must be at least 32 characters.");
  }
  return secret;
}

function response(statusCode, body, extraHeaders = {}) {
  return {
    statusCode,
    headers: {
      "Content-Type": "application/json",
      "Cache-Control": "no-store",
      ...extraHeaders
    },
    body: JSON.stringify(body)
  };
}

function methodNotAllowed(allowedMethods) {
  return response(405, { ok: false, error: "method_not_allowed" }, {
    Allow: allowedMethods.join(", ")
  });
}

function readJsonBody(event) {
  if (!event.body) {
    return {};
  }

  const text = event.isBase64Encoded
    ? Buffer.from(event.body, "base64").toString("utf8")
    : event.body;

  return JSON.parse(text);
}

function parseCookies(cookieHeader = "") {
  return cookieHeader.split(";").reduce((cookies, cookiePart) => {
    const index = cookiePart.indexOf("=");
    if (index < 0) {
      return cookies;
    }

    const key = cookiePart.slice(0, index).trim();
    const value = cookiePart.slice(index + 1).trim();
    if (key) {
      try {
        cookies[key] = decodeURIComponent(value);
      } catch {
        cookies[key] = value;
      }
    }
    return cookies;
  }, {});
}

function getSessionToken(event) {
  const header = event.headers.cookie || event.headers.Cookie || "";
  return parseCookies(header)[COOKIE_NAME] || "";
}

function hashSessionToken(token) {
  return crypto.createHmac("sha256", getSessionSecret()).update(token).digest("hex");
}

function createSessionCookie(token, expiresAt) {
  const parts = [
    `${COOKIE_NAME}=${encodeURIComponent(token)}`,
    "Path=/",
    "HttpOnly",
    "SameSite=Lax",
    `Expires=${expiresAt.toUTCString()}`,
    `Max-Age=${Math.max(1, Math.floor((expiresAt.getTime() - Date.now()) / 1000))}`
  ];

  if (COOKIE_SECURE) {
    parts.push("Secure");
  }

  return parts.join("; ");
}

function clearSessionCookie() {
  const parts = [
    `${COOKIE_NAME}=`,
    "Path=/",
    "HttpOnly",
    "SameSite=Lax",
    "Expires=Thu, 01 Jan 1970 00:00:00 GMT",
    "Max-Age=0"
  ];

  if (COOKIE_SECURE) {
    parts.push("Secure");
  }

  return parts.join("; ");
}

function sessionExpiryDate() {
  return new Date(Date.now() + SESSION_DAYS * 24 * 60 * 60 * 1000);
}

function toBase64Url(buffer) {
  return Buffer.from(buffer).toString("base64url");
}

function verifyPassword(password, storedHash) {
  const parts = String(storedHash || "").split("$");
  if (parts.length !== 4 || parts[0] !== "pbkdf2_sha256") {
    return false;
  }

  const iterations = Number.parseInt(parts[1], 10);
  const salt = parts[2];
  const expected = parts[3];
  if (!Number.isFinite(iterations) || iterations < 100000 || !salt || !expected) {
    return false;
  }

  const actual = toBase64Url(crypto.pbkdf2Sync(password, salt, iterations, 32, "sha256"));
  const actualBuffer = Buffer.from(actual);
  const expectedBuffer = Buffer.from(expected);
  return actualBuffer.length === expectedBuffer.length && crypto.timingSafeEqual(actualBuffer, expectedBuffer);
}

function userPayload(row) {
  return {
    id: row.id,
    email: row.email,
    displayName: row.display_name || "",
    studentId: row.student_id || ""
  };
}

async function findSessionUser(event) {
  const token = getSessionToken(event);
  if (!token) {
    return null;
  }

  const tokenHash = hashSessionToken(token);
  const result = await getPool().query(
    `select u.id::text as id, u.email, u.display_name, u.student_id
       from sessions s
       join users u on u.id = s.user_id
      where s.token_hash = $1
        and s.revoked_at is null
        and s.expires_at > now()
        and u.active = true
      limit 1`,
    [tokenHash]
  );

  return result.rows[0] || null;
}

function handleError(error) {
  if (error instanceof SyntaxError) {
    return response(400, { ok: false, error: "invalid_json" });
  }

  if (error instanceof HttpError) {
    return response(error.statusCode, { ok: false, error: error.message });
  }

  console.error(error);
  return response(500, { ok: false, error: "server_error" });
}

module.exports = {
  clearSessionCookie,
  createSessionCookie,
  findSessionUser,
  getPool,
  getSessionToken,
  handleError,
  hashSessionToken,
  methodNotAllowed,
  readJsonBody,
  response,
  sessionExpiryDate,
  userPayload,
  verifyPassword
};
