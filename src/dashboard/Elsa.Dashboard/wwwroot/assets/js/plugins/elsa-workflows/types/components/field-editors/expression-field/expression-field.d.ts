export declare class ExpressionField {
    name: string;
    label: string;
    hint: string;
    value: string;
    multiline: boolean;
    syntax: string;
    private selectSyntax;
    private onSyntaxOptionClick;
    renderInputField: () => any;
    render(): any;
}
