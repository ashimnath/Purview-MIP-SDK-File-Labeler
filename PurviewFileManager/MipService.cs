using System;
using System.IO;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Packaging;
using Microsoft.Identity.Client;
using Microsoft.InformationProtection;
using Microsoft.InformationProtection.File;
using MipSdkDotNetQuickstart;
using System.Collections.Generic;

namespace PurviewFileManager;

public class MipService
{    

    private IFileEngine? _fileEngine;
    private IFileProfile? _fileProfile;

    private IAuthDelegate? _authDelegate;

    private const string ClientId =
        "3a3994a7-3118-442d-a789-xxxxxxxxxxxx";

    private const string TenantId =
        "e91fc23a-f264-49c8-95ea-xxxxxxxxxxxxxx";

    private readonly string _clientId =
    "3a3994a7-3118-442d-a789-xxxxxxxxxx";

    private readonly string _tenantId =
        "e91fc23a-f264-49c8-95ea-xxxxxxxxxx";

    private readonly string _applicationName =
        "PurviewFileManager";

    private readonly string _applicationVersion =
        "1.0";
    private MipContext? _mipContext;

    private ApplicationInfo? _appInfo;

    public void CreateAuthDelegate()
    {
        var appInfo =
            CreateApplicationInfo();

        _authDelegate =
            new AuthDelegateImplementation(
                appInfo);
    }

    private ApplicationInfo CreateApplicationInfo()
    {
        return new ApplicationInfo()
        {
            ApplicationId = _clientId,
            ApplicationName = _applicationName,
            ApplicationVersion = _applicationVersion
        };
    }

    public void CreateMipContext()
    {
        _appInfo =
            CreateApplicationInfo();

        MipConfiguration mipConfiguration =
            new MipConfiguration(
                _appInfo,
                "mip_data",
                Microsoft.InformationProtection.LogLevel.Trace,
                false);

        _mipContext =
            MIP.CreateMipContext(
                mipConfiguration);
    }
    public async Task<string> SignInAsync()
    {
        var app = PublicClientApplicationBuilder
            .Create(ClientId)
            .WithAuthority(
                AzureCloudInstance.AzurePublic,
                TenantId)
            .WithDefaultRedirectUri()
            .Build();

        var result = await app
            .AcquireTokenInteractive(
                new[] { "User.Read" })
            .ExecuteAsync();

        return result.Account.Username;
    }
    
    public void CreateFileProfile()
    {
        if (_mipContext == null)
        {
            throw new Exception(
                "MipContext has not been created.");
        }

        var profileSettings =
            new FileProfileSettings(
                _mipContext,
                CacheStorageType.OnDiskEncrypted,
                new ConsentDelegateImplementation());

        _fileProfile =
            Task.Run(async () =>
                await MIP.LoadFileProfileAsync(
                    profileSettings))
            .Result;
    }

    public void CreateFileEngine()
    {
        if (_fileProfile == null)
        {
            throw new Exception(
                "File Profile has not been created.");
        }

        if (_authDelegate == null)
        {
            throw new Exception(
                "Auth Delegate has not been created.");
        }

        var identity =
            ((AuthDelegateImplementation)_authDelegate)
            .GetUserIdentity();

        var engineSettings =
            new FileEngineSettings(
                identity.Email,
                _authDelegate,
                "",
                "en-US")
            {
                Identity = identity
            };

        _fileEngine =
            Task.Run(async () =>
                await _fileProfile.AddEngineAsync(
                    engineSettings))
            .Result;
    }
    public async Task<List<LabelInfo>> GetLabelsAsync()
    {
        if (_fileEngine == null)
        {
            throw new Exception("File Engine has not been created.");
        }

        await Task.Delay(1);

        var labels =new List<LabelInfo>();

        foreach (var label in _fileEngine.SensitivityLabels)
        {
            if (!label.IsActive)
            {
                continue;
            }

            labels.Add(
                new LabelInfo
                {
                    Id = label.Id,
                    Name = label.Name
                });

            foreach (var child in label.Children)
            {
                if (!child.IsActive)
                {
                    continue;
                }

                labels.Add(
                    new LabelInfo
                    {
                        Id = child.Id,
                        Name = $"    {child.Name}"
                    });
            }
        }

        return labels;
    }
    public ContentLabel ReadLabel(
    string filePath)
    {
        if (_fileEngine == null)
        {
            throw new Exception(
                "File Engine has not been created.");
        }

        var handler =
            Task.Run(async () =>
                await _fileEngine.CreateFileHandlerAsync(
                    filePath,
                    filePath,
                    true))
            .Result;
        
        return handler.Label;
    }

    public bool ApplyLabel(string filePath, string labelId)
    {
        if (_fileEngine == null)
        {
            throw new Exception(
                "File Engine has not been created.");
        }

        var handler =
            Task.Run(async () =>
                await _fileEngine.CreateFileHandlerAsync(
                    filePath,
                    filePath,
                    true))
            .Result;

        LabelingOptions labelingOptions =
            new LabelingOptions()
            {
                AssignmentMethod =
                    AssignmentMethod.Standard
            };

        handler.SetLabel(
            _fileEngine.GetLabelById(labelId),
            labelingOptions,
            new ProtectionSettings());

        string tempFile =
            Path.Combine(
                Path.GetDirectoryName(filePath)!,
                Path.GetFileNameWithoutExtension(filePath) +
                "_TEMP" +
                Path.GetExtension(filePath));

        bool result = false;

        if (handler.IsModified())
        {
            result =
                Task.Run(async () =>
                    await handler.CommitAsync(tempFile))
                .Result;
        }

        if (result)
        {
            try
            {
                File.Copy(
                    tempFile,
                    filePath,
                    true);

                if (File.Exists(tempFile))
                {
                    File.Delete(
                        tempFile);
                }
            }
            catch (IOException)
            {
                throw new Exception(
                    "The file is currently open by another application. Please close the document and try again.");
            }

            return true;
        }

        return false;



        //return result;
    }
    public bool RemoveLabel(string filePath)
    {
        throw new NotSupportedException(
        "Remove Label is currently not supported by this SDK implementation. " +
        "The SDK rejects null labels (BadInputException).");
    }

    public string ReadFileContent(string filePath)
    {
        if (_fileEngine == null)
        {
            throw new Exception(
                "File Engine has not been created. Please load the labels first.");
        }

        string extension =
            Path.GetExtension(filePath)
                .ToLowerInvariant();

        string readableFilePath = filePath;
        string? decryptedTempFile = null;

        try
        {
            var handler =
                Task.Run(async () =>
                    await _fileEngine.CreateFileHandlerAsync(
                        filePath,
                        filePath,
                        true))
                .Result;

            if (handler.Protection != null)
            {
                decryptedTempFile =
                    Task.Run(async () =>
                        await handler.GetDecryptedTemporaryFileAsync())
                    .Result;

                readableFilePath = decryptedTempFile;
            }

            if (extension == ".txt")
            {
                return File.ReadAllText(
                    readableFilePath);
            }

            if (extension == ".docx")
            {
                using var doc =
                    WordprocessingDocument.Open(
                        readableFilePath,
                        false);

                var body =
                    doc.MainDocumentPart?
                       .Document?
                       .Body;

                if (body == null)
                {
                    return
                        "The Word document does not contain readable body text.";
                }

                return body.InnerText;
            }

            return
                $"Content preview not supported for {extension} files.";
        }
        catch (AggregateException ex)
        {
            Exception actualException =
                ex.InnerException ?? ex;

            throw new Exception(
                GetFriendlyReadError(
                    actualException),
                actualException);
        }
        catch (Exception ex)
        {
            throw new Exception(
                GetFriendlyReadError(ex),
                ex);
        }
        finally
        {
            if (!string.IsNullOrWhiteSpace(
                    decryptedTempFile) &&
                File.Exists(decryptedTempFile))
            {
                try
                {
                    File.Delete(
                        decryptedTempFile);
                }
                catch
                {
                    // Do not fail content reading only because
                    // temporary-file cleanup was unsuccessful.
                }
            }
        }
    }

    private string GetFriendlyReadError(
    Exception exception)
    {
        string message =
            exception.Message;

        if (message.Contains(
                "access",
                StringComparison.OrdinalIgnoreCase) ||
            message.Contains(
                "permission",
                StringComparison.OrdinalIgnoreCase) ||
            message.Contains(
                "rights",
                StringComparison.OrdinalIgnoreCase) ||
            message.Contains(
                "unauthorized",
                StringComparison.OrdinalIgnoreCase))
        {
            return
                "The signed-in account does not have permission to decrypt and read this file.";
        }

        if (message.Contains(
                "encrypted packages are not supported",
                StringComparison.OrdinalIgnoreCase))
        {
            return
                "The file is encrypted and could not be decrypted before the content preview was created.";
        }

        if (exception is IOException)
        {
            return
                "The file could not be read. Please close Word or any other application using the file, then try again.";
        }

        return
            "The file content could not be read. Reason: " +
            message;
    }
}
