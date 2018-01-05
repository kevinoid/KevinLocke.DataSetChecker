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
    using System.Data.SqlTypes;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Reflection;
    using System.Text;
    using System.Xml;
    using System.Xml.Schema;

    using static System.FormattableString;

    /// <summary>
    /// Checks the queries in a Typed DataSet XSD for correctness.
    /// </summary>
    public class DataSetChecker
    {
        private static readonly XmlNamespaceManager MsDsNsManager =
            CreateNamespaceManager();

        private static readonly Dictionary<SqlDbType, object> ParameterDefaultValues = new Dictionary<SqlDbType, object>
        {
            { SqlDbType.BigInt, 0L },
            { SqlDbType.Binary, new byte[0] },
            { SqlDbType.Bit, false },
            { SqlDbType.Char, " " },
            { SqlDbType.Date, new DateTime(1900, 1, 1) },
            { SqlDbType.DateTime, new DateTime(1900, 1, 1) },
            { SqlDbType.DateTime2, new DateTime(1900, 1, 1) },
            { SqlDbType.DateTimeOffset, new DateTimeOffset(new DateTime(1900, 1, 1)) },
            { SqlDbType.Decimal, 0m },
            { SqlDbType.Float, 0d },
            { SqlDbType.Image, new byte[0] },
            { SqlDbType.Int, 0 },
            { SqlDbType.Money, 0m },
            { SqlDbType.NChar, string.Empty },
            { SqlDbType.NText, string.Empty },
            { SqlDbType.NVarChar, string.Empty },
            { SqlDbType.Real, 0f },
            { SqlDbType.SmallDateTime, new DateTime(1900, 1, 1) },
            { SqlDbType.SmallInt, (short)0 },
            { SqlDbType.SmallMoney, 0m },
            { SqlDbType.Text, string.Empty },
            { SqlDbType.Time, default(TimeSpan) },
            { SqlDbType.Timestamp, new byte[0] },
            { SqlDbType.TinyInt, (byte)0 },
            { SqlDbType.UniqueIdentifier, default(Guid) },
            { SqlDbType.VarBinary, new byte[0] },
            { SqlDbType.VarChar, string.Empty },
            { SqlDbType.Xml, new SqlXml() },
        };

        private static readonly Dictionary<string, string> ParameterPropToAttr = new Dictionary<string, string>
        {
            { "IsNullable", "AllowDbNull" },
            { "ProviderType", "SqlDbType" }
        };

        private readonly bool hasSPDescribe;
        private readonly SqlConnection sqlConnection;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataSetChecker"/> class.
        /// </summary>
        /// <param name="sqlConnection">Connection which will be used for
        /// checking.</param>
        public DataSetChecker(SqlConnection sqlConnection)
        {
            this.sqlConnection = sqlConnection ?? throw new ArgumentNullException(nameof(sqlConnection));

            try
            {
                using (SqlCommand sqlCommand = new SqlCommand("sp_describe_first_result_set", sqlConnection))
                {
                    sqlCommand.CommandType = CommandType.StoredProcedure;
                    sqlCommand.Parameters.AddWithValue("tsql", "SELECT 1");

                    using (SqlDataReader sqlDataReader = sqlCommand.ExecuteReader())
                    {
                        this.hasSPDescribe = sqlDataReader.HasRows;
                    }
                }
            }
            catch (SqlException ex)
            {
                if (ex.Number != 2812)
                {
                    // Error was not "Could not find stored procedure"
                    throw;
                }
            }

            if (!this.hasSPDescribe)
            {
                using (SqlCommand sqlCommand = new SqlCommand("SET FMTONLY ON", sqlConnection))
                {
                    sqlCommand.ExecuteNonQuery();
                }
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
                checker.DataSetCheckerEventHandler += Checker_DataSetCheckerEventHandler;

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

            // TODO: Set appropriate error code based on reported errors
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

            foreach (XmlNode dbCommand in xsdDocument.SelectNodes("//msds:DbCommand", MsDsNsManager))
            {
                this.CheckDbCommand(dbCommand);
            }
        }

        protected void CheckDbCommand(XmlNode dbCommand)
        {
            if (dbCommand == null)
            {
                throw new ArgumentNullException(nameof(dbCommand));
            }

            string commandText = null;
            List<SqlParameter> sqlParameters = null;
            foreach (XmlNode childNode in dbCommand.ChildNodes)
            {
                if (childNode.NamespaceURI != DataSetConstants.MsDsNamespace)
                {
                    continue;
                }

                switch (childNode.LocalName)
                {
                    case "CommandText":
                        if (commandText != null)
                        {
                            this.LogError("DbCommand with multiple CommandText", dbCommand);
                            return;
                        }

                        if (childNode.ChildNodes.Count != 1)
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
                            if (parameterNode.NamespaceURI == DataSetConstants.MsDsNamespace && parameterNode.LocalName == "Parameter")
                            {
                                SqlParameter sqlParameter = this.ConvertParameter(parameterNode);
                                sqlParameter.Value = this.GetParameterValue(sqlParameter);
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

            XmlAttribute commandTypeAttr = dbCommand.Attributes["CommandType"];
            if (commandTypeAttr == null)
            {
                this.LogError("DbCommand missing CommandType attribute", dbCommand);
                return;
            }

            if (!Enum.TryParse(commandTypeAttr.Value, out CommandType commandType))
            {
                this.LogError("Unrecognized CommandType " + commandTypeAttr.Value, dbCommand);
                return;
            }

            try
            {
                this.CheckCommand(commandText, commandType, sqlParameters);
            }
            catch (SqlException ex)
            {
                switch (ex.Number)
                {
                    case 11509:
                        this.LogWarning("Command can return different result schemas", dbCommand, ex);
                        break;
                    case 11513: // dynamic SQL
                    case 11524: // indirect recursion
                    case 11525: // temporary tables
                        this.LogWarning("Unable to check command", dbCommand, ex);
                        break;
                    default:
                        this.LogError("Unable to execute query", dbCommand, ex);
                        break;
                }
            }
        }

        protected void CheckCommand(
            string commandText,
            CommandType commandType,
            ICollection<SqlParameter> sqlParameters)
        {
            if (this.hasSPDescribe)
            {
                this.CheckCommandSPDescribe(commandText, sqlParameters);
            }
            else
            {
                SqlParameter[] sqlParametersArray = null;
                if (sqlParameters != null)
                {
                    sqlParametersArray = new SqlParameter[sqlParameters.Count];
                    sqlParameters.CopyTo(sqlParametersArray, 0);
                }

                this.CheckCommandFormatOnly(commandText, commandType, sqlParametersArray);
            }
        }

        [SuppressMessage(
            "Microsoft.Security",
            "CA2100:Review SQL queries for security vulnerabilities",
            Justification = "CommandText from XML file")]
        protected void CheckCommandFormatOnly(
            string commandText,
            CommandType commandType,
            SqlParameter[] sqlParameters)
        {
            using (SqlCommand sqlCommand = new SqlCommand(commandText, this.sqlConnection))
            {
                sqlCommand.CommandType = commandType;
                sqlCommand.Parameters.AddRange(sqlParameters);
                using (SqlDataReader sqlDataReader = sqlCommand.ExecuteReader())
                {
                    Debug.Assert(!sqlDataReader.HasRows, "FMTONLY queries shouldn't have rows");
                    Debug.Assert(sqlDataReader.RecordsAffected <= 0, "FMTONLY queries shouldn't affect records");

                    // TODO: Check column names/types
                }
            }
        }

        protected void CheckCommandSPDescribe(
            string commandText,
            IEnumerable<SqlParameter> sqlParameters)
        {
            string paramsDecl = GetSqlDeclaration(sqlParameters);
            using (SqlCommand sqlCommand = new SqlCommand("sp_describe_first_result_set", this.sqlConnection))
            {
                sqlCommand.CommandType = CommandType.StoredProcedure;
                sqlCommand.Parameters.Add(new SqlParameter
                {
                    ParameterName = "tsql",
                    Size = -1,
                    SqlDbType = SqlDbType.NVarChar,
                    Value = commandText,
                });
                sqlCommand.Parameters.AddWithValue("params", paramsDecl);

                using (SqlDataReader sqlDataReader = sqlCommand.ExecuteReader())
                {
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

                if (!ParameterPropToAttr.TryGetValue(propName, out string attrName))
                {
                    attrName = propName;
                }

                XmlAttribute xmlAttribute = parameterNode.Attributes[attrName];
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

        protected virtual object GetParameterValue(SqlParameter sqlParameter)
        {
            if (sqlParameter == null)
            {
                throw new ArgumentNullException(nameof(sqlParameter));
            }

            if (sqlParameter.IsNullable)
            {
                return DBNull.Value;
            }

            return ParameterDefaultValues[sqlParameter.SqlDbType];
        }

        protected virtual void OnDataSetCheckerEventHandler(DataSetCheckerEventArgs eventArgs)
        {
            this.DataSetCheckerEventHandler?.Invoke(this, eventArgs);
        }

        private static void Checker_DataSetCheckerEventHandler(object sender, DataSetCheckerEventArgs eventArgs)
        {
            Console.Error.WriteLine(eventArgs);
        }

        private static XmlNamespaceManager CreateNamespaceManager()
        {
            XmlNamespaceManager msDsNsManager = new XmlNamespaceManager(new NameTable());
            msDsNsManager.AddNamespace("msds", DataSetConstants.MsDsNamespace);
            return msDsNsManager;
        }

        private static string GetSqlDeclaration(IEnumerable<SqlParameter> parameters)
        {
            if (parameters == null)
            {
                return string.Empty;
            }

            StringBuilder declaration = new StringBuilder();
            foreach (SqlParameter parameter in parameters)
            {
                declaration.Append(parameter.ParameterName)
                    .Append(' ')
                    .Append(parameter.SqlDbType)
                    .Append(", ");
            }

            if (declaration.Length == 0)
            {
                return string.Empty;
            }

            return declaration.ToString(0, declaration.Length - 2);
        }

        private void LogError(string message, XmlNode node, Exception exception = null)
        {
            // FIXME: Throw if no EventHandler?
            DataSetCheckerEventArgs eventArgs = new DataSetCheckerEventArgs(
                XmlSeverityType.Error,
                message,
                node,
                exception);
            this.OnDataSetCheckerEventHandler(eventArgs);
        }

        private void LogWarning(string message, XmlNode node, Exception exception = null)
        {
            // FIXME: Throw if no EventHandler?
            DataSetCheckerEventArgs eventArgs = new DataSetCheckerEventArgs(
                XmlSeverityType.Warning,
                message,
                node,
                exception);
            this.OnDataSetCheckerEventHandler(eventArgs);
        }
    }
}
