/** The host writes the base element per request; the same build supports any mount. */
export function studioBasePath() {
  return new URL(document.baseURI).pathname.replace(/\/$/, "");
}

export function studioUrl(path: string) {
  return `${studioBasePath()}/${path.replace(/^\//, "")}`;
}

export function studioRoute(path: string) {
  const base = studioBasePath();
  return base && (path === base || path.startsWith(base + "/")) ? path.slice(base.length) || "/" : path;
}
