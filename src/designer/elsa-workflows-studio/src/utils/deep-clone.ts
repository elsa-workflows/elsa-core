export function deepClone(value: any){
  const json = JSON.stringify(value);
  return JSON.parse(json);
}
