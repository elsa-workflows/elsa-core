export interface Port {
    id: string;
    group?: string;
}

export interface Ports
{
    items: Port[];
}

export interface Node
{
    id: string;
    ports: Ports;
    addPorts(ports: Port[]): void
    removePorts(ports: Port[]): void
}