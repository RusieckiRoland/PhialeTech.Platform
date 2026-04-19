const FORMAT_KINDS = new Set(["text", "date", "number", "currency"]);

export const SPECIAL_FIELD_KINDS = [
  "CurrentDate",
  "PageNumber",
  "TotalPages",
  "PageNumberOfTotalPages",
];

export const REPORT_SECTION_KEYS = [
  "reportHeader",
  "body",
  "reportFooter",
  "pageHeader",
  "pageFooter",
];

const SECTION_TARGET_PREFIX = "__section__:";

export function isSelectionTargetSelected(targetId, selectedTargetId) {
  if (!targetId || !selectedTargetId) {
    return false;
  }

  return String(targetId) === String(selectedTargetId);
}

export function getSectionTargetId(sectionKey) {
  return `${SECTION_TARGET_PREFIX}${String(sectionKey || "")}`;
}

export function isSectionTargetId(targetId) {
  return String(targetId || "").startsWith(SECTION_TARGET_PREFIX);
}

export function getSectionKeyFromTargetId(targetId) {
  if (!isSectionTargetId(targetId)) {
    return "";
  }

  const sectionKey = String(targetId).slice(SECTION_TARGET_PREFIX.length);
  return REPORT_SECTION_KEYS.includes(sectionKey) ? sectionKey : "";
}

export function shouldRenderPageSection(pageNumber, totalPages, skipFirstPage, skipLastPage) {
  const safePageNumber = Number.isFinite(pageNumber) ? pageNumber : 1;
  const safeTotalPages = Number.isFinite(totalPages) ? totalPages : 1;
  if (skipFirstPage && safePageNumber === 1) {
    return false;
  }

  if (skipLastPage && safePageNumber === safeTotalPages) {
    return false;
  }

  return true;
}

export function resolveActionTarget(actions, preferUnderlyingSelection = false) {
  const list = Array.isArray(actions) ? actions.filter(Boolean) : [];
  if (list.length === 0) {
    return null;
  }

  const first = list[0];
  const isSelectionAction = first.action === "select-block" || first.action === "select-page";
  if (!preferUnderlyingSelection || !isSelectionAction) {
    return first;
  }

  for (let index = 1; index < list.length; index += 1) {
    const candidate = list[index];
    if (candidate.action === "select-block" || candidate.action === "select-page") {
      return candidate;
    }
  }

  return first;
}

export function clampColumnCount(value) {
  const numeric = Number(value);
  if (!Number.isFinite(numeric)) {
    return 2;
  }

  return Math.min(Math.max(Math.round(numeric), 2), 3);
}

export function parseTableColumnsText(text) {
  return String(text || "")
    .split(/\r?\n/)
    .map((line) => line.trim())
    .filter((line) => line.length > 0)
    .map((line) => {
      const parts = line.split("|").map((part) => part.trim());
      const decimals = parts[5] ? Number(parts[5]) : null;
      return {
        header: parts[0] || "",
        binding: parts[1] || "",
        format: {
          kind: normalizeFormatKind(parts[2]),
          pattern: parts[3] || "",
          currency: parts[4] || "",
          decimals: Number.isFinite(decimals) ? decimals : null,
        },
        width: parts[6] || "",
        textAlign: parts[7] || "",
      };
    });
}

export function formatTableColumnsText(columns) {
  return (Array.isArray(columns) ? columns : [])
    .map((column) => {
      const decimals = column && column.format && column.format.decimals != null
        ? String(column.format.decimals)
        : "";
      return [
        column && column.header ? column.header : "",
        column && column.binding ? column.binding : "",
        column && column.format ? normalizeFormatKind(column.format.kind) : "text",
        column && column.format && column.format.pattern ? column.format.pattern : "",
        column && column.format && column.format.currency ? column.format.currency : "",
        decimals,
        column && column.width ? column.width : "",
        column && column.textAlign ? column.textAlign : "",
      ].join("|");
    })
    .join("\n");
}

export function parseFieldListText(text) {
  return String(text || "")
    .split(/\r?\n/)
    .map((line) => line.trim())
    .filter((line) => line.length > 0)
    .map((line) => {
      const parts = line.split("|").map((part) => part.trim());
      const decimals = parts[5] ? Number(parts[5]) : null;
      return {
        label: parts[0] || "",
        binding: parts[1] || "",
        format: {
          kind: normalizeFormatKind(parts[2]),
          pattern: parts[3] || "",
          currency: parts[4] || "",
          decimals: Number.isFinite(decimals) ? decimals : null,
        },
      };
    });
}

export function formatFieldListText(items) {
  return (Array.isArray(items) ? items : [])
    .map((item) => {
      const decimals = item && item.format && item.format.decimals != null
        ? String(item.format.decimals)
        : "";
      return [
        item && item.label ? item.label : "",
        item && item.binding ? item.binding : "",
        item && item.format ? normalizeFormatKind(item.format.kind) : "text",
        item && item.format && item.format.pattern ? item.format.pattern : "",
        item && item.format && item.format.currency ? item.format.currency : "",
        decimals,
      ].join("|");
    })
    .join("\n");
}

export function resolveSpecialFieldValue(kind, context = {}) {
  const normalizedKind = SPECIAL_FIELD_KINDS.includes(kind) ? kind : "CurrentDate";
  const locale = String(context.locale || "en");
  const currentPageNumber = Number.isFinite(context.currentPageNumber) ? context.currentPageNumber : 1;
  const totalPages = Number.isFinite(context.totalPages) ? context.totalPages : 1;
  const now = context.now instanceof Date ? context.now : new Date(context.now || Date.now());
  const datePattern = String(context.datePattern || "").trim();

  switch (normalizedKind) {
    case "PageNumber":
      return String(currentPageNumber);
    case "TotalPages":
      return String(totalPages);
    case "PageNumberOfTotalPages":
      return locale.toLowerCase().startsWith("pl")
        ? `Strona ${currentPageNumber} z ${totalPages}`
        : `Page ${currentPageNumber} of ${totalPages}`;
    case "CurrentDate":
    default:
      if (datePattern === "yyyy-MM-dd") {
        return `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, "0")}-${String(now.getDate()).padStart(2, "0")}`;
      }

      return now.toLocaleDateString(locale);
  }
}

function normalizeFormatKind(kind) {
  const raw = String(kind || "").trim().toLowerCase();
  return FORMAT_KINDS.has(raw) ? raw : "text";
}
