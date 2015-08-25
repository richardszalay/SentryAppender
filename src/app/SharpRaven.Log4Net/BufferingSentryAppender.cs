using System;
using System.Collections.Generic;
using System.Linq;

using SharpRaven.Data;
using SharpRaven.Log4Net.Extra;

using log4net.Appender;
using log4net.Core;

namespace SharpRaven.Log4Net
{
    public class BufferingSentryAppender : BufferingAppenderSkeleton
    {
        private static RavenClient ravenClient;
        public string DSN { get; set; }
        public string Logger { get; set; }
        private readonly IList<SentryTag> tagLayouts = new List<SentryTag>();

        public void AddTag(SentryTag tag)
        {
            tagLayouts.Add(tag);
        }

        protected override void SendBuffer(LoggingEvent[] events)
        {
            // Buffered events aren't supported until Sentry Protocol 7.0 - https://github.com/getsentry/sentry/issues/1092
            foreach (var loggingEvent in events)
            {
                SendToSentry(loggingEvent);
            }
        }

        private void SendToSentry(LoggingEvent loggingEvent)
        {
            if (ravenClient == null)
            {
                ravenClient = new RavenClient(DSN)
                {
                    Logger = Logger
                };
            }

            var extra = SentryAppender.GetExtra();

            var tags = tagLayouts.ToDictionary(t => t.Name, t => (t.Layout.Format(loggingEvent) ?? "").ToString());

            var exception = loggingEvent.ExceptionObject ?? loggingEvent.MessageObject as Exception;
            var level = SentryAppender.Translate(loggingEvent.Level);

            if (exception != null)
            {
                ravenClient.CaptureException(exception, null, level, tags: tags, extra: extra);
            }
            else
            {
                var message = loggingEvent.RenderedMessage;

                if (message != null)
                {
                    ravenClient.CaptureMessage(message, level, tags, extra);
                }
            }
        }
    }
}