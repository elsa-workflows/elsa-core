export function deepEquals<T = any>(a: T, b: T): boolean {
  const jsonA = JSON.stringify(a);
  const jsonB = JSON.stringify(b);
  return jsonA === jsonB;
}
