(function () {
  'use strict';

  var editor = null;
  var model = null;
  var latestValue = '';
  var latestLanguage = 'yaml';
  var latestTheme = 'light';
  var suppressChangeEvent = false;
  var bootstrapConfig = {
    value: '',
    language: 'yaml',
    theme: 'light'
  };

  function normalizeTheme(theme) {
    return String(theme || '').toLowerCase() === 'dark' ? 'vs-dark' : 'vs';
  }

  function postMessage(type, payload) {
    var message = Object.assign({ type: type }, payload || {});

    try {
      if (window.PhialeWebHost && typeof window.PhialeWebHost.postMessage === 'function') {
        window.PhialeWebHost.postMessage(message);
        return;
      }
    } catch (_) {}

    try {
      if (window.chrome && window.chrome.webview && window.chrome.webview.postMessage) {
        window.chrome.webview.postMessage(JSON.stringify(message));
        return;
      }
    } catch (_) {}
  }

  function bindHostBridge() {
    if (window.PhialeWebHost) {
      window.PhialeWebHost.onHostMessage = handleHostMessage;
    }
  }

  function applyTheme(theme) {
    latestTheme = String(theme || '').toLowerCase() === 'dark' ? 'dark' : 'light';
    if (editor) {
      monaco.editor.setTheme(normalizeTheme(latestTheme));
    }
  }

  function applyLanguage(language) {
    latestLanguage = String(language || '').trim() || 'plaintext';
    if (model) {
      monaco.editor.setModelLanguage(model, latestLanguage);
    }
  }

  function applyValue(value) {
    latestValue = value == null ? '' : String(value);
    if (!model || model.getValue() === latestValue) {
      return;
    }

    suppressChangeEvent = true;
    model.setValue(latestValue);
    suppressChangeEvent = false;
  }

  function handleHostMessage(message) {
    if (!message || typeof message !== 'object') {
      return;
    }

    switch (message.type) {
      case 'monaco.setValue':
        applyValue(message.value);
        break;
      case 'monaco.setLanguage':
        applyLanguage(message.language);
        break;
      case 'monaco.setTheme':
        applyTheme(message.theme);
        break;
      case 'monaco.focus':
        if (editor) {
          editor.focus();
        }
        break;
    }
  }

  function notifyError(message, detail) {
    postMessage('monaco.error', {
      message: String(message || 'MonacoEditor error'),
      detail: String(detail || '')
    });
  }

  window.addEventListener('phiale-webhost-bridge-ready', bindHostBridge);
  bindHostBridge();

  function initializeEditor() {
    latestValue = bootstrapConfig.value == null ? '' : String(bootstrapConfig.value);
    latestLanguage = String(bootstrapConfig.language || '').trim() || 'plaintext';
    latestTheme = String(bootstrapConfig.theme || '').toLowerCase() === 'dark' ? 'dark' : 'light';

    require(['vs/editor/editor.main'], function () {
      var shell = document.getElementById('editor-shell');
      var root = document.getElementById('editor-root');
      document.body.style.background = latestTheme === 'dark' ? '#0f172a' : '#f8fafc';
      document.documentElement.style.background = document.body.style.background;
      if (shell) {
        shell.style.background = document.body.style.background;
      }
      root.style.background = 'transparent';

      model = monaco.editor.createModel(latestValue, latestLanguage);
      editor = monaco.editor.create(root, {
        model: model,
        theme: normalizeTheme(latestTheme),
        automaticLayout: true,
        minimap: { enabled: false },
        scrollBeyondLastLine: false,
        wordWrap: 'on',
        fontSize: 14,
        tabSize: 2,
        padding: {
          top: 3,
          bottom: 3
        }
      });

      model.onDidChangeContent(function () {
        latestValue = model.getValue();
        if (!suppressChangeEvent) {
          postMessage('monaco.contentChanged', { value: latestValue });
        }
      });

      postMessage('monaco.ready');
    }, function (error) {
      notifyError('Failed to boot Monaco.', error && error.message ? error.message : String(error || ''));
    });
  }

  fetch('monaco-editor.bootstrap.json', { cache: 'no-store' })
    .then(function (response) {
      if (!response.ok) {
        throw new Error('Bootstrap config not found.');
      }

      return response.json();
    })
    .then(function (config) {
      if (config && typeof config === 'object') {
        bootstrapConfig = config;
      }
    })
    .catch(function () {
      bootstrapConfig = bootstrapConfig;
    })
    .finally(initializeEditor);

  window.onerror = function (message, source, line, column, error) {
    notifyError(message, error && error.stack ? error.stack : ((source || '') + ':' + (line || 0) + ':' + (column || 0)));
  };
})();
