import { Component, h, Prop, Event, EventEmitter, State, Watch } from '@stencil/core';
import { SelectListItem } from '../../../models';

@Component({
    tag: 'elsa-input-tags-dropdown',
    shadow: false,
})
export class ElsaInputTagsDropdown {

    @Prop() fieldName?: string;
    @Prop() fieldId?: string;
    @Prop() placeHolder?: string = 'Add tag';
    @Prop() values?: Array<string | SelectListItem> = [];
    @Prop() dropdownValues?: Array<SelectListItem> = [];
    @Event({ bubbles: true }) valueChanged: EventEmitter<Array<string | SelectListItem>>;
    @State() currentValues?: Array<SelectListItem> = [];

    @Watch('values')
    valuesChangedHandler(newValue: Array<string | SelectListItem>) {
        let values: Array<SelectListItem> = [];
        const dropdownValues = this.dropdownValues || [];
        
        if(!!newValue) {
            newValue.forEach(value => {
                dropdownValues.forEach(tag => {
                    if (value === tag.value) {
                        values.push(tag);
                    }
                })
            })
        }
        
        this.currentValues = values || [];
    }

    componentWillLoad() {
        const dropdownValues = this.dropdownValues || [];
        let values: Array<SelectListItem> = [];

        this.values.forEach(value => {
            dropdownValues.forEach(tag => {
                if (value === tag.value) {
                    values.push(tag);
                }
            })
        })
        
        this.currentValues = values;
    }

    async onTagSelected(e: any) {
        e.preventDefault();

        const input = e.target as HTMLSelectElement;
        const currentTag: SelectListItem = {
            text: input.options[input.selectedIndex].text.trim(),
            value: input.value
        }

        if (currentTag.value.length == 0)
            return;

        const values: Array<SelectListItem> = [...this.currentValues];
        values.push(currentTag);
        this.currentValues = values.distinct();
        input.value = "Add";
        await this.valueChanged.emit(values);
    }

    async onDeleteTagClick(e: any, currentTag: SelectListItem) {
        e.preventDefault();

        this.currentValues = this.currentValues.filter(tag => tag.value !== currentTag.value);
        await this.valueChanged.emit(this.currentValues);
    }

    render() {
        let values: Array<SelectListItem> = this.currentValues || [];
        let dropdownItems = this.dropdownValues.filter(x => values.findIndex(y => y.value === x.value) < 0);

        if (!Array.isArray(values))
            values = [];

        const valuesJson = JSON.stringify(values.map(tag => tag.value));

        return (
            <div class="elsa-py-2 elsa-px-3 elsa-bg-white elsa-shadow-sm elsa-border elsa-border-gray-300 elsa-rounded-md">
                {values.map(tag =>
                (
                    <a href="#" onClick={e => this.onDeleteTagClick(e, tag)} class="elsa-inline-block elsa-text-xs elsa-bg-blue-400 elsa-text-white elsa-py-2 elsa-px-3 elsa-mr-1 elsa-mb-1 rounded">
                        <input type="hidden" value={tag.value} />
                        <span>{tag.text}</span>
                        <span class="elsa-text-white hover:elsa-text-white elsa-ml-1">&times;</span>
                    </a>
                ))}

                <select id={this.fieldId} class="elsa-inline-block elsa-text-xs elsa-py-2 elsa-px-3 elsa-mr-1 elsa-mb-1 elsa-pr-8 elsa-border-gray-300 focus:elsa-outline-none focus:elsa-ring-blue-500 focus:elsa-border-blue-500 elsa-rounded" onChange={(e) => this.onTagSelected(e)}>
                    <option value="Add" disabled selected>{this.placeHolder}</option>
                    {dropdownItems.map(tag =>
                    (
                        <option value={tag.value}>{tag.text}</option>
                    )
                    )}
                </select>
                <input type="hidden" name={this.fieldName} value={valuesJson} />
            </div>
        )
    }
}