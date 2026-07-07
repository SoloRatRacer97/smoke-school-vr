const {
  findSessionUser,
  handleError,
  methodNotAllowed,
  response,
  userPayload
} = require("./_auth");

exports.handler = async function authMe(event) {
  if (event.httpMethod === "OPTIONS") {
    return response(204, {});
  }

  if (event.httpMethod !== "GET") {
    return methodNotAllowed(["GET"]);
  }

  try {
    const user = await findSessionUser(event);
    if (!user) {
      return response(401, { ok: false, error: "not_authenticated" });
    }

    return response(200, { ok: true, user: userPayload(user) });
  } catch (error) {
    return handleError(error);
  }
};
