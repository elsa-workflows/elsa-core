import { Validator, ValidatorEntry } from "./models";
import { defaultValidator, lengthValidator } from "./validators";

export enum Validators {
    Length = 'length'
}

export function combineValidators<A>(v1: Validator<A>, v2: Validator<A>): Validator<A> {
    let combined: Validator<A>;
    combined = {
        validate: (x: A) => {
            const res1: boolean = v1.validate(x);
            const res2: boolean = v2.validate(x);
            if (!res1) {
                combined.errorMessage = v1.errorMessage;
            } else if (!res2) {
                combined.errorMessage = v2.errorMessage;
            }
            return res1 && res2;
        },
    }
    return combined;
}

export function getValidator<A>(list: Array<string | ValidatorEntry | Validator<A>>): Validator<A> {
    return (list || []).map(v => {
        if (typeof v === 'string') {
            return validatorFactory(v, null);
        } else if ( v && (v as any).name) {
            v = v as ValidatorEntry;
            return validatorFactory(v.name, v.options);
        } else {
            return v as Validator<A>;
        }
    }).reduce(combineValidators, defaultValidator);
}

export function validatorFactory(name: string, options: any): Validator<any> {
    options = options || {};
    switch (name) {
        case (Validators.Length):
            return lengthValidator(options.min, options.max);
        default:
            return defaultValidator;
    }
}