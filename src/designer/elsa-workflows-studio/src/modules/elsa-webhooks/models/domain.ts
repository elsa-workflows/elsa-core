import {Map} from '../../../utils/utils';

export interface WebhookDefinition {
  id?: string;
  tenantId?: string;
  name?: string;
  path?: string;
  description?: string;
  payloadTypeName?: string;
  isEnabled?: boolean;
  variables?: Variables;
}

export interface WebhookDefinitionSummary {
    id?: string;
    tenantId?: string;
    name?: string;
    path?: string;
    description?: string;
    payloadTypeName?: string;
    isEnabled?: boolean;
}

export interface Variables {
  data: Map<any>;
}