﻿using System.IO;
using System.Threading.Tasks;
using SharpCrop.LocalFile.Properties;
using SharpCrop.Provider;
using SharpCrop.Provider.Forms;

namespace SharpCrop.LocalFile
{
    /// <summary>
    /// An IProvider implementation which writes the output to the local disk with File.IO.
    /// </summary>
    public class Provider : IProvider
    {
        private string state;

        public string Id => Config.ProviderId;

        public string Name => Resources.ProviderName;

        /// <summary>
        /// Register a path which will be saved as the state of this provider.
        /// </summary>
        /// <param name="savedState"></param>
        /// <param name="silent"></param>
        /// <returns></returns>
        public Task<string> Register(string savedState = null, bool silent = false)
        {
            var result = new TaskCompletionSource<string>();

            // Check if the saved path (state) is still exists - if it is, use it
            if (savedState != null && Directory.Exists(savedState))
            {
                state = savedState;

                result.SetResult(savedState);
                return result.Task;
            }

            // If the saved state was not usable and silent is true, return with failure
            if (silent)
            {
                result.SetResult(null);
                return result.Task;
            }

            // Get a new path with FolderForm
            var form = new FolderForm();
            var success = false;

            form.OnResult += path =>
            {
                success = true;
                state = path;

                result.SetResult(path);
                form.Close();
            };

            form.FormClosed += (sender, e) =>
            {
                if(success == false)
                {
                    result.SetResult(null);
                }
            };

            form.Show();

            return result.Task;
        }

        /// <summary>
        /// Write a MemoryStream with the given name to disk.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="stream"></param>
        /// <returns></returns>
        public Task<string> Upload(string name, MemoryStream stream)
        {
            var url = Path.Combine(state, name);

            File.WriteAllBytes(url, stream.ToArray());

            return Task.FromResult($"{Config.UrlPrefix}{url}");
        }
    }
}
