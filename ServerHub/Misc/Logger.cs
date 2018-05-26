﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace ServerHub.Misc {
    public class Logger {
        private static Logger _instance;

        public static Logger Instance {
            get {
                if (_instance != null) return _instance;
                _instance = new Logger();
                return _instance;
            }
        }

        public enum LoggerLevel {
            Info,
            Warning,
            Exception,
            Error
        }

        public struct LogMessage {
            public string Message { get; set; }
            public LoggerLevel Level { get; set; }

            public LogMessage(string msg, LoggerLevel lvl) {
                Message = msg;
                Level = lvl;
            }
        }

        private Thread LogHandler { get; set; }
        private Queue<LogMessage> LogQueue { get; set; } = new Queue<LogMessage>();
        private FileInfo LogFile { get; }
        private bool IsThreadRunning { get; set; }
        private LogMessage OldLogMessage { get; set; }

        Logger() {
            LogFile = GetPath();
            LogHandler = new Thread(QueueWatcher) {
                IsBackground = true
            };
            IsThreadRunning = true;
            LogHandler.Start();
        }

        #region Log Functions

        public void Log(object msg) {
            if (!LogHandler.IsAlive) throw new Exception("Logger is Closed!");
            LogQueue.Enqueue(new LogMessage($"[LOG @ {DateTime.Now:HH:mm:ss}] {msg}", LoggerLevel.Info));
        }

        public void Error(object msg) {
            if (!LogHandler.IsAlive) throw new Exception("Logger is Closed!");
            LogQueue.Enqueue(new LogMessage($"[ERROR @ {DateTime.Now:HH:mm:ss}] {msg}", LoggerLevel.Error));
        }

        public void Exception(object msg) {
            if (!LogHandler.IsAlive) throw new Exception("Logger is Closed!");
            LogQueue.Enqueue(new LogMessage($"[EXCEPTION @ {DateTime.Now:HH:mm:ss}] {msg}", LoggerLevel.Exception));
        }

        public void Warning(object msg) {
            if (!LogHandler.IsAlive) throw new Exception("Logger is Closed!");
            LogQueue.Enqueue(new LogMessage($"[WARNING @ {DateTime.Now:HH:mm:ss}] {msg}", LoggerLevel.Warning));
        }

        #endregion

        void QueueWatcher() {
            LogFile.Create().Close();
            using (var f = LogFile.AppendText()) {
                f.AutoFlush = true;
                while (IsThreadRunning) {
                    if (!LogQueue.Any()) Thread.Sleep(100);
                    LogHandler.IsBackground = false;
                    while (LogQueue.Any()) {
                        var o = LogQueue.Dequeue();
                        if (o.Message == OldLogMessage.Message) return;
                        OldLogMessage = o;
                        f.WriteLine(o.Message);
                        #if DEBUG
                        Console.ForegroundColor = o.GetColour();
                        Console.WriteLine(o.Message);
                        Console.ResetColor();
                        #endif
                    }
                }
                LogHandler.IsBackground = true;
            }

            LogHandler.Join();
        }

        FileInfo GetPath() {
            var logsDir = new DirectoryInfo($"./{Settings.Instance.Logger.LogsDir}/{DateTime.Now:dd-MM-yy}");
            logsDir.Create();
            return new FileInfo($"{logsDir.FullName}/{logsDir.GetFiles().Length}.txt");
        }

        public void Stop() {
            IsThreadRunning = false;
            _instance = null;
        }
    }
}