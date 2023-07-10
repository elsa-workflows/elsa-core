import { Component, h, Prop, State } from '@stencil/core';
import { ActivityDefinitionProperty, ActivityModel, ActivityPropertyDescriptor, RuntimeSelectListProviderSettings, SelectList, SyntaxNames } from "../../../../models";
import Tunnel from "../../../../data/workflow-editor";
import { getSelectListItems } from "../../../../utils/select-list-items";
import { awaitElement } from "../../../../utils/utils";

@Component({
  tag: 'elsa-dropdown-property',
  shadow: false,
})
export class ElsaDropdownProperty {

  @Prop() activityModel: ActivityModel;
  @Prop() propertyDescriptor: ActivityPropertyDescriptor;
  @Prop() propertyModel: ActivityDefinitionProperty;
  @Prop({ mutable: true }) serverUrl: string;
  @State() currentValue?: string;

  selectList: SelectList = { items: [], isFlagsEnum: false };

  async componentWillLoad() {
    const defaultSyntax = this.propertyDescriptor.defaultSyntax || SyntaxNames.Literal;
    this.currentValue = this.propertyModel.expressions[defaultSyntax] || undefined;
    const dependsOnEvent = this.propertyDescriptor.options && "context" in this.propertyDescriptor.options ? this.propertyDescriptor.options?.context?.dependsOnEvent : undefined;

    // Does this property have a dependency on another property?
    if (!!dependsOnEvent) {

      // Collect current property values of the activity.
      const initialDepsValue = {};

      for (const event of dependsOnEvent) {
        for (const value of this.activityModel.properties) {
          initialDepsValue[value.name] = value.expressions['Literal'];
        }

        // Listen for change events on the dependency dropdown list.
        const dependentInputElement: HTMLSelectElement = await awaitElement('#' + event);
        dependentInputElement.addEventListener('change', this.reloadSelectListFromDeps);

        // Get the current value of the dependency dropdown list.
        initialDepsValue[event] = dependentInputElement.value;
      }

      const existingOptions = this.propertyDescriptor.options as RuntimeSelectListProviderSettings;
      // Load the list items from the backend.
      const options: RuntimeSelectListProviderSettings = {
        context: {
          ...existingOptions.context,
          depValues: initialDepsValue
        },
        runtimeSelectListProviderType: existingOptions.runtimeSelectListProviderType

      };
      this.selectList = await getSelectListItems(this.serverUrl, { options: options } as ActivityPropertyDescriptor);

      if (this.currentValue == undefined) {
        const defaultValue = this.propertyDescriptor.defaultValue;
        if(defaultValue) {
          this.currentValue = defaultValue;
        }
        else {
          const firstOption: any = this.selectList.items[0];

          if (firstOption) {
            const optionIsObject = typeof (firstOption) == 'object';
            this.currentValue = optionIsObject ? firstOption.value : firstOption.toString();
          }
        }
      }

    } else {
      this.selectList = await getSelectListItems(this.serverUrl, this.propertyDescriptor);

      if (this.currentValue == undefined) {
        const defaultValue = this.propertyDescriptor.defaultValue;
        if(defaultValue) {
          this.currentValue = defaultValue;
        }
        else {
          const firstOption: any = this.selectList.items[0];

          if (firstOption) {
            const optionIsObject = typeof (firstOption) == 'object';
            this.currentValue = optionIsObject ? firstOption.value : firstOption.toString();
          }
        }
      }
    }

    if (this.currentValue != undefined) {
      this.propertyModel.expressions[defaultSyntax] = this.currentValue;
    }
  }

  private reloadSelectListFromDeps = async (e: InputEvent) => {
    const depValues = {};
    const options = this.propertyDescriptor.options as RuntimeSelectListProviderSettings;

    for (const dependencyPropName of options.context.dependsOnValue) {
      const value = this.activityModel.properties.find((prop) => {
        return prop.name == dependencyPropName;
      });
      depValues[dependencyPropName] = value.expressions["Literal"];
    }

    // Need to get the latest value of the component that just changed.
    // For this we need to get the value from the event.
    const currentTarget = e.currentTarget as HTMLSelectElement;
    depValues[currentTarget.id] = currentTarget.value;

    let newOptions: RuntimeSelectListProviderSettings = {
      context: {
        ...options.context,
        depValues: depValues
      },
      runtimeSelectListProviderType: options.runtimeSelectListProviderType
    }

    this.selectList = await getSelectListItems(this.serverUrl, { options: newOptions } as ActivityPropertyDescriptor);

    
    
    let currentSelectList = await awaitElement('#' + this.propertyDescriptor.name);

    const defaultValue = this.propertyDescriptor.defaultValue;
    if(defaultValue) {
      this.currentValue = defaultValue;
    }
    else {
      const firstOption: any = this.selectList.items[0];
      if (firstOption) {
        const optionIsObject = typeof (firstOption) == 'object';
        this.currentValue = optionIsObject ? firstOption.value : firstOption.toString();
      }
    }

    // Dispatch change event so that dependent dropdown elements refresh.
    // Do this after the current component has re-rendered, otherwise the current value will be sent to the backend, which is outdated.
    requestAnimationFrame(() => {
      currentSelectList.dispatchEvent(new Event("change"));
    });
  }

  private onChange(e: Event) {
    const select = (e.target as HTMLSelectElement);
    const defaultSyntax = this.propertyDescriptor.defaultSyntax || SyntaxNames.Literal;
    this.propertyModel.expressions[defaultSyntax] = this.currentValue = select.value;
  }

  private onDefaultSyntaxValueChanged(e: CustomEvent) {
    this.currentValue = e.detail;
  }

  render() {
    const propertyDescriptor = this.propertyDescriptor;
    const propertyModel = this.propertyModel;
    const propertyName = propertyDescriptor.name;
    const fieldId = propertyName;
    const fieldName = propertyName;
    let currentValue = this.currentValue;
    const { items } = this.selectList;

    if (currentValue == undefined) {
      const defaultValue = this.propertyDescriptor.defaultValue;
      currentValue = defaultValue ? defaultValue.toString() : undefined;
    }

    return (
      <elsa-property-editor
        activityModel={this.activityModel}
        propertyDescriptor={propertyDescriptor}
        propertyModel={propertyModel}
        onDefaultSyntaxValueChanged={e => this.onDefaultSyntaxValueChanged(e)}
        single-line={true}>
        <select id={fieldId}
          name={fieldName}
          onChange={e => this.onChange(e)}
          class="elsa-mt-1 elsa-block focus:elsa-ring-blue-500 focus:elsa-border-blue-500 elsa-w-full elsa-shadow-sm sm:elsa-max-w-xs sm:elsa-text-sm elsa-border-gray-300 elsa-rounded-md">
          {items.map(item => {
            const optionIsObject = typeof (item) == 'object';
            const value = optionIsObject ? item.value : item.toString();
            const text = optionIsObject ? item.text : item.toString();
            return <option value={value} selected={value === currentValue}>{text}</option>;
          })}
        </select>
      </elsa-property-editor>
    );
  }
}

Tunnel.injectProps(ElsaDropdownProperty, ['serverUrl']);
