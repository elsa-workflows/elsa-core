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

    async addItem(item: string)
    {
        const values = [...this.currentValues];
        values.push(item);
        this.currentValues = values.distinct();
        await this.valueChanged.emit(values);
    }

    async onInputKeyDown(e: KeyboardEvent) {
        if (e.key != "Enter")
            return;

        e.preventDefault();

        const input = e.target as HTMLInputElement;
        const value = input.value.trim();

        if (value.length == 0)
            return;

        await this.addItem(value);
        input.value = '';
    }

    async onInputBlur(e: Event) {
        const input = e.target as HTMLInputElement;
        const value = input.value.trim();

        if (value.length == 0)
            return;

        await this.addItem(value);
        input.value = '';
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
            <div class="elsa-py-2 elsa-px-3 elsa-bg-white elsa-shadow-sm elsa-border elsa-border-gray-300 elsa-rounded-md">
                {values.map(value => (
                    <a href="#" onClick={e => this.onDeleteTagClick(e, value)} class="elsa-inline-block elsa-text-xs elsa-bg-blue-400 elsa-text-white elsa-py-2 elsa-px-3 elsa-mr-1 elsa-mb-1 elsa-rounded">
                        <span>{value}</span>
                        <span class="elsa-text-white hover:elsa-text-white elsa-ml-1">&times;</span>
                    </a>
                ))}
                <input type="text" id={this.fieldId} 
                       onKeyDown={e => this.onInputKeyDown(e)}
                       onBlur={e => this.onInputBlur(e)}
                       class="elsa-tag-input elsa-inline-block elsa-text-sm elsa-outline-none focus:elsa-outline-none elsa-border-none shadow:none focus:elsa-border-none focus:elsa-border-transparent focus:shadow-none"
                       placeholder={this.placeHolder}/>
                <input type="hidden" name={this.fieldName} value={valuesJson}/>
            </div>
        )
    }
}
