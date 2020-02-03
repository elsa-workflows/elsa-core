export function deepClone(value) {
    const json = JSON.stringify(value);
    return JSON.parse(json);
}
