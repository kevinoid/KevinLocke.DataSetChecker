// <copyright file="DataSetChecker.cs" company="Kevin Locke">
// Copyright 2017 Kevin Locke &lt;kevin@kevinlocke.name&gt;
// This file is part of KevinLocke.DataSetChecker,
// publicly available under the MIT License.  See LICENSE.txt for details.
// </copyright>

namespace KevinLocke.DataSetChecker
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Data;
    using System.Data.SqlClient;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Reflection;
    using System.Xml;
    using System.Xml.Schema;

    using static System.FormattableString;

    /// <summary>
    /// Checks the queries in a Typed DataSet XSD for correctness.
    /// </summary>
    public class DataSetChecker
    {
        private const string MsDsNamespace = "urn:schemas-microsoft-com:xml-msdatasource";
        private static readonly XmlNamespaceManager MsDsNsManager;

        private readonly SqlConnection sqlConnection;

        static DataSetChecker()
        {
            MsDsNsManager = new XmlNamespaceManager(new NameTable());
            MsDsNsManager.AddNamespace("msds", MsDsNamespace);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataSetChecker"/> class.
        /// </summary>
        /// <param name="sqlConnection">Connection which will be used for
        /// checking.</param>
        public DataSetChecker(SqlConnection sqlConnection)
        {
            this.sqlConnection = sqlConnection ?? throw new ArgumentNullException(nameof(sqlConnection));

            using (SqlCommand sqlCommand = new SqlCommand("SET FMTONLY ON", sqlConnection))
            {
                sqlCommand.ExecuteNonQuery();
            }
        }

        public event EventHandler<DataSetCheckerEventArgs> DataSetCheckerEventHandler;

        /// <summary>
        /// Entry-point to check a named XSD against a given collection.
        /// </summary>
        /// <param name="args">Command-line arguments.  First is a connection
        /// string.  All subsequent arguments are paths to XSD files to
        /// check.</param>
        /// <returns>0 if the XSD passes all checks.  1 Otherwise.</returns>
        public static int Main(string[] args)
        {
            if (args == null || args.Length < 2)
            {
                Console.Error.WriteLine("Error: Missing required arguments.\n" +
                    "Usage: DataSetQueryChecker <Connection String> <XSD...>");
                return 1;
            }

            using (SqlConnection sqlConnection = new SqlConnection(args[0]))
            {
                sqlConnection.Open();

                DataSetChecker checker = new DataSetChecker(sqlConnection);

                bool first = true;
                foreach (string xsdPath in args)
                {
                    if (first)
                    {
                        first = false;
                        continue;
                    }

                    checker.Check(xsdPath);
                }
            }

            return 0;
        }

        [SuppressMessage(
            "Microsoft.Design",
            "CA1054:UriParametersShouldNotBeStrings",
            MessageId = "0#",
            Justification = "Passthrough to XMLReader.Create")]
        public void Check(string xsdUri)
        {
            XmlDocument xsdDocument;
            using (XmlReader xsdReader = XmlReader.Create(xsdUri))
            {
                xsdDocument = new XmlDocument();
                xsdDocument.Load(xsdReader);
            }

            this.Check(xsdDocument);
        }

        public void Check(XmlDocument xsdDocument)
        {
            if (xsdDocument == null)
            {
                throw new ArgumentNullException(nameof(xsdDocument));
            }

            foreach (XmlNode tableAdapter in xsdDocument.GetElementsByTagName("TableAdapter", MsDsNamespace))
            {
                this.CheckTableAdapter(tableAdapter);
            }
        }

        protected void CheckTableAdapter(XmlNode tableAdapter)
        {
            foreach (XmlNode dbSource in tableAdapter.SelectNodes("//DbCommand", MsDsNsManager))
            {
                this.CheckDbCommand(dbSource);
            }
        }

        protected void CheckDbCommand(XmlNode dbCommand)
        {
            string commandText = null;
            List<SqlParameter> sqlParameters = null;
            foreach (XmlNode childNode in dbCommand.ChildNodes)
            {
                if (childNode.NamespaceURI != MsDsNamespace)
                {
                    continue;
                }

                switch (childNode.LocalName)
                {
                    case "CommandText":
                        if (commandText == null)
                        {
                            this.LogError("DbCommand with multiple CommandText", dbCommand);
                            return;
                        }

                        if (childNode.ChildNodes.Count == 1)
                        {
                            this.LogError(
                                Invariant($"Invalid CommandText with {childNode.ChildNodes.Count} child nodes."),
                                childNode);
                            return;
                        }

                        XmlNode commandTextNode = childNode.FirstChild;
                        if (commandTextNode.NodeType != XmlNodeType.Text &&
                            commandTextNode.NodeType != XmlNodeType.CDATA)
                        {
                            this.LogError(
                                Invariant($"Invalid CommandText with {commandTextNode.NodeType} child node."),
                                childNode);
                            return;
                        }

                        commandText = commandTextNode.Value;
                        break;

                    case "Parameters":
                        sqlParameters = new List<SqlParameter>();
                        foreach (XmlNode parameterNode in childNode.ChildNodes)
                        {
                            if (parameterNode.NamespaceURI == MsDsNamespace && parameterNode.LocalName == "Parameter")
                            {
                                SqlParameter sqlParameter = this.ConvertParameter(parameterNode);
                                sqlParameters.Add(sqlParameter);
                            }
                        }

                        break;
                }
            }

            if (commandText == null)
            {
                this.LogError("DbCommand missing CommandText", dbCommand);
                return;
            }
        }

        protected void CheckSql(string sql, SqlParameter[] sqlParameters)
        {
            using (SqlCommand sqlCommand = new SqlCommand(sql, this.sqlConnection))
            {
                sqlCommand.Parameters.AddRange(sqlParameters);
                using (SqlDataReader sqlDataReader = sqlCommand.ExecuteReader())
                {
                    Debug.Assert(!sqlDataReader.HasRows, "FMTONLY queries shouldn't have rows");
                    Debug.Assert(sqlDataReader.RecordsAffected == 0, "FMTONLY queries shouldn't affect records");

                    // TODO: Check column names/types
                }
            }
        }

        protected SqlParameter ConvertParameter(XmlNode parameterNode)
        {
            if (parameterNode == null)
            {
                throw new ArgumentNullException(nameof(parameterNode));
            }

            SqlParameter sqlParameter = new SqlParameter();
            foreach (PropertyInfo propertyInfo in typeof(SqlParameter).GetProperties())
            {
                string propName = propertyInfo.Name;
                if (propName == "SqlDbType")
                {
                    propName = "ProviderType";
                }

                XmlAttribute xmlAttribute = parameterNode.Attributes[propName];
                if (xmlAttribute == null)
                {
                    continue;
                }

                try
                {
                    TypeConverter converter = TypeDescriptor.GetConverter(propertyInfo.PropertyType);
                    object value = converter.ConvertFromString(xmlAttribute.Value);
                    propertyInfo.SetValue(sqlParameter, value);
                }
                catch (ArgumentException ex)
                {
                    this.LogError(
                        Invariant($"Unable to set {propName} of Parameter"),
                        parameterNode,
                        ex);
                }
            }

            return sqlParameter;
        }

        protected void LogError(string message, XmlNode node, Exception exception = null)
        {
            DataSetCheckerEventArgs args = new DataSetCheckerEventArgs(
                XmlSeverityType.Error,
                message,
                node,
                exception);
            this.DataSetCheckerEventHandler(this, args);
        }
    }
}
