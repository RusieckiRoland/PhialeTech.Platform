// phiale-dsl-host.js
// Runs inside UWP WebView2. Monaco loaded via loader.js configured in index.html.

// ---- bridge: WebView2 postMessage (with legacy fallback) ----
function notify(type, payload) {
  let msg = '';
  try {
    msg = JSON.stringify(Object.assign({ type }, payload || {}));
  } catch (_) {
    return;
  }

  // Use one transport only (priority order) to avoid duplicate/out-of-order echoes.
  // This keeps behavior consistent across hosts.
  if (window.chrome && window.chrome.webview && window.chrome.webview.postMessage) {
    try { window.chrome.webview.postMessage(msg); return; } catch (_) {}
  }
  if (window.external && window.external.notify) {
    try { window.external.notify(msg); return; } catch (_) {}
  }

  // Fallback transports for hosts without direct bridge support.
  try { console.log('phiale:' + encodeURIComponent(msg)); return; } catch (_) {}
  try { location.hash = 'phiale:' + encodeURIComponent(msg); return; } catch (_) {}
  try { document.title = 'phiale:' + encodeURIComponent(msg); } catch (_) {}
}
notify('htmlLoaded');

// ---- Monaco boot ----
require(['vs/editor/editor.main'], function () {

  // ===== Language registration =====
  monaco.languages.register({ id: 'phiale' });

  // ===== Semantic ONLY: legend + tokens provided by C# =====
  const semEmitter = new monaco.Emitter();
  let semLegend = { tokenTypes: [], tokenModifiers: [] };
  let semTokens = { data: [] };

  monaco.languages.registerDocumentSemanticTokensProvider('phiale', {
    getLegend: () => semLegend,
    provideDocumentSemanticTokens: function () {
      const data = (semTokens && semTokens.data) ? new Uint32Array(semTokens.data) : new Uint32Array(0);
      return { data, resultId: '1' };
    },
    releaseDocumentSemanticTokens: function () {},
    onDidChange: semEmitter.event
  });

  // ===== Theme =====
  function hex(c) { return c ? (c.startsWith('#') ? c : ('#' + c)) : undefined; }

  function defineThemeFromLegend(legend) {
    const types = (legend && Array.isArray(legend.tokenTypes)) ? legend.tokenTypes : [];

    const palette = new Map([
      ['keyword',   { color: '#BD93F9', style: 'bold' }],
      ['operator',  { color: '#F8F8F2' }],
      ['number',    { color: '#FFB86C' }],
      ['string',    { color: '#F1FA8C' }],
      ['comment',   { color: '#6272A4', style: 'italic' }],
      ['function',  { color: '#50FA7B' }],
      ['type',      { color: '#8BE9FD', style: 'italic' }],
      ['variable',  { color: '#FF79C6' }],
      ['namespace', { color: '#FF79C6' }]
    ]);

    const fallback = { color: '#F8F8F2', style: '' };

    const semanticTokenRules = types.map(t => {
      const p = palette.get(t) || fallback;
      const rule = { token: t };
      if (p.color) rule.foreground = hex(p.color);
      if (p.style) rule.fontStyle = p.style;
      return rule;
    });

    monaco.editor.defineTheme('phiale-dracula', {
      base: 'vs-dark',
      inherit: true,
      rules: [],
      colors: {
        'editor.background': '#282A36',
        'editor.foreground': '#F8F8F2',
        'editorCursor.foreground': '#F8F8F2',
        'editorLineNumber.foreground': '#6272A4',
        'editorLineNumber.activeForeground': '#F8F8F2',
        'editor.selectionBackground': '#44475A',
        'editor.inactiveSelectionBackground': '#44475A',
        'editorGutter.background': '#282A36',
        'editorWhitespace.foreground': '#3B3A32'
      },
      semanticHighlighting: true,
      semanticTokenRules
    });

    monaco.editor.setTheme('phiale-dracula');
  }

  defineThemeFromLegend({ tokenTypes: [] });

  // ===== Completions (driven from C#) =====
  let lastCompletions = null;
  let suggestOpen = false;

  monaco.languages.registerCompletionItemProvider('phiale', {
    triggerCharacters: [' ', '"', "'", '.', '_', ':', '('],
    provideCompletionItems: function (model) {
      if (!lastCompletions) return { suggestions: [] };
      const startPos = model.getPositionAt(lastCompletions.start | 0);
      const endPos = model.getPositionAt(lastCompletions.end | 0);
      const range = new monaco.Range(
        startPos.lineNumber, startPos.column,
        endPos.lineNumber, endPos.column
      );
      const suggestions = (lastCompletions.items || []).map(it => ({
        label: it.label || it.insert || '',
        kind: monaco.languages.CompletionItemKind.Keyword,
        insertText: it.insert || it.label || '',
        range
      }));
      return { suggestions };
    }
  });

  // ===== Build status bars (top + bottom) =====
  const container = document.getElementById('container');

  const wrapper = document.createElement('div');
  Object.assign(wrapper.style, {
    display: 'flex',
    flexDirection: 'column',
    width: '100%',
    height: '100%',
    background: '#282A36',
    boxSizing: 'border-box'
  });
  container.parentNode.insertBefore(wrapper, container);
  wrapper.appendChild(container);

  // --- TOP BAR ---
  const statusBar = document.createElement('div');
  statusBar.id = 'ph-mode-bar';
  Object.assign(statusBar.style, {
    flex: '0 0 auto',
    display: 'flex',
    alignItems: 'center',
    background: '#282A36',
    color: '#F8F8F2',
    borderBottom: '1px solid #1e1f29',
    padding: '4px 0',
    gap: '8px',
    boxSizing: 'border-box',
    pointerEvents: 'none'
  });
  wrapper.insertBefore(statusBar, container);

  const stateDot = document.createElement('div');
  Object.assign(stateDot.style, {
    width:'8px', height:'8px', borderRadius:'50%',
    marginLeft:'8px',
    background:'#8a8fa3', pointerEvents:'none', flex:'0 0 auto'
  });
  statusBar.appendChild(stateDot);

  const barText = document.createElement('div');
  Object.assign(barText.style, {
    fontSize: '12px',
    opacity: '0.9',
    whiteSpace: 'nowrap',
    overflow: 'hidden',
    textOverflow: 'ellipsis',
    flex: '0 1 auto',     // rośnie do treści
    maxWidth: '60%',      // ale z limitem
    marginRight: '8px',
    pointerEvents: 'none'
  });
  statusBar.appendChild(barText);

  const barChip = document.createElement('div');
  Object.assign(barChip.style, {
    padding: '4px 10px',
    borderRadius: '8px',
    fontSize: '12px',
    lineHeight: '1.35',
    color: '#f8f8f2',
    background: 'rgba(68,71,90,.92)',
    boxShadow: '0 2px 8px rgba(0,0,0,.25)',
    border: '1px solid rgba(255,255,255,.10)',
    whiteSpace: 'nowrap',      // jednolinijkowo dopóki się mieści
    wordBreak: 'break-word',
    flex: '0 0 auto',
    width: 'max-content',      // dopasuj do treści
    maxWidth: '100%',
    opacity: '0',
    transform: 'translateY(-2px)',
    transition: 'opacity .12s ease, transform .12s ease',
    pointerEvents: 'auto',
    display: 'none'
  });
  barChip.setAttribute('role','status');
  barChip.setAttribute('aria-live','polite');
  barChip.className = 'ph-chip';
  statusBar.appendChild(barChip);

  // spacer — trzyma wszystko przy lewej
  const _spacer = document.createElement('div');
  Object.assign(_spacer.style, { flex: '1 1 auto' });
  statusBar.appendChild(_spacer);

  // --- BOTTOM BAR ---
  const footerBar = document.createElement('div');
  Object.assign(footerBar.style, {
    flex: '0 0 auto',
    height: '22px',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'flex-end',
    background: '#282A36',
    color: '#C7CBD6',
    borderTop: '1px solid #1e1f29',
    fontFamily: 'Consolas,Menlo,Monaco,"Courier New",monospace',
    fontSize: '11px',
    padding: '0 10px',
    gap: '16px',
    pointerEvents: 'none'
  });
  wrapper.appendChild(footerBar);

  const posSpan = document.createElement('span');
  const selSpan = document.createElement('span');
  footerBar.appendChild(posSpan);
  footerBar.appendChild(selSpan);

  // ===== Editor =====
  Object.assign(container.style, { flex: '1 1 auto', margin: '0', padding: '0', position: 'relative' });

  const editor = monaco.editor.create(container, {
    value: '',
    language: 'phiale',
    theme: 'phiale-dracula',
    readOnly: false,
    automaticLayout: true,
    wordWrap: 'off',
    minimap: { enabled: false },
    acceptSuggestionOnEnter: 'on',
    'semanticHighlighting.enabled': true,
    padding: { top: 0, bottom: 0 }
  });
  const model = editor.getModel();

  // ===== Layout helpers (chip szerokość, footer, placeholder) =====
  let _layoutRAF = 0;
  function scheduleLayout(){
    if (_layoutRAF) return;
    _layoutRAF = requestAnimationFrame(() => {
      _layoutRAF = 0;
      layoutBarChip();
      updateFooter();
      positionPlaceholder();
      adjustChipWrapping();
    });
  }

  function layoutBarChip() {
    try {
      const li = editor.getLayoutInfo();
      barChip.style.maxWidth = Math.max(160, li.contentWidth - 16) + 'px';
    } catch {}
  }

  // jednolinijkowo dopóki się mieści; w przeciwnym razie zawijamy
  function adjustChipWrapping() {
    if (barChip.style.display === 'none') return;
    barChip.style.whiteSpace = 'nowrap';
    requestAnimationFrame(() => {
      const needsWrap = barChip.scrollWidth > barChip.clientWidth;
      barChip.style.whiteSpace = needsWrap ? 'normal' : 'nowrap';
    });
  }

  editor.onDidLayoutChange(scheduleLayout);
  editor.onDidScrollChange(scheduleLayout);
  editor.onDidChangeConfiguration(scheduleLayout);
  window.addEventListener('resize', scheduleLayout);
  setTimeout(scheduleLayout, 0);

  // ===== Bar colors & FSM =====
  function setBarColors(kind) {
    const map = {
      idle:  { bg:'#282A36', fg:'#F8F8F2', dot:'#8a8fa3' },
      draw:  { bg:'#2b3a57', fg:'#E6EEF8', dot:'#7fb3ff' },
      error: { bg:'#572b2b', fg:'#FFEDEE', dot:'#ff7b7b' }
    };
    const c = map[kind] || map.idle;
    statusBar.style.background = c.bg;
    statusBar.style.color = c.fg;
    stateDot.style.background = c.dot;
  }

  // ===== Chip show/hide helpers =====
  function showChip(html) {
    barChip.style.display = 'inline-block';
    barChip.innerHTML = html || '';
    barChip.title = barChip.textContent;
    requestAnimationFrame(() => {
      barChip.style.opacity = '1';
      barChip.style.transform = 'translateY(0)';
      adjustChipWrapping();
    });
  }
  function hideChip() {
    barChip.style.opacity = '0';
    barChip.style.transform = 'translateY(-2px)';
    setTimeout(() => { barChip.style.display = 'none'; }, 130);
    barChip.innerHTML = '';
    barChip.removeAttribute('title');
  }

  // Ustawienie trybu i promptu (HTML pochodzi z akcji)
  window.__ph_setFSMState = (modeText, chipHtml, kind='idle') => {
    setBarColors(kind);
    barText.textContent = modeText || '';

    const raw = String(chipHtml || '');
    const stripped = raw.replace(/&nbsp;/gi,' ').replace(/<[^>]*>/g,'').trim();
    if (stripped.length === 0) {
      hideChip();
    } else {
      showChip(raw);
    }

    requestAnimationFrame(() => {
      scheduleLayout();
    });
  };

  // Initial state – bez chipu
  __ph_setFSMState("Idle mode", "", "idle");

  // ===== Placeholder "command:" =====
  let placeholderText = 'command:';
  const placeholder = document.createElement('div');
  placeholder.textContent = placeholderText;
  Object.assign(placeholder.style, {
    position: 'absolute',
    pointerEvents: 'none',
    userSelect: 'none',
    color: 'rgba(255,255,255,.35)',
    fontFamily: 'Consolas,Menlo,Monaco,"Courier New",monospace',
    fontSize: (editor.getOption(monaco.editor.EditorOption.fontInfo).fontSize || 14) + 'px',
    lineHeight: '1.4',
    whiteSpace: 'pre'
  });
  container.appendChild(placeholder);

  function positionPlaceholder() {
    try {
      const li = editor.getLayoutInfo();
      const y = editor.getTopForLineNumber(1) + 2;
      placeholder.style.left = (li.contentLeft + 4) + 'px';
      placeholder.style.top = y + 'px';
      placeholder.style.display = (model.getValueLength() === 0) ? 'block' : 'none';
    } catch {}
  }

  window.__ph_setPlaceholder = function (t) {
    placeholderText = String(t || '');
    placeholder.textContent = placeholderText;
    positionPlaceholder();
  };

  // ---- Bottom status updates ----
  function updateFooter() {
    try {
      const pos = editor.getPosition();
      posSpan.textContent = `Ln ${pos.lineNumber}, Col ${pos.column}`;

      const sel = editor.getSelection();
      let selCount = 0;
      if (sel && !sel.isEmpty()) {
        const a = model.getOffsetAt(sel.getStartPosition());
        const b = model.getOffsetAt(sel.getEndPosition());
        selCount = Math.abs(b - a);
      }
      selSpan.textContent = selCount > 0 ? `Sel ${selCount}` : '';
    } catch {}
  }

  editor.onDidChangeCursorPosition(updateFooter);
  editor.onDidChangeModelContent(function () {
    try {
      notify('text', { value: model.getValue(), caret: model.getOffsetAt(editor.getPosition()) });
    } catch {}
    positionPlaceholder();
    updateFooter();
  });
  editor.onDidLayoutChange(updateFooter);
  editor.onDidChangeCursorSelection?.(updateFooter);
  setTimeout(() => { positionPlaceholder(); updateFooter(); }, 0);

  // ---- Mode: command vs script ----
  let mode = 'command';
  function isSuggestOpen() {
    try {
      const c = editor.getContribution && editor.getContribution('editor.contrib.suggestController');
      const m = c && c.model;
      const st = (m && (typeof m.state === 'number' ? m.state : m._state));
      if (typeof st === 'number') return st !== 0;
    } catch (_) {}
    return !!suggestOpen;
  }

  // ---- Hotkeys ----
  editor.addCommand(monaco.KeyMod.CtrlCmd | monaco.KeyCode.Space, function () {
    notify('complete');
  });
  editor.addCommand(monaco.KeyCode.UpArrow, function () {
    if (isSuggestOpen()) { editor.trigger('ph', 'selectPrevSuggestion', {}); return; }
    if (mode === 'command') notify('historyUp'); else editor.trigger('ph', 'cursorUp', {});
  });
  editor.addCommand(monaco.KeyCode.DownArrow, function () {
    if (isSuggestOpen()) { editor.trigger('ph', 'selectNextSuggestion', {}); return; }
    if (mode === 'command') notify('historyDown'); else editor.trigger('ph', 'cursorDown', {});
  });
  editor.addCommand(monaco.KeyCode.Enter, function () {
    if (isSuggestOpen()) { editor.trigger('ph', 'acceptSelectedSuggestion', {}); suggestOpen = false; return; }
    if (mode === 'command') {
      const val = model.getValue();
      notify('enter', { value: val }); // pusty też leci do C#
    }
  });
  editor.addCommand(monaco.KeyCode.Tab, function () {
    if (isSuggestOpen()) { editor.trigger('ph', 'acceptSelectedSuggestion', {}); suggestOpen = false; return; }
    editor.trigger('ph', 'type', { text: '\t' });
  });
  editor.addCommand(monaco.KeyMod.CtrlCmd | monaco.KeyCode.Enter, function () {
    if (mode === 'script') notify('runScript', { value: model.getValue() });
  });
  editor.addCommand(monaco.KeyCode.Escape, function () {
    if (isSuggestOpen()) {
      editor.trigger('ph', 'hideSuggestWidget', {});
      suggestOpen = false;
      return;
    }
    notify('esc'); // przerwij akcję
  });

  // ===== API from C# =====
  window.__ph_setText = function (text) {
    const keep = model.getOffsetAt(editor.getPosition());
    editor.pushUndoStop();
    editor.executeEdits('ph', [{ range: model.getFullModelRange(), text: text || '' }]);
    const nextPos = model.getPositionAt(Math.min(keep, (text || '').length));
    editor.setPosition(nextPos);
    positionPlaceholder();
    updateFooter();
  };
  window.__ph_setReadOnly = function (flag) {
    editor.updateOptions({ readOnly: !!flag });
  };
  window.__ph_setMode = function (_mode) {
    mode = (_mode === 'script') ? 'script' : 'command';
    editor.updateOptions({ wordWrap: mode === 'script' ? 'on' : 'off' });
    positionPlaceholder();
  };
  window.__ph_focus = function () {
    editor.focus();
    positionPlaceholder();
  };
  window.__ph_clearInput = function () {
    editor.pushUndoStop();
    editor.executeEdits('ph', [{ range: model.getFullModelRange(), text: '' }]);
    editor.setPosition({ lineNumber: 1, column: 1 });
    positionPlaceholder();
    updateFooter();
  };
  window.__ph_showCompletions = function (items, start, end) {
    lastCompletions = { items: items || [], start: start | 0, end: end | 0 };
    suggestOpen = true;
    editor.focus();
    editor.trigger('ph', 'editor.action.triggerSuggest', {});
  };
  window.__ph_hideCompletions = function () {
    lastCompletions = null;
    suggestOpen = false;
    editor.trigger('ph', 'hideSuggestWidget', {});
  };
  window.__ph_replaceRange = function (start, end, insert) {
    const rs = model.getPositionAt(start | 0);
    const re = model.getPositionAt(end | 0);
    const range = new monaco.Range(rs.lineNumber, rs.column, re.lineNumber, re.column);
    editor.executeEdits('ph', [{ range, text: insert || '' }]);
    editor.focus();
    positionPlaceholder();
    updateFooter();
  };

  // SEMANTICS: inputs from C#
  window.__ph_setSemanticLegend = function (legend) {
    semLegend = legend || { tokenTypes: [], tokenModifiers: [] };
    defineThemeFromLegend(semLegend);
    semEmitter.fire();
  };
  window.__ph_applySemanticTokens = function (dto) {
    semTokens = dto || { data: [] };
    semEmitter.fire();
  };

  // ===== Diagnostics (validation) from C# → Monaco markers =====
  window.__ph_setDiagnostics = function (dto) {
    try {
      const list = Array.isArray(dto) ? dto : (dto && dto.diagnostics) || [];
      const markers = list.map(d => {
        const line = Math.max(1, (d.line|0));
        const col  = Math.max(1, (d.column|0));
        const len  = Math.max(1, (d.length|0));
        let sev = monaco.MarkerSeverity.Error;
        const s = (d.severity || '').toLowerCase();
        if (s === 'warning') sev = monaco.MarkerSeverity.Warning;
        else if (s === 'info') sev = monaco.MarkerSeverity.Info;
        return {
          severity: sev,
          message: d.message || '',
          startLineNumber: line,
          startColumn: col,
          endLineNumber: line,
          endColumn: col + len
        };
      });
      monaco.editor.setModelMarkers(model, 'phiale', markers);
    } catch (e) {
      notify('jsError', { msg: 'setDiagnostics failed: ' + String(e) });
    }
  };

  window.__ph_clearDiagnostics = function () {
    monaco.editor.setModelMarkers(model, 'phiale', []);
  };

  // start
  positionPlaceholder();
  notify('monacoReady');
});

// Bubble JS errors up to C#
window.onerror = function (msg, url, line, col, err) {
  notify('jsError', {
    msg: String(msg),
    url: String(url || ''),
    line: line | 0,
    col: col | 0,
    stack: err && String(err.stack || '')
  });
};
