import {Component, Event, EventEmitter, h, Method, Prop, State, Watch} from '@stencil/core';
import {Container} from "typedi";
import {EventBus} from "../../../services";
import {WorkflowDefinition} from "../models/entities";
import {PropertiesTabModel, TabModel, Widget, WorkflowDefinitionPropsUpdatedArgs, WorkflowPropertiesEditorDisplayingArgs, WorkflowPropertiesEditorEventTypes, WorkflowPropertiesEditorModel} from "../models/ui";
import {FormEntry} from "../../../components/shared/forms/form-entry";
import {isNullOrWhitespace} from "../../../utils";
import {InfoList} from "../../../components/shared/forms/info-list";
import {TabChangedArgs, Variable, VersionedEntity} from "../../../models";
import {WorkflowDefinitionsApi} from "../services/api";

@Component({
  tag: 'elsa-widgets',
})
export class Widgets {
  @Prop() widgets: Array<Widget> = [];

  public render() {
    const widgets = this.widgets.sort((a, b) => a.order < b.order ? -1 : a.order > b.order ? 1 : 0);

    return <div>
      {widgets.map(widget => widget.content())}
    </div>
  }
}
