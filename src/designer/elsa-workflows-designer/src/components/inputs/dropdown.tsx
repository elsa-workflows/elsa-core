import {Component, Prop, h} from '@stencil/core';
import {LiteralExpression, SelectList, SyntaxNames} from "../../models";
import {NodeInputContext} from "../../services/node-input-driver";
import {getInputPropertyValue, getSelectListItems} from "../../utils";

@Component({
  tag: 'elsa-dropdown-input',
  shadow: false
})
export class SingleLineInput {
  @Prop() public inputContext: NodeInputContext;

  private selectList: SelectList = {items: [], isFlagsEnum: false};

  public async componentWillLoad() {
    this.selectList = await getSelectListItems(this.inputContext.inputDescriptor);
  }

  public render() {
    const inputContext = this.inputContext;
    const inputDescriptor = inputContext.inputDescriptor;
    const fieldName = inputDescriptor.name;
    const fieldId = inputDescriptor.name;
    const input = getInputPropertyValue(inputContext);
    const value = (input?.expression as LiteralExpression)?.value;
    const {items} = this.selectList;
    let currentValue = value;

    if (currentValue == undefined) {
      const defaultValue = inputDescriptor.defaultValue;
      currentValue = defaultValue ? defaultValue.toString() : undefined;
    }

    return (
      <select id={fieldId} name={fieldName} onChange={e => this.onChange(e)}>
        {items.map(item => {
          const optionIsObject = typeof (item) == 'object';
          const value = optionIsObject ? item.value : item.toString();
          const text = optionIsObject ? item.text : item.toString();
          return <option value={value} selected={value === currentValue}>{text}</option>;
        })}
      </select>
    );
  }

  private onChange = (e: Event) => {
    const inputElement = e.target as HTMLSelectElement;
    this.inputContext.inputChanged(inputElement.value, SyntaxNames.Literal);
  }
}
