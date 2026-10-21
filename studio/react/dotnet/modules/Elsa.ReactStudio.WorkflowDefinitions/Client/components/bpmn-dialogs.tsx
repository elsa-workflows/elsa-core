import { useEffect, useRef, useState } from "react";
import { useElsaClient } from "@elsa-workflows/react-studio-api-client/react";
import { ApiError } from "@elsa-workflows/react-studio-api-client/http";
import type { BpmnImportAnalysis, BpmnImportResult } from "@elsa-workflows/react-studio-api-client/types/bpmn";
import { Button } from "@elsa-workflows/react-studio-ui/button";
import { Input } from "@elsa-workflows/react-studio-ui/input";
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from "@elsa-workflows/react-studio-ui/dialog";
import { ErrorAlert } from "@elsa-workflows/react-studio-ui/common/error-alert";
import { downloadBlob } from "@elsa-workflows/react-studio-sdk/utils/download";

/** Stable server codes select explanations; unknown envelopes retain the server message. */
function Refusal({ error }: { error: unknown }) {
  const raw = error instanceof ApiError ? error.problem.raw : undefined;
  const envelope = raw && typeof raw === "object" ? raw as Record<string, unknown> : {};
  const data = envelope.data && typeof envelope.data === "object" ? envelope.data as Record<string, unknown> : {};
  const strings = (value: unknown) => Array.isArray(value) ? value.filter((item): item is string => typeof item === "string") : [];
  const capabilities = strings(data.capabilities);
  const elements = strings(data.elementIds);
  const messages: Record<string, string> = {
    "bpmn.import.capability-unsupported": "This deployment does not support every BPMN capability the document requires. Import was refused.",
    "bpmn.import.condition-unsupported": "These sequence flows carry expression conditions that Elsa does not evaluate. Import was refused to prevent unconditional routing. Remove the conditions or use conditionOutcome.",
    "bpmn.export.not-imported": "This workflow has no recorded BPMN source to export.",
    "bpmn.export.source-version-unknown": "The recorded BPMN source version is unknown, so a current export cannot be verified.",
    "bpmn.export.source-stale": "The workflow changed after import. Export was refused because the recorded BPMN source is stale.",
  };
  const message = typeof envelope.code === "string" ? messages[envelope.code] : undefined;
  return <>
    {message && <p role="alert">{message}</p>}
    {!!capabilities.length && <div><h3>Missing capabilities</h3><ul>{capabilities.map(item => <li key={item}>{item}</li>)}</ul></div>}
    {!!elements.length && <div><h3>{envelope.code === "bpmn.import.condition-unsupported" ? "Conditional sequence flows" : "Offending elements"}</h3><ul>{elements.map(item => <li key={item}>{item}</li>)}</ul></div>}
    <ErrorAlert error={error} title="BPMN request failed" />
  </>;
}

function useRequest() {
  const request = useRef<AbortController | undefined>(undefined);
  useEffect(() => () => request.current?.abort(), []);
  return () => { request.current?.abort(); return (request.current = new AbortController()).signal; };
}

/** Mounted only while open: closing cancels work and discards the selected document. */
export function BpmnImportDialog({ onClose, onImported }: { onClose: () => void; onImported: (result: BpmnImportResult) => void }) {
  const client = useElsaClient();
  const begin = useRequest();
  const [file, setFile] = useState<File>();
  const [analysis, setAnalysis] = useState<BpmnImportAnalysis>();
  const [processId, setProcessId] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<unknown>();
  const analyze = async (document: File | undefined) => {
    setAnalysis(undefined); setError(undefined); setFile(undefined); setProcessId("");
    if (!document) return;
    if (!document.name.toLowerCase().endsWith(".bpmn") || document.size > 10 * 1024 * 1024) { setError(new Error("Select a .bpmn document no larger than 10 MB.")); return; }
    setBusy(true);
    try {
      const result = await client.bpmn.analyze(document, begin());
      setFile(document); setAnalysis(result); setProcessId(result.processIds.length === 1 ? result.processIds[0] : "");
    } catch (failure) { setError(failure); } finally { setBusy(false); }
  };
  const submit = async () => {
    if (!file || !analysis || (analysis.processIds.length > 1 && !processId)) return;
    setBusy(true); setError(undefined);
    try { onImported(await client.bpmn.import(file, { processId: processId || undefined }, begin())); }
    catch (failure) { setError(failure); } finally { setBusy(false); }
  };
  return <Dialog open onOpenChange={open => { if (!open) onClose(); }}><DialogContent className="max-h-[85vh] overflow-y-auto">
    <DialogHeader><DialogTitle>Import BPMN</DialogTitle><DialogDescription>Review findings before importing. Dropped elements will not run; Degraded elements do not run exactly as authored.</DialogDescription></DialogHeader>
    <label htmlFor="bpmn-file">BPMN document (up to 10 MB)</label>
    <Input id="bpmn-file" type="file" accept=".bpmn" disabled={busy} onChange={event => void analyze(event.target.files?.[0])} />
    {busy && <p role="status">Processing BPMN document…</p>}
    <Refusal error={error} />
    {analysis && <>
      {analysis.processIds.length > 1 && <label>Process<select aria-label="Process" value={processId} disabled={busy} onChange={event => setProcessId(event.target.value)}><option value="">Select a process</option>{analysis.processIds.map(id => <option key={id} value={id}>{id}</option>)}</select></label>}
      {analysis.issues.length === 0 ? <p>No findings. This document reads without loss.</p> : [...new Set(analysis.issues.map(issue => issue.severity))].map(severity => <section key={severity}><h3>{severity}</h3><ul>{analysis.issues.filter(issue => issue.severity === severity).map((issue, index) => <li key={index}>{issue.elementId && <strong>[{issue.elementId}] </strong>}{issue.message}{issue.processId && <span> (process {issue.processId})</span>}</li>)}</ul></section>)}
    </>}
    <DialogFooter><Button variant="outline" onClick={onClose}>Cancel</Button><Button disabled={busy || !analysis || (analysis.processIds.length > 1 && !processId)} onClick={() => void submit()}>Import</Button></DialogFooter>
  </DialogContent></Dialog>;
}

export function BpmnExportDialog({ definitionId, onClose }: { definitionId: string; onClose: () => void }) {
  const client = useElsaClient();
  const begin = useRequest();
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<unknown>();
  const submit = async () => {
    setBusy(true); setError(undefined);
    try { downloadBlob(await client.bpmn.export(definitionId, begin()), `${definitionId}.bpmn`); onClose(); }
    catch (failure) { setError(failure); } finally { setBusy(false); }
  };
  return <Dialog open onOpenChange={open => { if (!open) onClose(); }}><DialogContent>
    <DialogHeader><DialogTitle>Export BPMN</DialogTitle><DialogDescription>The exported file contains this workflow&apos;s binding configuration and expressions. Only current, unchanged imported BPMN source can be exported.</DialogDescription></DialogHeader>
    <Refusal error={error} />
    <DialogFooter><Button variant="outline" onClick={onClose}>Cancel</Button><Button disabled={busy} onClick={() => void submit()}>{busy ? "Exporting…" : "Export"}</Button></DialogFooter>
  </DialogContent></Dialog>;
}
