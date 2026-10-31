import { createElement as h, useContext } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { contribute, ThemeContext, type StudioApi } from "@elsa-workflows/react-studio-sdk";
import "./labels.css";

function Labels() {
  const theme = useContext(ThemeContext);
  const query = useQuery({ queryKey: ["shared-counter"], initialData: 0, enabled: false });
  const client = useQueryClient();
  return h("section", { className: "labels-module" },
    h("h2", null, "Labels from NuGet"),
    h("p", null, `Theme: ${theme?.theme ?? "missing context"}`),
    h("p", { "data-testid": "labels-counter" }, `Shared count: ${query.data}`),
    h("button", { onClick: () => client.setQueryData<number>(["shared-counter"], n => (n ?? 0) + 1) }, "Add label"));
}

function LabelEditor() {
  return h("label", null, "Label editor ", h("input", { "aria-label": "Label editor", placeholder: "New label" }));
}

export function register(api: StudioApi) {
  contribute(api.routes, { id: "labels", order: 10, value: { path: "/labels", component: Labels } });
  contribute(api.navigation, { id: "labels", order: 10, value: { path: "/labels", label: "Labels" } });
  contribute(api.editors, { id: "labels", order: 10, value: { hint: "label", component: LabelEditor } });
}

if (import.meta.hot) {
  import.meta.hot.accept(module => {
    if (module) document.dispatchEvent(new CustomEvent("studio:module-update", { detail: module.register }));
  });
}
