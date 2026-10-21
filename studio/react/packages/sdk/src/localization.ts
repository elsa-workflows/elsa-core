import { createInstance, type i18n, type TOptions } from "i18next";
import { useTranslation as useReactTranslation } from "react-i18next";
import { createContext, useCallback, useContext } from "react";
import english from "./locales/en.json" with { type: "json" };
import dutch from "./locales/nl.json" with { type: "json" };

/** The browser's shared translation instance. Modules use the hooks below. */
export const studioI18n: i18n = createInstance();
void studioI18n.init({
  lng: "en", fallbackLng: "en", defaultNS: "studio", fallbackNS: "studio",
  keySeparator: false, nsSeparator: false, initAsync: false,
  interpolation: { escapeValue: false },
  react: { bindI18nStore: "added removed" },
  resources: { en: { studio: english }, nl: { studio: dutch } },
});

export type TranslationValues = Record<string, unknown>;
export type Translate = (message: string, values?: TranslationValues, moduleId?: string) => string;
/** Marks a static metadata message for catalog extraction; translate it when rendered. */
export function message(value: string): string { return value; }
export const translationNamespace = (moduleId?: string) => moduleId === undefined ? "studio" : `module:${moduleId}`;

/** Translate UI messages in non-React helpers. Components use useTranslation for updates. */
export const translate: Translate = (message, values, moduleId) => studioI18n.t(message, {
  ...values, ns: translationNamespace(moduleId), defaultValue: message,
} as TOptions) as string;

/** Subscribes to culture/resource changes without remounting a component or its editor state. */
export function useTranslation(moduleId?: string): Translate {
  const { t } = useReactTranslation(translationNamespace(moduleId), {
    i18n: studioI18n, useSuspense: false,
  });
  return useCallback((message, values, owner = moduleId) => t(message, {
    ...values, ns: translationNamespace(owner), defaultValue: message,
  } as TOptions) as string, [t, moduleId]);
}

export function culturePreferenceKey(serverUrl: string, subject: string): string {
  return "elsa-studio:culture:" + JSON.stringify([serverUrl.replace(/\/+$/, ""), subject]);
}

export interface CultureContextValue {
  culture: string;
  cultures: string[];
  selectCulture: (culture: string) => void;
  error?: string;
}
const CultureContext = createContext<CultureContextValue>({ culture: "en", cultures: ["en", "nl"], selectCulture: () => {} });
export const CultureProvider = CultureContext.Provider;
export function useCulture(): CultureContextValue { return useContext(CultureContext); }
