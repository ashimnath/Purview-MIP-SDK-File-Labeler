# Purview File Manager

Purview File Manager is a Windows desktop application built with C# and WPF that demonstrates integration with the Microsoft Information Protection (MIP) File SDK.

The application connects to Microsoft Purview, retrieves sensitivity labels published to the signed-in user, reads label and protection information from files, applies sensitivity labels, changes existing labels, and reads the text content of supported files.

> This project is intended for development, learning, and lab validation. Review security, authentication, logging, and deployment requirements before using it in a production environment.

## Features

- Authenticate a user against Microsoft Entra ID
- Initialize the MIP SDK context, file profile, and file engine
- Retrieve sensitivity labels published to the signed-in user
- Exclude inactive sensitivity labels from the label list
- Display parent labels and child labels
- Browse and select a local file
- Read the current sensitivity label
- Display whether label-based protection is applied
- Apply a sensitivity label
- Change an existing sensitivity label
- Upgrade or downgrade a sensitivity label
- Update the original file without leaving `_TEMP` files
- Read text from unprotected DOCX files
- Authorize and decrypt protected DOCX files through the MIP SDK
- Read text from an authorized temporary decrypted copy
- Delete the temporary decrypted copy after reading
- Read plain-text files
- Display operational results and friendly errors in the main application window

## Current Application Workflow

```text
Connect to Microsoft Purview
        |
        v
Initialize MIP context, profile, and file engine
        |
        v
Load sensitivity labels
        |
        v
Browse and select a file
        |
        +----> Read label and protection status
        |
        +----> Read supported file content
        |
        +----> Apply or change a sensitivity label

Supported Content Preview: 
File type	Unprotected file	Purview-protected file.docx	Supported	Supported when the signed-in user has permission
.txt	Supported	Depends on the MIP SDK-supported protected-file workflow
.pdf	Not implemented	Not implemented
.xlsx	Not implemented	Not implemented

A file type may still support MIP labeling even when content preview is not implemented in this application.

Protected DOCX Workflow

Open XML cannot directly open a Purview-encrypted Office package. For protected DOCX files, the application uses this workflow:

Plain Text
Protected DOCX
|
v
Create MIP file handler
|
v
Authorize the signed-in identity
|
v
Create a decrypted temporary file
|
v
Read text using the Open XML SDK
|
v
Delete the decrypted temporary file

The original protected file is not decrypted in place during content preview.

If the signed-in account does not have sufficient rights, the application returns a user-friendly access error instead of displaying the file content.

Technology
C#
.NET
Windows Presentation Foundation (WPF)
Microsoft Information Protection File SDK
Microsoft Authentication Library
Microsoft Entra ID
DocumentFormat.OpenXml
Prerequisites

Before building the application, verify that the following are available:

Windows development machine
Visual Studio with .NET desktop development support
Microsoft 365 test or development tenant
Microsoft Purview sensitivity labels
A sensitivity label policy published to the test user
Microsoft Entra application registration
MIP SDK NuGet dependencies
DocumentFormat.OpenXml NuGet package
An account authorized to access the labels and protected test files
Microsoft Entra Application Registration

Configure a Microsoft Entra application registration for the application.

The project must be updated with the appropriate:

Plain Text
Tenant ID
Client ID
Authority
Redirect URI

Do not commit client secrets, access tokens, refresh tokens, certificates, tenant-specific credentials, or production identifiers to the repository.

For a public client desktop application, ensure that the authentication configuration and redirect URI match the application's authentication implementation.

Configuration

Locate the application configuration values used when building ApplicationInfo, the authentication delegate, and the MIP engine.

Replace development placeholders with values from the intended lab tenant.

Example:

C#
string tenantId = "<YOUR-TENANT-ID>";
string clientId = "<YOUR-CLIENT-ID>";

Before sharing or publishing the repository, verify that the source code does not contain:

Client secrets
Access tokens
Refresh tokens
User passwords
Private certificates
Production tenant details
Protected business documents
Decrypted temporary documents
Sensitive logs
Personally identifiable information
Install Dependencies

Restore the NuGet packages from Visual Studio or use the .NET CLI:

PowerShell
dotnet restore
 

The project currently uses packages corresponding to these capabilities:

Plain Text
Microsoft Information Protection File SDK
Microsoft authentication
Open XML document processing

Package names and versions should be verified in the project file.

Build
Visual Studio
Plain Text
Open the solution
Build
Build Solution
.NET CLI

From the directory containing the project or solution:

PowerShell
dotnet build
Run
Start the application.
Initialize or connect to Microsoft Purview.
Complete authentication when prompted.
Wait for the sensitivity labels to load.
Browse to a test file.
Use Read File to inspect the current label, protection state, and supported content.
Select a sensitivity label.
Use Apply Label to apply or change the label.
Use Read File again to verify the result.
Applying and Changing Labels

The application creates an MIP file handler for the selected file and calls SetLabel() with the selected label.

The change is written to an output file through CommitAsync(). After a successful commit, the application copies the committed output over the original file and removes the temporary output.

Close the file in Word or another application before changing its label. If another process locks the file, Windows may prevent the application from updating it.

Label Downgrade and Justification

The application can change a higher-sensitivity label to a lower active label.

Whether justification is required depends on the sensitivity label policy available to the signed-in user. In the current tested lab configuration, label downgrade succeeded without requesting justification.

A future enhancement can catch:

C#
JustificationRequiredException

and collect a business justification before retrying the operation with:

C#
labelingOptions.IsDowngradeJustified = true;
labelingOptions.JustificationMessage = justification;
Remove Label

The Remove Label UI control is currently retained, but label removal is not implemented successfully by the installed SDK path used by this project.

Calling:

C#
handler.SetLabel(
null,
labelingOptions,
new ProtectionSettings());
``

returns a BadInputException because the SDK rejects a null label.

The application therefore displays a friendly not-supported message instead of showing a raw exception.

Label removal should be revisited using an API explicitly supported by the installed MIP SDK version and the relevant policy requirements.

Important File-Handling Behavior
Close files before changing labels

A selected file should not be open in:

Microsoft Word
Microsoft Excel
Adobe Acrobat or Reader
Another editor
Windows Explorer Preview Pane
Another process that locks the file

If the file is locked, the application displays a message asking the user to close the application using the file and retry.

Temporary files

Label application uses a temporary output file during CommitAsync().

After a successful operation:

The committed output is copied over the original file.
The temporary output is deleted.

Protected DOCX content reading also uses a decrypted temporary file. The application attempts to delete that file in a finally block after extracting the text.

Error Handling

Expected operational errors are displayed in the main application window rather than as raw exception dialogs.

Handled scenarios include:

File engine not initialized
No file selected
No label selected
File locked by another process
Insufficient rights to decrypt a protected file
Unsupported content-preview format
Encrypted package not readable by Open XML
Inactive sensitivity label
Unsupported label removal

During development, full exception details may still be useful for diagnostics. Avoid exposing full stack traces, local paths, tenant identifiers, or correlation data to end users in production builds.

Known Limitations
Content preview is currently focused on DOCX and TXT files.
PDF content extraction is not implemented.
Excel content extraction is not implemented.
Remove Label is not implemented.
A user-facing downgrade justification dialog is not implemented.
The application processes one file at a time.
The UI is intended as a lab utility rather than a production-ready file-management product.
Text extraction from DOCX displays document body text and may not preserve formatting, tables, headers, footers, images, or other document structure.
Protected content can only be read when the authenticated identity has sufficient rights.
Files locked by another application cannot be replaced safely.
Security Considerations

Protected files must be handled carefully.

When the application creates a decrypted temporary file:

Keep the temporary file only for the minimum required duration.
Delete the temporary file after reading.
Do not log decrypted content.
Do not upload decrypted files to external services.
Do not place decrypted files in source control.
Do not expose decrypted text to unauthorized users.
Consider restricting temporary-file permissions in a production implementation.
Consider secure cleanup and endpoint security requirements.
Validate the user's rights before enabling content-processing actions.
Review audit and compliance requirements before production deployment.

This sample application should not be treated as a mechanism to bypass Microsoft Purview protection. Protected content should only be processed when the authenticated identity is authorized.

Recommended .gitignore

Ensure that the repository excludes build artifacts, Visual Studio files, local configuration, logs, test documents, and temporary files.

.ignore
# Build output
bin/
obj/
 
# Visual Studio
.vs/
*.user
*.suo
*.userosscache
*.*ln.docstates
 
#*JetBrains
.idea*
 
#*Logs
*.log
 
**Temporary labeling output
*_TEMP.*
*_TEMP_TEMP.*
 
# Local test files
TestFiles/
LocalFiles/
ProtectedFiles/
DecryptedFiles/
 
# Local configuration and secrets
appsettings.Development.json
appsettings.Local.json
secrets.json
.env
.env.*
 
# Certificates and keys
*.pfx
*.p12
*.key**.pem
 
#*Packages
packages/
 
# Operating sy*tem files
Thumbs.db
.DS*Store

Review already-committed files as well. Adding an entry to .gitignore does not remove a file that Git is already tracking.

Suggested Project Structure
Plain Text
Purview*ileManager/
|
+--*Main*indow.xaml
+-- MainWindow.xaml.cs
*-- MipService.cs
+*- AuthDelegateImplementation.cs
**- ConsentDelegateImplementation.cs*+-- LabelInfo.cs
+*- App.xaml
+-- App.xaml.cs
*-- PurviewFileManager.csproj
*-- README.md
+*- .gitignore

The exact structure may differ depending on the current solution.

Suggested Test Scenarios

Use non-sensitive lab files.

Read an unprotected DOCX
Plain Text
Expected:
*abel information*is displayed.
Protection*status is displayed.
Document*body text is displayed.
``*
Read a protected DOCX with permission
Plain Text
Expected:
The*MIP SDK authorizes access.
A tempo*ary decrypted copy is created.
Doc*ment body text is displayed.
The t*mporary copy is deleted.
Read a protected DOCX without permission
Plain Text
Expected:
Content is*not displayed.
A meaningful permis*ion*message is shown.
Apply a label
Plain Text
Expected:
The*selected label is applied.
The ori*inal file is updated.
No temporary*output remains.
Downgrade a label
Plain Text
Expe*ted:
The lower*active label is applied when permi*ted by policy.
If justification is*required by policy, additional app*ication logic is needed.
Modify a file that is open in Word
Plain Text
Expected:
The file*update is blocked.
A*meaningful*file*in-use message is displayed.
Troubleshooting
An inactive label was specified

Reload the sensitivity labels and verify that inactive labels are excluded from the ComboBox. Confirm that the intended label is still active and published to the signed-in user.

Access to the path is denied

Close Word, Excel, Adobe Reader, Windows Explorer Preview Pane, or any other application using the file.

Also verify:

The user has write access to the folder.
The file is not read-only.
Endpoint security software is not holding the file.
The application is not selecting an old _TEMP output.
The process cannot access the file because another process is using it

Close the application that has the file open and retry. The application cannot safely replace a file while another process holds an incompatible lock.

Encrypted packages are not supported

Open XML cannot directly open a Purview-encrypted DOCX package. The application must use the MIP SDK to create an authorized decrypted temporary copy before opening it with Open XML.

The signed-in account cannot read protected content

Confirm that:

The expected account authenticated successfully.
The account is included in the label's protection permissions.
The account received a usage right that allows the requested operation.
The file was protected using the expected tenant and label.
Authentication and policy caches are current.
Label removal reports that a null label is invalid

The installed SDK path rejects SetLabel(null, ...). Label removal remains an unsupported application feature until it is implemented using an API available in the installed SDK version.

Roadmap

Possible future enhancements:

PDF text extraction
Excel worksheet text extraction
Label removal using a supported SDK API
Downgrade justification dialog
Bulk file processing
Drag-and-drop file selection
Progress indicators
Structured file-information panel
Detailed rights display
Audit-event support
Safer and more restrictive temporary-file storage
Application logging with sensitive-data redaction
Automated tests
Installer and deployment packaging
Disclaimer

This repository is an independent lab and development project. It is not an official Microsoft product and does not provide a production support commitment.

Microsoft Purview, Microsoft Information Protection, Microsoft Entra, Microsoft 365, Windows, and Visual Studio are Microsoft products or services.

Use only test data unless the application has completed the security, privacy, compliance, supportability, and operational reviews required by the intended organization.

License

No license has been selected for this private repository.

If the repository will be shared, reused, or published, add an appropriate license after reviewing organizational and third-party dependency requirements.


## Important checks before committing

Because the repository is private but still hosted remotely, verify these items before pushing the README:

1. Search the project for the actual tenant ID and client ID.
2. Confirm that no client secret, token, certificate, or password is committed.
3. Remove protected and decrypted test documents from Git tracking.
4. Add patterns for `_TEMP` files to `.gitignore`.
5. Keep tenant-specific configuration outside committed source where practical.
6. Confirm the README does not contain credentials or private repository URLs.

### Writing improvements

The README separates implemented functionality from planned functionality, clearly documents the protected-file flow, and avoids claiming that unsupported PDF, Excel, label removal, or justification features currently work. It also highlights the security implications of producing decrypted temporary files, which is especially important for a MIP SDK application.
