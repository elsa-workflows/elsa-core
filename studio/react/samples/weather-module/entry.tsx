import { useAuth } from "@elsa-workflows/react-studio-auth";
import { Button } from "@elsa-workflows/react-studio-ui/button";
import type { StudioApi } from "@elsa-workflows/react-studio-sdk";
import type { WorkflowDefinitionSummary } from "@elsa-workflows/react-studio-api-client";
import { useTranslation } from "@elsa-workflows/react-studio-sdk/localization";

export function Weather() {
  const t = useTranslation("weather");
  const { user } = useAuth();
  return <Button variant="outline">{t("Weather for {{name}}", { name: user?.name ?? t("guest") })}</Button>;
}

export function register(api: StudioApi) {
  api.contribute("translations", { id: "en", culture: "en", messages: { "Weather for {{name}}": "Weather for {{name}}", guest: "guest", "Weather report": "Weather report" } });
  api.contribute("translations", { id: "nl", culture: "nl", messages: { "Weather for {{name}}": "Het weer voor {{name}}", guest: "gast", "Weather report": "Weerbericht" } });
  api.contribute("headerItems", { id: "weather", order: 100, component: Weather });
  api.contribute("definitionRowActions", {
    id: "weather-report",
    label: "Weather report",
    onSelect: (definition: WorkflowDefinitionSummary, helpers) => {
      helpers.navigate(`/weather?definition=${encodeURIComponent(definition.definitionId)}`);
    },
  });
}
