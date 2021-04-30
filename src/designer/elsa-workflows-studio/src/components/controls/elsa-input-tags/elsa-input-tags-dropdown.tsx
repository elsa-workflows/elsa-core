import { Component, h, Prop, Event, EventEmitter, State, Watch } from '@stencil/core';
import { SelectListItem } from '../../../models';

@Component({
    tag: 'elsa-input-tags-dropdown',
    styleUrl: 'elsa-input-tags-dropdown.css',
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
    @State() dropdownTags?: Array<SelectListItem> = [];

    @Watch('values')
    valuesChangedHandler(newValue: Array<string | SelectListItem>) {
        let values: Array<SelectListItem> = [];

        newValue.forEach(value => {
            this.dropdownValues.forEach(tag => {
                if (value === tag.value) {
                    values.push(tag);
                }
            })
        })
        this.currentValues = values || [];
    }

    componentWillLoad() {
        this.dropdownTags = this.dropdownValues;
        let values: Array<SelectListItem> = [];

        this.values.forEach(value => {
            this.dropdownValues.forEach(tag => {
                if (value === tag.value) {
                    values.push(tag);
                }
            })
        })
        this.currentValues = values;

        const tagsToRemove: Array<string> = this.currentValues.map(tag => tag.value);
        this.dropdownTags = this.dropdownTags.filter(tag => !tagsToRemove.includes(tag.value));
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
        this.dropdownTags = this.dropdownTags.filter(tag => tag.value !== currentTag.value);
        await this.valueChanged.emit(values);
    }

    async onDeleteTagClick(e: any, currentTag: SelectListItem) {
        e.preventDefault();

        this.currentValues = this.currentValues.filter(tag => tag.value !== currentTag.value);
        this.dropdownTags = [...this.dropdownTags, currentTag].sort((tag1, tag2) => tag1.value.localeCompare(tag2.value));
        await this.valueChanged.emit(this.currentValues);
    }

    render() {
        let values: Array<SelectListItem> = this.currentValues || [];

        if (!Array.isArray(values))
            values = [];

        const valuesJson = JSON.stringify(values.map(tag => tag.value));

        return (
            <div class="py-2 px-3 bg-white shadow-sm border border-gray-300 rounded-md">
                {values.map(tag =>
                (
                    <a href="#" onClick={e => this.onDeleteTagClick(e, tag)} class="inline-block text-xs bg-blue-400 text-white py-2 px-3 mr-1 mb-1 rounded">
                        <input type="hidden" value={tag.value} />
                        <span>{tag.text}</span>
                        <span class="text-white hover:text-white ml-1">&times;</span>
                    </a>
                ))}

                <select id={this.fieldId} class="inline-block text-xs bg-green-400 text-white py-2 px-3 mr-1 mb-1 pr-8 rounded" onChange={(e) => this.onTagSelected(e)}>
                    <option value="Add" disabled selected>{this.placeHolder}</option>
                    {this.dropdownTags.map(tag =>
                    (
                        <option value={tag.value}>
                            <span>{tag.text}</span>
                        </option>
                    )
                    )}
                </select>
                <input type="hidden" name={this.fieldName} value={valuesJson} />
            </div>
        )
    }
}