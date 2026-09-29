export function scrollToBottomById(id) {
  const element = document.getElementById(id);
  if (!element) {
    return;
  }

  element.scrollTo({ top: element.scrollHeight });
}

export function downloadTextFile(fileName, content, contentType) {
  const blob = new Blob([content], { type: contentType || "text/plain;charset=utf-8" });
  const url = URL.createObjectURL(blob);
  const anchor = document.createElement("a");

  anchor.href = url;
  anchor.download = fileName;
  anchor.style.display = "none";
  document.body.appendChild(anchor);
  anchor.click();
  anchor.remove();
  setTimeout(() => URL.revokeObjectURL(url), 1000);
}
