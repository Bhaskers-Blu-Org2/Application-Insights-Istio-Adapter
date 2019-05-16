﻿namespace Microsoft.IstioMixerPlugin.Library
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Net;
    using Microsoft.IstioMixerPlugin.Common;
    using System.IO;
    using System.Runtime.Serialization.Json;
    using Microsoft.IstioMixerPlugin.Library.Inputs;

    public class WebServer
    {
        private HttpListener listener;
        private volatile bool isRunning = false;
        public WebServer(string[] prefixes)
        {
            if (isRunning)
            {
                Diagnostics.LogWarn(FormattableString.Invariant($"web server running . aborting"));
                return;
            }
            if (!HttpListener.IsSupported)
            {
                string logLine = FormattableString.Invariant($"HttpListener is not supported");
                Diagnostics.LogError(logLine);
                throw new InvalidOperationException(logLine);
            }

            // URI prefixes are required,
            // for example "http://*:8080/index/".
            if (prefixes == null || prefixes.Length == 0)
            {
                string logLine = FormattableString.Invariant($"WebServer prefixes missing");
                Diagnostics.LogError(logLine);
                throw new ArgumentException(logLine);
            }

            // Create a listener.
            this.listener = new HttpListener();
            // Add the prefixes.
            foreach (string s in prefixes)
            {
                this.listener.Prefixes.Add(s);
            }

            this.listener.Start();
            this.isRunning = true;
            listener.BeginGetContext(new AsyncCallback(this.ListenerCallbackAsync), this.listener);
            Diagnostics.LogInfo(FormattableString.Invariant($"Webserver running"));
        }

        private void ListenerCallbackAsync(IAsyncResult result)
        {
            HttpListener listener = (HttpListener)result.AsyncState;
            // Call EndGetContext to complete the asynchronous operation.
            HttpListenerContext context = listener.EndGetContext(result);
            HttpListenerRequest request = context.Request;

            DataContractJsonSerializer serializer =
                new DataContractJsonSerializer(typeof(JsonPayloadObject));
            JsonPayloadObject payloadObject = (JsonPayloadObject)serializer.ReadObject(request.InputStream);

            Diagnostics.LogInfo(FormattableString.Invariant($"received payload with id : {payloadObject.id}"));

            HttpListenerResponse response = context.Response;
            response.StatusCode = (int)HttpStatusCode.Accepted;
            response.Close();

            if (this.isRunning)
            {
                listener.BeginGetContext(new AsyncCallback(this.ListenerCallbackAsync), this.listener);
                Diagnostics.LogInfo("Restarting listening");
            }
        }
    }

}
