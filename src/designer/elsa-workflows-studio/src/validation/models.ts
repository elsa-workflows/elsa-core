export interface Validator<A> {
    validate: (x: A) => boolean;
    errorMessage?: string;
}

export interface AsyncValidator<A> {
    validate: (x: A) => Promise<boolean>;
    errorMessage?: string;
}

export interface ValidatorEntry {
    name: string;
    options?: any;
}
