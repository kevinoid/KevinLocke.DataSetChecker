// <copyright file="DataSetCheckerEventArgs.cs" company="Kevin Locke">
// Copyright 2017 Kevin Locke &lt;kevin@kevinlocke.name&gt;
// This file is part of KevinLocke.DataSetChecker,
// publicly available under the MIT License.  See LICENSE.txt for details.
// </copyright>

namespace KevinLocke.DataSetChecker
{
    using System;
    using System.Xml;
    using System.Xml.Schema;

    public class DataSetCheckerEventArgs : EventArgs
    {
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
    }
}
