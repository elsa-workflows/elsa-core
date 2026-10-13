"use client";

import { Braces, Code2 } from "lucide-react";
import type { ReactNode } from "react";
import { CodeEditor } from "@/components/common/code-editor";
import { Button } from "@/components/ui/button";
import { DropdownMenu, DropdownMenuContent, DropdownMenuRadioGroup, DropdownMenuRadioItem, DropdownMenuTrigger } from "@/components/ui/dropdown-menu";
import { Tooltip, TooltipContent, TooltipTrigger } from "@/components/ui/tooltip";
import type { ActivityInput } from "@/lib/api/types/activities";
import type { InputDescriptor } from "@/lib/api/types/descriptors";
import { expressionText, isBindingSyntax, languageForSyntax, LITERAL, makeInput, OBJECT, syntaxOf } from "./expression";
import { BindingPicker } from "./editors/binding-picker";
import { expressionCompletions } from "@/modules/designer/model/data-flow";
import type { UiHintContext } from "./types";

const SYNTAX_LABELS: Record<string, string> = { Literal: "Literal", JavaScript: "JavaScript", CSharp: "C#", Python: "Python", Liquid: "Liquid", Object: "JSON", Variable: "Variable", Input: "Workflow input" };

/**
 * Wraps a literal control with a syntax switcher. In `Literal` mode the child control edits the raw
 * value; in any other mode a code editor edits the expression text. This is the single place that
 * knows how wrapped inputs are encoded, so individual UI-hint editors only deal with literal values.
 */
export function ExpressionEditor({
  descriptor,
  value,
  onChange,
  syntaxes,
  readOnly,
  children,
  /** Syntaxes allowed for this control (e.g. a JSON editor offers Object as its literal). */
  literalSyntax = LITERAL,
  context,
}: {
  descriptor: InputDescriptor;
  value: ActivityInput | undefined;
  onChange: (value: ActivityInput | undefined) => void;
  syntaxes: string[];
  readOnly?: boolean;
  children: (literal: { value: unknown; onChange: (v: unknown) => void }) => ReactNode;
  literalSyntax?: typeof LITERAL | typeof OBJECT;
  context?: UiHintContext;
}) {
  const current = syntaxOf(value, descriptor);
  const language = languageForSyntax(current);
  const completionLanguage = language === "javascript" || language === "csharp" || language === "python" || language === "liquid" ? language : "plaintext";
  const completions = context ? expressionCompletions(context.variables, context.inputs ?? [], context.activities ?? [], completionLanguage) : [];
  const isLiteral = current === LITERAL || current === OBJECT || current === "Json";
  // Variable / Input bind to a workflow member and are edited with a picker, never as text.
  const isBinding = isBindingSyntax(current);
  const options = syntaxes.includes(literalSyntax) ? syntaxes : [literalSyntax, ...syntaxes.filter((s) => s !== LITERAL)];

  const setSyntax = (syntax: string) => {
    if (syntax === current) return;
    // A binding holds an object, so its text form is meaningless to carry into a code editor.
    const text = isBinding ? "" : expressionText(value?.expression);
    if (isBindingSyntax(syntax)) onChange(makeInput(descriptor, { type: syntax, value: undefined }));
    else if (syntax === LITERAL || syntax === OBJECT) onChange(makeInput(descriptor, { type: syntax, value: syntax === OBJECT ? tryParse(text) : isBinding ? undefined : value?.expression?.value }));
    else onChange(makeInput(descriptor, { type: syntax, value: isLiteral || isBinding ? "" : text }));
  };

  return (
    <div className="flex gap-1.5">
      <div className="min-w-0 flex-1">
        {isBinding ? (
          <BindingPicker syntax={current} descriptor={descriptor} value={value} onChange={onChange} context={context} readOnly={readOnly} />
        ) : isLiteral ? (
          children({
            value: value?.expression?.value,
            onChange: (v) => onChange(makeInput(descriptor, { type: literalSyntax === OBJECT ? OBJECT : current === "Json" ? OBJECT : current, value: v })),
          })
        ) : (
          <CodeEditor
            value={expressionText(value?.expression)}
            onChange={(text) => onChange(makeInput(descriptor, { type: current, value: text }))}
            language={language}
            height="auto"
            minimal
            readOnly={readOnly}
            completions={completions}
            placeholder={`${SYNTAX_LABELS[current] ?? current} expression`}
          />
        )}
      </div>
      {options.length > 1 && (
        <DropdownMenu>
          <Tooltip>
            <TooltipTrigger render={<DropdownMenuTrigger render={<Button variant="outline" size="icon-sm" disabled={readOnly} aria-label="Syntax" />} />}>
              {isBinding ? <Braces /> : isLiteral ? <span className="text-[10px] font-semibold">{literalSyntax === OBJECT ? "{}" : "Aa"}</span> : <Code2 />}
            </TooltipTrigger>
            <TooltipContent>Syntax: {SYNTAX_LABELS[current] ?? current}</TooltipContent>
          </Tooltip>
          <DropdownMenuContent align="end">
            <DropdownMenuRadioGroup value={isLiteral ? literalSyntax : current} onValueChange={(v) => setSyntax(String(v))}>
              {options.map((s) => (
                <DropdownMenuRadioItem key={s} value={s}>
                  {SYNTAX_LABELS[s] ?? s}
                </DropdownMenuRadioItem>
              ))}
            </DropdownMenuRadioGroup>
          </DropdownMenuContent>
        </DropdownMenu>
      )}
    </div>
  );
}

function tryParse(text: string): unknown {
  try {
    return text ? JSON.parse(text) : undefined;
  } catch {
    return text;
  }
}
