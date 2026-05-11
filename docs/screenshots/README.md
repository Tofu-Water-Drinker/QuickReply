# Screenshot sources

The `.png` files in this folder are HTML mockups rendered to image. They
use the exact palette, fonts, and layout proportions from
`src/QuickReply/Theme.cs` so a fresh reader of the GitHub README can see
what the app looks like before downloading anything.

## Regenerating

```pwsh
$chrome = "C:\Program Files\Google\Chrome\Application\chrome.exe"
& $chrome --headless --disable-gpu --hide-scrollbars `
    --window-size=760,750 --screenshot=picker.png `
    "file:///$pwd/picker.html"
& $chrome --headless --disable-gpu --hide-scrollbars `
    --window-size=960,720 --screenshot=manage.png `
    "file:///$pwd/manage.html"
& $chrome --headless --disable-gpu --hide-scrollbars `
    --window-size=1040,700 --screenshot=signature.png `
    "file:///$pwd/signature.html"
```

If the real WinForms UI changes meaningfully (palette, layout, copy), update
the matching HTML file here, re-render, commit both. Anyone replacing these
with actual app screenshots later can delete the HTML files; they exist only
to keep the README's visual proof reproducible.
