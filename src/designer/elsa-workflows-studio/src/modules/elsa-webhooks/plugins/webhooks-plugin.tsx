import {h} from '@stencil/core';
import {eventBus, ElsaPlugin} from "../../../services";
import {EventTypes, SyntaxNames, ActivityDesignDisplayContext, ConfigureDashboardMenuContext} from "../../../models";
import {htmlEncode} from "../../../utils/utils";

export class WebhooksPlugin implements ElsaPlugin {

  constructor() {
    eventBus.on(EventTypes.ActivityDesignDisplaying, this.onActivityDisplaying);
    eventBus.on(EventTypes.Dashboard.Appearing, this.onLoadingMenu);
  }

  onActivityDisplaying(context: ActivityDesignDisplayContext) {
    const activityModel = context.activityModel;

    if (!activityModel.type.endsWith('Webhook'))
      return;

    const props = activityModel.properties || [];
    const path = props.find(x => x.name == 'Path') || {
      name: 'Path',
      expressions: {'Literal': '', syntax: SyntaxNames.Literal}
    };
    const syntax = path.syntax || SyntaxNames.Literal;
    const bodyDisplay = htmlEncode(path.expressions[syntax]);
    context.bodyDisplay = `<p>${bodyDisplay}</p>`;
  }

  onLoadingMenu(context: ConfigureDashboardMenuContext) {

    const menuItems: any[] = [["webhook-definitions", "Webhook Definitions"]];
    const routes: any[] = [["webhook-definitions", "elsa-studio-webhook-definitions-list", true],
      ["webhook-definitions/:id", "elsa-studio-webhook-definitions-edit", false]];

    context.data.menuItems = [...context.data.menuItems, ...menuItems];
    context.data.routes = [...context.data.routes, ...routes];
  }
}
