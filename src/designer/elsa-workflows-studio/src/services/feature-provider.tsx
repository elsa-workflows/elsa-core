import {ElsaStudio} from "../models";
import {eventBus} from './event-bus';
import {EventTypes, ConfigureFeatureContext} from "../models";

export class FeatureProvider {

    elsaStudio: ElsaStudio;
    initialized: boolean;
    features: Array<ConfigureFeatureContext> = [];

    initialize(elsaStudio: ElsaStudio) {
        if (this.initialized)
        return;

        this.elsaStudio = elsaStudio;
        this.configureFeatures(elsaStudio.featuresString, elsaStudio.serverUrl);
        this.initialized = true;
    }

    async configureFeatures(featuresString: string, serverUrl: string) {
        const parsedFeatures: string[] = featuresString.split(',');
    
        for (const featureName of parsedFeatures)
        {
          const featureContext: ConfigureFeatureContext = {
            featureName: featureName,
            component: null,
            data: null,
            params: null
          }
          this.features.push(featureContext);
        }
    }

    load(featureName: string, component: string) {

        let featureContext = this.features.find(x => x.featureName == featureName);

        if (!!featureContext)
        {
            featureContext.component = component;
            eventBus.emit(EventTypes.ConfigureFeature, this, featureContext);
            return featureContext;
        }
        return null;
    }
}

export const featureProvider = new FeatureProvider();