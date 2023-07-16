import { ActivityPropertyDescriptor } from '../../../models';
import {Map} from '../../../utils/utils';

export interface Secret {
  category: string;
  customAttributes: any;
  description: string;
  displayName: string;
  inputProperties: SecretProperties[];
  type: string;
}

export interface SecretModel {
  id?: string;
  type: string;
  name: string;
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
  outcomes: Array<string>;
  browsable: boolean;
  inputProperties: Array<SecretPropertyDescriptor>;
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
}

export interface SecretProperties extends ActivityPropertyDescriptor {
  isBrowsable?: boolean;
  order?: number;
}

export interface SecretEditorRenderProps {
  secretDescriptor?: SecretDescriptor;
  secretModel?: SecretModel;
  defaultProperties?: Array<SecretPropertyDescriptor>;
}
