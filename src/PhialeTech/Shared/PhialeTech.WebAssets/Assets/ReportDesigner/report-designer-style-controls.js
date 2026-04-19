export const FONT_FAMILY_OPTIONS = [
  { value: "", labelKey: "Option.Default" },
  { value: "Segoe UI, sans-serif", label: "Segoe UI" },
  { value: "Arial, sans-serif", label: "Arial" },
  { value: "Tahoma, sans-serif", label: "Tahoma" },
  { value: "Verdana, sans-serif", label: "Verdana" },
  { value: "Georgia, serif", label: "Georgia" },
  { value: "'Courier New', monospace", label: "Courier New" },
];

export const FONT_WEIGHT_OPTIONS = [
  { value: "", labelKey: "Option.Default" },
  { value: "400", labelKey: "Option.FontWeight.Normal" },
  { value: "500", labelKey: "Option.FontWeight.Medium" },
  { value: "600", labelKey: "Option.FontWeight.Semibold" },
  { value: "700", labelKey: "Option.FontWeight.Bold" },
];

export const FONT_STYLE_OPTIONS = [
  { value: "", labelKey: "Option.Default" },
  { value: "normal", labelKey: "Option.FontStyle.Normal" },
  { value: "italic", labelKey: "Option.FontStyle.Italic" },
  { value: "oblique", labelKey: "Option.FontStyle.Oblique" },
];

export const TEXT_DECORATION_OPTIONS = [
  { value: "", labelKey: "Option.Default" },
  { value: "none", labelKey: "Option.TextDecoration.None" },
  { value: "underline", labelKey: "Option.TextDecoration.Underline" },
  { value: "line-through", labelKey: "Option.TextDecoration.LineThrough" },
];

export const TEXT_ALIGN_OPTIONS = [
  { value: "", labelKey: "Option.Default" },
  { value: "left", labelKey: "Option.TextAlign.Left" },
  { value: "center", labelKey: "Option.TextAlign.Center" },
  { value: "right", labelKey: "Option.TextAlign.Right" },
  { value: "justify", labelKey: "Option.TextAlign.Justify" },
];

export const DISPLAY_OPTIONS = [
  { value: "", labelKey: "Option.Default" },
  { value: "block", labelKey: "Option.Display.Block" },
  { value: "inline-block", labelKey: "Option.Display.InlineBlock" },
  { value: "flex", labelKey: "Option.Display.Flex" },
  { value: "grid", labelKey: "Option.Display.Grid" },
];

export const BORDER_STYLE_OPTIONS = [
  { value: "solid", labelKey: "Option.BorderStyle.Solid" },
  { value: "dashed", labelKey: "Option.BorderStyle.Dashed" },
  { value: "dotted", labelKey: "Option.BorderStyle.Dotted" },
  { value: "double", labelKey: "Option.BorderStyle.Double" },
];

export const SIZE_UNITS = ["px", "mm", "%", "rem"];

export const BOX_SPACING_UNITS = ["px", "mm", "%", "rem"];

export const BASIC_COLOR_SWATCHES = [
  "#0f172a",
  "#334155",
  "#64748b",
  "#0f766e",
  "#2563eb",
  "#7c3aed",
  "#dc2626",
  "#ea580c",
  "#f59e0b",
  "#16a34a",
  "#ffffff",
  "#000000",
];

export const DIMENSION_OPTIONS = {
  width: [
    { value: "", labelKey: "Option.Default" },
    { value: "auto", labelKey: "Option.Dimension.Auto" },
    { value: "100%", labelKey: "Option.Dimension.FullWidth" },
    { value: "120px", label: "120px" },
    { value: "240px", label: "240px" },
    { value: "320px", label: "320px" },
  ],
  minWidth: [
    { value: "", labelKey: "Option.Default" },
    { value: "0", label: "0" },
    { value: "120px", label: "120px" },
    { value: "240px", label: "240px" },
  ],
  maxWidth: [
    { value: "", labelKey: "Option.Default" },
    { value: "none", labelKey: "Option.Dimension.None" },
    { value: "100%", labelKey: "Option.Dimension.FullWidth" },
    { value: "320px", label: "320px" },
    { value: "480px", label: "480px" },
  ],
  height: [
    { value: "", labelKey: "Option.Default" },
    { value: "auto", labelKey: "Option.Dimension.Auto" },
    { value: "40px", label: "40px" },
    { value: "64px", label: "64px" },
    { value: "120px", label: "120px" },
  ],
};

const LENGTH_TOKEN_PATTERN = /^(-?\d+(?:\.\d+)?)(px|mm|%|rem)?$/i;
const HEX_COLOR_PATTERN = /^#(?:[0-9a-f]{3}|[0-9a-f]{6})$/i;
const BORDER_STYLE_VALUES = new Set(BORDER_STYLE_OPTIONS.map((option) => option.value));

export function getDimensionOptions(fieldName) {
  return DIMENSION_OPTIONS[fieldName] || [{ value: "", labelKey: "Option.Default" }];
}

export function normalizeHexColor(value) {
  const raw = String(value || "").trim();
  if (!HEX_COLOR_PATTERN.test(raw)) {
    return "";
  }

  const normalized = raw.toLowerCase();
  if (normalized.length === 4) {
    return `#${normalized[1]}${normalized[1]}${normalized[2]}${normalized[2]}${normalized[3]}${normalized[3]}`;
  }

  return normalized;
}

export function toColorInputValue(value, fallback = "#0f172a") {
  return normalizeHexColor(value)
    || normalizeHexColor(fallback)
    || "#0f172a";
}

export function parseLengthValue(value, fallbackUnit = "px") {
  const raw = String(value || "").trim();
  if (!raw) {
    return {
      kind: "length",
      amount: "",
      unit: fallbackUnit,
    };
  }

  const match = raw.match(LENGTH_TOKEN_PATTERN);
  if (!match) {
    return {
      kind: "raw",
      raw,
    };
  }

  return {
    kind: "length",
    amount: match[1],
    unit: (match[2] || fallbackUnit).toLowerCase(),
  };
}

export function composeLengthValue(model, fallbackUnit = "px") {
  if (!model || typeof model !== "object") {
    return "";
  }

  if (model.kind === "raw") {
    return String(model.raw || "").trim();
  }

  const amount = String(model.amount || "").trim();
  if (!amount) {
    return "";
  }

  if (amount === "0") {
    return "0";
  }

  const unit = String(model.unit || fallbackUnit || "px").trim() || "px";
  return `${amount}${unit}`;
}

export function parseBoxSpacingValue(value, fallbackUnit = "px") {
  const raw = String(value || "").trim();
  if (!raw) {
    return {
      kind: "box",
      top: "",
      right: "",
      bottom: "",
      left: "",
      unit: fallbackUnit,
    };
  }

  const tokens = raw.split(/\s+/).filter(Boolean);
  if (tokens.length < 1 || tokens.length > 4) {
    return { kind: "raw", raw };
  }

  const expanded = expandBoxTokens(tokens);
  const parsedTokens = expanded.map((token) => token.match(LENGTH_TOKEN_PATTERN));
  if (parsedTokens.some((match) => !match)) {
    return { kind: "raw", raw };
  }

  const discoveredUnit = parsedTokens
    .map((match) => (match[2] || "").toLowerCase())
    .find((unit) => !!unit);
  const unit = discoveredUnit || fallbackUnit;
  if (parsedTokens.some((match) => (match[2] || unit).toLowerCase() !== unit)) {
    return { kind: "raw", raw };
  }

  return {
    kind: "box",
    top: parsedTokens[0][1],
    right: parsedTokens[1][1],
    bottom: parsedTokens[2][1],
    left: parsedTokens[3][1],
    unit,
  };
}

export function composeBoxSpacingValue(model, fallbackUnit = "px") {
  if (!model || typeof model !== "object") {
    return "";
  }

  if (model.kind === "raw") {
    return String(model.raw || "").trim();
  }

  const unit = String(model.unit || fallbackUnit || "px").trim() || "px";
  const tokens = [model.top, model.right, model.bottom, model.left]
    .map((amount) => String(amount || "").trim())
    .map((amount) => formatLengthToken(amount, unit));

  if (tokens.every((token) => !token)) {
    return "";
  }

  return tokens.map((token) => token || "0").join(" ");
}

export function parseBorderValue(value) {
  const raw = String(value || "").trim();
  if (!raw) {
    return createDefaultBorderModel(false);
  }

  if (raw.toLowerCase() === "none") {
    return createDefaultBorderModel(false);
  }

  const tokens = raw.split(/\s+/).filter(Boolean);
  const widthToken = tokens.find((token) => LENGTH_TOKEN_PATTERN.test(token)) || "1px";
  const styleToken = tokens.find((token) => BORDER_STYLE_VALUES.has(token.toLowerCase())) || "solid";
  const colorTokens = tokens.filter((token) => token !== widthToken && token.toLowerCase() !== styleToken.toLowerCase());
  const width = parseLengthValue(widthToken);
  if (width.kind === "raw") {
    return { kind: "raw", raw };
  }

  return {
    kind: "border",
    enabled: true,
    width: width.amount || "1",
    unit: width.unit || "px",
    style: styleToken.toLowerCase(),
    color: colorTokens.join(" ") || "#cbd5e1",
  };
}

export function composeBorderValue(model) {
  if (!model || typeof model !== "object") {
    return "";
  }

  if (model.kind === "raw") {
    return String(model.raw || "").trim();
  }

  if (!model.enabled || !model.style || model.style === "none") {
    return "none";
  }

  const width = composeLengthValue({
    kind: "length",
    amount: model.width || "1",
    unit: model.unit || "px",
  }, "px");

  return [width || "1px", model.style, model.color || "#cbd5e1"]
    .filter(Boolean)
    .join(" ");
}

function expandBoxTokens(tokens) {
  if (tokens.length === 1) {
    return [tokens[0], tokens[0], tokens[0], tokens[0]];
  }

  if (tokens.length === 2) {
    return [tokens[0], tokens[1], tokens[0], tokens[1]];
  }

  if (tokens.length === 3) {
    return [tokens[0], tokens[1], tokens[2], tokens[1]];
  }

  return tokens.slice(0, 4);
}

function createDefaultBorderModel(enabled) {
  return {
    kind: "border",
    enabled,
    width: "1",
    unit: "px",
    style: "solid",
    color: "#cbd5e1",
  };
}

function formatLengthToken(amount, unit) {
  if (!amount) {
    return "";
  }

  return amount === "0" ? "0" : `${amount}${unit}`;
}
