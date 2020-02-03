export class FormUpdater {
    static updateEditor(activity, formData) {
        const newState = Object.assign({}, activity.state);
        formData.forEach((value, key) => {
            newState[key] = value;
        });
        return Object.assign({}, activity, { state: newState });
    }
}
