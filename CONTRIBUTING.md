# Contributing to AutoClicker

Thank you for helping improve AutoClicker. Bug reports, focused feature ideas,
documentation improvements, and code contributions are welcome.

By participating, you agree to follow the [Code of Conduct](CODE_OF_CONDUCT.md).

## Before You Start

- Search the existing issues to avoid duplicates.
- Use the bug-report form for reproducible problems.
- Use the feature-request form for proposed behavior changes.
- Open an issue before making a large UI, architecture, or behavior change so
  the approach can be discussed first.
- Do not include passwords, tokens, personal data, or other sensitive
  information in an issue or pull request.

## Development Requirements

AutoClicker is a Windows x64 WinForms application. A local build requires:

- Windows 10 or Windows 11
- Windows PowerShell 5.1 or later
- The 64-bit C# compiler included with .NET Framework 4.x at
  `%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe`

Visual Studio and the modern .NET SDK are not required.

## Make a Contribution

1. Fork the repository and create a branch from `main`.
2. Keep the change focused and avoid unrelated formatting or generated files.
3. Update documentation when behavior or controls change.
4. Build the local artifact without copying it to the desktop:

   ```powershell
   .\Build.ps1 -SkipDesktopCopy
   ```

5. Run `dist\AutoClicker.exe` and test the affected behavior on Windows.
6. Commit the source and documentation changes. Do not commit the generated
   executable from `dist\`.
7. Open a pull request and complete the pull-request checklist.

## Validation Guidance

For behavior changes, test the relevant combinations of:

- Start and stop by button and the global `F6` hotkey
- Position capture by `F7`, delayed capture, coordinates, and target marker
- Left, right, and middle mouse buttons
- Single and double clicks
- Unlimited and fixed repeat counts
- Minimum, preset, and custom intervals with and without random variance
- Emergency stop, window closing, and invalid input handling
- Multiple-monitor coordinates when the change affects targeting

Use a harmless target such as a text editor or local test window. Do not test
against services where automated clicking violates terms or could cause a
purchase, submission, deletion, or other irreversible action.

## Code and Documentation Style

- Preserve compatibility with the .NET Framework compiler used by `Build.ps1`.
- Keep UI access on the UI thread and keep click-loop cancellation responsive.
- Check native API return values and keep cleanup paths reliable.
- Compile with the existing warning level and do not introduce new warnings.
- Prefer descriptive names and comments that explain non-obvious constraints.
- Keep user-facing documentation accurate and concise.

## Reporting Security Problems

Do not publish exploitable security details in a public issue. Contact the
maintainer privately at ezharddscope@gmail.com with a description, reproduction
steps, affected versions, and any suggested mitigation.

## License

By contributing, you agree that your contributions will be licensed under the
[MIT License](LICENSE).
