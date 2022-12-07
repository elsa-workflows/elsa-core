export class MonacoEditorDialogService {
  public monacoEditor: HTMLElsaMonacoElement = null;
  public monacoEditorDialog: HTMLElsaModalDialogElement = null;
  public valueChanged: (e: CustomEvent) => void = null;

  show(language: string, value: string, onChanged: (e: CustomEvent) => void) {
    if (!this.monacoEditor || !this.monacoEditorDialog) {
      return;
    }
    this.monacoEditor.language = language;
    this.monacoEditor.setValue(value);

    this.valueChanged = onChanged;
    this.monacoEditorDialog.show();
  }
}

export const monacoEditorDialogService = new MonacoEditorDialogService();
