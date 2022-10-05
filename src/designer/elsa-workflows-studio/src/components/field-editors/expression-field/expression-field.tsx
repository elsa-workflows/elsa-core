import { Component, h, Host, Prop, State, Watch } from '@stencil/core';

@Component({
  tag: 'wf-expression-field',
  styleUrl: 'expression-field.scss',
  shadow: false
})
export class ExpressionField {

  @Prop({ reflect: true })
  name: string;

  @Prop({ reflect: true })
  label: string;

  @Prop({ reflect: true })
  hint: string;

  @Prop({ reflect: true })
  value: string;

  @Prop({ reflect: true })
  multiline: boolean;

  @Prop({ reflect: true, mutable: true })
  syntax: string;

  private selectSyntax = (syntax) => this.syntax = syntax;

  private onSyntaxOptionClick = (e: Event, syntax: string) => {
    e.preventDefault();
    this.selectSyntax(syntax);
  };

  renderInputField = () => {
    const name = this.name;
    const value = this.value;

    if (this.multiline)
      return <textarea id={ name } name={ `${ name }.expression` } class="form-control" rows={ 3 }>{ value }</textarea>;

    return <input id={ name } name={ `${ name }.expression` } value={ value } type="text" class="form-control" />;
  };

  render() {
    const name = this.name;
    const label = this.label;
    const hint = this.hint;
    const syntaxes = ['Literal', 'JavaScript', 'Liquid'];
    const selectedSyntax = this.syntax || 'Literal';

    return (
      <host>
        <label htmlFor={ name }>{ label }</label>
        <div class="input-group">
          <input name={ `${ name }.syntax` } value={ selectedSyntax } type="hidden" />
          { this.renderInputField() }
          <div class="input-group-append">
            <button class="btn btn-primary dropdown-toggle" type="button" id={ `${ name }_dropdownMenuButton` } data-toggle="dropdown" aria-haspopup="true" aria-expanded="false">{ selectedSyntax }</button>
            <div class="dropdown-menu" aria-labelledby={ `${ name }_dropdownMenuButton` }>
              { syntaxes.map(x =>
                <a onClick={ e => this.onSyntaxOptionClick(e, x) } class="dropdown-item" href="#">{ x }</a>) }
            </div>
          </div>
        </div>
        <small class="form-text text-muted">{ hint }</small>
      </host>);
  }
}
