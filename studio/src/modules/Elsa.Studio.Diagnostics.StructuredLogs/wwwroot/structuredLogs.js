export function scrollToBottomById(id) {
  const element = document.getElementById(id);
  if (!element) {
    return;
  }

  element.scrollTo({ top: element.scrollHeight });
}
