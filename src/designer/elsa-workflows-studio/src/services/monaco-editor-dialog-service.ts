export class MonacoEditorDialogService {
  public monacoEditor: HTMLElsaMonacoElement = null;
  public monacoEditorDialog: HTMLElsaModalDialogElement = null;
  public currentValue: string;
  public valueSaved: (val: string) => void = null;

  show(language: string, value: string, onChanged: (val: string) => void) {
    if (!this.monacoEditor || !this.monacoEditorDialog) {
      return;
    }
    this.currentValue = value;
    this.monacoEditor.language = language;
    this.monacoEditor.setValue(value);

    this.valueSaved = onChanged;
    this.monacoEditorDialog.show();
  }
  save() {
    if (this.valueSaved) {
      this.valueSaved(this.currentValue);
    }
  }
}

export const monacoEditorDialogService = new MonacoEditorDialogService();
