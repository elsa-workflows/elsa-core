export function joinAuthEndpoint(baseUrl: string, path: string): string {
  let baseEnd = baseUrl.length;
  while (baseEnd > 0 && baseUrl[baseEnd - 1] === "/") baseEnd--;

  let pathStart = 0;
  while (path[pathStart] === "/") pathStart++;

  return `${baseUrl.slice(0, baseEnd)}/${path.slice(pathStart)}`;
}
