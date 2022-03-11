import { Component, h, Prop, State } from "@stencil/core";
import cronstrue from "cronstrue";
import { ActivityDefinitionProperty, ActivityModel, ActivityPropertyDescriptor, SyntaxNames } from "../../../../models";

export interface CronTabModel {
    tabName: string,
    unitName: string,
    range: Array<string>,
    period: Array<string>
}

@Component({
    tag: 'elsa-cron-expression-property',
})
export class ElsaCronExpressionProperty {

    @Prop() activityModel: ActivityModel;
    @Prop() propertyDescriptor: ActivityPropertyDescriptor;
    @Prop() propertyModel: ActivityDefinitionProperty;
    @State() currentValue: string;
    @State() valueDescription: string;
    @State() tabs?: Array<CronTabModel>;
    @State() selectedTabName: string;

    onChange(e: Event) {
        const input = e.currentTarget as HTMLInputElement;
        const defaultSyntax = this.propertyDescriptor.defaultSyntax || SyntaxNames.Literal;
        this.propertyModel.expressions[defaultSyntax] = this.currentValue = input.value;
        this.updateDescription();
    }

    componentWillLoad() {
        const defaultSyntax = this.propertyDescriptor.defaultSyntax || SyntaxNames.Literal;
        this.currentValue = this.propertyModel.expressions[defaultSyntax] || undefined;
        this.updateDescription();
    }

    onDefaultSyntaxValueChanged(e: CustomEvent) {
        this.currentValue = e.detail;
    }

    updateDescription() {
        this.valueDescription = cronstrue.toString(this.currentValue, { throwExceptionOnParseError: false });
    }

    componentWillRender() {
        let tabs: Array<CronTabModel> = [];

        tabs.push({
            tabName: 'Seconds',
            unitName: 'second',
            range: Array.from({ length: 60 }, (value, key) => key.toString()),
            period: Array.from({ length: 60 }, (value, key) => (key + 1).toString())
        });

        tabs.push({
            tabName: 'Minutes',
            unitName: 'minute',
            range: Array.from({ length: 60 }, (value, key) => key.toString()),
            period: Array.from({ length: 60 }, (value, key) => (key + 1).toString())
        });

        tabs.push({
            tabName: 'Hours',
            unitName: 'hour',
            range: Array.from({ length: 24 }, (value, key) => key.toString()),
            period: Array.from({ length: 24 }, (value, key) => (key + 1).toString())
        });

        tabs.push({
            tabName: 'Day',
            unitName: 'day',
            range: ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'],
            period: Array.from({ length: 31 }, (value, key) => (key + 1).toString())
        });

        tabs.push({
            tabName: 'Month',
            unitName: 'month',
            range: ['January', 'Februrary', 'March', 'April', 'May', 'June', 'July',
                'August', 'September', 'October', 'November', 'December'
            ],
            period: Array.from({ length: 31 }, (value, key) => (key + 1).toString())
        });

        tabs.push({
            tabName: 'Year',
            unitName: 'year',
            range: Array.from({ length: 3000 - new Date().getFullYear() }, (value, key) => (key + new Date().getFullYear()).toString()),
            period: Array.from({ length: 100 }, (value, key) => (key + 1).toString())
        });


        this.tabs = tabs;

        let selectedTabName = this.selectedTabName
        tabs = this.tabs;

        if (!selectedTabName)
            this.selectedTabName = tabs[0].tabName;

        if (tabs.findIndex(x => x.tabName === selectedTabName) < 0)
            this.selectedTabName = selectedTabName = tabs[0].tabName;
    }

    onTabClick = (e: Event, tab: CronTabModel) => {
        e.preventDefault();
        this.selectedTabName = tab.tabName;
    };

    render() {
        const propertyDescriptor = this.propertyDescriptor;
        const propertyModel = this.propertyModel;
        const propertyName = propertyDescriptor.name;
        const isReadOnly = propertyDescriptor.isReadOnly;
        const fieldId = propertyName;
        const fieldName = propertyName;
        const tabs = this.tabs;
        const selectedTabName = this.selectedTabName;

        let value = this.currentValue;

        const inactiveClass = 'elsa-border-transparent elsa-text-gray-500 hover:elsa-text-gray-700 hover:elsa-border-gray-300';
        const selectedClass = 'elsa-border-blue-500 elsa-text-blue-600';

        if (value == undefined) {
            const defaultValue = this.propertyDescriptor.defaultValue;
            value = defaultValue ? defaultValue.toString() : undefined;
        }

        if (isReadOnly) {
            const defaultSyntax = this.propertyDescriptor.defaultSyntax || SyntaxNames.Literal;
            this.propertyModel.expressions[defaultSyntax] = value;
        }

        return (
            <elsa-property-editor
                activityModel={this.activityModel}
                propertyDescriptor={propertyDescriptor}
                propertyModel={propertyModel}
                onDefaultSyntaxValueChanged={e => this.onDefaultSyntaxValueChanged(e)}
                editor-height="5em"
                single-line={true}>
                <div>
                    <input type="text" id={fieldId} name={fieldName} value={value} onChange={e => this.onChange(e)}
                        class="disabled:elsa-opacity-50 disabled:elsa-cursor-not-allowed focus:elsa-ring-blue-500 focus:elsa-border-blue-500 elsa-block elsa-w-full elsa-min-w-0 elsa-rounded-md sm:elsa-text-sm elsa-border-gray-300"
                        disabled={isReadOnly} />
                    <p class="elsa-mt-2 elsa-text-sm elsa-text-gray-500">{this.valueDescription}</p>
                    <div class="elsa-border-b elsa-border-gray-200">
                        <nav class="-elsa-mb-px elsa-flex elsa-space-x-8" aria-label="Tabs">
                            {tabs.map(tab => {
                                const isSelected = tab.tabName === selectedTabName;
                                const cssClass = isSelected ? selectedClass : inactiveClass;
                                return <a href="#" onClick={e => this.onTabClick(e, tab)}
                                    class={`${cssClass} elsa-whitespace-nowrap elsa-py-4 elsa-px-1 elsa-border-b-2 elsa-font-medium elsa-text-sm`}>{tab.tabName}</a>;
                            })}
                        </nav>
                    </div>
                    <div>
                        {this.renderTabs(tabs)}
                    </div>
                </div>

            </elsa-property-editor>
        );
    }

    renderTabs(tabs: Array<CronTabModel>) {
        return tabs.map(x =>
        (
            <div class={`flex ${this.getHiddenClass(x.tabName)}`}>
                <elsa-control content={this.renderCronTab(x)} />
            </div>
        ));
    }

    renderCronTab(tab: CronTabModel) {
        return (
            <div class="elsa-max-w-lg elsa-space-y-3 elsa-my-4">
                <div class="elsa-relative elsa-flex elsa-items-start">
                    <div class="elsa-flex elsa-items-center elsa-h-5">
                        <input id='{tab.unitName}_star' type="radio" radioGroup={tab.unitName} value='{tab.unitName}_star'
                            class="elsa-focus:ring-blue-500 elsa-h-4 elsa-w-4 elsa-text-blue-600 elsa-border-gray-300" />
                    </div>
                    <div class="elsa-ml-3 elsa-mt-1 elsa-text-sm">
                        <label htmlFor='{tab.unitName}_star' class="elsa-font-medium elsa-text-gray-700"> Every {tab.tabName}</label>
                    </div>
                </div>
                <div class="elsa-relative elsa-flex elsa-items-start">
                    <div class="elsa-flex elsa-items-center elsa-h-5">
                        <input id='{tab.unitName}_period' type="radio" radioGroup={tab.unitName} value='{tab.unitName}_period'
                            class="elsa-focus:ring-blue-500 elsa-h-4 elsa-w-4 elsa-text-blue-600 elsa-border-gray-300" />
                    </div>
                    <div class="elsa-ml-3 elsa-mt-1 elsa-text-sm">
                        <label htmlFor='{tab.unitName}_period' class="elsa-font-medium elsa-text-gray-700"> Every &nbsp;
                            <select class="elsa-border-gray-300 elsa-rounded-md elsa-py-0">
                                {tab.period.map(p => {
                                    return <option>{p}</option>;
                                })}
                            </select> {tab.unitName}(s) starting at {tab.unitName} &nbsp;
                            <select class="elsa-border-gray-300 elsa-rounded-md elsa-py-0">
                                {tab.range.map(p => {
                                    return <option>{p}</option>;
                                })}
                            </select>
                        </label>
                    </div>
                </div>
            </div>
        );
    }

    getHiddenClass(tab: string) {
        return this.selectedTabName == tab ? '' : 'hidden';
    }

}