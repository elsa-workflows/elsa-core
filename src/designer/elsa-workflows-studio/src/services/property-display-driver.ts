import {ActivityModel, ActivityPropertyDescriptor} from "../models";

export interface PropertyDisplayDriver {
  display(activity: ActivityModel, property: ActivityPropertyDescriptor)

  update?(activity: ActivityModel, property: ActivityPropertyDescriptor, form: FormData)
}

