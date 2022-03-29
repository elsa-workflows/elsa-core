import {Map} from '../../../utils/utils';

export interface Secret {
  category: string;
  customAttributes: any;
  description: string;
  displayName: string;
  inputProperties: SecretProperties[]
  outcomes: any[];
  outputProperties: SecretProperties[]
  properties: SecretProperties[];
  type: string;
}

export interface SecretModel {
  id?: string;
  type: string;
  name?: string;
  displayName?: string;
  properties: Array<SecretDefinitionProperty>;
}

export interface SecretDefinitionProperty {
  name: string;
  syntax?: string;
  expressions: Map<string>;
  value?: any;
}


export interface SecretDescriptor {
  type: string;
  displayName: string;
  description?: string;
  category: string;
  //traits: ActivityTraits;
  outcomes: Array<string>;
  browsable: boolean;
  inputProperties: Array<SecretPropertyDescriptor>;
  outputProperties: Array<SecretPropertyDescriptor>;
  customAttributes: any;
}
export interface SecretPropertyDescriptor {
  name: string;
  uiHint: string;
  label?: string;
  hint?: string;
  options?: any;
  category?: string;
  defaultValue?: any;
  defaultSyntax?: string;
  supportedSyntaxes: Array<string>;
  isReadOnly?: boolean;
  defaultWorkflowStorageProvider?: string;
  disableWorkflowProviderSelection: boolean;
  considerValuesAsOutcomes: boolean;
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

export interface TabModel {
  tabName: string;
  renderContent: () => any;
}

export interface SecretEditorRenderProps {
  secretDescriptor?: SecretDescriptor;
  secretModel?: SecretModel;
  propertyCategories?: Array<string>;
  defaultProperties?: Array<SecretPropertyDescriptor>;
  tabs?: Array<TabModel>;
  selectedTabName?: string;
}
