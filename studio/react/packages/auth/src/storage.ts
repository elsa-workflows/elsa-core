/** Browser-side persistence for auth tokens. Keyed per backend so multiple environments can coexist. */

export interface StoredTokens {
  accessToken: string;
  refreshToken?: string;
}

const PREFIX = "elsa-studio:auth:";

function key(scope: string) {
  return `${PREFIX}${scope}`;
}

export function readTokens(scope: string): StoredTokens | undefined {
  if (typeof window === "undefined") return undefined;
  try {
    const raw = window.localStorage.getItem(key(scope));
    return raw ? (JSON.parse(raw) as StoredTokens) : undefined;
  } catch {
    return undefined;
  }
}

export function writeTokens(scope: string, tokens: StoredTokens | undefined) {
  if (typeof window === "undefined") return;
  try {
    if (!tokens) window.localStorage.removeItem(key(scope));
    else window.localStorage.setItem(key(scope), JSON.stringify(tokens));
  } catch {
    /* storage unavailable (private mode etc.) */
  }
}
