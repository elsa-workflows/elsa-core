/* tslint:disable */
/* auto-generated angular directive proxies */
import { ChangeDetectionStrategy, ChangeDetectorRef, Component, ElementRef, EventEmitter, NgZone } from '@angular/core';

import { ProxyCmp, proxyOutputs } from './angular-component-lib/utils';

import { Components } from '@elsa-workflows/elsa-workflows-designer';


@ProxyCmp({
  inputs: ['input'],
  methods: ['getInput']
})
@Component({
  selector: 'elsa-activity-input-editor-dialog-content',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: '<ng-content></ng-content>',
  // eslint-disable-next-line @angular-eslint/no-inputs-metadata-property
  inputs: ['input'],
})
export class ElsaActivityInputEditorDialogContent {
  protected el: HTMLElement;
  constructor(c: ChangeDetectorRef, r: ElementRef, protected z: NgZone) {
    c.detach();
    this.el = r.nativeElement;
    proxyOutputs(this, this.el, ['inputChanged']);
  }
}


import type { InputDefinition as IElsaActivityInputEditorDialogContentInputDefinition } from '@elsa-workflows/elsa-workflows-designer';

export declare interface ElsaActivityInputEditorDialogContent extends Components.ElsaActivityInputEditorDialogContent {

  inputChanged: EventEmitter<CustomEvent<IElsaActivityInputEditorDialogContentInputDefinition>>;
}


@ProxyCmp({
  inputs: ['output'],
  methods: ['getOutput']
})
@Component({
  selector: 'elsa-activity-output-editor-dialog-content',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: '<ng-content></ng-content>',
  // eslint-disable-next-line @angular-eslint/no-inputs-metadata-property
  inputs: ['output'],
})
export class ElsaActivityOutputEditorDialogContent {
  protected el: HTMLElement;
  constructor(c: ChangeDetectorRef, r: ElementRef, protected z: NgZone) {
    c.detach();
    this.el = r.nativeElement;
    proxyOutputs(this, this.el, ['outputChanged']);
  }
}


import type { OutputDefinition as IElsaActivityOutputEditorDialogContentOutputDefinition } from '@elsa-workflows/elsa-workflows-designer';

export declare interface ElsaActivityOutputEditorDialogContent extends Components.ElsaActivityOutputEditorDialogContent {

  outputChanged: EventEmitter<CustomEvent<IElsaActivityOutputEditorDialogContentOutputDefinition>>;
}


@ProxyCmp({
  inputs: ['activity', 'activityExecutionLog', 'activityPropertyTabIndex'],
  methods: ['show', 'hide', 'updateSelectedTab']
})
@Component({
  selector: 'elsa-activity-properties',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: '<ng-content></ng-content>',
  // eslint-disable-next-line @angular-eslint/no-inputs-metadata-property
  inputs: ['activity', 'activityExecutionLog', 'activityPropertyTabIndex'],
})
export class ElsaActivityProperties {
  protected el: HTMLElement;
  constructor(c: ChangeDetectorRef, r: ElementRef, protected z: NgZone) {
    c.detach();
    this.el = r.nativeElement;
  }
}


export declare interface ElsaActivityProperties extends Components.ElsaActivityProperties {}


@ProxyCmp({
  inputs: ['activity', 'outputs', 'variables', 'workflowDefinitionId'],
  methods: ['show', 'hide']
})
@Component({
  selector: 'elsa-activity-properties-editor',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: '<ng-content></ng-content>',
  // eslint-disable-next-line @angular-eslint/no-inputs-metadata-property
  inputs: ['activity', 'outputs', 'variables', 'workflowDefinitionId'],
})
export class ElsaActivityPropertiesEditor {
  protected el: HTMLElement;
  constructor(c: ChangeDetectorRef, r: ElementRef, protected z: NgZone) {
    c.detach();
    this.el = r.nativeElement;
    proxyOutputs(this, this.el, ['activityUpdated', 'deleteActivityRequested']);
  }
}


import type { ActivityUpdatedArgs as IElsaActivityPropertiesEditorActivityUpdatedArgs } from '@elsa-workflows/elsa-workflows-designer';
import type { DeleteActivityRequestedArgs as IElsaActivityPropertiesEditorDeleteActivityRequestedArgs } from '@elsa-workflows/elsa-workflows-designer';

export declare interface ElsaActivityPropertiesEditor extends Components.ElsaActivityPropertiesEditor {

  activityUpdated: EventEmitter<CustomEvent<IElsaActivityPropertiesEditorActivityUpdatedArgs>>;

  deleteActivityRequested: EventEmitter<CustomEvent<IElsaActivityPropertiesEditorDeleteActivityRequestedArgs>>;
}


@ProxyCmp({
  inputs: ['buttons']
})
@Component({
  selector: 'elsa-button-group',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: '<ng-content></ng-content>',
  // eslint-disable-next-line @angular-eslint/no-inputs-metadata-property
  inputs: ['buttons'],
})
export class ElsaButtonGroup {
  protected el: HTMLElement;
  constructor(c: ChangeDetectorRef, r: ElementRef, protected z: NgZone) {
    c.detach();
    this.el = r.nativeElement;
  }
}


export declare interface ElsaButtonGroup extends Components.ElsaButtonGroup {}


@ProxyCmp({
  inputs: ['inputContext']
})
@Component({
  selector: 'elsa-check-list-input',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: '<ng-content></ng-content>',
  // eslint-disable-next-line @angular-eslint/no-inputs-metadata-property
  inputs: ['inputContext'],
})
export class ElsaCheckListInput {
  protected el: HTMLElement;
  constructor(c: ChangeDetectorRef, r: ElementRef, protected z: NgZone) {
    c.detach();
    this.el = r.nativeElement;
  }
}


export declare interface ElsaCheckListInput extends Components.ElsaCheckListInput {}


@ProxyCmp({
  inputs: ['inputContext']
})
@Component({
  selector: 'elsa-checkbox-input',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: '<ng-content></ng-content>',
  // eslint-disable-next-line @angular-eslint/no-inputs-metadata-property
  inputs: ['inputContext'],
})
export class ElsaCheckboxInput {
  protected el: HTMLElement;
  constructor(c: ChangeDetectorRef, r: ElementRef, protected z: NgZone) {
    c.detach();
    this.el = r.nativeElement;
  }
}


export declare interface ElsaCheckboxInput extends Components.ElsaCheckboxInput {}


@ProxyCmp({
  inputs: ['inputContext']
})
@Component({
  selector: 'elsa-code-editor-input',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: '<ng-content></ng-content>',
  // eslint-disable-next-line @angular-eslint/no-inputs-metadata-property
  inputs: ['inputContext'],
})
export class ElsaCodeEditorInput {
  protected el: HTMLElement;
  constructor(c: ChangeDetectorRef, r: ElementRef, protected z: NgZone) {
    c.detach();
    this.el = r.nativeElement;
  }
}


export declare interface ElsaCodeEditorInput extends Components.ElsaCodeEditorInput {}


@ProxyCmp({
  inputs: ['anchorPoint', 'hideButton', 'menuItems'],
  methods: ['open', 'close']
})
@Component({
  selector: 'elsa-context-menu',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: '<ng-content></ng-content>',
  // eslint-disable-next-line @angular-eslint/no-inputs-metadata-property
  inputs: ['anchorPoint', 'hideButton', 'menuItems'],
})
export class ElsaContextMenu {
  protected el: HTMLElement;
  constructor(c: ChangeDetectorRef, r: ElementRef, protected z: NgZone) {
    c.detach();
    this.el = r.nativeElement;
  }
}


export declare interface ElsaContextMenu extends Components.ElsaContextMenu {}


@ProxyCmp({
  inputs: ['value']
})
@Component({
  selector: 'elsa-copy-button',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: '<ng-content></ng-content>',
  // eslint-disable-next-line @angular-eslint/no-inputs-metadata-property
  inputs: ['value'],
})
export class ElsaCopyButton {
  protected el: HTMLElement;
  constructor(c: ChangeDetectorRef, r: ElementRef, protected z: NgZone) {
    c.detach();
    this.el = r.nativeElement;
  }
}


export declare interface ElsaCopyButton extends Components.ElsaCopyButton {}


@ProxyCmp({
  inputs: ['activityId', 'activityType', 'activityTypeVersion', 'displayType']
})
@Component({
  selector: 'elsa-default-activity-template',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: '<ng-content></ng-content>',
  // eslint-disable-next-line @angular-eslint/no-inputs-metadata-property
  inputs: ['activityId', 'activityType', 'activityTypeVersion', 'displayType'],
})
export class ElsaDefaultActivityTemplate {
  protected el: HTMLElement;
  constructor(c: ChangeDetectorRef, r: ElementRef, protected z: NgZone) {
    c.detach();
    this.el = r.nativeElement;
    proxyOutputs(this, this.el, ['editChildActivity', 'childActivitySelected']);
  }
}


import type { EditChildActivityArgs as IElsaDefaultActivityTemplateEditChildActivityArgs } from '@elsa-workflows/elsa-workflows-designer';
import type { ChildActivitySelectedArgs as IElsaDefaultActivityTemplateChildActivitySelectedArgs } from '@elsa-workflows/elsa-workflows-designer';

export declare interface ElsaDefaultActivityTemplate extends Components.ElsaDefaultActivityTemplate {

  editChildActivity: EventEmitter<CustomEvent<IElsaDefaultActivityTemplateEditChildActivityArgs>>;

  childActivitySelected: EventEmitter<CustomEvent<IElsaDefaultActivityTemplateChildActivitySelectedArgs>>;
}


@ProxyCmp({
  inputs: ['handler', 'icon', 'items', 'origin', 'text', 'theme']
})
@Component({
  selector: 'elsa-dropdown-button',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: '<ng-content></ng-content>',
  // eslint-disable-next-line @angular-eslint/no-inputs-metadata-property
  inputs: ['handler', 'icon', 'items', 'origin', 'text', 'theme'],
})
export class ElsaDropdownButton {
  protected el: HTMLElement;
  constructor(c: ChangeDetectorRef, r: ElementRef, protected z: NgZone) {
    c.detach();
    this.el = r.nativeElement;
    proxyOutputs(this, this.el, ['itemSelected']);
  }
}


import type { DropdownButtonItem as IElsaDropdownButtonDropdownButtonItem } from '@elsa-workflows/elsa-workflows-designer';

export declare interface ElsaDropdownButton extends Components.ElsaDropdownButton {

  itemSelected: EventEmitter<CustomEvent<IElsaDropdownButtonDropdownButtonItem>>;
}


@ProxyCmp({
  inputs: ['inputContext']
})
@Component({
  selector: 'elsa-dropdown-input',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: '<ng-content></ng-content>',
  // eslint-disable-next-line @angular-eslint/no-inputs-metadata-property
  inputs: ['inputContext'],
})
export class ElsaDropdownInput {
  protected el: HTMLElement;
  constructor(c: ChangeDetectorRef, r: ElementRef, protected z: NgZone) {
    c.detach();
    this.el = r.nativeElement;
  }
}


export declare interface ElsaDropdownInput extends Components.ElsaDropdownInput {}


@ProxyCmp({
  inputs: ['inputContext']
})
@Component({
  selector: 'elsa-flow-switch-editor',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: '<ng-content></ng-content>',
  // eslint-disable-next-line @angular-eslint/no-inputs-metadata-property
  inputs: ['inputContext'],
})
export class ElsaFlowSwitchEditor {
  protected el: HTMLElement;
  constructor(c: ChangeDetectorRef, r: ElementRef, protected z: NgZone) {
    c.detach();
    this.el = r.nativeElement;
  }
}


export declare interface ElsaFlowSwitchEditor extends Components.ElsaFlowSwitchEditor {}


@ProxyCmp({
  inputs: ['interactiveMode', 'rootActivity', 'silent'],
  methods: ['newRoot', 'updateGraph', 'getGraph', 'reset', 'updateLayout', 'zoomToFit', 'scrollToStart', 'autoLayout', 'addActivity', 'updateActivity', 'getActivity', 'renameActivity', 'export']
})
@Component({
  selector: 'elsa-flowchart',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: '<ng-content></ng-content>',
  // eslint-disable-next-line @angular-eslint/no-inputs-metadata-property
  inputs: ['interactiveMode', 'rootActivity', 'silent'],
})
export class ElsaFlowchart {
  protected el: HTMLElement;
  constructor(c: ChangeDetectorRef, r: ElementRef, protected z: NgZone) {
    c.detach();
    this.el = r.nativeElement;
    proxyOutputs(this, this.el, ['activitySelected', 'activityDeleted', 'containerSelected', 'childActivitySelected', 'graphUpdated', 'workflowUpdated']);
  }
}


import type { ActivitySelectedArgs as IElsaFlowchartActivitySelectedArgs } from '@elsa-workflows/elsa-workflows-designer';
import type { ActivityDeletedArgs as IElsaFlowchartActivityDeletedArgs } from '@elsa-workflows/elsa-workflows-designer';
import type { ContainerSelectedArgs as IElsaFlowchartContainerSelectedArgs } from '@elsa-workflows/elsa-workflows-designer';
import type { ChildActivitySelectedArgs as IElsaFlowchartChildActivitySelectedArgs } from '@elsa-workflows/elsa-workflows-designer';
import type { GraphUpdatedArgs as IElsaFlowchartGraphUpdatedArgs } from '@elsa-workflows/elsa-workflows-designer';
import type { WorkflowUpdatedArgs as IElsaFlowchartWorkflowUpdatedArgs } from '@elsa-workflows/elsa-workflows-designer';

export declare interface ElsaFlowchart extends Components.ElsaFlowchart {

  activitySelected: EventEmitter<CustomEvent<IElsaFlowchartActivitySelectedArgs>>;

  activityDeleted: EventEmitter<CustomEvent<IElsaFlowchartActivityDeletedArgs>>;

  containerSelected: EventEmitter<CustomEvent<IElsaFlowchartContainerSelectedArgs>>;

  childActivitySelected: EventEmitter<CustomEvent<IElsaFlowchartChildActivitySelectedArgs>>;

  graphUpdated: EventEmitter<CustomEvent<IElsaFlowchartGraphUpdatedArgs>>;

  workflowUpdated: EventEmitter<CustomEvent<IElsaFlowchartWorkflowUpdatedArgs>>;
}


@ProxyCmp({
  inputs: ['actions', 'mainTitle', 'orientation', 'selectedTabIndex', 'subTitle', 'tabs']
})
@Component({
  selector: 'elsa-form-panel',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: '<ng-content></ng-content>',
  // eslint-disable-next-line @angular-eslint/no-inputs-metadata-property
  inputs: ['actions', 'mainTitle', 'orientation', 'selectedTabIndex', 'subTitle', 'tabs'],
})
export class ElsaFormPanel {
  protected el: HTMLElement;
  constructor(c: ChangeDetectorRef, r: ElementRef, protected z: NgZone) {
    c.detach();
    this.el = r.nativeElement;
    proxyOutputs(this, this.el, ['submitted', 'selectedTabIndexChanged', 'actionInvoked']);
  }
}


import type { TabChangedArgs as IElsaFormPanelTabChangedArgs } from '@elsa-workflows/elsa-workflows-designer';
import type { PanelActionClickArgs as IElsaFormPanelPanelActionClickArgs } from '@elsa-workflows/elsa-workflows-designer';

export declare interface ElsaFormPanel extends Components.ElsaFormPanel {

  submitted: EventEmitter<CustomEvent<FormData>>;

  selectedTabIndexChanged: EventEmitter<CustomEvent<IElsaFormPanelTabChangedArgs>>;

  actionInvoked: EventEmitter<CustomEvent<IElsaFormPanelPanelActionClickArgs>>;
}


@ProxyCmp({
})
@Component({
  selector: 'elsa-home-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: '<ng-content></ng-content>',
  // eslint-disable-next-line @angular-eslint/no-inputs-metadata-property
  inputs: [],
})
export class ElsaHomePage {
  protected el: HTMLElement;
  constructor(c: ChangeDetectorRef, r: ElementRef, protected z: NgZone) {
    c.detach();
    this.el = r.nativeElement;
  }
}


export declare interface ElsaHomePage extends Components.ElsaHomePage {}


@ProxyCmp({
  inputs: ['inputContext']
})
@Component({
  selector: 'elsa-http-status-codes-editor',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: '<ng-content></ng-content>',
  // eslint-disable-next-line @angular-eslint/no-inputs-metadata-property
  inputs: ['inputContext'],
})
export class ElsaHttpStatusCodesEditor {
  protected el: HTMLElement;
  constructor(c: ChangeDetectorRef, r: ElementRef, protected z: NgZone) {
    c.detach();
    this.el = r.nativeElement;
  }
}


export declare interface ElsaHttpStatusCodesEditor extends Components.ElsaHttpStatusCodesEditor {}


@ProxyCmp({
  inputs: ['activityType', 'codeEditorHeight', 'codeEditorSingleLineMode', 'context', 'defaultSyntax', 'expression', 'fieldName', 'hideLabel', 'hint', 'isReadOnly', 'label', 'propertyName', 'supportedSyntaxes', 'syntax', 'workflowDefinitionId']
})
@Component({
  selector: 'elsa-input-control-switch',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: '<ng-content></ng-content>',
  // eslint-disable-next-line @angular-eslint/no-inputs-metadata-property
  inputs: ['activityType', 'codeEditorHeight', 'codeEditorSingleLineMode', 'context', 'defaultSyntax', 'expression', 'fieldName', 'hideLabel', 'hint', 'isReadOnly', 'label', 'propertyName', 'supportedSyntaxes', 'syntax', 'workflowDefinitionId'],
})
export class ElsaInputControlSwitch {
  protected el: HTMLElement;
  constructor(c: ChangeDetectorRef, r: ElementRef, protected z: NgZone) {
    c.detach();
    this.el = r.nativeElement;
    proxyOutputs(this, this.el, ['syntaxChanged', 'expressionChanged']);
  }
}


import type { ExpressionChangedArs as IElsaInputControlSwitchExpressionChangedArs } from '@elsa-workflows/elsa-workflows-designer';

export declare interface ElsaInputControlSwitch extends Components.ElsaInputControlSwitch {

  syntaxChanged: EventEmitter<CustomEvent<string>>;

  expressionChanged: EventEmitter<CustomEvent<IElsaInputControlSwitchExpressionChangedArs>>;
}


@ProxyCmp({
  inputs: ['fieldId', 'placeHolder', 'values']
})
@Component({
  selector: 'elsa-input-tags',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: '<ng-content></ng-content>',
  // eslint-disable-next-line @angular-eslint/no-inputs-metadata-property
  inputs: ['fieldId', 'placeHolder', 'values'],
})
export class ElsaInputTags {
  protected el: HTMLElement;
  constructor(c: ChangeDetectorRef, r: ElementRef, protected z: NgZone) {
    c.detach();
    this.el = r.nativeElement;
    proxyOutputs(this, this.el, ['valueChanged']);
  }
}


export declare interface ElsaInputTags extends Components.ElsaInputTags {

  valueChanged: EventEmitter<CustomEvent<Array<string>>>;
}


@ProxyCmp({
  inputs: ['dropdownValues', 'fieldId', 'fieldName', 'placeHolder', 'values']
})
@Component({
  selector: 'elsa-input-tags-dropdown',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: '<ng-content></ng-content>',
  // eslint-disable-next-line @angular-eslint/no-inputs-metadata-property
  inputs: ['dropdownValues', 'fieldId', 'fieldName', 'placeHolder', 'values'],
})
export class ElsaInputTagsDropdown {
  protected el: HTMLElement;
  constructor(c: ChangeDetectorRef, r: ElementRef, protected z: NgZone) {
    c.detach();
    this.el = r.nativeElement;
    proxyOutputs(this, this.el, ['valueChanged']);
  }
}


import type { SelectListItem as IElsaInputTagsDropdownSelectListItem } from '@elsa-workflows/elsa-workflows-designer';

export declare interface ElsaInputTagsDropdown extends Components.ElsaInputTagsDropdown {

  valueChanged: EventEmitter<CustomEvent<IElsaInputTagsDropdownSelectListItem[]| string[]>>;
}


@ProxyCmp({
  inputs: ['buttonClass', 'containerClass', 'selectedLabels']
})
@Component({
  selector: 'elsa-label-picker',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: '<ng-content></ng-content>',
  // eslint-disable-next-line @angular-eslint/no-inputs-metadata-property
  inputs: ['buttonClass', 'containerClass', 'selectedLabels'],
})
export class ElsaLabelPicker {
  protected el: HTMLElement;
  constructor(c: ChangeDetectorRef, r: ElementRef, protected z: NgZone) {
    c.detach();
    this.el = r.nativeElement;
    proxyOutputs(this, this.el, ['selectedLabelsChanged']);
  }
}


export declare interface ElsaLabelPicker extends Components.ElsaLabelPicker {

  selectedLabelsChanged: EventEmitter<CustomEvent<Array<string>>>;
}


@ProxyCmp({
})
@Component({
  selector: 'elsa-login-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: '<ng-content></ng-content>',
  // eslint-disable-next-line @angular-eslint/no-inputs-metadata-property
  inputs: [],
})
export class ElsaLoginPage {
  protected el: HTMLElement;
  constructor(c: ChangeDetectorRef, r: ElementRef, protected z: NgZone) {
    c.detach();
    this.el = r.nativeElement;
    proxyOutputs(this, this.el, ['signedIn']);
  }
}


import type { SignedInArgs as IElsaLoginPageSignedInArgs } from '@elsa-workflows/elsa-workflows-designer';

export declare interface ElsaLoginPage extends Components.ElsaLoginPage {

  signedIn: EventEmitter<CustomEvent<IElsaLoginPageSignedInArgs>>;
}


@ProxyCmp({
  inputs: ['actions', 'autoHide', 'content', 'modalDialogInstance', 'size', 'type'],
  methods: ['show', 'hide']
})
@Component({
  selector: 'elsa-modal-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: '<ng-content></ng-content>',
  // eslint-disable-next-line @angular-eslint/no-inputs-metadata-property
  inputs: ['actions', 'autoHide', 'content', 'modalDialogInstance', 'size', 'type'],
})
export class ElsaModalDialog {
  protected el: HTMLElement;
  constructor(c: ChangeDetectorRef, r: ElementRef, protected z: NgZone) {
    c.detach();
    this.el = r.nativeElement;
    proxyOutputs(this, this.el, ['shown', 'hidden', 'actionInvoked']);
  }
}


import type { ModalActionClickArgs as IElsaModalDialogModalActionClickArgs } from '@elsa-workflows/elsa-workflows-designer';

export declare interface ElsaModalDialog extends Components.ElsaModalDialog {

  shown: EventEmitter<CustomEvent<any>>;

  hidden: EventEmitter<CustomEvent<any>>;

  actionInvoked: EventEmitter<CustomEvent<IElsaModalDialogModalActionClickArgs>>;
}


@ProxyCmp({
})
@Component({
  selector: 'elsa-modal-dialog-container',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: '<ng-content></ng-content>',
  // eslint-disable-next-line @angular-eslint/no-inputs-metadata-property
  inputs: [],
})
export class ElsaModalDialogContainer {
  protected el: HTMLElement;
  constructor(c: ChangeDetectorRef, r: ElementRef, protected z: NgZone) {
    c.detach();
    this.el = r.nativeElement;
  }
}


export declare interface ElsaModalDialogContainer extends Components.ElsaModalDialogContainer {}


@ProxyCmp({
  inputs: ['editorHeight', 'language', 'padding', 'singleLineMode', 'value'],
  methods: ['setValue', 'addJavaScriptLib']
})
@Component({
  selector: 'elsa-monaco-editor',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: '<ng-content></ng-content>',
  // eslint-disable-next-line @angular-eslint/no-inputs-metadata-property
  inputs: ['editorHeight', 'language', 'padding', 'singleLineMode', 'value'],
})
export class ElsaMonacoEditor {
  protected el: HTMLElement;
  constructor(c: ChangeDetectorRef, r: ElementRef, protected z: NgZone) {
    c.detach();
    this.el = r.nativeElement;
    proxyOutputs(this, this.el, ['valueChanged']);
  }
}


import type { MonacoValueChangedArgs as IElsaMonacoEditorMonacoValueChangedArgs } from '@elsa-workflows/elsa-workflows-designer';

export declare interface ElsaMonacoEditor extends Components.ElsaMonacoEditor {

  valueChanged: EventEmitter<CustomEvent<IElsaMonacoEditorMonacoValueChangedArgs>>;
}


@ProxyCmp({
  inputs: ['inputContext']
})
@Component({
  selector: 'elsa-multi-line-input',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: '<ng-content></ng-content>',
  // eslint-disable-next-line @angular-eslint/no-inputs-metadata-property
  inputs: ['inputContext'],
})
export class ElsaMultiLineInput {
  protected el: HTMLElement;
  constructor(c: ChangeDetectorRef, r: ElementRef, protected z: NgZone) {
    c.detach();
    this.el = r.nativeElement;
  }
}


export declare interface ElsaMultiLineInput extends Components.ElsaMultiLineInput {}


@ProxyCmp({
  inputs: ['inputContext']
})
@Component({
  selector: 'elsa-multi-text-input',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: '<ng-content></ng-content>',
  // eslint-disable-next-line @angular-eslint/no-inputs-metadata-property
  inputs: ['inputContext'],
})
export class ElsaMultiTextInput {
  protected el: HTMLElement;
  constructor(c: ChangeDetectorRef, r: ElementRef, protected z: NgZone) {
    c.detach();
    this.el = r.nativeElement;
  }
}


export declare interface ElsaMultiTextInput extends Components.ElsaMultiTextInput {}


@ProxyCmp({
  inputs: ['notification']
})
@Component({
  selector: 'elsa-notification-template',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: '<ng-content></ng-content>',
  // eslint-disable-next-line @angular-eslint/no-inputs-metadata-property
  inputs: ['notification'],
})
export class ElsaNotificationTemplate {
  protected el: HTMLElement;
  constructor(c: ChangeDetectorRef, r: ElementRef, protected z: NgZone) {
    c.detach();
    this.el = r.nativeElement;
  }
}


export declare interface ElsaNotificationTemplate extends Components.ElsaNotificationTemplate {}


@ProxyCmp({
  inputs: ['modalState']
})
@Component({
  selector: 'elsa-notifications-manager',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: '<ng-content></ng-content>',
  // eslint-disable-next-line @angular-eslint/no-inputs-metadata-property
  inputs: ['modalState'],
})
export class ElsaNotificationsManager {
  protected el: HTMLElement;
  constructor(c: ChangeDetectorRef, r: ElementRef, protected z: NgZone) {
    c.detach();
    this.el = r.nativeElement;
  }
}


export declare interface ElsaNotificationsManager extends Components.ElsaNotificationsManager {}


@ProxyCmp({
  inputs: ['inputContext']
})
@Component({
  selector: 'elsa-outcome-picker-input',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: '<ng-content></ng-content>',
  // eslint-disable-next-line @angular-eslint/no-inputs-metadata-property
  inputs: ['inputContext'],
})
export class ElsaOutcomePickerInput {
  protected el: HTMLElement;
  constructor(c: ChangeDetectorRef, r: ElementRef, protected z: NgZone) {
    c.detach();
    this.el = r.nativeElement;
  }
}


export declare interface ElsaOutcomePickerInput extends Components.ElsaOutcomePickerInput {}


@ProxyCmp({
  inputs: ['inputContext']
})
@Component({
  selector: 'elsa-output-picker-input',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: '<ng-content></ng-content>',
  // eslint-disable-next-line @angular-eslint/no-inputs-metadata-property
  inputs: ['inputContext'],
})
export class ElsaOutputPickerInput {
  protected el: HTMLElement;
  constructor(c: ChangeDetectorRef, r: ElementRef, protected z: NgZone) {
    c.detach();
    this.el = r.nativeElement;
  }
}


export declare interface ElsaOutputPickerInput extends Components.ElsaOutputPickerInput {}


@ProxyCmp({
  inputs: ['page', 'pageSize', 'totalCount']
})
@Component({
  selector: 'elsa-pager',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: '<ng-content></ng-content>',
  // eslint-disable-next-line @angular-eslint/no-inputs-metadata-property
  inputs: ['page', 'pageSize', 'totalCount'],
})
export class ElsaPager {
  protected el: HTMLElement;
  constructor(c: ChangeDetectorRef, r: ElementRef, protected z: NgZone) {
    c.detach();
    this.el = r.nativeElement;
    proxyOutputs(this, this.el, ['paginated']);
  }
}


import type { PagerData as IElsaPagerPagerData } from '@elsa-workflows/elsa-workflows-designer';

export declare interface ElsaPager extends Components.ElsaPager {

  paginated: EventEmitter<CustomEvent<IElsaPagerPagerData>>;
}


@ProxyCmp({
  inputs: ['position']
})
@Component({
  selector: 'elsa-panel',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: '<ng-content></ng-content>',
  // eslint-disable-next-line @angular-eslint/no-inputs-metadata-property
  inputs: ['position'],
})
export class ElsaPanel {
  protected el: HTMLElement;
  constructor(c: ChangeDetectorRef, r: ElementRef, protected z: NgZone) {
    c.detach();
    this.el = r.nativeElement;
    proxyOutputs(this, this.el, ['expandedStateChanged']);
  }
}


import type { PanelStateChangedArgs as IElsaPanelPanelStateChangedArgs } from '@elsa-workflows/elsa-workflows-designer';

export declare interface ElsaPanel extends Components.ElsaPanel {

  expandedStateChanged: EventEmitter<CustomEvent<IElsaPanelPanelStateChangedArgs>>;
}


@ProxyCmp({
  inputs: ['inputContext']
})
@Component({
  selector: 'elsa-radio-list-input',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: '<ng-content></ng-content>',
  // eslint-disable-next-line @angular-eslint/no-inputs-metadata-property
  inputs: ['inputContext'],
})
export class ElsaRadioListInput {
  protected el: HTMLElement;
  constructor(c: ChangeDetectorRef, r: ElementRef, protected z: NgZone) {
    c.detach();
    this.el = r.nativeElement;
  }
}


export declare interface ElsaRadioListInput extends Components.ElsaRadioListInput {}


@ProxyCmp({
})
@Component({
  selector: 'elsa-shell',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: '<ng-content></ng-content>',
  // eslint-disable-next-line @angular-eslint/no-inputs-metadata-property
  inputs: [],
})
export class ElsaShell {
  protected el: HTMLElement;
  constructor(c: ChangeDetectorRef, r: ElementRef, protected z: NgZone) {
    c.detach();
    this.el = r.nativeElement;
  }
}


export declare interface ElsaShell extends Components.ElsaShell {}


@ProxyCmp({
  inputs: ['inputContext']
})
@Component({
  selector: 'elsa-single-line-input',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: '<ng-content></ng-content>',
  // eslint-disable-next-line @angular-eslint/no-inputs-metadata-property
  inputs: ['inputContext'],
})
export class ElsaSingleLineInput {
  protected el: HTMLElement;
  constructor(c: ChangeDetectorRef, r: ElementRef, protected z: NgZone) {
    c.detach();
    this.el = r.nativeElement;
  }
}


export declare interface ElsaSingleLineInput extends Components.ElsaSingleLineInput {}


@ProxyCmp({
  inputs: ['actions', 'expand', 'headerText', 'selectedTab', 'tabs'],
  methods: ['show', 'hide']
})
@Component({
  selector: 'elsa-slide-over-panel',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: '<ng-content></ng-content>',
  // eslint-disable-next-line @angular-eslint/no-inputs-metadata-property
  inputs: ['actions', 'expand', 'headerText', 'selectedTab', 'tabs'],
})
export class ElsaSlideOverPanel {
  protected el: HTMLElement;
  constructor(c: ChangeDetectorRef, r: ElementRef, protected z: NgZone) {
    c.detach();
    this.el = r.nativeElement;
    proxyOutputs(this, this.el, ['collapsed']);
  }
}


export declare interface ElsaSlideOverPanel extends Components.ElsaSlideOverPanel {

  collapsed: EventEmitter<CustomEvent<any>>;
}


@ProxyCmp({
  inputs: ['enableFlexiblePorts', 'monacoLibPath', 'serverUrl']
})
@Component({
  selector: 'elsa-studio',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: '<ng-content></ng-content>',
  // eslint-disable-next-line @angular-eslint/no-inputs-metadata-property
  inputs: ['enableFlexiblePorts', 'monacoLibPath', 'serverUrl'],
})
export class ElsaStudio {
  protected el: HTMLElement;
  constructor(c: ChangeDetectorRef, r: ElementRef, protected z: NgZone) {
    c.detach();
    this.el = r.nativeElement;
  }
}


export declare interface ElsaStudio extends Components.ElsaStudio {}


@ProxyCmp({
  inputs: ['inputContext']
})
@Component({
  selector: 'elsa-switch-editor',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: '<ng-content></ng-content>',
  // eslint-disable-next-line @angular-eslint/no-inputs-metadata-property
  inputs: ['inputContext'],
})
export class ElsaSwitchEditor {
  protected el: HTMLElement;
  constructor(c: ChangeDetectorRef, r: ElementRef, protected z: NgZone) {
    c.detach();
    this.el = r.nativeElement;
  }
}


export declare interface ElsaSwitchEditor extends Components.ElsaSwitchEditor {}


@ProxyCmp({
})
@Component({
  selector: 'elsa-toast-manager',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: '<ng-content></ng-content>',
  // eslint-disable-next-line @angular-eslint/no-inputs-metadata-property
  inputs: [],
})
export class ElsaToastManager {
  protected el: HTMLElement;
  constructor(c: ChangeDetectorRef, r: ElementRef, protected z: NgZone) {
    c.detach();
    this.el = r.nativeElement;
  }
}


export declare interface ElsaToastManager extends Components.ElsaToastManager {}


@ProxyCmp({
  inputs: ['notification', 'showDuration']
})
@Component({
  selector: 'elsa-toast-notification',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: '<ng-content></ng-content>',
  // eslint-disable-next-line @angular-eslint/no-inputs-metadata-property
  inputs: ['notification', 'showDuration'],
})
export class ElsaToastNotification {
  protected el: HTMLElement;
  constructor(c: ChangeDetectorRef, r: ElementRef, protected z: NgZone) {
    c.detach();
    this.el = r.nativeElement;
  }
}


export declare interface ElsaToastNotification extends Components.ElsaToastNotification {}


@ProxyCmp({
  inputs: ['tooltipContent', 'tooltipPosition']
})
@Component({
  selector: 'elsa-tooltip',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: '<ng-content></ng-content>',
  // eslint-disable-next-line @angular-eslint/no-inputs-metadata-property
  inputs: ['tooltipContent', 'tooltipPosition'],
})
export class ElsaTooltip {
  protected el: HTMLElement;
  constructor(c: ChangeDetectorRef, r: ElementRef, protected z: NgZone) {
    c.detach();
    this.el = r.nativeElement;
  }
}


export declare interface ElsaTooltip extends Components.ElsaTooltip {}


@ProxyCmp({
  inputs: ['inputContext']
})
@Component({
  selector: 'elsa-type-picker-input',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: '<ng-content></ng-content>',
  // eslint-disable-next-line @angular-eslint/no-inputs-metadata-property
  inputs: ['inputContext'],
})
export class ElsaTypePickerInput {
  protected el: HTMLElement;
  constructor(c: ChangeDetectorRef, r: ElementRef, protected z: NgZone) {
    c.detach();
    this.el = r.nativeElement;
  }
}


export declare interface ElsaTypePickerInput extends Components.ElsaTypePickerInput {}


@ProxyCmp({
  inputs: ['variable'],
  methods: ['getVariable']
})
@Component({
  selector: 'elsa-variable-editor-dialog-content',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: '<ng-content></ng-content>',
  // eslint-disable-next-line @angular-eslint/no-inputs-metadata-property
  inputs: ['variable'],
})
export class ElsaVariableEditorDialogContent {
  protected el: HTMLElement;
  constructor(c: ChangeDetectorRef, r: ElementRef, protected z: NgZone) {
    c.detach();
    this.el = r.nativeElement;
    proxyOutputs(this, this.el, ['variableChanged']);
  }
}


import type { Variable as IElsaVariableEditorDialogContentVariable } from '@elsa-workflows/elsa-workflows-designer';

export declare interface ElsaVariableEditorDialogContent extends Components.ElsaVariableEditorDialogContent {

  variableChanged: EventEmitter<CustomEvent<IElsaVariableEditorDialogContentVariable>>;
}


@ProxyCmp({
  inputs: ['inputContext', 'workflowDefinition']
})
@Component({
  selector: 'elsa-variable-picker-input',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: '<ng-content></ng-content>',
  // eslint-disable-next-line @angular-eslint/no-inputs-metadata-property
  inputs: ['inputContext', 'workflowDefinition'],
})
export class ElsaVariablePickerInput {
  protected el: HTMLElement;
  constructor(c: ChangeDetectorRef, r: ElementRef, protected z: NgZone) {
    c.detach();
    this.el = r.nativeElement;
  }
}


export declare interface ElsaVariablePickerInput extends Components.ElsaVariablePickerInput {}


@ProxyCmp({
  inputs: ['variables']
})
@Component({
  selector: 'elsa-variables-editor',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: '<ng-content></ng-content>',
  // eslint-disable-next-line @angular-eslint/no-inputs-metadata-property
  inputs: ['variables'],
})
export class ElsaVariablesEditor {
  protected el: HTMLElement;
  constructor(c: ChangeDetectorRef, r: ElementRef, protected z: NgZone) {
    c.detach();
    this.el = r.nativeElement;
    proxyOutputs(this, this.el, ['variablesChanged']);
  }
}


import type { Variable as IElsaVariablesEditorVariable } from '@elsa-workflows/elsa-workflows-designer';

export declare interface ElsaVariablesEditor extends Components.ElsaVariablesEditor {

  variablesChanged: EventEmitter<CustomEvent<IElsaVariablesEditorVariable[]>>;
}


@ProxyCmp({
  inputs: ['variables', 'workflowDefinition', 'workflowInstance']
})
@Component({
  selector: 'elsa-variables-viewer',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: '<ng-content></ng-content>',
  // eslint-disable-next-line @angular-eslint/no-inputs-metadata-property
  inputs: ['variables', 'workflowDefinition', 'workflowInstance'],
})
export class ElsaVariablesViewer {
  protected el: HTMLElement;
  constructor(c: ChangeDetectorRef, r: ElementRef, protected z: NgZone) {
    c.detach();
    this.el = r.nativeElement;
  }
}


export declare interface ElsaVariablesViewer extends Components.ElsaVariablesViewer {}


@ProxyCmp({
  inputs: ['widgets']
})
@Component({
  selector: 'elsa-widgets',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: '<ng-content></ng-content>',
  // eslint-disable-next-line @angular-eslint/no-inputs-metadata-property
  inputs: ['widgets'],
})
export class ElsaWidgets {
  protected el: HTMLElement;
  constructor(c: ChangeDetectorRef, r: ElementRef, protected z: NgZone) {
    c.detach();
    this.el = r.nativeElement;
  }
}


export declare interface ElsaWidgets extends Components.ElsaWidgets {}


@ProxyCmp({
  inputs: ['renderContext']
})
@Component({
  selector: 'elsa-workflow-definition-activity-version-settings',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: '<ng-content></ng-content>',
  // eslint-disable-next-line @angular-eslint/no-inputs-metadata-property
  inputs: ['renderContext'],
})
export class ElsaWorkflowDefinitionActivityVersionSettings {
  protected el: HTMLElement;
  constructor(c: ChangeDetectorRef, r: ElementRef, protected z: NgZone) {
    c.detach();
    this.el = r.nativeElement;
  }
}


export declare interface ElsaWorkflowDefinitionActivityVersionSettings extends Components.ElsaWorkflowDefinitionActivityVersionSettings {}


@ProxyCmp({
})
@Component({
  selector: 'elsa-workflow-definition-browser',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: '<ng-content></ng-content>',
  // eslint-disable-next-line @angular-eslint/no-inputs-metadata-property
  inputs: [],
})
export class ElsaWorkflowDefinitionBrowser {
  protected el: HTMLElement;
  constructor(c: ChangeDetectorRef, r: ElementRef, protected z: NgZone) {
    c.detach();
    this.el = r.nativeElement;
    proxyOutputs(this, this.el, ['workflowDefinitionSelected', 'newWorkflowDefinitionSelected']);
  }
}


import type { WorkflowDefinitionSummary as IElsaWorkflowDefinitionBrowserWorkflowDefinitionSummary } from '@elsa-workflows/elsa-workflows-designer';

export declare interface ElsaWorkflowDefinitionBrowser extends Components.ElsaWorkflowDefinitionBrowser {

  workflowDefinitionSelected: EventEmitter<CustomEvent<IElsaWorkflowDefinitionBrowserWorkflowDefinitionSummary>>;

  newWorkflowDefinitionSelected: EventEmitter<CustomEvent<any>>;
}


@ProxyCmp({
  inputs: ['monacoLibPath', 'workflowDefinition'],
  methods: ['getFlowchart', 'registerActivityDrivers', 'getWorkflowDefinition', 'importWorkflow', 'updateWorkflowDefinition', 'newWorkflow', 'loadWorkflowVersions', 'updateActivity']
})
@Component({
  selector: 'elsa-workflow-definition-editor',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: '<ng-content></ng-content>',
  // eslint-disable-next-line @angular-eslint/no-inputs-metadata-property
  inputs: ['monacoLibPath', 'workflowDefinition'],
})
export class ElsaWorkflowDefinitionEditor {
  protected el: HTMLElement;
  constructor(c: ChangeDetectorRef, r: ElementRef, protected z: NgZone) {
    c.detach();
    this.el = r.nativeElement;
    proxyOutputs(this, this.el, ['workflowUpdated']);
  }
}


import type { WorkflowDefinitionUpdatedArgs as IElsaWorkflowDefinitionEditorWorkflowDefinitionUpdatedArgs } from '@elsa-workflows/elsa-workflows-designer';

export declare interface ElsaWorkflowDefinitionEditor extends Components.ElsaWorkflowDefinitionEditor {

  workflowUpdated: EventEmitter<CustomEvent<IElsaWorkflowDefinitionEditorWorkflowDefinitionUpdatedArgs>>;
}


@ProxyCmp({
  inputs: ['zoomToFit']
})
@Component({
  selector: 'elsa-workflow-definition-editor-toolbar',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: '<ng-content></ng-content>',
  // eslint-disable-next-line @angular-eslint/no-inputs-metadata-property
  inputs: ['zoomToFit'],
})
export class ElsaWorkflowDefinitionEditorToolbar {
  protected el: HTMLElement;
  constructor(c: ChangeDetectorRef, r: ElementRef, protected z: NgZone) {
    c.detach();
    this.el = r.nativeElement;
    proxyOutputs(this, this.el, ['autoLayout']);
  }
}


import type { LayoutDirection as IElsaWorkflowDefinitionEditorToolbarLayoutDirection } from '@elsa-workflows/elsa-workflows-designer';

export declare interface ElsaWorkflowDefinitionEditorToolbar extends Components.ElsaWorkflowDefinitionEditorToolbar {

  autoLayout: EventEmitter<CustomEvent<IElsaWorkflowDefinitionEditorToolbarLayoutDirection>>;
}


@ProxyCmp({
  inputs: ['graph']
})
@Component({
  selector: 'elsa-workflow-definition-editor-toolbox',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: '<ng-content></ng-content>',
  // eslint-disable-next-line @angular-eslint/no-inputs-metadata-property
  inputs: ['graph'],
})
export class ElsaWorkflowDefinitionEditorToolbox {
  protected el: HTMLElement;
  constructor(c: ChangeDetectorRef, r: ElementRef, protected z: NgZone) {
    c.detach();
    this.el = r.nativeElement;
  }
}


export declare interface ElsaWorkflowDefinitionEditorToolbox extends Components.ElsaWorkflowDefinitionEditorToolbox {}


@ProxyCmp({
  inputs: ['graph']
})
@Component({
  selector: 'elsa-workflow-definition-editor-toolbox-activities',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: '<ng-content></ng-content>',
  // eslint-disable-next-line @angular-eslint/no-inputs-metadata-property
  inputs: ['graph'],
})
export class ElsaWorkflowDefinitionEditorToolboxActivities {
  protected el: HTMLElement;
  constructor(c: ChangeDetectorRef, r: ElementRef, protected z: NgZone) {
    c.detach();
    this.el = r.nativeElement;
  }
}


export declare interface ElsaWorkflowDefinitionEditorToolboxActivities extends Components.ElsaWorkflowDefinitionEditorToolboxActivities {}


@ProxyCmp({
  inputs: ['inputs', 'outcomes', 'outputs']
})
@Component({
  selector: 'elsa-workflow-definition-input-output-settings',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: '<ng-content></ng-content>',
  // eslint-disable-next-line @angular-eslint/no-inputs-metadata-property
  inputs: ['inputs', 'outcomes', 'outputs'],
})
export class ElsaWorkflowDefinitionInputOutputSettings {
  protected el: HTMLElement;
  constructor(c: ChangeDetectorRef, r: ElementRef, protected z: NgZone) {
    c.detach();
    this.el = r.nativeElement;
    proxyOutputs(this, this.el, ['inputsChanged', 'outputsChanged', 'outcomesChanged']);
  }
}


import type { InputDefinition as IElsaWorkflowDefinitionInputOutputSettingsInputDefinition } from '@elsa-workflows/elsa-workflows-designer';
import type { OutputDefinition as IElsaWorkflowDefinitionInputOutputSettingsOutputDefinition } from '@elsa-workflows/elsa-workflows-designer';

export declare interface ElsaWorkflowDefinitionInputOutputSettings extends Components.ElsaWorkflowDefinitionInputOutputSettings {

  inputsChanged: EventEmitter<CustomEvent<IElsaWorkflowDefinitionInputOutputSettingsInputDefinition[]>>;

  outputsChanged: EventEmitter<CustomEvent<IElsaWorkflowDefinitionInputOutputSettingsOutputDefinition[]>>;

  outcomesChanged: EventEmitter<CustomEvent<Array<string>>>;
}


@ProxyCmp({
  inputs: ['inputContext']
})
@Component({
  selector: 'elsa-workflow-definition-picker-input',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: '<ng-content></ng-content>',
  // eslint-disable-next-line @angular-eslint/no-inputs-metadata-property
  inputs: ['inputContext'],
})
export class ElsaWorkflowDefinitionPickerInput {
  protected el: HTMLElement;
  constructor(c: ChangeDetectorRef, r: ElementRef, protected z: NgZone) {
    c.detach();
    this.el = r.nativeElement;
  }
}


export declare interface ElsaWorkflowDefinitionPickerInput extends Components.ElsaWorkflowDefinitionPickerInput {}


@ProxyCmp({
  inputs: ['workflowDefinition', 'workflowVersions'],
  methods: ['show', 'hide']
})
@Component({
  selector: 'elsa-workflow-definition-properties-editor',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: '<ng-content></ng-content>',
  // eslint-disable-next-line @angular-eslint/no-inputs-metadata-property
  inputs: ['workflowDefinition', 'workflowVersions'],
})
export class ElsaWorkflowDefinitionPropertiesEditor {
  protected el: HTMLElement;
  constructor(c: ChangeDetectorRef, r: ElementRef, protected z: NgZone) {
    c.detach();
    this.el = r.nativeElement;
    proxyOutputs(this, this.el, ['workflowPropsUpdated', 'versionSelected', 'deleteVersionClicked', 'revertVersionClicked']);
  }
}


import type { WorkflowDefinitionPropsUpdatedArgs as IElsaWorkflowDefinitionPropertiesEditorWorkflowDefinitionPropsUpdatedArgs } from '@elsa-workflows/elsa-workflows-designer';
import type { WorkflowDefinition as IElsaWorkflowDefinitionPropertiesEditorWorkflowDefinition } from '@elsa-workflows/elsa-workflows-designer';

export declare interface ElsaWorkflowDefinitionPropertiesEditor extends Components.ElsaWorkflowDefinitionPropertiesEditor {

  workflowPropsUpdated: EventEmitter<CustomEvent<IElsaWorkflowDefinitionPropertiesEditorWorkflowDefinitionPropsUpdatedArgs>>;

  versionSelected: EventEmitter<CustomEvent<IElsaWorkflowDefinitionPropertiesEditorWorkflowDefinition>>;

  deleteVersionClicked: EventEmitter<CustomEvent<IElsaWorkflowDefinitionPropertiesEditorWorkflowDefinition>>;

  revertVersionClicked: EventEmitter<CustomEvent<IElsaWorkflowDefinitionPropertiesEditorWorkflowDefinition>>;
}


@ProxyCmp({
  inputs: ['selectedVersion', 'serverUrl', 'workflowVersions']
})
@Component({
  selector: 'elsa-workflow-definition-version-history',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: '<ng-content></ng-content>',
  // eslint-disable-next-line @angular-eslint/no-inputs-metadata-property
  inputs: ['selectedVersion', 'serverUrl', 'workflowVersions'],
})
export class ElsaWorkflowDefinitionVersionHistory {
  protected el: HTMLElement;
  constructor(c: ChangeDetectorRef, r: ElementRef, protected z: NgZone) {
    c.detach();
    this.el = r.nativeElement;
    proxyOutputs(this, this.el, ['versionSelected', 'deleteVersionClicked', 'revertVersionClicked']);
  }
}


import type { WorkflowDefinition as IElsaWorkflowDefinitionVersionHistoryWorkflowDefinition } from '@elsa-workflows/elsa-workflows-designer';

export declare interface ElsaWorkflowDefinitionVersionHistory extends Components.ElsaWorkflowDefinitionVersionHistory {

  versionSelected: EventEmitter<CustomEvent<IElsaWorkflowDefinitionVersionHistoryWorkflowDefinition>>;

  deleteVersionClicked: EventEmitter<CustomEvent<IElsaWorkflowDefinitionVersionHistoryWorkflowDefinition>>;

  revertVersionClicked: EventEmitter<CustomEvent<IElsaWorkflowDefinitionVersionHistoryWorkflowDefinition>>;
}


@ProxyCmp({
})
@Component({
  selector: 'elsa-workflow-instance-browser',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: '<ng-content></ng-content>',
  // eslint-disable-next-line @angular-eslint/no-inputs-metadata-property
  inputs: [],
})
export class ElsaWorkflowInstanceBrowser {
  protected el: HTMLElement;
  constructor(c: ChangeDetectorRef, r: ElementRef, protected z: NgZone) {
    c.detach();
    this.el = r.nativeElement;
    proxyOutputs(this, this.el, ['workflowInstanceSelected']);
  }
}


import type { WorkflowInstanceSummary as IElsaWorkflowInstanceBrowserWorkflowInstanceSummary } from '@elsa-workflows/elsa-workflows-designer';

export declare interface ElsaWorkflowInstanceBrowser extends Components.ElsaWorkflowInstanceBrowser {

  workflowInstanceSelected: EventEmitter<CustomEvent<IElsaWorkflowInstanceBrowserWorkflowInstanceSummary>>;
}


@ProxyCmp({
  inputs: ['workflowDefinition', 'workflowInstance'],
  methods: ['show', 'hide']
})
@Component({
  selector: 'elsa-workflow-instance-properties',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: '<ng-content></ng-content>',
  // eslint-disable-next-line @angular-eslint/no-inputs-metadata-property
  inputs: ['workflowDefinition', 'workflowInstance'],
})
export class ElsaWorkflowInstanceProperties {
  protected el: HTMLElement;
  constructor(c: ChangeDetectorRef, r: ElementRef, protected z: NgZone) {
    c.detach();
    this.el = r.nativeElement;
  }
}


export declare interface ElsaWorkflowInstanceProperties extends Components.ElsaWorkflowInstanceProperties {}


@ProxyCmp({
  inputs: ['monacoLibPath', 'workflowDefinition', 'workflowInstance'],
  methods: ['getCanvas', 'registerActivityDrivers', 'getWorkflow', 'importWorkflow', 'updateWorkflowDefinition']
})
@Component({
  selector: 'elsa-workflow-instance-viewer',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: '<ng-content></ng-content>',
  // eslint-disable-next-line @angular-eslint/no-inputs-metadata-property
  inputs: ['monacoLibPath', 'workflowDefinition', 'workflowInstance'],
})
export class ElsaWorkflowInstanceViewer {
  protected el: HTMLElement;
  constructor(c: ChangeDetectorRef, r: ElementRef, protected z: NgZone) {
    c.detach();
    this.el = r.nativeElement;
  }
}


export declare interface ElsaWorkflowInstanceViewer extends Components.ElsaWorkflowInstanceViewer {}


@ProxyCmp({
  inputs: ['workflowDefinition', 'workflowInstance']
})
@Component({
  selector: 'elsa-workflow-journal',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: '<ng-content></ng-content>',
  // eslint-disable-next-line @angular-eslint/no-inputs-metadata-property
  inputs: ['workflowDefinition', 'workflowInstance'],
})
export class ElsaWorkflowJournal {
  protected el: HTMLElement;
  constructor(c: ChangeDetectorRef, r: ElementRef, protected z: NgZone) {
    c.detach();
    this.el = r.nativeElement;
    proxyOutputs(this, this.el, ['journalItemSelected']);
  }
}


import type { JournalItemSelectedArgs as IElsaWorkflowJournalJournalItemSelectedArgs } from '@elsa-workflows/elsa-workflows-designer';

export declare interface ElsaWorkflowJournal extends Components.ElsaWorkflowJournal {

  journalItemSelected: EventEmitter<CustomEvent<IElsaWorkflowJournalJournalItemSelectedArgs>>;
}


@ProxyCmp({
  inputs: ['items', 'rootActivity']
})
@Component({
  selector: 'elsa-workflow-navigator',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: '<ng-content></ng-content>',
  // eslint-disable-next-line @angular-eslint/no-inputs-metadata-property
  inputs: ['items', 'rootActivity'],
})
export class ElsaWorkflowNavigator {
  protected el: HTMLElement;
  constructor(c: ChangeDetectorRef, r: ElementRef, protected z: NgZone) {
    c.detach();
    this.el = r.nativeElement;
    proxyOutputs(this, this.el, ['navigate']);
  }
}


import type { FlowchartPathItem as IElsaWorkflowNavigatorFlowchartPathItem } from '@elsa-workflows/elsa-workflows-designer';

export declare interface ElsaWorkflowNavigator extends Components.ElsaWorkflowNavigator {

  navigate: EventEmitter<CustomEvent<IElsaWorkflowNavigatorFlowchartPathItem>>;
}


@ProxyCmp({
  inputs: ['publishing']
})
@Component({
  selector: 'elsa-workflow-publish-button',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: '<ng-content></ng-content>',
  // eslint-disable-next-line @angular-eslint/no-inputs-metadata-property
  inputs: ['publishing'],
})
export class ElsaWorkflowPublishButton {
  protected el: HTMLElement;
  constructor(c: ChangeDetectorRef, r: ElementRef, protected z: NgZone) {
    c.detach();
    this.el = r.nativeElement;
    proxyOutputs(this, this.el, ['newClicked', 'publishClicked', 'unPublishClicked', 'exportClicked', 'importClicked']);
  }
}


import type { PublishClickedArgs as IElsaWorkflowPublishButtonPublishClickedArgs } from '@elsa-workflows/elsa-workflows-designer';

export declare interface ElsaWorkflowPublishButton extends Components.ElsaWorkflowPublishButton {

  newClicked: EventEmitter<CustomEvent<any>>;

  publishClicked: EventEmitter<CustomEvent<IElsaWorkflowPublishButtonPublishClickedArgs>>;

  unPublishClicked: EventEmitter<CustomEvent<any>>;

  exportClicked: EventEmitter<CustomEvent<any>>;

  importClicked: EventEmitter<CustomEvent<File>>;
}


@ProxyCmp({
})
@Component({
  selector: 'elsa-workflow-toolbar',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: '<ng-content></ng-content>',
  // eslint-disable-next-line @angular-eslint/no-inputs-metadata-property
  inputs: [],
})
export class ElsaWorkflowToolbar {
  protected el: HTMLElement;
  constructor(c: ChangeDetectorRef, r: ElementRef, protected z: NgZone) {
    c.detach();
    this.el = r.nativeElement;
  }
}


export declare interface ElsaWorkflowToolbar extends Components.ElsaWorkflowToolbar {}


@ProxyCmp({
})
@Component({
  selector: 'elsa-workflow-toolbar-menu',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: '<ng-content></ng-content>',
  // eslint-disable-next-line @angular-eslint/no-inputs-metadata-property
  inputs: [],
})
export class ElsaWorkflowToolbarMenu {
  protected el: HTMLElement;
  constructor(c: ChangeDetectorRef, r: ElementRef, protected z: NgZone) {
    c.detach();
    this.el = r.nativeElement;
  }
}


export declare interface ElsaWorkflowToolbarMenu extends Components.ElsaWorkflowToolbarMenu {}


