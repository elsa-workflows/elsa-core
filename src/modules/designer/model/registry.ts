import type { ActivityDescriptor } from "@/lib/api/types/descriptors";

/** Fast lookup over activity descriptors by type name (+ version). */
export class ActivityRegistry {
  private readonly byType = new Map<string, ActivityDescriptor[]>();

  constructor(public readonly descriptors: ActivityDescriptor[]) {
    for (const d of descriptors) {
      const list = this.byType.get(d.typeName) ?? [];
      list.push(d);
      this.byType.set(d.typeName, list);
    }
    for (const list of this.byType.values()) list.sort((a, b) => b.version - a.version);
  }

  get(typeName: string, version?: number): ActivityDescriptor | undefined {
    const list = this.byType.get(typeName);
    if (!list?.length) return undefined;
    if (version === undefined) return list[0];
    return list.find((d) => d.version === version) ?? list[0];
  }

  /** Browsable descriptors grouped by `/`-separated category path. */
  get browsable(): ActivityDescriptor[] {
    return this.descriptors.filter((d) => d.isBrowsable !== false);
  }

  static readonly Empty = new ActivityRegistry([]);
}
