import type { InputDescriptor, SelectListItem } from "@/lib/api/types/descriptors";

/**
 * Finds the select-list items for a dropdown/checklist/radiolist input. The server puts them under
 * `uiSpecifications.<hint>.selectList.items` (or `checkList.items`); older descriptors use `options`.
 */
export function selectListItems(descriptor: InputDescriptor): SelectListItem[] {
  const specs = descriptor.uiSpecifications ?? {};
  for (const spec of Object.values(specs)) {
    const s = spec as { selectList?: { items?: SelectListItem[] }; checkList?: { items?: SelectListItem[] } } | undefined;
    const items = s?.selectList?.items ?? s?.checkList?.items;
    if (Array.isArray(items)) return items.map(normalize);
  }
  const options = descriptor.options as unknown;
  if (Array.isArray(options)) return options.map(normalize);
  if (options && typeof options === "object" && Array.isArray((options as { items?: unknown[] }).items)) {
    return ((options as { items: unknown[] }).items).map(normalize);
  }
  return [];
}

function normalize(item: unknown): SelectListItem {
  if (typeof item === "string" || typeof item === "number") return { text: String(item), value: String(item) };
  const o = item as { text?: string; value?: unknown; label?: string };
  return { text: o.text ?? o.label ?? String(o.value ?? ""), value: String(o.value ?? o.text ?? "") };
}

export function codeLanguageOf(descriptor: InputDescriptor): string | undefined {
  const spec = descriptor.uiSpecifications?.["code-editor"] as { language?: string } | undefined;
  return spec?.language;
}
