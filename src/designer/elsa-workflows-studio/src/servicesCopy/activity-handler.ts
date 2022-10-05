import { Activity, ActivityDefinitionCopy, RenderDesignerResult } from "../modelsCopy";

export interface ActivityHandler {
  renderDesigner?: (activity: Activity, definition: ActivityDefinitionCopy) => RenderDesignerResult
  updateEditor?: (activity: Activity, formData: FormData) => Activity;
  getOutcomes?: (activity: Activity, definition: ActivityDefinitionCopy) => Array<string>;
}
