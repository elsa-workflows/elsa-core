import { Secret } from "../secret.model";

export const Token: Secret = {
  category: "Http",
  customAttributes: {},
  description: "Authorization token",
  displayName: "Authorization",
  inputProperties: [
    {
        disableWorkflowProviderSelection: false,
        isBrowsable: true,
        isReadOnly: false,
        label: "Authorization",
        name: "Authorization",
        order: 0,
        supportedSyntaxes: ["JavaScript", "Liquid"],
        type: "System.String",
        uiHint: "single-line",
      }
  ],
  type: "Authorization"
}
