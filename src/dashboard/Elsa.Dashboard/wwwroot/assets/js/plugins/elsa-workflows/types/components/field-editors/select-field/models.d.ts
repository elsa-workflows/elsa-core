export declare type SelectOptionPair = {
    label: string;
    value: string | number;
};
export declare type SelectOption = string | number | SelectOptionPair;
export declare type SelectGroup = {
    label: string;
    options: Array<SelectOption>;
};
export declare type SelectItem = SelectOption | SelectGroup;
