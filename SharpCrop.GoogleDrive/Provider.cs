﻿using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Services;
using SharpCrop.GoogleDrive.Properties;
using SharpCrop.GoogleDrive.Utils;
using SharpCrop.Provider;
using SharpCrop.Provider.Utils;
using File = Google.Apis.Drive.v3.Data.File;

namespace SharpCrop.GoogleDrive
{
    /// <summary>
    /// An IProvider implementation for Google Drive. 
    /// </summary>
    public class Provider : IProvider
    {
        private DriveService service;
        private MemoryStore state;

        public string Id => Config.ProviderId;

        public string Name => Resources.ProviderName;

        /// <summary>
        /// Get a working Provider for Google Drive.
        /// </summary>
        /// <param name="savedState">Serialized TokenResponse from the Google API (and folderId).</param>
        /// <param name="silent"></param>
        /// <returns></returns>
        public async Task<string> Register(string savedState = null, bool silent = false)
        {
            var result = new TaskCompletionSource<string>();

            try
            {
                state = new MemoryStore(Obscure.Base64Decode(savedState));

                // Try to use the previously saved TokenResponse and if it fails, show the registration form (if the showForm parameter allows it)
                var secret = new ClientSecrets { ClientId = Obscure.CaesarDecode(Config.AppKey), ClientSecret = Obscure.CaesarDecode(Config.AppSecret) };
                var receiver = new CodeReceiver(silent);

                var credentials = await GoogleWebAuthorizationBroker.AuthorizeAsync(secret, Config.Scopes, "token", CancellationToken.None, state, receiver);

                service = new DriveService(new BaseClientService.Initializer
                {
                    HttpClientInitializer = credentials,
                    ApplicationName = "SharpCrop"
                });
                
                // Try to use the saved application folder or get a new one
                // If the access token was expired or corrupted, it is safer to get the folder id again
                if(receiver.Executed || await state.GetAsync<string>("folderId") == null)
                {
                    var folder = await GetFolder();

                    await state.DeleteAsync<string>("folderId");
                    await state.StoreAsync("folderId", folder?.Id);
                }

                // Export the serialized TokenResponse which gonna be saved into the config
                result.SetResult(Obscure.Base64Encode(state.Export()));
            }
            catch
            {
                result.SetResult(null);
            }

            return await result.Task;
        }

        /// <summary>
        /// Upload a stream with the given name to Google Drive, share it and return its link.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="stream"></param>
        /// <returns></returns>
        public async Task<string> Upload(string name, MemoryStream stream)
        {
            var folderId = await state.GetAsync<string>("folderId");
            var uploadBody = new File { Name = name,  Parents = new List<string> { folderId } };
            var uploadType = $"image/{MimeLookup.Find(Path.GetExtension(name))}";
            var uploadRequest = service.Files.Create(uploadBody, stream, uploadType);

            await uploadRequest.UploadAsync();

            var uploadPermission = service.Permissions.Create(new Permission { Type = "anyone", Role = "reader" }, uploadRequest.ResponseBody.Id);

            await uploadPermission.ExecuteAsync();

            return $"https://drive.google.com/open?id={uploadRequest.ResponseBody.Id}";
        }

        /// <summary>
        /// Get the application folder from Google Drive.
        /// </summary>
        /// <returns></returns>
        private async Task<File> GetFolder()
        {
            // Use a random generated number as a description to always find the folder
            var folderBody = new File
            {
                Name = Config.FolderName,
                Description = Config.FolderDescription,
                MimeType = "application/vnd.google-apps.folder"
            };

            var listRequest = service.Files.List();

            listRequest.Q = $"name = '{folderBody.Name}' and fullText contains '{folderBody.Description}' and mimeType = '{folderBody.MimeType}' and trashed = false";
            listRequest.Spaces = "drive";
            listRequest.Fields = "files(id)";

            var result = listRequest.Execute();

            if (result.Files.Count > 0)
            {
                return result.Files[0];
            }

            var createRequest = service.Files.Create(folderBody);

            createRequest.Fields = "id";

            return await createRequest.ExecuteAsync();
        }
    }
}
