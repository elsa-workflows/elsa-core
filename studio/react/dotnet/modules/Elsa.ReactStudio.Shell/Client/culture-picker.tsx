import { useCulture, useTranslation } from "@elsa-workflows/react-studio-sdk/localization";
import { SimpleSelect } from "@elsa-workflows/react-studio-ui/common/simple-select";

export function CulturePicker() {
  const t = useTranslation();
  const { culture, cultures, selectCulture, error } = useCulture();
  const names = new Intl.DisplayNames([culture], { type: "language" });
  return <div>
    <SimpleSelect aria-label={t("Language")} value={culture}
      options={cultures.map(value => ({ value, label: names.of(value) ?? value }))} onChange={value => { if (value) selectCulture(value); }} />
    {error && <span role="alert">{error}</span>}
  </div>;
}
