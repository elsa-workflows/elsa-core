import { Activity, ActivityPropertyDescriptor, RenderDesignerResult } from "../modelsCopy";

export interface FieldDriver {
  displayEditor: (activity: Activity, property: ActivityPropertyDescriptor) => RenderDesignerResult;
  updateEditor: (activity: Activity, property: ActivityPropertyDescriptor, formData: FormData) => void;
}
