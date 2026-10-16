// Server-only: reads the API's "a session probably exists" hint (no secret) on page requests.
import { cookies } from "next/headers";

/** Must match AuthEndpoints.SessionHintCookie in the API. */
export const SESSION_HINT_COOKIE = "if_session";

/**
 * True when the browser holds the session hint. Visitors without it get the landing page straight from the server and
 * skip the refresh call; visitors with it are sent to their dashboard once the session is confirmed (ADR 0018).
 * Example: `const sessionHint = await hasSessionHint();`
 */
export async function hasSessionHint(): Promise<boolean> {
  return (await cookies()).has(SESSION_HINT_COOKIE);
}
