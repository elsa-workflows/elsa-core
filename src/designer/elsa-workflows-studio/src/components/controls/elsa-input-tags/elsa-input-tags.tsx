import {Component, h, Prop, Event, EventEmitter, State, Watch} from '@stencil/core';

@Component({
    tag: 'elsa-input-tags',
    styleUrl: 'elsa-input-tags.css',
    shadow: false,
})
export class ElsaInputTags {

    @Prop() fieldName?: string;
    @Prop() fieldId?: string;
    @Prop() placeHolder?: string = 'Add tag';
    @Prop() values?: Array<string> = [];
    @Event({bubbles: true}) valueChanged: EventEmitter<Array<string>>;
    @State() currentValues?: Array<string> = [];

    @Watch('values')
    valuesChangedHandler(newValue: Array<string>) {
        this.currentValues = newValue || [];
    }

    componentWillLoad() {
        this.currentValues = this.values;
    }

    async onInputKeyDown(e: KeyboardEvent) {
        if (e.key != "Enter")
            return;

        e.preventDefault();

        const input = e.target as HTMLInputElement;
        const value = input.value.trim();

        if (value.length == 0)
            return;

        const values = [...this.currentValues];
        values.push(value);
        this.currentValues = values.distinct();
        input.value = '';
        await this.valueChanged.emit(values);
    }

    async onDeleteTagClick(e: Event, tag: string) {
        e.preventDefault();

        this.currentValues = this.currentValues.filter(x => x !== tag);
        await this.valueChanged.emit(this.currentValues);
    }

    render() {
        let values = this.currentValues || [];

        if (!Array.isArray(values))
            values = [];

        const valuesJson = JSON.stringify(values);

        return (
            <div class="py-2 px-3 bg-white shadow-sm border border-gray-300 rounded-md">
                {values.map(value => (
                    <a href="#" onClick={e => this.onDeleteTagClick(e, value)} class="inline-block text-xs bg-blue-400 text-white py-2 px-3 mr-1 mb-1 rounded">
                        <span>{value}</span>
                        <span class="text-white hover:text-white ml-1">&times;</span>
                    </a>
                ))}
                <input type="text" id={this.fieldId} onKeyDown={e => this.onInputKeyDown(e)}
                       class="tag-input inline-block text-sm outline-none focus:outline-none border-none shadow:none focus:border-none focus:border-transparent focus:shadow-none"
                       placeholder={this.placeHolder}/>
                <input type="hidden" name={this.fieldName} value={valuesJson}/>
            </div>
        )
    }
}
