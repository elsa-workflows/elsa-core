export interface Secret {
  category: string;
  customAttributes: any;
  description: string;
  displayName: string;
  inputProperties: SecretProperties[]
  outcomes: any[];
  outputProperties: SecretProperties[]
  properties: SecretProperties[];
  traits: number;
  type: string;
}

export interface SecretProperties {
  considerValuesAsOutcomes?: boolean;
  disableWorkflowProviderSelection?: boolean;
  hint?: string;
  isBrowsable?: boolean;
  isDesignerCritical?: boolean;
  isReadOnly?: boolean;
  label?: string;
  name?: string;
  order?: number;
  supportedSyntaxes?: string[];
  type?: string;
  uiHint?: string;
}