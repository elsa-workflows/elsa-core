import { createElement as h, useContext } from "react";
import { useQuery } from "@tanstack/react-query";
import { ThemeContext, type StudioApi } from "@elsa-workflows/react-studio-sdk";
import "./weather.css";

function Weather() {
  const theme = useContext(ThemeContext);
  const query = useQuery({ queryKey: ["shared-counter"], initialData: 0, enabled: false });
  return h("section", { className: "weather-module" },
    h("h2", null, "Third-party weather from NuGet"),
    h("p", null, `Theme: ${theme?.theme ?? "missing context"}`),
    h("p", { "data-testid": "weather-counter" }, `Shared count: ${query.data}`));
}

export function register(api: StudioApi) {
  api.routes.push({ id: "weather", order: 20, value: { path: "/weather", component: Weather } });
  api.navigation.push({ id: "weather", order: 20, value: { path: "/weather", label: "Weather" } });
}
