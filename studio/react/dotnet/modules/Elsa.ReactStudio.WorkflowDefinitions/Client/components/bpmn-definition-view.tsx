
import { useTranslation } from "@elsa-workflows/react-studio-sdk/localization";
import { useState } from "react";
import Link from "@elsa-workflows/react-studio-sdk/navigation/link";
import type { WorkflowDefinition } from "@elsa-workflows/react-studio-api-client/types/workflow-definitions";
import { Button } from "@elsa-workflows/react-studio-ui/button";
import { PageHeader } from "@elsa-workflows/react-studio-ui/common/page-header";
import { JsonViewer } from "@elsa-workflows/react-studio-ui/common/json-viewer";
import { BpmnExportDialog } from "./bpmn-dialogs";

/** The generic designer normalizes unknown roots. Keep BPMN outside that workspace
 * until the dedicated canvas in #8929 can edit its native process model. */
export function BpmnDefinitionView({ definition }: { definition: WorkflowDefinition }) {
  const _t = useTranslation();
  const [exporting, setExporting] = useState(false);
  return <div className="p-4">
    <PageHeader title={definition.name ?? definition.definitionId} eyebrow={_t("Read-only BPMN definition")}
      description={_t("This Preview preserves the BPMN process without editing or autosaving it. BPMN canvas editing is not available yet.")}
      actions={<><Link href="/workflows/definitions">{_t("Workflow definitions")}</Link>{definition.isLatest && <Button onClick={() => setExporting(true)}>{_t("Export BPMN")}</Button>}</>} />
    <p>{_t("Version")}{" "}{definition.version}</p>
    <JsonViewer value={definition} />
    {exporting && <BpmnExportDialog definitionId={definition.definitionId} onClose={() => setExporting(false)} />}
  </div>;
}
