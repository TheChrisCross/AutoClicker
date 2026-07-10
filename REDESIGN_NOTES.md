# AutoClicker Redesign Notes

## Design direction

AutoClicker is treated as a compact precision utility rather than a gaming-themed tool or a dense settings dialog.

- Visual language: cold graphite surfaces with one teal accent
- Hierarchy: run state and Start / Stop action first, settings second
- Density: compact enough for everyday use, but with clear task grouping
- Motion: native hover and pressed feedback only
- Typography: native Segoe UI for Windows consistency and reliable rendering
- Shape system: 16-20 px panels, 9-14 px controls
- Accessibility: keyboard navigation, visible focus cues, tooltips, accessible field names, DPI scaling, and readable contrast

## Implemented in the local redesign

### Interface

- New 760×642 client layout with Timing and Target sections
- Large run-state panel with semantic status indicator
- Primary Start / Stop button with distinct ready and running states
- Live click counter and finite-run progress
- Interval preset buttons
- Two-second target capture workflow
- Optional always-on-top main window
- Dark Windows title bar where supported
- Cleaner draggable target marker

### Features

- Single and double click modes
- Repeat count with `0` for unlimited
- Escape stop when the app has focus
- Hotkey registration failure reporting
- Virtual-desktop-aware coordinate limits
- Settings lockout while a run is active
- Automatic marker hiding before a run

### Reliability

- Replaced deprecated `mouse_event` with `SendInput`
- Snapshot run settings on the UI thread
- Removed synchronous worker-to-UI `Invoke` calls from the click loop
- Added event-based cancellation for immediate stops
- Prevented overlapping runs during shutdown
- Made the target marker non-activating
- Added application metadata and version 1.1.0.0
- Changed builds to compile into `dist` first, then copy the successful artifact

### Icon

- New pointer-and-click-ripple symbol
- Graphite tile with a single teal accent
- Nine embedded Windows sizes: 16, 20, 24, 32, 40, 48, 64, 128, and 256 px
- Verified visually at native small sizes

## Recommended next improvements

These should be considered before the next public release, but were intentionally left out of the current pass to keep the utility focused.

1. **Settings persistence and Reset defaults**
   - Save the last interval, variance, click type, button, repeat count, and always-on-top preference under Local AppData.
   - Never persist an active running state.

2. **Configurable global hotkeys**
   - Keep F6 and F7 as defaults.
   - Detect conflicts immediately and let the user choose alternatives.

3. **Single-instance behavior**
   - Bring the existing window forward when a second copy launches.
   - Avoid duplicate hotkey registration and user confusion.

4. **Optional system-tray mode**
   - Minimize to tray, show current state, and provide Start / Stop and Exit actions.
   - Keep this opt-in so closing behavior remains predictable.

5. **Test click**
   - Send one clearly labeled test click after a short countdown.
   - Useful for verifying target and mouse button without starting a run.

6. **Start countdown option**
   - Optional 1-5 second delay before a run begins.
   - Helpful when the user must switch to another application first.

7. **Release trust**
   - Add Authenticode signing when practical.
   - Publish SHA-256 checksums with GitHub releases.
   - Add a lightweight release checklist covering icon resources, version metadata, SmartScreen expectations, and malware scanning.

## Features intentionally not recommended

- Macro recording
- Scripting language
- Cloud synchronization
- Accounts or telemetry
- Large profile management system
- Heavy theme customization

Those features would turn a dependable single-purpose utility into a more complex automation product and increase maintenance and trust costs.
