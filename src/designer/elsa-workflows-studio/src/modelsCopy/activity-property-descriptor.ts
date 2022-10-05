export interface ActivityPropertyDescriptor {
  name: string;
  type: string;
  label: string;
  hint?: string;
  options?: any;

  uiHint?: string;
  category?: string;
  defaultValue?: any;
  defaultSyntax?: string;
  supportedSyntaxes?: Array<string>;
  isReadOnly?: boolean;
  defaultWorkflowStorageProvider?: string;
  disableWorkflowProviderSelection?: boolean;
  considerValuesAsOutcomes?: boolean;
}
