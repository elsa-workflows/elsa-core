import { PagedList } from '../../models';
import {ActivityDefinitionSummary} from "./models";

export function updateSelectedActivityDefinitions(isChecked: boolean, ActivityDefinitions: PagedList<ActivityDefinitionSummary>, selectedActivityDefinitionIds: Array<string>) {
  const currentItems = ActivityDefinitions.items.map(x => x.definitionId);

  return isChecked
    ? selectedActivityDefinitionIds.concat(currentItems.filter(item => !selectedActivityDefinitionIds.includes(item)))
    : selectedActivityDefinitionIds.filter(item => !currentItems.includes(item));
}
