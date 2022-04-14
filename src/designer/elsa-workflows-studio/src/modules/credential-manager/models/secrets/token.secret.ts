import { Secret } from "../secret.model";

export const Token: Secret = {
  category: "Http",
  customAttributes: {},
  description: "Authorization token secret",
  displayName: "Authorization",
  inputProperties: [
    {
      considerValuesAsOutcomes: false,
      disableWorkflowProviderSelection: false,
      isBrowsable: true,
      isDesignerCritical: false,
      isReadOnly: false,
      label: "Authorization",
      name: "Authorization",
      order: 0,
      supportedSyntaxes: ["JavaScript", "Liquid"],
      type: "System.String",
      uiHint: "single-line",
    }
  ],
  properties: [
    {
        considerValuesAsOutcomes: false,
        disableWorkflowProviderSelection: false,
        isBrowsable: true,
        isDesignerCritical: false,
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