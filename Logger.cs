namespace ICN.Tools.Diagnostics {
    using System;
    using System.Configuration;
    using System.Data;

    using ICN.Tools.Extensions;

    using NLog;
    using NLog.Config;
    using NLog.Targets;
    using NLog.Web.Targets;

    /// <summary>
    /// The logger.
    /// </summary>
    public class Logger : ILogging {
        #region Fields and Constants

        /// <summary>
        /// The _default entry layout.  Based on the default NLog layout.
        /// </summary>
        private const string ConsoleEntryLayout =
            "${longdate} | ${pad:padding=-5:fixedLength=true:inner=${level:uppercase=true}} | ${message} | ${event-context:item=source} | ${event-context:item=additionalInfo} | ${event-context:item=referenceKey} ${onexception:inner=${newline}${newline}${exception:format=tostring}${newline}}";

        /// <summary>
        /// The _file entry layout.
        /// </summary>
        private const string FileEntryLayout =
            "${longdate} | ${pad:padding=-5:fixedLength=true:inner=${level:uppercase=true}} | ${message} | ${event-context:item=source} | ${machinename} | ${identity} | ${event-context:item=additionalInfo} | ${event-context:item=referenceKey} ${onexception:inner=${newline}${newline}${exception:format=tostring}${newline}}";

        /// <summary>
        /// The trace entry layout.
        /// </summary>
        private const string TraceEntryLayout =
            "${longdate} | ${pad:padding=-5:fixedLength=true:inner=${level:uppercase=true}} | ${message} | ${event-context:item=source} | ${event-context:item=referenceKey}";

        /// <summary>
        /// The _logger.
        /// </summary>
        private NLog.Logger _logger;

        /// <summary>
        /// Gets or sets the configuration.
        /// </summary>
        /// <value>
        /// The configuration.
        /// </value>
        private LoggingConfiguration _configuration;

        /// <summary>
        /// The _log to console.
        /// </summary>
        private bool _logToConsole = true;

        /// <summary>
        /// The _log name.
        /// </summary>
        private string _logName = "Log";

        /// <summary>
        /// The _log path.
        /// </summary>
        private string _logPath = "${basedir}";

        /// <summary>
        /// The _archive monthly.
        /// </summary>
        private bool _archiveMonthly = true;

        /// <summary>
        /// The _log to database.
        /// </summary>
        private bool _logToDatabase = true;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Logger"/> class.
        ///     Prevents a default instance of the <see cref="Logger"/> class from being created.
        /// </summary>
        public Logger() {
            this.InitFromConfig();
            this.Init();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Logger"/> class.
        /// </summary>
        /// <param name="logName">
        /// The log name.
        /// </param>
        /// <param name="initFromConfig">
        /// The init from config.
        /// </param>
        public Logger(string logName, bool initFromConfig = true) {
            if (initFromConfig) {
                this.InitFromConfig();
            }

            this.LogName = logName;
            this.Init();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Logger"/> class.
        /// </summary>
        /// <param name="logPath">
        /// The log path.
        /// </param>
        /// <param name="logName">
        /// The log name.
        /// </param>
        /// <param name="initFromConfig">
        /// The init from config.
        /// </param>
        public Logger(string logPath, string logName, bool initFromConfig = true) {
            if (initFromConfig) {
                this.InitFromConfig();
            }

            this.LogName = logName;
            this.LogPath = logPath;
            this.Init();
        }

        #endregion

        #region Public Enums

        /// <summary>
        /// The logging level.
        /// </summary>
        public enum LoggingLevel {
            /// <summary>
            /// The off.
            /// </summary>
            Off = -1, 

            /// <summary>
            /// The trace.
            /// </summary>
            Trace, 

            /// <summary>
            /// The debug.
            /// </summary>
            Debug, 

            /// <summary>
            /// The info.
            /// </summary>
            Info, 

            /// <summary>
            /// The warn.
            /// </summary>
            Warn, 

            /// <summary>
            /// The error.
            /// </summary>
            Error, 

            /// <summary>
            /// The fatal.
            /// </summary>
            Fatal
        }

        #endregion

        #region Properties and Indexers

        /// <summary>
        /// Gets or sets a value indicating whether to archive monthly.
        /// </summary>
        /// <value>
        /// The archive monthly.
        /// </value>
        public bool ArchiveMonthly {
            get {
                return this._archiveMonthly;
            }

            set {
                this._archiveMonthly = value;
            }
        }

        /// <summary>
        /// Gets or sets the connection string.
        /// </summary>
        /// <value>
        /// The connection string.
        /// </value>
        public string ConnectionString { get; set; }

        /// <summary>
        /// Gets or sets the console log level.
        /// </summary>
        /// <value>
        /// The console log level.
        /// </value>
        public LoggingLevel LogLevel { get; set; }

        /// <summary>
        /// Gets or sets the log name.
        /// </summary>
        /// <value>
        /// The log name.
        /// </value>
        public string LogName {
            get {
                return this._logName;
            }

            set {
                this._logName = value;
            }
        }

        /// <summary>
        /// Gets or sets the base directory.
        /// </summary>
        /// <value>
        /// The base directory.
        /// </value>
        public string LogPath {
            get {
                return this._logPath;
            }

            set {
                this._logPath = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether log to console.
        /// </summary>
        /// <value>
        /// The log to console.
        /// </value>
        public bool LogToConsole {
            get {
                return this._logToConsole;
            }

            set {
                this._logToConsole = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether log to database.
        /// </summary>
        /// <value>
        /// The log to database.
        /// </value>
        public bool LogToDatabase {
            get {
                return this._logToDatabase;
            }

            set {
                this._logToDatabase = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether log to file.
        /// </summary>
        /// <value>
        /// The log to file.
        /// </value>
        public bool LogToFile { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether log to trace.
        /// </summary>
        /// <value>
        /// The log to trace.
        /// </value>
        public bool LogToTrace { get; set; }

        #endregion

        #region Public Methods

        /// <summary>
        /// The initialize.
        /// </summary>
        public void Init() {
            this._configuration = new LoggingConfiguration();
            if (this.LogToConsole) {
                var consoleTarget = new ColoredConsoleTarget("console") { Layout = ConsoleEntryLayout };
                this._configuration.AddTarget("console", consoleTarget);

                var logLevel = GetNLogLevel(this.LogLevel);
                var rule = new LoggingRule("*", logLevel, consoleTarget);
                this._configuration.LoggingRules.Add(rule);
            }

            if (this.LogToFile) {
                var extensionIndex = this.LogName.LastIndexOf('.');
                var logName = extensionIndex > 0 ? this.LogName.Substring(0, extensionIndex) : this.LogName;
                var logExtension = extensionIndex > 0 ? this.LogName.Substring(extensionIndex + 1) : "log";
                var fileName = string.Format("{0}/{1}.{2}", this.LogPath, logName, logExtension);
                var archiveFileName = string.Format("{0}/{1}.{2}.{3}", this.LogPath, logName, "{#}", logExtension);
                var target = new FileTarget("file") { FileName = fileName, Layout = FileEntryLayout, CreateDirs = true };

                if (this.ArchiveMonthly) {
                    target.ArchiveEvery = FileArchivePeriod.Month;
                    target.ArchiveNumbering = ArchiveNumberingMode.Date;
                    target.ArchiveDateFormat = "yyyyMMdd";
                    target.ArchiveFileName = archiveFileName;
                    target.EnableArchiveFileCompression = true;
                }

                this._configuration.AddTarget("file", target);

                var logLevel = GetNLogLevel(this.LogLevel);
                var rule = new LoggingRule("*", logLevel, target);
                this._configuration.LoggingRules.Add(rule);
            }

            if (this.LogToDatabase) {
                var target = new DatabaseTarget("database") {
                                                                DBProvider = "System.Data.SqlClient", 
                                                                CommandText = "[dbo].[AddLoggingEntry]",
                                                                CommandType = CommandType.StoredProcedure, 
                                                                ConnectionString = ConfigurationManager.ConnectionStrings["Logging.Database"].ConnectionString
                                                            };

                target.Parameters.Add(new DatabaseParameterInfo("@LogName", "${logger}"));
                target.Parameters.Add(new DatabaseParameterInfo("@EntryDate", "${date}"));
                target.Parameters.Add(new DatabaseParameterInfo("@LogLevel", "${level}"));
                target.Parameters.Add(new DatabaseParameterInfo("@Message", "${message}"));
                target.Parameters.Add(new DatabaseParameterInfo("@Source", "${event-context:item=source}"));
                target.Parameters.Add(new DatabaseParameterInfo("@AdditionalInfo", "${event-context:item=additionalInfo}"));
                target.Parameters.Add(new DatabaseParameterInfo("@MachineName", "${machinename}"));
                target.Parameters.Add(new DatabaseParameterInfo("@Identity", "${identity}"));
                target.Parameters.Add(new DatabaseParameterInfo("@ExceptionData", "${exception:tostring}"));
                target.Parameters.Add(new DatabaseParameterInfo("@URL", "${aspnet-request:serverVariable=HTTP_URL}"));
                target.Parameters.Add(new DatabaseParameterInfo("@ServerName", "${aspnet-request:serverVariable=SERVER_NAME}"));
                target.Parameters.Add(new DatabaseParameterInfo("@ReferenceKey", "${event-context:item=referenceKey}"));

                this._configuration.AddTarget("database", target);

                var logLevel = GetNLogLevel(this.LogLevel);
                var rule = new LoggingRule("*", logLevel, target);
                this._configuration.LoggingRules.Add(rule);
            }

            if (this.LogToTrace) {
                var target = new AspNetTraceTarget { Layout = TraceEntryLayout };
                this._configuration.AddTarget("trace", target);
                
                var logLevel = GetNLogLevel(this.LogLevel);
                var rule = new LoggingRule("*", logLevel, target);
                this._configuration.LoggingRules.Add(rule);
            }

            LogManager.Configuration = this._configuration;
            LogManager.ReconfigExistingLoggers();
            this._logger = LogManager.GetLogger(this.LogName);
        }

        /// <summary>
        /// The write entry.
        /// </summary>
        /// <param name="message">
        /// The message.
        /// </param>
        /// <param name="logLevel">
        /// The log level.
        /// </param>
        /// <param name="source">
        /// The source.
        /// </param>
        /// <param name="additionalInfo">
        /// The additional info.
        /// </param>
        /// <param name="exception">
        /// The exception.
        /// </param>
        public void WriteEntry(string message, LoggingLevel logLevel = LoggingLevel.Debug, string source = "", string additionalInfo = "", Exception exception = null) {
            if (message.IsNullOrEmpty()) {
                return;
            }

            var eventInfo = new LogEventInfo(GetNLogLevel(logLevel), this.LogName, message);
            eventInfo.Properties["source"] = source;
            eventInfo.Properties["additionalInfo"] = additionalInfo;
            eventInfo.Properties["referenceKey"] = Guid.NewGuid().ToString();

            if (exception != null) {
                eventInfo.Exception = exception;
            }

            this._logger.Log(eventInfo);
        }

        /// <summary>
        /// The write error message.
        /// </summary>
        /// <param name="message">
        /// The message.
        /// </param>
        public void WriteErrorMessage(string message) {
            this.WriteEntry(message, LoggingLevel.Error);
        }

        /// <summary>
        /// The write exception.
        /// </summary>
        /// <param name="exception">
        /// The exception.
        /// </param>
        public void WriteException(Exception exception) {
            this.WriteEntry(exception.Message, LoggingLevel.Error, exception.TargetSite.ToString(), string.Empty, exception);
        }

        /// <summary>
        /// The write info message.
        /// </summary>
        /// <param name="message">
        /// The message.
        /// </param>
        public void WriteInfoMessage(string message) {
            this.WriteEntry(message, LoggingLevel.Info);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// The get NLog logging level.
        /// </summary>
        /// <param name="loggingLevel">
        /// The logging level.
        /// </param>
        /// <returns>
        /// The <see cref="LogLevel"/>.
        /// </returns>
        private static LogLevel GetNLogLevel(LoggingLevel loggingLevel) {
            switch (loggingLevel) {
                case LoggingLevel.Debug:
                    return NLog.LogLevel.Debug;
                case LoggingLevel.Error:
                    return NLog.LogLevel.Error;
                case LoggingLevel.Fatal:
                    return NLog.LogLevel.Fatal;
                case LoggingLevel.Info:
                    return NLog.LogLevel.Info;
                case LoggingLevel.Trace:
                    return NLog.LogLevel.Trace;
                case LoggingLevel.Warn:
                    return NLog.LogLevel.Warn;
                default:
                    return NLog.LogLevel.Off;
            }
        }

        /// <summary>
        /// The init.
        /// </summary>
        private void InitFromConfig() {
            this.LogLevel = (ConfigurationManager["Logging.Level"] ?? "Debug").ToEnum<LoggingLevel>(true);
            this.LogName = ConfigurationManager["ApplicationName"];

            if (!ConfigurationManager["Logging.FileName"].IsNullOrWhiteSpace() || !ConfigurationManager["Logging.LogPath"].IsNullOrWhiteSpace()) {
                this.LogToFile = true;
                this.LogPath = !Config.LogPath.IsNullOrWhiteSpace() ? ConfigurationManager["Logging.LogPath"] : this.LogPath;
            }
        }

        #endregion
    }
}