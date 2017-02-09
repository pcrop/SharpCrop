﻿using SharpCrop.FTP.Models;
using System;
using System.IO;
using System.Net;

namespace SharpCrop.FTP.Utils
{
    public class FTPUploader
    {
        /// <summary>
        /// Upload a MemoryStream object to the given FTP server.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="path"></param>
        /// <param name="username"></param>
        /// <param name="password"></param>
        public static void Upload(MemoryStream stream, string path, string username, string password)
        {
            // Create a Uri instance with the specified URI string.
            // If the URI is not correctly formed, the Uri constructor
            // will throw an exception.
            var target = new Uri(path);

            var state = new FTPState();
            var request = (FtpWebRequest)WebRequest.Create(target);

            request.Method = WebRequestMethods.Ftp.UploadFile;
            request.Credentials = new NetworkCredential(username, password);

            // Store the request in the object that we pass into the asynchronous operations.
            state.Request = request;
            state.Stream = stream;

            // Get the event to wait on.
            var waitObject = state.OperationComplete;

            // Asynchronously get the stream for the file contents.
            request.BeginGetRequestStream(new AsyncCallback(EndGetStreamCallback), state);

            // Block the current thread until all operations are complete.
            waitObject.WaitOne();

            // The operations either completed or threw an exception.
            if (state.OperationException != null)
            {
                throw state.OperationException;
            }
        }

        /// <summary>
        /// Actual uploading.
        /// </summary>
        /// <param name="ar"></param>
        private static void EndGetStreamCallback(IAsyncResult ar)
        {
            var state = (FTPState)ar.AsyncState;
            Stream requestStream = null;

            // End the asynchronous call to get the request stream.
            try
            {
                // Copy the file contents to the request stream.
                requestStream = state.Request.EndGetRequestStream(ar);

                const int bufferLength = 2048;

                byte[] buffer = new byte[bufferLength];
                int readBytes = 0;
                int count = 0;

                do
                {
                    readBytes = state.Stream.Read(buffer, 0, bufferLength);
                    requestStream.Write(buffer, 0, readBytes);
                    count += readBytes;
                }
                while (readBytes != 0);

                // IMPORTANT: Close the request stream before sending the request.
                requestStream.Close();

                // Asynchronously get the response to the upload request.
                state.Request.BeginGetResponse(new AsyncCallback(EndGetResponseCallback), state);
            }
            catch (Exception e)
            {
                // Return exceptions to the main application thread
                state.OperationException = e;
                state.OperationComplete.Set();
                return;
            }

        }

        /// <summary>
        /// The EndGetResponseCallback method completes a call to BeginGetResponse
        /// </summary>
        /// <param name="ar"></param>
        private static void EndGetResponseCallback(IAsyncResult ar)
        {
            FTPState state = (FTPState)ar.AsyncState;
            FtpWebResponse response = null;

            try
            {
                response = (FtpWebResponse)state.Request.EndGetResponse(ar);
                response.Close();

                // Signal the main application thread that the operation is complete
                state.StatusDescription = response.StatusDescription;
                state.OperationComplete.Set();
            }
            catch (Exception e)
            {
                // Return exceptions to the main application thread
                state.OperationException = e;
                state.OperationComplete.Set();
            }
        }
    }
}
