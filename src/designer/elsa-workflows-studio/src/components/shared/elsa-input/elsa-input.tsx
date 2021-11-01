import { Component, Event, EventEmitter, Prop, h } from "@stencil/core";

import { Validator, ValidatorEntry } from "../../../validation/models";
import { getValidator, Validators } from "../../../validation/validator.factory";
import { defaultValidator } from "../../../validation/validators";

@Component({
    tag: 'elsa-input'
})
export class ElsaInput {
    @Prop({mutable: true}) value: string;
    @Prop() validator: Array<string | ValidatorEntry>;

    @Event() changed: EventEmitter<string>

    _validator: Validator<string> = defaultValidator;

    componentWillLoad() {
        this._validator = getValidator(this.validator);
    }
 
    componentWillUpdate() {
        this._validator = getValidator(this.validator);
    }

    handleChange(ev) {
        this.value = ev.target ? ev.target.value : null;
        this.changed.emit(this.value);
    }

    render() {
        return (
            <div>
                <div>
                    <input type="text" value={this.value} onInput={ev => this.handleChange(ev)} class="focus:elsa-ring-blue-500 focus:elsa-border-blue-500 elsa-block elsa-w-full elsa-min-w-0 elsa-rounded-md sm:elsa-text-sm elsa-border-gray-300"/>
                </div>
                <div >{!this._validator.validate(this.value) ? 
                    <span class="elsa-text-red-800 elsa-text-xs">{this._validator.errorMessage}</span>
                : null }</div>
            </div>
        );
    }
}