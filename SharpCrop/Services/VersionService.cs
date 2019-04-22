﻿using System;
using System.IO;
using System.Net;
using Newtonsoft.Json.Linq;
using SharpCrop.Models;

namespace SharpCrop.Services
{
    public static class VersionService
    {
        /// <summary>
        /// Check for updates.
        /// </summary>
        /// <returns>URL of the new version or null.</returns>
        public static string GetLatestLink()
        {
            var request = (HttpWebRequest)WebRequest.Create(Config.LatestVersion);

            request.UserAgent = "SharpCrop";

            try
            {
                var response = (HttpWebResponse)request.GetResponse();
                var stream = response.GetResponseStream();

                if (stream == null)
                {
                    return null;
                }

                var reader = new StreamReader(stream);

                dynamic parsed = JObject.Parse(reader.ReadToEnd());

                var tagName = (string) parsed.tag_name;
                var remoteVersion = tagName.Split('.');
                var remoteMajor = int.Parse(remoteVersion[0]);
                var remoteMinor = int.Parse(remoteVersion[1]);

                if (remoteMajor > Config.VersionMajor || 
                    remoteMajor == Config.VersionMajor && remoteMinor > Config.VersionMinor)
                {
                    return parsed.html_url;
                }
            }
            catch
            {
                // Ignored
            }

            return null;
        }

        /// <summary>
        /// Get the type of the operation system.
        /// </summary>
        /// <returns></returns>
        public static PlatformType GetPlatform()
        {
            var os = Environment.OSVersion.Platform;

            switch (os)
            {
                case PlatformID.Win32Windows:
                case PlatformID.Win32NT:
                case PlatformID.Win32S:
                case PlatformID.WinCE:
                case PlatformID.Xbox:
                    return PlatformType.Windows;
                case PlatformID.MacOSX:
                    return PlatformType.Mac;
                default:
                    return PlatformType.Linux;
            }
        }
    }
}
