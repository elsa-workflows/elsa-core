import {eventBus, ElsaPlugin} from "../services";
import {
  ActivityDesignDisplayContext,
  ActivityUpdatedContext,
  ActivityValidatingContext,
  EventTypes,
  ConfigureComponentCustomButtonContext,
  ComponentCustomButtonClickContext,
  SyntaxNames} from "../models";
import {htmlEncode} from "../utils/utils";
import Ajv from "ajv"
import {convert} from 'json-to-json-schema';

export class HttpEndpointPlugin implements ElsaPlugin {
  constructor() {
    eventBus.on(EventTypes.ActivityDesignDisplaying, this.onActivityDisplaying);
    eventBus.on(EventTypes.ActivityPluginUpdated, this.onActivityUpdated);
    eventBus.on(EventTypes.ActivityPluginValidating, this.onActivityValidating);
    eventBus.on(EventTypes.ComponentLoadingCustomButton, this.onComponentLoadingCustomButton);
    eventBus.on(EventTypes.ComponentCustomButtonClick, this.onComponentCustomButtonClick);
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

  onComponentLoadingCustomButton(context: ConfigureComponentCustomButtonContext) {
    if (context.activityType !== 'HttpEndpoint')
      return;

    if (context.component === 'elsa-script-property') {
      if (context.prop !== 'Schema')
        return;
      const label: string = 'Convert to Json Schema';
      context.data = {label};
    }

    if (context.component === 'elsa-workflow-definition-editor-screen') {
      const label: string = 'Use as Schema';
      context.data = {label};
    }
  }

  onComponentCustomButtonClick(context: ComponentCustomButtonClickContext) {
    if (context.activityType !== 'HttpEndpoint')
      return;

    if (context.component === 'elsa-script-property') {
      if (context.prop !== 'Schema')
        return;
      window.open('https://www.convertsimple.com/convert-json-to-json-schema/');
    }

    if (context.component === 'elsa-workflow-definition-editor-screen') {
      const activityUpdatedContext: ActivityUpdatedContext = {
        activityModel: context.params[0],
        data: JSON.stringify(convert(context.params[1]?.Body), null, 1)
      };
      eventBus.emit(EventTypes.ActivityPluginUpdated, this, activityUpdatedContext);
    }
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
