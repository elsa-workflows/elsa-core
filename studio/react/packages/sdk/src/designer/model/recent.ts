const KEY = "elsa-studio:recent-activities";
const LIMIT = 8;

export function readRecentActivities(): string[] {
  try {
    return JSON.parse(localStorage.getItem(KEY) ?? "[]") as string[];
  } catch {
    return [];
  }
}

export function rememberActivity(typeName: string) {
  try {
    const next = [typeName, ...readRecentActivities().filter((t) => t !== typeName)].slice(0, LIMIT);
    localStorage.setItem(KEY, JSON.stringify(next));
  } catch {
    /* storage unavailable */
  }
}
