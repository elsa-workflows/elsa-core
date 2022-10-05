import { ActivityPropertyDescriptor } from "./"

export type Lambda<T = any> = string | T

export interface ActivityDefinitionCopy {
  type: string
  displayName: string
  description?: string
  runtimeDescription?: Lambda<string>,
  category?: string
  icon?: string
  properties?: Array<ActivityPropertyDescriptor>
  outcomes?: Lambda<Array<string>>
}
