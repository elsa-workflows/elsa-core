import {h} from '@stencil/core';
import {Service} from "typedi";
import {UIHint} from "../models";

export type RenderActivityPropInputControl = (InputContext) => any;

// A registry of input controls mapped against UI hints.
@Service()
export class InputControlRegistry {
  private inputMap: Map<UIHint, RenderActivityPropInputControl> = new Map<UIHint, RenderActivityPropInputControl>();

  constructor() {
    this.add('single-line', c => <elsa-single-line-input inputContext={c}/>);
    this.add('multi-line', c => <elsa-multi-line-input inputContext={c}/>);
    this.add('dropdown', c => <elsa-dropdown-input inputContext={c}/>);
    this.add('check-list', c => <elsa-check-list-input inputContext={c}/>);
    this.add('radio-list', c => <elsa-radio-list-input inputContext={c}/>);
    this.add('multi-text', c => <elsa-multi-text-input inputContext={c}/>);
    this.add('code-editor', c => <elsa-code-editor-input inputContext={c}/>);
    this.add('checkbox', c => <elsa-checkbox-input inputContext={c}/>);
  }

  public add(uiHint: UIHint, control: RenderActivityPropInputControl) {
    this.inputMap.set(uiHint, control);
  }

  public get(uiHint: UIHint): RenderActivityPropInputControl {
    return this.inputMap.get(uiHint);
  }

  has(uiHint: string): boolean {
    return this.inputMap.has(uiHint);
  }
}
