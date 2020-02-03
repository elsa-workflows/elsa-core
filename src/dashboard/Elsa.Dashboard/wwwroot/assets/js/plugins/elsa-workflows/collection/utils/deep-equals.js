export function deepEquals(a, b) {
    const jsonA = JSON.stringify(a);
    const jsonB = JSON.stringify(b);
    return jsonA === jsonB;
}
