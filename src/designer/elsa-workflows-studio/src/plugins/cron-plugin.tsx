import {eventBus, ElsaPlugin} from "../services";
import {ActivityDesignDisplayContext, EventTypes, SyntaxNames} from "../models";
import {h} from "@stencil/core";
import {htmlEncode} from "../utils/utils";
import cronstrue from "cronstrue";
import { isValidCron } from 'cron-validator';

export class CronPlugin implements ElsaPlugin {
  constructor() {
    eventBus.on(EventTypes.ActivityDesignDisplaying, this.onActivityDesignDisplaying);
  }

  onActivityDesignDisplaying(context: ActivityDesignDisplayContext) {
    const activityModel = context.activityModel;
    if (activityModel.type !== 'Cron')
      return;

    const props = activityModel.properties || [];
    const condition = props.find(x => x.name == 'CronExpression') || { name: 'CronExpression', expressions: {'Literal': ''}, syntax: SyntaxNames.Literal};
    const expression = htmlEncode(condition.expressions[condition.syntax || 'Literal'] || '');
    const cronDescription = isValidCron(expression,  { seconds: true, allowBlankDay: true }) ? cronstrue.toString(expression) : '';
    context.bodyDisplay = `<p style="overflow: hidden;text-overflow: ellipsis;" title="${cronDescription}">${cronDescription}</p>`;
  }
}
