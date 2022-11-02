import {ActivityModel, ActivityPropertyDescriptor} from "../models";
import {SecretModel, SecretPropertyDescriptor} from "../modules/credential-manager/models/secret.model";

export interface PropertyDisplayDriver {
  display(model: ActivityModel | SecretModel, property: ActivityPropertyDescriptor | SecretPropertyDescriptor, onUpdated?: () => void)

  update?(model: ActivityModel | SecretModel, property: ActivityPropertyDescriptor | SecretPropertyDescriptor, form: FormData)
}

