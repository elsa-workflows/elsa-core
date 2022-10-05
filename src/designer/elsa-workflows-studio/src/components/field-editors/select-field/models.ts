export type SelectOptionPair = { label: string, value: string | number };
export type SelectOption = string | number | SelectOptionPair;
export type SelectGroup = { label: string, options: Array<SelectOption> };
export type SelectItem = SelectOption | SelectGroup;
