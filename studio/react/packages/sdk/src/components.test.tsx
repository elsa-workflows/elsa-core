import { renderToStaticMarkup } from "react-dom/server";
import { expect, it } from "vitest";
import { DesignerSurface, StudioComponentsProvider } from "./components.js";
import { StudioRegistry } from "./host.js";
import { DesignerProvider, useDesignerState } from "./designer/context.js";
import { createDesignerStore } from "./designer/store.js";
import { emptyFlowchart } from "./designer/model/flowchart-ops.js";
import { ActivityRegistry } from "./designer/model/registry.js";

it("renders a separately registered designer through the shared context without source imports", async () => {
  const registry = new StudioRegistry();
  const store = createDesignerStore(emptyFlowchart("shared-workflow"));
  function ExternalDesigner() {
    const id = useDesignerState(state => state.root.id);
    return <p>{id}</p>;
  }
  await registry.load({ id: "designer", version: "1", hostApiVersion: 1, sdkApiVersion: 1, entry: "designer.js" }, async () => ({
    register(api) { api.contribute("components", { id: "surface", name: "DesignerSurface", component: ExternalDesigner }); },
  }));
  const markup = renderToStaticMarkup(
    <StudioComponentsProvider components={registry.components()}>
      <DesignerProvider store={store} registry={new ActivityRegistry([])}>
        <DesignerSurface />
      </DesignerProvider>
    </StudioComponentsProvider>,
  );
  expect(markup).toBe("<p>shared-workflow</p>");
  registry.remove("designer");
  expect(registry.components()).toEqual({});
});
