import {Component, Host, h, Event, EventEmitter, Prop, State} from '@stencil/core';
import {FeatureConfig, featuresDataManager} from "../../../services";
import {LayoutDirection} from "../../designers/tree/elsa-designer-tree/models";
import {i18n} from "i18next";
import {resources} from "./localizations";
import {loadTranslations} from "../../i18n/i18n-loader";

@Component({
  tag: 'elsa-designer-panel',
  shadow: false,
})

export class ElsaDesignerPanel {

  @Prop() culture: string;
  @State() lastChangeTime: Date;
  @Event() featureChanged: EventEmitter<string>;
  @Event() featureStatusChanged: EventEmitter<string>;

  i18next: i18n;

  async componentWillLoad() {
    this.i18next = await loadTranslations(this.culture, resources);
  }

  t = (key: string, options?: any) => this.i18next.t(key, options);

  render() {
    const features = featuresDataManager.getUIFeatureList();
    const {t} = this;

    return (
      <Host>
        <div class="elsa-mt-4">
          {features.map(name => {
            const feature = featuresDataManager.getFeatureConfig(name);

            return (
              <div>
                <div class="elsa-relative elsa-flex elsa-items-start elsa-ml-1">
                  <div class="elsa-flex elsa-items-center elsa-h-5">
                    <input
                      id={name}
                      name={name}
                      type="checkbox"
                      value={`${feature.enabled}`}
                      checked={feature.enabled}
                      onChange={e => this.onToggleChange(e, name)}
                      class="focus:elsa-ring-blue-500 elsa-h-4 elsa-w-4 elsa-text-blue-600 elsa-border-gray-300 rounded"
                    />
                  </div>
                  <div class="elsa-ml-3 elsa-text-sm">
                    <label htmlFor={name} class="elsa-font-medium elsa-text-gray-700">{t(`${name}Name`)}</label>
                    <p class="elsa-text-gray-500">{t(`${name}Description`)}</p>
                  </div>
                </div>
                <div class="elsa-ml-1 elsa-my-4">
                  {this.renderFeatureData(name, feature)}
                </div>
              </div>
            )
          })}
        </div>
      </Host>
    );
  }

  renderFeatureData = (name: string, feature: FeatureConfig) => {
    if (!feature.enabled) {
      return null;
    }

    const {t} = this;
    switch (name) {
      case featuresDataManager.supportedFeatures.workflowLayout:
        return (
          <select id={name} name={name} onChange={e => this.onPropertyChange(e, name)} class="block focus:elsa-ring-blue-500 focus:elsa-border-blue-500 elsa-w-full elsa-shadow-sm sm:elsa-text-sm elsa-border-gray-300 elsa-rounded-md">
            {Object.keys(LayoutDirection).map(key => {
              return <option value={LayoutDirection[key]} selected={LayoutDirection[key] === feature.value}>{t(key)}</option>;
            })}
          </select>
        )
      default:
        return null;
    }
  }

  onToggleChange = (e: Event, name: string) => {
    const element = e.target as HTMLInputElement;
    featuresDataManager.setEnableStatus(name, element.checked);
    this.lastChangeTime = new Date();
    this.featureStatusChanged.emit(name);
  }

  onPropertyChange = (e: Event, name: string) => {
    const element = e.target as HTMLInputElement;
    featuresDataManager.setFeatureConfig(name, element.value.trim());
    this.featureChanged.emit(name);
  }
}
