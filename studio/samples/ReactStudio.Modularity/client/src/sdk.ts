import { createContext, type ComponentType, type Dispatch, type SetStateAction } from "react";

/** Temporary SDK v0 for #8839. Not a stable or published contract. */
export interface Slot<T> {
  id: string;
  order: number;
  value: T;
}

export interface StudioApi {
  routes: Slot<{ path: string; component: ComponentType }>[];
  navigation: Slot<{ label: string; path: string }>[];
  editors: Slot<{ hint: string; component: ComponentType }>[];
}

export function ordered<T>(slots: Slot<T>[]): Slot<T>[] {
  return [...slots].sort((a, b) => a.order - b.order || (a.id < b.id ? -1 : a.id > b.id ? 1 : 0));
}

export function contribute<T>(slots: Slot<T>[], slot: Slot<T>) {
  const index = slots.findIndex(item => item.id === slot.id);
  if (index < 0) slots.push(slot);
  else slots[index] = slot;
}

export const ThemeContext = createContext<{
  theme: string;
  setTheme: Dispatch<SetStateAction<string>>;
} | null>(null);
