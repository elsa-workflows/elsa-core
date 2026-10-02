// Mirrors the X6Graph DTO shape that the Blazor side already serializes
// (see Elsa.Studio.Workflows.Designer/Models/X6*.cs). Keeping the same
// payload lets the React Flow designer be a drop-in replacement on the
// transport layer while the renderer changes underneath.

export interface ElsaActivity {
    id: string;
    type: string;
    version: number;
    metadata?: any;
    [key: string]: any;
}

export interface ElsaPort {
    id: string;
    group?: string;
    type?: string;
    position?: string;
    // X6 carries the port's display name in attrs.text.text (see ActivityMapper).
    // We surface it as the visible label next to the handle.
    attrs?: { text?: { text?: string } } & Record<string, any>;
}

export interface ElsaPorts {
    items: ElsaPort[];
}

export interface ElsaPosition {
    x: number;
    y: number;
}

export interface ElsaSize {
    width: number;
    height: number;
}

export interface ElsaActivityStats {
    faulted?: boolean;
    blocked?: boolean;
    completed?: number;
    started?: number;
    uncompleted?: number;
    metadata?: { [key: string]: any };
}

export interface ElsaActivityNode {
    id: string;
    shape: string;
    position: ElsaPosition;
    size: ElsaSize;
    data: ElsaActivity;
    ports: ElsaPorts;
    activityStats?: ElsaActivityStats;
}

export interface ElsaEndpoint {
    cell: string;
    port: string;
}

export interface ElsaEdge {
    source: ElsaEndpoint;
    target: ElsaEndpoint;
    shape?: string;
    vertices?: ElsaPosition[];
}

export type DesignerMode = 'flowchart' | 'sequence';
export type SequenceLayoutOrientation = 'vertical' | 'horizontal';

export interface ElsaGraph {
    nodes: ElsaActivityNode[];
    edges: ElsaEdge[];
    layoutOrientation?: SequenceLayoutOrientation;
}

export interface DotNetComponentRef {
    invokeMethodAsync<T>(methodName: string, ...args: any[]): Promise<T>;
    dispose(): void;
}

export interface ReactDesignerSettings {
    readOnly?: boolean;
    mode?: DesignerMode;
    grid?: {
        visible?: boolean;
        color?: string;
        size?: number;
    };
}

// Mirrors Elsa.Studio.Workflows.Designer.Models.ActivityDescriptorDto.
// PascalCase keys because Blazor JSInterop serializes records with the
// default camelCase policy of the runtime — but our serializer is the
// JSInterop default which produces camelCase. Keep keys lowerCamel.
export interface ActivityDescriptorDto {
    typeName: string;
    version: number;
    name: string;
    displayName: string;
    category?: string | null;
    description?: string | null;
    color: string;
    icon?: string | null;
}
