﻿import {ElsaPlugin} from "../services/elsa-plugin";
import {eventBus} from '../services/event-bus';
import {ActivityDesignDisplayContext, EventTypes} from "../models";
import {h} from "@stencil/core";

export class SendEmailPlugin implements ElsaPlugin {
  constructor() {
    eventBus.on(EventTypes.ActivityDesignDisplaying, this.onActivityDesignDisplaying);
  }

  onActivityDesignDisplaying(context: ActivityDesignDisplayContext) {
    const activityModel = context.activityModel;

    if (activityModel.type !== 'SendEmail')
      return;

    const props = activityModel.properties || [];
    const to = props.find(x => x.name == 'To') || { expressions: {'Literal': ''}, syntax: 'Literal' };
    const expression = to.expressions[to.syntax || 'Literal'] || '';
    const description = activityModel.description;
    const bodyText = description && description.length > 0 ? description : expression;
    context.bodyDisplay = <p>To: {bodyText}</p>;
  }
}
