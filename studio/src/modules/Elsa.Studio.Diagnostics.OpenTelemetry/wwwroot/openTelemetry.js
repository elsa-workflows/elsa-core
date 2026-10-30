export async function copyText(text) {
  if (navigator.clipboard && window.isSecureContext) {
    await navigator.clipboard.writeText(text);
    return;
  }

  const textarea = document.createElement("textarea");
  textarea.value = text;
  textarea.setAttribute("readonly", "");
  textarea.style.position = "fixed";
  textarea.style.opacity = "0";
  document.body.appendChild(textarea);
  textarea.select();

  try {
    document.execCommand("copy");
  } finally {
    document.body.removeChild(textarea);
  }
}

export async function copyEnvironmentVariables(variables) {
  await copyText(formatEnvironmentVariables(variables));
}

export function downloadText(filename, text, contentType = "text/plain") {
  const blob = new Blob([text ?? ""], { type: contentType });
  const url = URL.createObjectURL(blob);
  const anchor = document.createElement("a");
  anchor.href = url;
  anchor.download = filename || "elsa-otel-export.txt";
  anchor.style.display = "none";
  document.body.appendChild(anchor);

  try {
    anchor.click();
  } finally {
    document.body.removeChild(anchor);
    URL.revokeObjectURL(url);
  }
}

export function formatEnvironmentVariables(variables) {
  return (variables ?? [])
    .filter(variable => readValue(variable, "name"))
    .map(variable => `export ${readValue(variable, "name")}=${quoteShellValue(readValue(variable, "value") ?? "")}`)
    .join("\n");
}

function readValue(source, key) {
  return source?.[key] ?? source?.[capitalize(key)];
}

function capitalize(value) {
  return value.charAt(0).toUpperCase() + value.slice(1);
}

function quoteShellValue(value) {
  return `"${String(value).replaceAll("\\", "\\\\").replaceAll("\"", "\\\"").replaceAll("$", "\\$").replaceAll("`", "\\`")}"`;
}
