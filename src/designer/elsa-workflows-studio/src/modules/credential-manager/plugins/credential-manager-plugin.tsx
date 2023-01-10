import { ActivityDesignDisplayContext, ConfigureDashboardMenuContext, ElsaPlugin, eventBus, EventTypes, SyntaxNames } from "../../..";
import { htmlEncode } from "../../../utils/utils";

export class CredentialManagerPlugin implements ElsaPlugin {
  constructor() {
    eventBus.on(EventTypes.ActivityDesignDisplaying, this.onActivityDisplaying);
    eventBus.on(EventTypes.Dashboard.Appearing, this.onLoadingMenu);
  }

  onActivityDisplaying(context: ActivityDesignDisplayContext) {
    const activityModel = context.activityModel;

    if (!activityModel.type.endsWith('Manager'))
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

    const menuItems: any[] = [["credential-manager", "Credential Manager"]];
    const routes: any[] = [["credential-manager", "elsa-credential-manager-items-list", true]]

    context.data.menuItems = [...context.data.menuItems, ...menuItems];
    context.data.routes = [...context.data.routes, ...routes];
  }
}
