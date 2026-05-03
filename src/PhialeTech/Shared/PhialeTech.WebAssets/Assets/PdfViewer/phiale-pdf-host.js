import { getDocument, GlobalWorkerOptions } from "./build/pdf.mjs";
import {
  EventBus,
  PDFFindController,
  PDFLinkService,
  PDFViewer,
} from "./web/pdf_viewer.mjs";

GlobalWorkerOptions.workerSrc = "./build/pdf.worker.mjs";

const viewerShell = document.getElementById("viewerShell");
const viewerContainer = document.getElementById("viewerContainer");
const viewerElement = document.getElementById("viewer");
const toolbarStatus = document.getElementById("toolbarStatus");
const fileInput = document.getElementById("fileInput");

const openButton = document.getElementById("openButton");
const downloadButton = document.getElementById("downloadButton");
const printButton = document.getElementById("printButton");
const rotateLeftButton = document.getElementById("rotateLeftButton");
const rotateRightButton = document.getElementById("rotateRightButton");
const previousPageButton = document.getElementById("previousPageButton");
const nextPageButton = document.getElementById("nextPageButton");
const pageNumberInput = document.getElementById("pageNumberInput");
const pageCountLabel = document.getElementById("pageCountLabel");
const zoomOutButton = document.getElementById("zoomOutButton");
const zoomInButton = document.getElementById("zoomInButton");
const zoomSelect = document.getElementById("zoomSelect");
const selectToolButton = document.getElementById("selectToolButton");
const handToolButton = document.getElementById("handToolButton");

const FIND_SOURCE = { source: "PhialeTech.PdfViewer" };

const ICONS = {
  open: `
    <svg viewBox="0 0 24 24" aria-hidden="true">
      <path d="M4 19a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V9a2 2 0 0 0-2-2h-7l-2-2H6a2 2 0 0 0-2 2z"></path>
    </svg>`,
  download: `
    <svg viewBox="0 0 24 24" aria-hidden="true">
      <path d="M12 3v12"></path>
      <path d="m7 10 5 5 5-5"></path>
      <path d="M5 21h14"></path>
    </svg>`,
  print: `
    <svg viewBox="0 0 24 24" aria-hidden="true">
      <path d="M6 9V2h12v7"></path>
      <path d="M6 18H4a2 2 0 0 1-2-2v-5a2 2 0 0 1 2-2h16a2 2 0 0 1 2 2v5a2 2 0 0 1-2 2h-2"></path>
      <path d="M6 14h12v8H6z"></path>
    </svg>`,
  rotateCcw: `
    <svg viewBox="0 0 24 24" aria-hidden="true">
      <path d="M3 2v6h6"></path>
      <path d="M3 8a9 9 0 1 1 2.6 6.4"></path>
    </svg>`,
  rotateCw: `
    <svg viewBox="0 0 24 24" aria-hidden="true">
      <path d="M21 2v6h-6"></path>
      <path d="M21 8a9 9 0 1 0-2.6 6.4"></path>
    </svg>`,
  chevronLeft: `
    <svg viewBox="0 0 24 24" aria-hidden="true">
      <path d="m15 18-6-6 6-6"></path>
    </svg>`,
  chevronRight: `
    <svg viewBox="0 0 24 24" aria-hidden="true">
      <path d="m9 18 6-6-6-6"></path>
    </svg>`,
  zoomOut: `
    <svg viewBox="0 0 24 24" aria-hidden="true">
      <circle cx="11" cy="11" r="7"></circle>
      <path d="M8 11h6"></path>
      <path d="m20 20-3.5-3.5"></path>
    </svg>`,
  zoomIn: `
    <svg viewBox="0 0 24 24" aria-hidden="true">
      <circle cx="11" cy="11" r="7"></circle>
      <path d="M11 8v6"></path>
      <path d="M8 11h6"></path>
      <path d="m20 20-3.5-3.5"></path>
    </svg>`,
  select: `
    <svg viewBox="0 0 24 24" aria-hidden="true">
      <path d="m6 4 12 8-5 1 2 5-2 1-2-5-5 4z"></path>
    </svg>`,
  hand: `
    <svg viewBox="0 0 24 24" aria-hidden="true">
      <path d="M8 11V5a1 1 0 0 1 2 0v5"></path>
      <path d="M12 11V4a1 1 0 0 1 2 0v7"></path>
      <path d="M16 11V6a1 1 0 0 1 2 0v8"></path>
      <path d="M6 12.5V10a1 1 0 0 1 2 0v2.5"></path>
      <path d="M6 12.5c0 6.5 3 8.5 6 8.5s6-2 6-7.5V11"></path>
    </svg>`,
};

const state = {
  readyAnnounced: false,
  isPrintInProgress: false,
  theme: "light",
  currentSource: "",
  currentDisplayName: "",
  currentSearchQuery: "",
  loadingTask: null,
  pdfDocument: null,
  eventBus: null,
  linkService: null,
  findController: null,
  pdfViewer: null,
  currentPage: 1,
  pageCount: 0,
  currentScaleFactor: 1,
  currentScaleValue: "page-width",
  pagesRotation: 0,
  handToolEnabled: false,
  localObjectUrl: "",
  dragPointerId: null,
  dragStartX: 0,
  dragStartY: 0,
  dragScrollLeft: 0,
  dragScrollTop: 0,
};

function postToHost(message) {
  try {
    if (window.PhialeWebHost && typeof window.PhialeWebHost.postMessage === "function") {
      return window.PhialeWebHost.postMessage(message);
    }

    if (window.chrome && window.chrome.webview && typeof window.chrome.webview.postMessage === "function") {
      return window.chrome.webview.postMessage(JSON.stringify(message));
    }
  } catch (_) {
    // best-effort bridge
  }

  return false;
}

function normalizeTheme(theme) {
  return String(theme || "").trim().toLowerCase() === "dark" ? "dark" : "light";
}

function applyTheme(theme) {
  state.theme = normalizeTheme(theme);
  document.documentElement.dataset.theme = state.theme;
  document.body.dataset.theme = state.theme;
  viewerShell.dataset.theme = state.theme;
}

function setStatus(text) {
  toolbarStatus.textContent = text || "";
}

function stringifyError(error) {
  if (!error) {
    return "";
  }

  const parts = [];
  if (error.message) {
    parts.push(error.message);
  }

  if (error.stack) {
    parts.push(error.stack);
  }

  return parts.join("\n");
}

function showError(message, detail) {
  setStatus(detail ? `${message} ${detail}` : message);
  postToHost({
    type: "pdf.error",
    message: message || "Unknown PDF viewer error.",
    detail: detail || "",
  });
}

function notifyReady() {
  if (state.readyAnnounced) {
    return;
  }

  state.readyAnnounced = true;

  try {
    if (window.PhialeWebHost && typeof window.PhialeWebHost.notifyReady === "function") {
      window.PhialeWebHost.notifyReady({
        detail: "pdf viewer ready",
      });
      return;
    }
  } catch (_) {
    // best-effort bridge
  }

  postToHost({
    type: "pdf.ready",
    detail: "pdf viewer ready",
  });
}

function subscribeEventBus(name, handler) {
  if (!state.eventBus) {
    return;
  }

  if (typeof state.eventBus.on === "function") {
    state.eventBus.on(name, handler);
    return;
  }

  if (typeof state.eventBus._on === "function") {
    state.eventBus._on(name, handler);
  }
}

function updateToolbarIcons() {
  openButton.innerHTML = ICONS.open;
  downloadButton.innerHTML = ICONS.download;
  printButton.innerHTML = ICONS.print;
  rotateLeftButton.innerHTML = ICONS.rotateCcw;
  rotateRightButton.innerHTML = ICONS.rotateCw;
  previousPageButton.innerHTML = ICONS.chevronLeft;
  nextPageButton.innerHTML = ICONS.chevronRight;
  zoomOutButton.innerHTML = ICONS.zoomOut;
  zoomInButton.innerHTML = ICONS.zoomIn;
  selectToolButton.innerHTML = ICONS.select;
  handToolButton.innerHTML = ICONS.hand;
}

function renderToolbarState() {
  const hasDocument = !!state.pdfDocument;
  const canNavigate = hasDocument && state.pageCount > 0;

  previousPageButton.disabled = !canNavigate || state.currentPage <= 1;
  nextPageButton.disabled = !canNavigate || state.currentPage >= state.pageCount;
  pageNumberInput.disabled = !canNavigate;
  zoomOutButton.disabled = !hasDocument;
  zoomInButton.disabled = !hasDocument;
  zoomSelect.disabled = !hasDocument;
  rotateLeftButton.disabled = !hasDocument;
  rotateRightButton.disabled = !hasDocument;
  downloadButton.disabled = !state.currentSource;
  printButton.disabled = !hasDocument;

  pageNumberInput.value = String(state.currentPage || 1);
  pageCountLabel.textContent = `/ ${state.pageCount || 0}`;
  viewerContainer.dataset.handTool = state.handToolEnabled ? "true" : "false";
  selectToolButton.classList.toggle("is-active", !state.handToolEnabled);
  handToolButton.classList.toggle("is-active", state.handToolEnabled);

  const zoomPreset = getZoomSelectValue();
  if (zoomSelect.value !== zoomPreset) {
    zoomSelect.value = zoomPreset;
  }
}

function getZoomSelectValue() {
  const currentValue = String(state.currentScaleValue || "").trim().toLowerCase();
  if (currentValue === "page-width" || currentValue === "page-fit" || currentValue === "page-actual") {
    return currentValue;
  }

  const rounded = String(Math.round((state.currentScaleFactor || 1) * 100) / 100);
  const exactOption = Array.from(zoomSelect.options).find((option) => option.value === rounded);
  return exactOption ? exactOption.value : "1";
}

function createViewer() {
  if (state.eventBus && state.linkService && state.findController && state.pdfViewer) {
    return;
  }

  state.eventBus = new EventBus();
  state.linkService = new PDFLinkService({
    eventBus: state.eventBus,
  });
  state.findController = new PDFFindController({
    eventBus: state.eventBus,
    linkService: state.linkService,
  });
  state.pdfViewer = new PDFViewer({
    container: viewerContainer,
    viewer: viewerElement,
    eventBus: state.eventBus,
    linkService: state.linkService,
    findController: state.findController,
    textLayerMode: 1,
    removePageBorders: false,
  });

  state.linkService.setViewer(state.pdfViewer);

  subscribeEventBus("pagesinit", () => {
    if (!state.pdfViewer) {
      return;
    }

    state.pdfViewer.currentScaleValue = state.currentScaleValue || "page-width";
    state.pdfViewer.pagesRotation = state.pagesRotation;
    renderToolbarState();
  });

  subscribeEventBus("pagesloaded", (event) => {
    state.pageCount = event && event.pagesCount ? event.pagesCount : (state.pdfDocument ? state.pdfDocument.numPages : 0);
    state.currentPage = state.pdfViewer ? state.pdfViewer.currentPageNumber || 1 : 1;
    renderToolbarState();
    setStatus(state.currentDisplayName || "PDF document loaded");
    postToHost({
      type: "pdf.documentLoaded",
      source: state.currentSource,
      displayName: state.currentDisplayName,
      pageCount: state.pageCount,
      currentPage: state.currentPage,
    });
  });

  subscribeEventBus("pagechanging", (event) => {
    state.currentPage = event && event.pageNumber ? event.pageNumber : (state.pdfViewer ? state.pdfViewer.currentPageNumber || 1 : 1);
    renderToolbarState();
    postToHost({
      type: "pdf.pageChanged",
      pageNumber: state.currentPage,
      pageCount: state.pageCount,
    });
  });

  subscribeEventBus("scalechanging", (event) => {
    state.currentScaleValue = state.pdfViewer ? state.pdfViewer.currentScaleValue || "auto" : "auto";
    state.currentScaleFactor = state.pdfViewer ? state.pdfViewer.currentScale || 1 : (event && event.scale ? event.scale : 1);
    renderToolbarState();
    postToHost({
      type: "pdf.zoomChanged",
      scaleValue: state.currentScaleValue,
      scaleFactor: state.currentScaleFactor,
    });
  });

  subscribeEventBus("updatefindmatchescount", (event) => {
    postToHost({
      type: "pdf.searchStateChanged",
      query: state.currentSearchQuery,
      current: event && event.matchesCount ? event.matchesCount.current || 0 : 0,
      total: event && event.matchesCount ? event.matchesCount.total || 0 : 0,
    });
  });

  subscribeEventBus("updatefindcontrolstate", (event) => {
    postToHost({
      type: "pdf.searchStateChanged",
      query: state.currentSearchQuery,
      state: event && typeof event.state === "number" ? event.state : 0,
      previous: !!(event && event.previous),
      rawQuery: event && event.rawQuery ? event.rawQuery : state.currentSearchQuery,
    });
  });
}

function ensureViewerReady() {
  if (state.eventBus && state.linkService && state.findController && state.pdfViewer) {
    return true;
  }

  try {
    createViewer();
    return !!(state.eventBus && state.linkService && state.findController && state.pdfViewer);
  } catch (error) {
    showError("PDF viewer failed to initialize.", error && error.message ? error.message : "");
    return false;
  }
}

async function destroyCurrentDocument() {
  if (state.loadingTask && typeof state.loadingTask.destroy === "function") {
    try {
      await state.loadingTask.destroy();
    } catch (_) {
      // best-effort cleanup
    }
  }

  state.loadingTask = null;

  if (state.pdfViewer) {
    try {
      state.pdfViewer.setDocument(null);
    } catch (_) {
      // best-effort cleanup
    }
  }

  if (state.linkService) {
    try {
      state.linkService.setDocument(null, null);
    } catch (_) {
      // best-effort cleanup
    }
  }

  if (state.pdfDocument && typeof state.pdfDocument.destroy === "function") {
    try {
      await state.pdfDocument.destroy();
    } catch (_) {
      // best-effort cleanup
    }
  }

  state.pdfDocument = null;
  state.pageCount = 0;
  state.currentPage = 1;
  renderToolbarState();
}

function resolveSource(source) {
  if (!source) {
    return "";
  }

  try {
    return new URL(source, window.location.href).href;
  } catch (_) {
    return source;
  }
}

function revokeLocalObjectUrl() {
  if (state.localObjectUrl) {
    try {
      URL.revokeObjectURL(state.localObjectUrl);
    } catch (_) {
      // ignore
    }

    state.localObjectUrl = "";
  }
}

async function openSource(message) {
  const rawSource = message && message.source ? message.source : "";
  if (!rawSource) {
    showError("PDF source was not provided.", "");
    return;
  }

  if (!ensureViewerReady()) {
    return;
  }

  setStatus("Loading PDF document...");
  postToHost({
    type: "pdf.documentLoading",
    source: rawSource,
  });

  await destroyCurrentDocument();

  if (!message || !message.isLocalSelection) {
    revokeLocalObjectUrl();
  }

  state.currentSource = rawSource;
  state.currentDisplayName = message && message.displayName ? message.displayName : "";
  state.currentSearchQuery = "";
  state.pagesRotation = 0;
  state.currentScaleFactor = 1;
  state.currentScaleValue = "page-width";

  const loadingTask = getDocument({
    url: resolveSource(rawSource),
    cMapUrl: "./cmaps/",
    cMapPacked: true,
    standardFontDataUrl: "./standard_fonts/",
    wasmUrl: "./wasm/",
    iccUrl: "./iccs/",
  });

  state.loadingTask = loadingTask;

  try {
    const pdfDocument = await loadingTask.promise;
    if (state.loadingTask !== loadingTask) {
      try {
        await pdfDocument.destroy();
      } catch (_) {
        // best-effort cleanup
      }
      return;
    }

    state.pdfDocument = pdfDocument;
    state.pageCount = pdfDocument.numPages || 0;
    state.currentPage = 1;
    state.linkService.setDocument(pdfDocument, null);
    state.pdfViewer.setDocument(pdfDocument);
    state.pdfViewer.currentPageNumber = 1;
    state.pdfViewer.currentScaleValue = "page-width";
    state.pdfViewer.pagesRotation = 0;
    renderToolbarState();
    viewerContainer.focus();
  } catch (error) {
    const messageText = error && error.message ? error.message : "Failed to open the PDF document.";
    showError("Failed to open the PDF document.", messageText);
  }
}

function setPage(pageNumber) {
  if (!state.pdfViewer || !state.pdfDocument) {
    return;
  }

  const safePageNumber = Math.min(
    Math.max(Number(pageNumber) || 1, 1),
    state.pdfDocument.numPages || 1,
  );

  state.currentPage = safePageNumber;
  state.pdfViewer.currentPageNumber = safePageNumber;
  renderToolbarState();
  viewerContainer.focus();
}

function setZoom(message) {
  if (!state.pdfViewer) {
    return;
  }

  if (message && typeof message.zoomMode === "string" && message.zoomMode) {
    state.currentScaleValue = message.zoomMode;
    state.pdfViewer.currentScaleValue = message.zoomMode;
    return;
  }

  if (message && typeof message.scaleFactor === "number" && Number.isFinite(message.scaleFactor) && message.scaleFactor > 0) {
    state.currentScaleFactor = message.scaleFactor;
    state.currentScaleValue = String(message.scaleFactor);
    state.pdfViewer.currentScale = message.scaleFactor;
  }
}

function setSearchQuery(query) {
  state.currentSearchQuery = query || "";

  if (!state.currentSearchQuery) {
    clearSearch();
    return;
  }

  state.eventBus.dispatch("find", {
    source: FIND_SOURCE,
    type: "",
    query: state.currentSearchQuery,
    phraseSearch: true,
    caseSensitive: false,
    entireWord: false,
    highlightAll: true,
    findPrevious: false,
    matchDiacritics: false,
  });
}

function dispatchFind(findPrevious) {
  if (!state.currentSearchQuery) {
    return;
  }

  state.eventBus.dispatch("find", {
    source: FIND_SOURCE,
    type: "again",
    query: state.currentSearchQuery,
    phraseSearch: true,
    caseSensitive: false,
    entireWord: false,
    highlightAll: true,
    findPrevious: !!findPrevious,
    matchDiacritics: false,
  });
}

function clearSearch() {
  state.currentSearchQuery = "";
  state.eventBus.dispatch("findbarclose", { source: FIND_SOURCE });
  postToHost({
    type: "pdf.searchStateChanged",
    query: "",
    current: 0,
    total: 0,
  });
}

function printDocument() {
  if (!state.pdfDocument) {
    return;
  }

  state.isPrintInProgress = true;
  postToHost({
    type: "pdf.printStarted",
    source: state.currentSource,
  });

  try {
    window.print();
  } catch (error) {
    state.isPrintInProgress = false;
    showError("Failed to start print flow.", error && error.message ? error.message : "");
  }
}

async function downloadCurrentDocument() {
  if (!state.currentSource) {
    return;
  }

  try {
    const response = await fetch(resolveSource(state.currentSource));
    const blob = await response.blob();
    const url = URL.createObjectURL(blob);
    const link = document.createElement("a");
    link.href = url;
    link.download = state.currentDisplayName || "document.pdf";
    document.body.appendChild(link);
    link.click();
    link.remove();
    URL.revokeObjectURL(url);
    setStatus(`Downloaded ${link.download}`);
  } catch (error) {
    showError("Failed to download the PDF document.", error && error.message ? error.message : "");
  }
}

function rotateDocument(delta) {
  if (!state.pdfViewer || !state.pdfDocument) {
    return;
  }

  state.pagesRotation = ((state.pagesRotation + delta) % 360 + 360) % 360;
  state.pdfViewer.pagesRotation = state.pagesRotation;
  renderToolbarState();
}

function setHandToolEnabled(isEnabled) {
  state.handToolEnabled = !!isEnabled;
  viewerContainer.classList.remove("is-dragging");
  state.dragPointerId = null;
  renderToolbarState();
}

function focusViewer() {
  viewerContainer.focus();
}

function handlePageNumberCommit() {
  const parsed = parseInt(pageNumberInput.value, 10);
  if (!Number.isFinite(parsed)) {
    pageNumberInput.value = String(state.currentPage || 1);
    return;
  }

  setPage(parsed);
}

function handleZoomSelection(value) {
  if (!value) {
    return;
  }

  if (value === "page-width" || value === "page-fit" || value === "page-actual") {
    setZoom({ zoomMode: value });
    return;
  }

  const numericValue = Number(value);
  if (Number.isFinite(numericValue) && numericValue > 0) {
    setZoom({ scaleFactor: numericValue });
  }
}

function bindToolbar() {
  updateToolbarIcons();
  renderToolbarState();

  openButton.addEventListener("click", () => fileInput.click());
  downloadButton.addEventListener("click", () => handleHostMessage({ type: "pdf.download" }));
  printButton.addEventListener("click", () => handleHostMessage({ type: "pdf.print" }));
  rotateLeftButton.addEventListener("click", () => handleHostMessage({ type: "pdf.rotateCounterClockwise" }));
  rotateRightButton.addEventListener("click", () => handleHostMessage({ type: "pdf.rotateClockwise" }));
  previousPageButton.addEventListener("click", () => setPage(state.currentPage - 1));
  nextPageButton.addEventListener("click", () => setPage(state.currentPage + 1));
  zoomOutButton.addEventListener("click", () => setZoom({ scaleFactor: Math.max(0.25, (state.currentScaleFactor || 1) * 0.85) }));
  zoomInButton.addEventListener("click", () => setZoom({ scaleFactor: Math.min(5, (state.currentScaleFactor || 1) * 1.15) }));
  selectToolButton.addEventListener("click", () => setHandToolEnabled(false));
  handToolButton.addEventListener("click", () => handleHostMessage({ type: "pdf.toggleHandTool" }));

  pageNumberInput.addEventListener("keydown", (event) => {
    if (event.key === "Enter") {
      event.preventDefault();
      handlePageNumberCommit();
    }
  });
  pageNumberInput.addEventListener("blur", handlePageNumberCommit);

  zoomSelect.addEventListener("change", () => handleZoomSelection(zoomSelect.value));

  fileInput.addEventListener("change", () => {
    const file = fileInput.files && fileInput.files[0] ? fileInput.files[0] : null;
    if (!file) {
      return;
    }

    revokeLocalObjectUrl();
    state.localObjectUrl = URL.createObjectURL(file);
    openSource({
      type: "pdf.openSource",
      source: state.localObjectUrl,
      displayName: file.name,
      isLocalSelection: true,
    });
    fileInput.value = "";
  });
}

function bindHandTool() {
  viewerContainer.addEventListener("pointerdown", (event) => {
    if (!state.handToolEnabled || event.button !== 0) {
      return;
    }

    state.dragPointerId = event.pointerId;
    state.dragStartX = event.clientX;
    state.dragStartY = event.clientY;
    state.dragScrollLeft = viewerContainer.scrollLeft;
    state.dragScrollTop = viewerContainer.scrollTop;
    viewerContainer.classList.add("is-dragging");
    viewerContainer.setPointerCapture(event.pointerId);
    event.preventDefault();
  });

  viewerContainer.addEventListener("pointermove", (event) => {
    if (!state.handToolEnabled || state.dragPointerId !== event.pointerId) {
      return;
    }

    viewerContainer.scrollLeft = state.dragScrollLeft - (event.clientX - state.dragStartX);
    viewerContainer.scrollTop = state.dragScrollTop - (event.clientY - state.dragStartY);
  });

  function releaseDrag(event) {
    if (state.dragPointerId == null || (event && state.dragPointerId !== event.pointerId)) {
      return;
    }

    try {
      viewerContainer.releasePointerCapture(state.dragPointerId);
    } catch (_) {
      // ignore
    }

    state.dragPointerId = null;
    viewerContainer.classList.remove("is-dragging");
  }

  viewerContainer.addEventListener("pointerup", releaseDrag);
  viewerContainer.addEventListener("pointercancel", releaseDrag);
  viewerContainer.addEventListener("lostpointercapture", releaseDrag);
}

async function handleHostMessage(rawMessage) {
  let message = rawMessage;

  if (typeof rawMessage === "string") {
    try {
      message = JSON.parse(rawMessage);
    } catch (_) {
      return;
    }
  }

  if (!message || typeof message !== "object") {
    return;
  }

  switch (message.type) {
    case "pdf.openSource":
      await openSource(message);
      break;

    case "pdf.setPage":
      setPage(message.pageNumber);
      break;

    case "pdf.setZoom":
      setZoom(message);
      break;

    case "pdf.setSearchQuery":
      setSearchQuery(message.query || "");
      break;

    case "pdf.findNext":
      dispatchFind(false);
      break;

    case "pdf.findPrevious":
      dispatchFind(true);
      break;

    case "pdf.clearSearch":
      clearSearch();
      break;

    case "pdf.print":
      printDocument();
      break;

    case "pdf.focus":
      focusViewer();
      break;

    case "pdf.setTheme":
      applyTheme(message.theme);
      break;

    case "pdf.download":
      await downloadCurrentDocument();
      break;

    case "pdf.rotateClockwise":
      rotateDocument(90);
      break;

    case "pdf.rotateCounterClockwise":
      rotateDocument(-90);
      break;

    case "pdf.toggleHandTool":
      setHandToolEnabled(!state.handToolEnabled);
      break;
  }
}

function bindHostBridge() {
  window.PhialeWebHost.onHostMessage = handleHostMessage;
  notifyReady();
}

window.addEventListener("phiale-webhost-bridge-ready", bindHostBridge);
window.addEventListener("phiale-webhost-message", (event) => handleHostMessage(event.detail));

window.addEventListener("afterprint", () => {
  if (!state.isPrintInProgress) {
    return;
  }

  state.isPrintInProgress = false;
  postToHost({
    type: "pdf.printCompleted",
    source: state.currentSource,
  });
});

window.addEventListener("wheel", (event) => {
  if (!event.ctrlKey) {
    return;
  }

  event.preventDefault();
}, { passive: false });

window.addEventListener("error", (event) => {
  showError("Unexpected error in PdfViewer.", event && event.message ? event.message : "");
});

window.addEventListener("unhandledrejection", (event) => {
  showError("Unexpected error in PdfViewer.", stringifyError(event ? event.reason : null));
});

bindToolbar();
bindHandTool();
ensureViewerReady();
applyTheme(state.theme);
setStatus("Waiting for document source...");

if (window.PhialeWebHost && typeof window.PhialeWebHost.postMessage === "function") {
  bindHostBridge();
}
