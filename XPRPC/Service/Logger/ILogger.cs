/*
 * Copyright 2020, Guan Xiaopeng
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */
 
using System;
using System.Text;

namespace XPRPC.Service.Logger
{
    public interface ILogger : IDisposable
    {
        void Debug(string tag, string message);
        void Info(string tag, string message);
        void Warning(string tag, string message);
        void Error(string tag, string message);
    }

    public static class LoggerExtension
    {
        public static void Exception(this ILogger logger, string tag, Exception ex)
        {
            logger.Error(tag, ExceptionString(ex));
        }

        private static string ExceptionString(Exception ex)
        {
            var builder = new StringBuilder(ex.Message);
            builder.Append("\n").Append(ex.StackTrace);
            if (ex.InnerException != null)
            {
                builder.Append("\n").Append(ExceptionString(ex.InnerException));
            }
            return builder.ToString();
        }
    }

    public sealed class TaggedLogger
    {
        readonly ILogger _logger;
        readonly string _tag;
        public TaggedLogger(ILogger logger, string tag)
        {
            _logger = logger;
            _tag = tag;
        }

        public void D(string message) { _logger.Debug(_tag, message); }
        public void I(string message) { _logger.Info(_tag, message); }
        public void W(string message) { _logger.Warning(_tag, message); }
        public void E(string message) { _logger.Error(_tag, message); }
        public void Ex(Exception ex) { _logger.Exception(_tag, ex); }
    }
}
