import {eventBus, ElsaPlugin} from "../services";
import {ActivityDesignDisplayContext, ActivityUpdatedContext, ActivityValidatingContext, EventTypes, SyntaxNames} from "../models";
import {htmlEncode} from "../utils/utils";
import Ajv from "ajv"

export class HttpEndpointPlugin implements ElsaPlugin {
  constructor() {
    eventBus.on(EventTypes.ActivityDesignDisplaying, this.onActivityDisplaying);
    eventBus.on(EventTypes.ActivityPluginUpdated, this.onActivityUpdated);
    eventBus.on(EventTypes.ActivityPluginValidating, this.onActivityValidating);
  }

  onActivityDisplaying(context: ActivityDesignDisplayContext) {
    const activityModel = context.activityModel;
    
    if (activityModel.type !== 'HttpEndpoint')
      return;
      
    const props = activityModel.properties || [];
    const path = props.find(x => x.name == 'Path') || { name: 'Path', expressions: { 'Literal': '', syntax: SyntaxNames.Literal } };
    const syntax = path.syntax || SyntaxNames.Literal;
    const bodyDisplay = htmlEncode(path.expressions[syntax]);
    context.bodyDisplay = `<p>${bodyDisplay}</p>`;
  }

  onActivityUpdated(context: ActivityUpdatedContext) {
    const activityModel = context.activityModel;
    if (activityModel.type !== 'HttpEndpoint')
      return;
      
    const props = activityModel.properties || [];
    const prop = props.find(x => x.name == 'Schema') || { name: 'Schema', expressions: { 'Literal': '', syntax: SyntaxNames.Literal } };
    prop.expressions[SyntaxNames.Literal] = context.data;
    eventBus.emit(EventTypes.UpdateActivity, this, prop);
  }

  onActivityValidating(context: ActivityValidatingContext) {
    
    if (context.activityType !== 'HttpEndpoint' || context.prop !== 'Schema')
      return;
    
    const jsonSchema = context.value;
    let isValid = true;
    
    if (jsonSchema == '') return;

    const ajv = new Ajv();
    let json: object;
    try{
      json = JSON.parse(jsonSchema)
    }
    catch (e){
      isValid = false;
    }

    if (json != undefined)
    {
      try {
        const validate = ajv.compile(json);
        const errors = validate.errors;
        if (errors != null)
          isValid = false;
      }
      catch (e){
        const err = e;
          isValid = false;
      }
    }

    context.isValidated = true;
    context.isValid = isValid;
    context.data = isValid ? 'Json is valid' : 'Json is invalid';
  }
}
