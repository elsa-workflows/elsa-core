import {eventBus, ElsaPlugin} from "../services";
import {ActivityDesignDisplayContext, EventTypes, SyntaxNames} from "../models";
import {h} from "@stencil/core";
import {
  ActivityEditorAppearingEventArgs,
  ActivityEditorDisappearingEventArgs
} from "../components/screens/workflow-definition-editor/elsa-activity-editor-modal/elsa-activity-editor-modal";
import {parseJson} from "../utils/utils";

export class SendHttpRequestPlugin implements ElsaPlugin {
  constructor() {
    eventBus.on(EventTypes.ActivityEditor.Appearing, this.onActivityEditorAppearing);
    eventBus.on(EventTypes.ActivityEditor.Disappearing, this.onActivityEditorDisappearing);
  }

  onActivityEditorAppearing = (args: ActivityEditorAppearingEventArgs) => {
    if (args.activityDescriptor.type != 'SendHttpRequest')
      return;

    document.querySelector('#ReadContent').addEventListener('change', this.updateUI);
    document.querySelector('#ResponseContentParserName').addEventListener('change', this.updateUI);
    this.updateUI();
  };

  onActivityEditorDisappearing = (args: ActivityEditorDisappearingEventArgs) => {
    if (args.activityDescriptor.type != 'SendHttpRequest')
      return;

    document.querySelector('#ReadContent').removeEventListener('change', this.updateUI);
    document.querySelector('#ResponseContentParserName').removeEventListener('change', this.updateUI);
  };

  updateUI = () => {
    const readContentCheckbox = document.querySelector('#ReadContent') as HTMLInputElement;
    const parserList = document.querySelector('#ResponseContentParserName') as HTMLSelectElement;
    const responseContentParserListControl = document.querySelector('#ResponseContentParserNameControl') as HTMLElsaControlElement;
    const responseContentTargetTypeControl = document.querySelector('#ResponseContentTargetTypeControl') as HTMLElsaControlElement;
    const selectedParserName = parserList.value;

    responseContentParserListControl.classList.toggle('hidden', !readContentCheckbox.checked)
    responseContentTargetTypeControl.classList.toggle('hidden', (!readContentCheckbox.checked || selectedParserName != '.NET Type'));
  };
}
