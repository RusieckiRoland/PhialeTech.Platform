# PhialeGis.WebAssets

Self-hosted, offline web assets package for reuse across **UWP, WPF, WinUI, Avalonia** in PhialeGis projects.
Current content ships **Monaco Editor** (`min/vs`) so it can run without Internet access.

## What’s inside

* `Assets/Monaco/min/vs/**` – Monaco Editor (loader, core, workers, languages)
* *No* app code here – just static files.

> After adding this NuGet to an app, the assets will be copied into the app package/output under `Assets/Monaco/min/vs/...`.

## Folder layout

```
Assets/
  Monaco/
    min/
      vs/
        loader.js
        editor/
        language/
        base/
        ...
```

## How to use

### UWP (WebView)

```html
<!-- phiale-dsl-host.html in your app -->
<script src="ms-appx-web:///Assets/Monaco/min/vs/loader.js"></script>
<script>
  require.config({ paths: { vs: 'ms-appx-web:///Assets/Monaco/min/vs' } });
  require(['vs/editor/editor.main'], function () {
    // your boot code here
  });
</script>
```

### WinUI 3 / WPF (WebView2)

```html
<script src="Assets/Monaco/min/vs/loader.js"></script>
<script>
  require.config({ paths: { vs: 'Assets/Monaco/min/vs' } });
  require(['vs/editor/editor.main'], function () {
    // your boot code here
  });
</script>
```

*(Ensure `Assets/Monaco/min/vs` is copied to the app's output. ContentFiles from this NuGet will do it.)*

### Avalonia (WebView)

Same as WPF/WinUI – use a relative path to `Assets/Monaco/min/vs`.

## Why a separate package?

* One source of truth for web assets (no copying between UWP/WPF/WinUI/Avalonia).
* No Internet/CDN dependency.
* Stable versioning (apps reference an exact asset version).

## License & attribution

This package **bundles Monaco Editor** files as permitted by its MIT license.

* **Monaco Editor** © Microsoft Corporation – Licensed under the MIT License.
* Original license and notices are included in the shipped content (see `Assets/Monaco/LICENSE` and `Assets/Monaco/ThirdPartyNotices.txt`) and are preserved unmodified.

Wrapper metadata:

* © Roland Rusiecki / PhialeGis – wrapper that redistributes the unmodified Monaco “min” assets for offline use.

If you upgrade Monaco, keep the upstream `LICENSE` and `ThirdPartyNotices.txt` alongside the assets.

## Notes

* Workers are resolved automatically by Monaco using the configured `vs` base path.
* Do **not** mix CDN and local paths in one page.
* If you add your own JS bridge (e.g., `phiale-dsl-host.js`), keep it next to the HTML and load it after `loader.js`.


