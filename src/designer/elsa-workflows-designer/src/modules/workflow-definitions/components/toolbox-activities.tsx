import {Component, h, Prop, State, Watch} from "@stencil/core";
import {Addon, Graph} from '@antv/x6';
import groupBy from 'lodash/groupBy';
import {Container} from 'typedi';
import {ActivityDescriptor} from "../../../models";
import descriptorsStore from "../../../data/descriptors-store";
import {ActivityDriverRegistry} from "../../../services";

interface ActivityCategoryModel {
  category: string;
  activities: Array<ActivityDescriptor>;
  expanded: boolean;
}

@Component({
  tag: 'elsa-workflow-definition-editor-toolbox-activities',
})
export class ToolboxActivities {
  @Prop() graph: Graph;
  private dnd: Addon.Dnd;
  @State() private expandedCategories: Array<string> = [];

  @Watch('graph')
  handleGraphChanged(value: Graph) {

    if (this.dnd)
      this.dnd.dispose();

    this.dnd = new Addon.Dnd({
      target: value,
      scaled: false,
      animation: true,
    });
  }

  private static onActivityStartDrag(e: DragEvent, activityDescriptor: ActivityDescriptor) {
    const json = JSON.stringify(activityDescriptor);
    e.dataTransfer.setData('activity-descriptor', json);
  }

  private onToggleActivityCategory(categoryModel: ActivityCategoryModel) {
    const category = categoryModel.category;
    const expandedCategories = this.expandedCategories;
    const isExpanded = !!expandedCategories.find(x => x == category);

    if(isExpanded)
      this.expandedCategories = expandedCategories.filter(x => x != category);
    else
      this.expandedCategories = [...expandedCategories, category];
  }

  buildModel = (): any => {
    const browsableDescriptors = descriptorsStore.activityDescriptors.filter(x => x.isBrowsable);
    const categorizedActivitiesLookup = groupBy(browsableDescriptors, x => x.category);
    const categories = Object.keys(categorizedActivitiesLookup);
    const renderedActivities: Map<string, string> = new Map<string, string>();

    // Group activities by category
    const activityCategoryModels = categories.map(x => {
      const model: ActivityCategoryModel = {
        category: x,
        expanded: !!this.expandedCategories.find(c => c == x),
        activities: categorizedActivitiesLookup[x]
      };

      return model;
    });

    // Render activities.
    const activityDriverRegistry = Container.get(ActivityDriverRegistry);

    for (const activityDescriptor of browsableDescriptors) {
      const activityType = activityDescriptor.typeName;
      const driver = activityDriverRegistry.createDriver(activityType);
      const html = driver.display({displayType: 'picker', activityDescriptor: activityDescriptor});

      renderedActivities.set(activityType, html);
    }

    return {
      categories: activityCategoryModels,
      activities: renderedActivities
    };
  }

  render() {

    const model = this.buildModel();
    const categoryModels:Array<ActivityCategoryModel> = model.categories;
    const renderedActivities = model.activities;

    return <nav class="flex-1 px-2 space-y-1 font-sans text-sm text-gray-600">
      {categoryModels.map(categoryModel => {

          const category = categoryModel.category;
          const activityDescriptors: Array<ActivityDescriptor> = categoryModel.activities;
          const categoryButtonClass = categoryModel.expanded ? 'rotate-90' : '';
          const categoryContentClass = categoryModel.expanded ? '' : 'hidden';

          return <div class="space-y-1">
            <button type="button"
                    onClick={() => this.onToggleActivityCategory(categoryModel)}
                    class="text-gray-600 hover:bg-gray-50 hover:text-gray-900 group w-full flex items-center pr-2 py-2 text-left text-sm font-medium rounded-md focus:outline-none">
              <svg
                class={`${categoryButtonClass} text-gray-300 mr-2 flex-shrink-0 h-5 w-5 transform group-hover:text-gray-400 transition-colors ease-in-out duration-150`}
                viewBox="0 0 20 20" aria-hidden="true">
                <path d="M6 6L14 10L6 14V6Z" fill="currentColor"/>
              </svg>
              {category}
            </button>

            <div class={`space-y-0.5 ${categoryContentClass}`}>

              {activityDescriptors.map(activityDescriptor => {
                const activityHtml = renderedActivities.get(activityDescriptor.typeName);
                return (
                  <div class="w-full flex items-center pl-10 pr-2 py-2">
                    <div class="relative cursor-move" onDragStart={e => ToolboxActivities.onActivityStartDrag(e, activityDescriptor)}>
                      <elsa-tooltip tooltipPosition="right" tooltipContent={activityDescriptor.description}>
                        <div innerHTML={activityHtml} draggable={true}/>
                      </elsa-tooltip>
                    </div>
                  </div>
                );
              })}

            </div>
          </div>;
        }
      )}
    </nav>
  }
}
