
import { message } from "@elsa-workflows/react-studio-sdk/localization";
import { defineModule } from "@elsa-workflows/react-studio-sdk";
import { EnvironmentPicker } from "./components/environment-picker";

export { EnvironmentPicker } from "./components/environment-picker";

export const environmentsModule = defineModule({
  id: "environments",
  name: "Environments",
  description: message("Switch between the configured Elsa servers from the header."),
  extensions: {
    headerItems: [{ id: "environment-picker", order: 10, component: EnvironmentPicker }],
  },
});
