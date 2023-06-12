import {Component, h, Prop, State, Watch} from "@stencil/core";
import {Addon, Graph} from '@antv/x6';
import groupBy from 'lodash/groupBy';
import uniqBy from 'lodash/uniqBy';
import {Container} from 'typedi';
import {ActivityDescriptor} from "../../../models";
import descriptorsStore from "../../../data/descriptors-store";
import {ActivityDescriptorManager, ActivityDriverRegistry} from "../../../services";

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
  @Prop() isReadonly: boolean;
  private dnd: Addon.Dnd;
  @State() private expandedCategories: Array<string> = [];

  @State() private categoryModels: Array<any> = [];
  @State() private renderedActivities: any = [];

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

  componentWillLoad() {
    const activityDescriptorManager = Container.get(ActivityDescriptorManager);
    activityDescriptorManager.onActivityDescriptorsUpdated(this.buildModel);
    
    this.buildModel();
  }

  private static onActivityStartDrag(e: DragEvent, activityDescriptor: ActivityDescriptor) {
    const json = JSON.stringify(activityDescriptor);
    e.dataTransfer.setData('activity-descriptor', json);
  }

  private onToggleActivityCategory(categoryModel: ActivityCategoryModel) {
    const category = categoryModel.category;
    const expandedCategories = this.expandedCategories;
    const isExpanded = !!expandedCategories.find(x => x == category);

    if (isExpanded)
      this.expandedCategories = expandedCategories.filter(x => x != category);
    else
      this.expandedCategories = [...expandedCategories, category];
    categoryModel.expanded = !categoryModel.expanded;
  }

  buildModel = (searchVal: string = ''): any => {
    let browsableDescriptors = uniqBy(descriptorsStore.activityDescriptors
      .filter(x => x.isBrowsable !== false)
      .sort((a, b) => a.version > b.version ? 1 : -1), x => `${x.typeName}:${x.version}`);

    if (searchVal) {
      browsableDescriptors = browsableDescriptors.filter((activity) => activity.displayName.toLocaleLowerCase().includes(searchVal.toLocaleLowerCase()));
    }

    const categorizedActivitiesLookup = groupBy(browsableDescriptors, x => x.category);
    const categories = Object.keys(categorizedActivitiesLookup).sort((a, b) => a.localeCompare(b));
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

    this.categoryModels = activityCategoryModels;
    this.renderedActivities = renderedActivities;

    return {
      categories: activityCategoryModels,
      activities: renderedActivities
    };
  }

  filterActivities(ev: any) {
    let val = ev.target?.value || '';
    this.buildModel(val);
  }

  render() {
    return <nav class="tw-flex-1 tw-px-2 tw-space-y-1 tw-font-sans tw-text-sm tw-text-gray-600">
      <input class="tw-my-1" placeholder="Search Activities" type="text" name="activity-search" id="activitySearch" onInput={this.filterActivities.bind(this)} />

      {this.categoryModels.map(categoryModel => {
        const category = categoryModel.category;
        const activityDescriptors: Array<ActivityDescriptor> = categoryModel.activities;
        const categoryButtonClass = categoryModel.expanded ? 'tw-rotate-90' : '';
        const categoryContentClass = categoryModel.expanded ? '' : 'hidden';

        return <div class="tw-space-y-1">
          <button type="button"
            onClick={() => this.onToggleActivityCategory(categoryModel)}
            class="tw-text-gray-600 hover:tw-bg-gray-50 hover:tw-text-gray-900 tw-group tw-w-full tw-flex tw-items-center tw-pr-2 tw-py-2 tw-text-left tw-text-sm tw-font-medium tw-rounded-md focus:tw-outline-none">
            <svg
              class={`${categoryButtonClass} tw-text-gray-300 tw-mr-2 tw-flex-shrink-0 tw-h-5 tw-w-5 tw-transform group-hover:tw-text-gray-400 tw-transition-colors tw-ease-in-out tw-duration-150`}
              viewBox="0 0 20 20" aria-hidden="true">
                <path d="M6 6L14 10L6 14V6Z" fill="currentColor"/>
            </svg>
            {category}
          </button>

          <div class={`tw-space-y-0.5 ${categoryContentClass}`}>

            {activityDescriptors.map(activityDescriptor => {
              const activityHtml = this.renderedActivities.get(activityDescriptor.typeName);
              return (
                <div class="tw-w-full tw-flex tw-items-center tw-pl-10 tw-pr-2 tw-py-2">
                      <div class="tw-relative tw-cursor-move" onDragStart={e => ToolboxActivities.onActivityStartDrag(e, activityDescriptor)}>
                    <elsa-tooltip tooltipPosition="right" tooltipContent={activityDescriptor.description}>
                      <div innerHTML={activityHtml} draggable={!this.isReadonly}/>
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
