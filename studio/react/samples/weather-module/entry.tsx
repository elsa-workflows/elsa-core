import { useAuth } from "@elsa-workflows/react-studio-auth";
import { Button } from "@elsa-workflows/react-studio-ui/button";
import type { StudioApi } from "@elsa-workflows/react-studio-sdk";
import type { WorkflowDefinitionSummary } from "@elsa-workflows/react-studio-api-client";

export function Weather() {
  const { user } = useAuth();
  return <Button variant="outline">Weather for {user?.name ?? "guest"}</Button>;
}

export function register(api: StudioApi) {
  api.contribute("headerItems", { id: "weather", order: 100, component: Weather });
  api.contribute("definitionRowActions", {
    id: "weather-report",
    label: "Weather report",
    onSelect: (definition: WorkflowDefinitionSummary, helpers) => {
      helpers.navigate(`/weather?definition=${encodeURIComponent(definition.definitionId)}`);
    },
  });
}
