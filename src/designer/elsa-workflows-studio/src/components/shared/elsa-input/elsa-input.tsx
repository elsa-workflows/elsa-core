import { Component, Event, EventEmitter, Prop, h, State } from "@stencil/core";

import { Validator, ValidatorEntry } from "../../../validation/models";
import { getValidator, Validators } from "../../../validation/validator.factory";
import { defaultValidator } from "../../../validation/validators";
import { ValidationStatus } from "../../../validation/workflow-definition-property-validation/workflow-definition-property.messages";

@Component({
    tag: 'elsa-input'
})
export class ElsaInput {
    @Prop({mutable: true}) value: string;
    @Prop() validator: Array<string | ValidatorEntry>;

    @State() valid: boolean = true;

    @Event() changed: EventEmitter<string>
    @Event() validationChanged: EventEmitter<ValidationStatus>

    _validator: Validator<string> = defaultValidator;

    componentWillLoad() {
        this._validator = getValidator(this.validator);
    }
 
    componentWillUpdate() {
        this._validator = getValidator(this.validator);
    }

    handleChange(ev) {
        this.value = ev.target ? ev.target.value : null;
        this._validator.validate(this.value);
        this.valid = !this._validator.errorMessage;

        const validationStatus: ValidationStatus  = {
            valid: this.valid,
            message: this._validator.errorMessage ? this._validator.errorMessage : "" 
        }

        this.changed.emit(this.value);
        this.validationChanged.emit(validationStatus);
    }

    render() {
        return (
            <div>
                <div>
                    {this.valid
                        ? 
                        <input type="text" value={this.value} onInput={ev => this.handleChange(ev)} class="focus:elsa-ring-blue-500 focus:elsa-border-blue-500 elsa-block elsa-w-full elsa-min-w-0 elsa-rounded-md sm:elsa-text-sm elsa-border-gray-300"/>
                        :
                        <input type="text" value={this.value} onInput={ev => this.handleChange(ev)} class="elsa-border-red-400 elsa-block elsa-w-full elsa-min-w-0 elsa-rounded-md sm:elsa-text-sm elsa-border-gray-300"/>
                    }
                    
                </div>
            </div>
        );
    }
}