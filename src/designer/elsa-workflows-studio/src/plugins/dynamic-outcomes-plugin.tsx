import { eventBus, ElsaPlugin } from "../services";
import { ActivityDesignDisplayContext, EventTypes, SyntaxNames } from "../models";
import { parseJson } from "../utils/utils";

export class DynamicOutcomesPlugin implements ElsaPlugin {
    constructor() {
        eventBus.on(EventTypes.ActivityDesignDisplaying, this.onActivityDesignDisplaying);
    }

    onActivityDesignDisplaying = (context: ActivityDesignDisplayContext) => {
        const propValuesAsOutcomes = context.activityDescriptor.inputProperties.filter(prop => prop.considerValuesAsOutcomes);
        
        if (propValuesAsOutcomes.length > 0)
        {
            const props = context.activityModel.properties || [];
            const syntax = SyntaxNames.Json;
            let dynamicOutcomes = [];
            props
                .filter(prop => propValuesAsOutcomes.find(desc => desc.name == prop.name) != undefined)
                .forEach(prop => {
                    const expression = prop.expressions[syntax] || [];
                    const dynamicPropertyOutcomes = !!expression['$values'] ? expression['$values'] : Array.isArray(expression) ? expression : parseJson(expression) || [];
                    dynamicOutcomes = [...dynamicOutcomes, ...dynamicPropertyOutcomes];
                });
            context.outcomes = [...dynamicOutcomes, ...context.outcomes];
        }
    }
}