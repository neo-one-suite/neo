using Akka.Actor;
using Akka.Event;
using Neo.Plugins;
using System.Text;

namespace Neo
{
    /// <summary>
    /// A utility class that provides common functions.
    /// </summary>
    public static class Utility
    {
        internal class Logger : ReceiveActor
        {
            public Logger()
            {
                Receive<InitializeLogger>(_ => Sender.Tell(new LoggerInitialized()));
                Receive<LogEvent>(e => Log(e.LogSource, (LogLevel)e.LogLevel(), e.Message));
            }
        }

        /// <summary>
        /// A strict UTF8 encoding used in NEO system.
        /// </summary>
        public static Encoding StrictUTF8 { get; }

        static Utility()
        {
            StrictUTF8 = (Encoding)Encoding.UTF8.Clone();
            StrictUTF8.DecoderFallback = DecoderFallback.ExceptionFallback;
            StrictUTF8.EncoderFallback = EncoderFallback.ExceptionFallback;
        }

        /// <param name="config">Configuration</param>
        /// <returns>IConfigurationRoot</returns>
        public static IConfigurationRoot LoadConfig(string config)
        {
            var env = Environment.GetEnvironmentVariable("NEO_NETWORK");
            var configFile = string.IsNullOrWhiteSpace(env) ? $"{config}.json" : $"{config}.{env}.json";

            // Working directory
            var file = Path.Combine(Environment.CurrentDirectory, configFile);
            if (!File.Exists(file))
            {
                // EntryPoint folder
                try {
                    file = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), configFile);
                } catch {
                    // do nothing
                }
                if (!File.Exists(file))
                {
                    // neo.dll folder
                    file = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), configFile);
                    if (!File.Exists(file))
                    {
                        // default config
                        return new ConfigurationBuilder().Build();
                    }
                }
            }

            return new ConfigurationBuilder()
                .AddJsonFile(file, true)
                .Build();
        }

        /// <summary>
        /// Writes a log.
        /// </summary>
        /// <param name="source">The source of the log. Used to identify the producer of the log.</param>
        /// <param name="level">The level of the log.</param>
        /// <param name="message">The message of the log.</param>
        public static void Log(string source, LogLevel level, object message)
        {
            foreach (ILogPlugin plugin in Plugin.Loggers)
                plugin.Log(source, level, message);
        }
    }
}
