import { afterEach, expect, it } from "vitest";
import { culturePreferenceKey, studioI18n, translate, translationNamespace } from "./localization.js";

afterEach(async () => {
  for (const culture of ["en", "nl"]) studioI18n.removeResourceBundle(culture, translationNamespace("example"));
  await studioI18n.changeLanguage("en");
});

it("uses a module's packaged resources, falls back to its English catalog, and preserves interpolation as text", async () => {
  studioI18n.addResourceBundle("en", translationNamespace("example"), { Greeting: "Hello {{name}}", MissingDutch: "English fallback", "a.b:c": "Literal key" });
  studioI18n.addResourceBundle("nl", translationNamespace("example"), { Greeting: "Hallo {{name}}" });
  await studioI18n.changeLanguage("nl");
  expect(translate("Greeting", { name: "<workflow>" }, "example")).toBe("Hallo <workflow>");
  expect(translate("MissingDutch", undefined, "example")).toBe("English fallback");
  expect(translate("a.b:c", undefined, "example")).toBe("Literal key");
  expect(translate("Greeting")).toBe("Greeting");
});

it("separates a user's culture preference from other subjects and API servers", () => {
  expect(culturePreferenceKey("https://a.test/api/", "alice")).toBe(culturePreferenceKey("https://a.test/api", "alice"));
  expect(culturePreferenceKey("https://a.test/api", "alice")).not.toBe(culturePreferenceKey("https://a.test/api", "bob"));
  expect(culturePreferenceKey("https://a.test/api", "alice")).not.toBe(culturePreferenceKey("https://b.test/api", "alice"));
});
