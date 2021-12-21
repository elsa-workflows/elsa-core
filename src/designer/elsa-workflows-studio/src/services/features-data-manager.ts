import {ElsaStudio} from "../models/";

export interface FeatureConfig {
  enabled: boolean;
  ui: boolean;
  defaultValue: string;
  value?: string;
}

type FeatureConfigMap = {
  [key:string]: FeatureConfig;
}

export class FeaturesDataManager {

  elsaStudio: ElsaStudio;
  initialized: boolean;
  features: FeatureConfigMap;
  supportedFeatures = {
    workflowLayout: 'workflowLayout'
  }

  initialize(elsaStudio: ElsaStudio) {
    if (this.initialized)
      return;

    this.elsaStudio = elsaStudio;
    this.initialized = true;
    this.features = elsaStudio.features || {};
  }

  getFeatureList = () => {
    return Object.keys(this.features);
  }

  getUIFeatureList = () => {
    return Object.keys(this.features).filter(feature => this.features[feature].ui);
  }

  getFeatureConfig = (name: string) => {
    const value = localStorage.getItem(`elsa.properties.${name}`);
    const enabled = localStorage.getItem(`elsa.properties.${name}.enabled`);
    const feature = this.features[name];

    if (feature) {
      return {
        ...feature,
        value: value === null ? feature.defaultValue : value,
        'enabled': enabled === null ? feature.enabled : enabled === 'true'
      }
    }
  }

  setFeatureConfig = (name: string, value: string) => {
    const feature = this.features[name];

    if (feature) {
      localStorage.setItem(`elsa.properties.${name}`, value);
    }
  }

  setEnableStatus = (name: string, value: boolean) => {
    const feature = this.features[name];

    if (feature) {
      localStorage.setItem(`elsa.properties.${name}.enabled`, `${value}`);
      console.log(`elsa.properties.${name}-enabled`, value);
    }
  }

}

export const featuresDataManager = new FeaturesDataManager();
