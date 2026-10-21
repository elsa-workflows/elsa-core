import { useLayoutEffect, useMemo, useState, type ReactNode } from "react";
import type { StudioModule } from "@elsa-workflows/react-studio-sdk";
import { CultureProvider, culturePreferenceKey, studioI18n, translationNamespace, useTranslation } from "@elsa-workflows/react-studio-sdk/localization";
import { useStudioConfig } from "@elsa-workflows/react-studio-sdk/config-provider";
import { useActiveServer } from "@elsa-workflows/react-studio-sdk/environments/active-server";
import { tenantIdentityKey } from "@elsa-workflows/react-studio-sdk/tenancy";
import { useAuth } from "@/lib/auth/provider";

export function LocalizationProvider({ modules, children }: { modules: StudioModule[]; children: ReactNode }) {
  const { ui } = useStudioConfig();
  const server = useActiveServer();
  const auth = useAuth();
  const t = useTranslation();
  const [storageFailed, setStorageFailed] = useState(false);
  const key = culturePreferenceKey(server.url, tenantIdentityKey(auth.user));
  const cultures = useMemo(() => [...new Set(["en", "nl", ...modules.flatMap(module =>
    module.extensions?.translations?.map(resource => resource.culture) ?? [])])].sort(), [modules]);

  useLayoutEffect(() => {
    const namespaces = new Set<string>();
    for (const module of modules) {
      const namespace = translationNamespace(module.id);
      for (const resource of module.extensions?.translations ?? []) {
        namespaces.add(namespace);
        studioI18n.addResourceBundle(resource.culture, namespace, resource.messages, true, true);
      }
    }
    return () => {
      for (const culture of cultures) for (const namespace of namespaces) studioI18n.removeResourceBundle(culture, namespace);
    };
  }, [modules, cultures]);

  useLayoutEffect(() => {
    if (auth.status === "initializing") return;
    let saved: string | null = null;
    try { saved = localStorage.getItem(key); } catch { /* Reading preferences is optional. */ }
    const culture = saved && cultures.includes(saved) ? saved : cultures.includes(ui.defaultCulture ?? "en") ? ui.defaultCulture ?? "en" : "en";
    void studioI18n.changeLanguage(culture);
    setStorageFailed(false);
  }, [key, cultures, ui.defaultCulture, auth.status]);

  const culture = studioI18n.language;
  useLayoutEffect(() => {
    document.documentElement.lang = culture;
    document.documentElement.dir = studioI18n.dir(culture);
  }, [culture]);
  return <CultureProvider value={{ culture, cultures, error: storageFailed ? t("Language preference could not be saved. Enable browser storage and try again.") : undefined,
    selectCulture: value => {
      if (!cultures.includes(value)) return;
      try { localStorage.setItem(key, value); }
      catch { setStorageFailed(true); return; }
      setStorageFailed(false);
      void studioI18n.changeLanguage(value);
    } }}>
    {children}
  </CultureProvider>;
}
