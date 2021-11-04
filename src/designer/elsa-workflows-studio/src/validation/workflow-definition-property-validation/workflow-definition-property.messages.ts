export const WorkflowDefinitionPropertyValidationMessages = {
    PropertyKeyNameError: "Only letters and digits are allowed",
    PropertyUniqueError: "Property name should be unique. Duplications: "
}

export interface ValidationStatus {
    valid?: boolean;
    message?: string;
}

export class WorkflowDefinitionPropertyValidationErrors {
    PropertyKeyNameError?: ValidationStatus;
    PropertyUniqueError?: ValidationStatus;
}