import i18next from 'i18next';

export async function loadTranslations(language: string, resources: any, defaultNamespace?: string): Promise<any> {
  const instance = i18next.createInstance();
  await instance.init({
    lng: language,
    fallbackLng: "en",
    resources: resources,
    defaultNS: defaultNamespace || 'default',
    debug: false
  });

  return instance;
}