// <copyright file="DataSetCheckerEventArgs.cs" company="Kevin Locke">
// Copyright 2017-2025 Kevin Locke &lt;kevin@kevinlocke.name&gt;
// This file is part of KevinLocke.DataSetChecker,
// publicly available under the MIT License.  See LICENSE.txt for details.
// </copyright>

namespace KevinLocke.DataSetChecker
{
    using System;
    using System.Text;
    using System.Xml;
    using System.Xml.Schema;

    public class DataSetCheckerEventArgs : EventArgs
    {
        private static readonly DataSetXPathFinder XPathFinder = new();

        internal DataSetCheckerEventArgs(XmlSeverityType severity, string message, XmlNode? node, Exception? exception)
        {
            this.Message = message ?? throw new ArgumentNullException(nameof(message));
            this.Severity = severity;
            this.Node = node;
            this.Exception = exception;
        }

        public Exception? Exception { get; }

        public string Message { get; }

        public XmlNode? Node { get; }

        public XmlSeverityType Severity { get; }

        public override string ToString()
        {
            StringBuilder errMsg = new();
            errMsg.Append(this.Severity).Append(": ").Append(this.Message);

            if (this.Node != null)
            {
                errMsg.Append(" (at ")
                    .Append(XPathFinder.FindXPathFromTablesOrRoot(this.Node))
                    .Append(" in ")
                    .Append(this.Node.BaseURI)
                    .Append(')');
            }

            if (this.Exception != null)
            {
                errMsg.Append('\n').Append(this.Exception);
            }

            return errMsg.ToString();
        }
    }
}
