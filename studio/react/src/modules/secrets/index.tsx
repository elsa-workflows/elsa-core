import { KeyRound } from "lucide-react";
import { defineModule, NavGroups } from "@/lib/modules/types";
import { Resources, Verbs } from "@/lib/permissions";
import { SecretPickerEditor } from "./components/secret-picker-editor";
import { SecretDetailPage } from "./pages/secret-detail-page";
import { SecretsPage } from "./pages/secrets-page";
import { SecretRoutes } from "./routes";

export { SecretRoutes };

export { SecretPicker, type SecretPickerProps } from "./components/secret-picker";
export { SecretPickerEditor, secretNameOf } from "./components/secret-picker-editor";
export { SECRET_EXPRESSION_TYPE, type SecretModel, type SecretReference } from "./api";

export const secretsModule = defineModule({
  id: "secrets",
  name: "Secrets",
  description: "Encrypted credentials workflows reference by name.",
  requiresServerFeatures: ["Elsa.Secrets"],
  routes: [
    { path: SecretRoutes.list, component: SecretsPage, title: "Secrets", permissions: [{ resource: Resources.secrets, verb: Verbs.view }] },
    { path: SecretRoutes.detail, component: SecretDetailPage, title: "Secret", permissions: [{ resource: Resources.secrets, verb: Verbs.view }] },
  ],
  nav: [
    {
      id: "secrets",
      label: "Secrets",
      href: SecretRoutes.list,
      icon: KeyRound,
      group: NavGroups.administration.id,
      order: 300,
      permissions: [{ resource: Resources.secrets, verb: Verbs.view }],
    },
  ],
  extensions: {
    uiHints: { "secret-picker": SecretPickerEditor },
  },
});
