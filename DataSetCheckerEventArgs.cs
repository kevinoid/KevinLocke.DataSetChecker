// <copyright file="DataSetCheckerEventArgs.cs" company="Kevin Locke">
// Copyright 2017 Kevin Locke &lt;kevin@kevinlocke.name&gt;
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
        private static readonly DataSetXPathFinder XPathFinder = new DataSetXPathFinder();

        private readonly Exception exception;
        private readonly string message;
        private readonly XmlNode node;
        private readonly XmlSeverityType severity;

        internal DataSetCheckerEventArgs(XmlSeverityType severity, string message, XmlNode node, Exception exception)
        {
            this.message = message ?? throw new ArgumentNullException(nameof(message));
            this.severity = severity;
            this.node = node;
            this.exception = exception;
        }

        public Exception Exception => this.exception;

        public string Message => this.message;

        public XmlNode Node => this.node;

        public XmlSeverityType Severity => this.severity;

        public override string ToString()
        {
            StringBuilder errMsg = new StringBuilder();
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
