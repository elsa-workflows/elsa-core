export const WorkflowDefinitionPropertyValidationMessages = {
    PropertyKeyNameError: "Only letters and digits are allowed",
    PropertyUniqueError: "Property name should be unique. Duplications: "
}

export interface ValidationStatus {
    valid?: boolean;
    message?: string;
}

export interface ValidationStatusIdentifyed {
    id: number | string;
    validation: ValidationStatus
}

export class WorkflowDefinitionPropertyValidationErrors {
    PropertyKeyNameError?: Array<ValidationStatusIdentifyed>;
    PropertyUniqueError?: ValidationStatus;
}