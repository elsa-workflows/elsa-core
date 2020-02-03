import { SelectGroup, SelectItem, SelectOption } from "./models";
export declare class SelectField {
    element: HTMLWfSelectFieldElement;
    name: string;
    label: string;
    value: string;
    hint: string;
    items: Array<SelectItem>;
    componentWillLoad(): void;
    render(): any;
    renderItem: (item: SelectItem) => any;
    renderOption: (option: SelectOption) => any;
    renderGroup: (group: SelectGroup) => any;
}
