import { getDocument, GlobalWorkerOptions } from "./build/pdf.mjs";
import {
  EventBus,
  PDFFindController,
  PDFLinkService,
  PDFViewer,
} from "./web/pdf_viewer.mjs";

GlobalWorkerOptions.workerSrc = "./build/pdf.worker.mjs";

const viewerContainer = document.getElementById("viewerContainer");
const viewerElement = document.getElementById("viewer");
const statusCard = document.getElementById("statusCard");
const statusText = document.getElementById("statusText");

const FIND_SOURCE = { source: "PhialeTech.PdfViewer" };

const state = {
  readyAnnounced: false,
  isPrintInProgress: false,
  currentSource: "",
  currentDisplayName: "",
  currentSearchQuery: "",
  loadingTask: null,
  pdfDocument: null,
  eventBus: null,
  linkService: null,
  findController: null,
  pdfViewer: null,
};

function postToHost(message) {
  try {
    if (window.PhialeWebHost && typeof window.PhialeWebHost.postMessage === "function") {
      return window.PhialeWebHost.postMessage(message);
    }
  } catch (_) {
    // best-effort bridge
  }

  return false;
}

function stringifyError(error) {
  if (!error) {
    return "";
  }

  const parts = [];
  if (error?.message) {
    parts.push(error.message);
  }

  if (error?.stack) {
    parts.push(error.stack);
  }

  return parts.join("\n");
}

function notifyReady() {
  if (state.readyAnnounced) {
    return;
  }

  state.readyAnnounced = true;
  setStatus("", true, false, false);

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

function setStatus(text, muted = false, visible = true, loading = false) {
  if (!statusCard || !statusText) {
    return;
  }

  statusCard.dataset.state = visible ? "visible" : "hidden";
  statusText.textContent = text || "";
  statusText.dataset.muted = muted ? "true" : "false";
  statusText.classList.toggle("loadingPulse", !!loading);
}

function showError(message, detail) {
  const composed = detail ? `${message} ${detail}` : message;
  setStatus(composed, false, true, false);
  postToHost({
    type: "pdf.error",
    message: message || "Unknown PDF viewer error.",
    detail: detail || "",
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
    if (state.pdfViewer) {
      state.pdfViewer.currentScaleValue = "page-width";
    }
  });

  subscribeEventBus("pagesloaded", (event) => {
    postToHost({
      type: "pdf.documentLoaded",
      source: state.currentSource,
      displayName: state.currentDisplayName,
      pageCount: event?.pagesCount || state.pdfDocument?.numPages || 0,
      currentPage: state.pdfViewer?.currentPageNumber || 1,
    });

    setStatus("", true, false, false);
  });

  subscribeEventBus("pagechanging", (event) => {
    postToHost({
      type: "pdf.pageChanged",
      pageNumber: event?.pageNumber || state.pdfViewer?.currentPageNumber || 1,
      pageCount: state.pdfDocument?.numPages || 0,
    });
  });

  subscribeEventBus("scalechanging", (event) => {
    const scaleValue = state.pdfViewer?.currentScaleValue || event?.presetValue || "auto";
    const scaleFactor = state.pdfViewer?.currentScale || event?.scale || 1;
    postToHost({
      type: "pdf.zoomChanged",
      scaleValue,
      scaleFactor,
    });
  });

  subscribeEventBus("updatefindmatchescount", (event) => {
    postToHost({
      type: "pdf.searchStateChanged",
      query: state.currentSearchQuery,
      current: event?.matchesCount?.current ?? 0,
      total: event?.matchesCount?.total ?? 0,
    });
  });

  subscribeEventBus("updatefindcontrolstate", (event) => {
    postToHost({
      type: "pdf.searchStateChanged",
      query: state.currentSearchQuery,
      state: event?.state ?? 0,
      previous: !!event?.previous,
      rawQuery: event?.rawQuery || state.currentSearchQuery,
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
    showError("PDF viewer failed to initialize.", error?.message || "");
    return false;
  }
}

async function destroyCurrentDocument() {
  if (state.loadingTask && typeof state.loadingTask.destroy === "function") {
    try {
      await state.loadingTask.destroy();
    } catch (error) {
      // best-effort cleanup
    }
  }

  state.loadingTask = null;

  if (state.pdfViewer) {
    try {
      state.pdfViewer.setDocument(null);
    } catch (error) {
      // best-effort cleanup
    }
  }

  if (state.linkService) {
    try {
      state.linkService.setDocument(null, null);
    } catch (error) {
      // best-effort cleanup
    }
  }

  if (state.pdfDocument && typeof state.pdfDocument.destroy === "function") {
    try {
      await state.pdfDocument.destroy();
    } catch (error) {
      // best-effort cleanup
    }
  }

  state.pdfDocument = null;
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

function dispatchFind(findPrevious) {
  if (!state.currentSearchQuery) {
    return;
  }

  state.eventBus.dispatch("find", {
    source: FIND_SOURCE,
    type: findPrevious ? "again" : "again",
    query: state.currentSearchQuery,
    phraseSearch: true,
    caseSensitive: false,
    entireWord: false,
    highlightAll: true,
    findPrevious: !!findPrevious,
    matchDiacritics: false,
  });
}

async function openSource(message) {
  const rawSource = message?.source || "";
  if (!rawSource) {
    showError("PDF source was not provided.", "");
    return;
  }

  if (!ensureViewerReady()) {
    return;
  }

  setStatus("Loading PDF document...", true, true, true);
  postToHost({
    type: "pdf.documentLoading",
    source: rawSource,
  });

  await destroyCurrentDocument();

  state.currentSource = rawSource;
  state.currentDisplayName = message?.displayName || "";
  state.currentSearchQuery = "";

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
      } catch (error) {
        // best-effort cleanup
      }
      return;
    }

    state.pdfDocument = pdfDocument;
    state.linkService.setDocument(pdfDocument, null);
    state.pdfViewer.setDocument(pdfDocument);

    state.pdfViewer.currentPageNumber = 1;
    state.pdfViewer.currentScaleValue = "page-width";
    viewerContainer.focus();
  } catch (error) {
    const messageText = error?.message || "Failed to open the PDF document.";
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

  state.pdfViewer.currentPageNumber = safePageNumber;
  viewerContainer.focus();
}

function setZoom(message) {
  if (!state.pdfViewer) {
    return;
  }

  if (typeof message?.zoomMode === "string" && message.zoomMode) {
    state.pdfViewer.currentScaleValue = message.zoomMode;
    return;
  }

  if (typeof message?.scaleFactor === "number" && Number.isFinite(message.scaleFactor) && message.scaleFactor > 0) {
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
    showError("Failed to start print flow.", error?.message || "");
  }
}

function focusViewer() {
  viewerContainer.focus();
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
  }
}

window.addEventListener("phiale-webhost-bridge-ready", () => {
  window.PhialeWebHost.onHostMessage = handleHostMessage;
  notifyReady();
});

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
  showError("Unexpected error in PdfViewer.", event?.message || "");
});

window.addEventListener("unhandledrejection", (event) => {
  showError("Unexpected error in PdfViewer.", stringifyError(event?.reason));
});

ensureViewerReady();
setStatus("Waiting for document source...", true, true, false);

if (window.PhialeWebHost && typeof window.PhialeWebHost.postMessage === "function") {
  window.PhialeWebHost.onHostMessage = handleHostMessage;
  notifyReady();
}
