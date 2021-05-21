import {ElsaPlugin} from "../services/elsa-plugin";
import {eventBus} from '../services/event-bus';
import {ActivityDesignDisplayContext, EventTypes, SyntaxNames} from "../models";
import {h} from "@stencil/core";

export class SignalReceivedPlugin implements ElsaPlugin {
  constructor() {
    eventBus.on(EventTypes.ActivityDesignDisplaying, this.onActivityDisplaying);
  }

  onActivityDisplaying(context: ActivityDesignDisplayContext) {
    const activityModel = context.activityModel;

    if (activityModel.type !== 'SignalReceived')
      return;

    const props = activityModel.properties || [];
    const signalName = props.find(x => x.name == 'Signal') || { name: 'Signal', expressions: { 'Literal': '', syntax: SyntaxNames.Literal } };
    const syntax = signalName.syntax || SyntaxNames.Literal;
    context.bodyDisplay = `<p>${signalName.expressions[syntax]}</p>`;
  }
}
