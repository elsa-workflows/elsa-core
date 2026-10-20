import { Component, type ComponentType, type ReactNode } from "react";

interface BoundaryProps {
  moduleId: string;
  slotId: string;
  onError: (error: Error) => void;
  children: ReactNode;
}

/** Keeps a failed module contribution from unmounting the surrounding Studio. */
class ModuleBoundary extends Component<BoundaryProps, { failed: boolean }> {
  state = { failed: false };

  static getDerivedStateFromError() { return { failed: true }; }

  componentDidCatch(error: Error) { this.props.onError(error); }

  render() {
    return this.state.failed
      ? <p role="alert">Module {this.props.moduleId} could not render {this.props.slotId}.</p>
      : this.props.children;
  }
}

/** Wraps a module-owned component without adding a DOM element or creating another context instance. */
export function isolateModuleComponent<P extends object>(component: ComponentType<P>, moduleId: string,
  slotId: string, onError: (error: Error) => void): ComponentType<P> {
  const Contribution = component;
  return function IsolatedContribution(props: P) {
    return <ModuleBoundary moduleId={moduleId} slotId={slotId} onError={onError}>
      <Contribution {...props} />
    </ModuleBoundary>;
  };
}
