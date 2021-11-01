import { Validator } from "../models";

export const keyNameValidatior: Validator<string> = {
    validate: (value: string) => {
        const res = value.match('^[a-zA-Z0-9]*$');

        return !!res;
    },
    errorMessage: `Only letters and digits are allowed`
}