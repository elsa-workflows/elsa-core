import { Validator } from "./models";

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
