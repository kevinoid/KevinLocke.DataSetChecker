// <copyright file="DataSetChecker.cs" company="Kevin Locke">
// Copyright 2017-2025 Kevin Locke &lt;kevin@kevinlocke.name&gt;
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
    using System.Text.RegularExpressions;
    using System.Xml;
    using System.Xml.Schema;

    using Mono.Options;

    using static System.FormattableString;

    /// <summary>
    /// Checks the queries in a Typed DataSet XSD for correctness.
    /// </summary>
    public class DataSetChecker : IDisposable
    {
        private static readonly XmlNamespaceManager MsDsNsManager =
            CreateNamespaceManager();

        private static readonly Dictionary<SqlDbType, object> ParameterDefaultValues = new()
        {
            { SqlDbType.BigInt, 0L },
            { SqlDbType.Binary, Array.Empty<byte>() },
            { SqlDbType.Bit, false },
            { SqlDbType.Char, " " },
            { SqlDbType.Date, new DateTime(1900, 1, 1) },
            { SqlDbType.DateTime, new DateTime(1900, 1, 1) },
            { SqlDbType.DateTime2, new DateTime(1900, 1, 1) },
            { SqlDbType.DateTimeOffset, new DateTimeOffset(new DateTime(1900, 1, 1)) },
            { SqlDbType.Decimal, 0m },
            { SqlDbType.Float, 0d },
            { SqlDbType.Image, Array.Empty<byte>() },
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
            { SqlDbType.Timestamp, Array.Empty<byte>() },
            { SqlDbType.TinyInt, (byte)0 },
            { SqlDbType.UniqueIdentifier, default(Guid) },
            { SqlDbType.VarBinary, Array.Empty<byte>() },
            { SqlDbType.VarChar, string.Empty },
            { SqlDbType.Xml, new SqlXml() },
        };

        private static readonly Dictionary<string, string> ParameterPropToAttr = new()
        {
            { "IsNullable", "AllowDbNull" },
            { "ProviderType", "SqlDbType" },
        };

        /// <summary>
        /// Regular expression to match a valid SQL Server parameter.
        /// </summary>
        /// <remarks>
        /// Leading @ made optional due to <see cref="SqlParameter"/> adding it
        /// (or behaving as if it does).
        /// </remarks>
        /// <remarks>
        /// SQL Server actually allows using <c>@</c> as a variable.
        /// (Tested on Microsoft SQL Server 12.0.5207.0)
        /// <see cref="SqlParameter.ParameterName"/> supports <c>"@"</c>, not
        /// <c>""</c>.
        /// </remarks>
        /// <seealso href="https://docs.microsoft.com/sql/relational-databases/databases/database-identifiers">
        /// SQL Server Database Identifiers
        /// </seealso>
        private static readonly Regex SqlServerParameterNameRegex =
            new(@"^[\p{L}\p{Nd}@#$_]+$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private readonly SqlConnection sqlConnectionFmtOnly;
        private readonly SqlConnection sqlConnectionSpDescribe;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataSetChecker"/> class.
        /// </summary>
        /// <param name="connectionString">Connection string which will be used
        /// for checking.</param>
        public DataSetChecker(string connectionString)
        {
            if (connectionString == null)
            {
                throw new ArgumentNullException(nameof(connectionString));
            }

            SqlConnection sqlConnection = new(connectionString);
            sqlConnection.Open();

            bool hasSpDescribe = false;
            try
            {
                using SqlCommand sqlCommand = new("sp_describe_first_result_set", sqlConnection);
                sqlCommand.CommandType = CommandType.StoredProcedure;
                sqlCommand.Parameters.AddWithValue("tsql", "SELECT 1");

                using SqlDataReader sqlDataReader = sqlCommand.ExecuteReader();
                hasSpDescribe = sqlDataReader.HasRows;
            }
            catch (SqlException ex)
            when (ex.Number == 2812)
            {
                // Fallthrough for "Could not find stored procedure"
            }

            if (hasSpDescribe)
            {
                this.sqlConnectionSpDescribe = new SqlConnection(sqlConnection.ConnectionString);
                this.sqlConnectionSpDescribe.Open();
            }

            using (SqlCommand sqlCommand = new("SET FMTONLY ON", sqlConnection))
            {
                sqlCommand.ExecuteNonQuery();
                this.sqlConnectionFmtOnly = sqlConnection;
            }
        }

        public event EventHandler<DataSetCheckerEventArgs> DataSetCheckerEventHandler;

        public void Dispose()
        {
            this.sqlConnectionFmtOnly?.Dispose();
            this.sqlConnectionSpDescribe?.Dispose();
        }

        /// <summary>
        /// Entry-point to check a named XSD against a given collection.
        /// </summary>
        /// <param name="args">Command-line arguments.  First is a connection
        /// string.  All subsequent arguments are paths to XSD files to
        /// check.</param>
        /// <returns>0 if the XSD passes all checks.  1 Otherwise.</returns>
        public static int Main(string[] args)
        {
            DataSetCheckerOptions options = new();
            OptionSet optionSet = GetOptionSetFor(options);
            try
            {
                optionSet.Parse(args);
            }
            catch (OptionException ex)
            {
                Console.Error.WriteLine("Error parsing options: {0}", ex);
                return 1;
            }

            if (options.ShowHelp)
            {
                optionSet.WriteOptionDescriptions(Console.Out);
                return 0;
            }

            if (options.DataSetFilePaths.Count == 0)
            {
                Console.Error.WriteLine("Error: Missing required arguments.");
                optionSet.WriteOptionDescriptions(Console.Error);
                return 1;
            }

            int exitCode = 0;
            using (DataSetChecker checker = new(options.ConnectionString))
            {
                checker.DataSetCheckerEventHandler += (object sender, DataSetCheckerEventArgs eventArgs) =>
                {
                    if (eventArgs.Severity == XmlSeverityType.Error || !options.NoWarnings)
                    {
                        Console.Error.WriteLine(eventArgs);
                    }
                };

                foreach (string xsdPath in options.DataSetFilePaths)
                {
                    try
                    {
                        // TODO: Set exit code based on non-fatal validation errors
                        checker.Check(xsdPath);
                    }
                    catch (XmlException ex)
                    {
                        Console.Error.WriteLine("Error checking {0}: {1}", xsdPath, ex);
                        exitCode = 1;
                    }
                }
            }

            return exitCode;
        }

        [SuppressMessage(
            "Microsoft.Design",
            "CA1054:UriParametersShouldNotBeStrings",
            MessageId = "0#",
            Justification = "Passthrough to XMLReader.Create")]
        public void Check(string xsdUri)
        {
            // Disable DTD processing and external references to avoid issues
            // with untrusted or partially trusted XSD files.
            // (e.g. XML Bombs and resolver information leaks)
            // https://docs.microsoft.com/en-us/visualstudio/code-quality/ca3075-insecure-dtd-processing
            //
            // If anyone is using these in practice, can add option to allow.
            XmlReaderSettings xmlReaderSettings = new()
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
            };

            XmlDocument xsdDocument;
            using (XmlReader xsdReader = XmlReader.Create(xsdUri, xmlReaderSettings))
            {
                xsdDocument = new XmlDocument { XmlResolver = null };
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

#pragma warning disable CS3002 // Return type is not CLS-compliant
        internal static OptionSet GetOptionSetFor(DataSetCheckerOptions options)
#pragma warning restore CS3002 // Return type is not CLS-compliant
        {
            return new OptionSet
            {
                { "h|help", "show this message and exit", (bool showHelp) => options.ShowHelp = showHelp },
                { "w", "suppress warning messages", noWarn => options.NoWarnings = noWarn != null },
                {
                    "<>",
                    arg =>
                    {
                        if (options.ConnectionString == null)
                        {
                            // First argument is connection string
                            options.ConnectionString = arg;
                        }
                        else
                        {
                            // Subsequent arguments are XSDs
                            options.DataSetFilePaths.Add(arg);
                        }
                    }
                },
            };
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
                        sqlParameters = [];
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
            if (this.sqlConnectionSpDescribe != null)
            {
                this.CheckCommandSPDescribe(commandText, sqlParameters);
            }

            if (this.sqlConnectionFmtOnly != null)
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
            using SqlCommand sqlCommand = new(commandText, this.sqlConnectionFmtOnly);
            sqlCommand.CommandType = commandType;
            sqlCommand.Parameters.AddRange(sqlParameters);
            using SqlDataReader sqlDataReader = sqlCommand.ExecuteReader();
            Debug.Assert(!sqlDataReader.HasRows, "FMTONLY queries shouldn't have rows");
            Debug.Assert(sqlDataReader.RecordsAffected <= 0, "FMTONLY queries shouldn't affect records");

            // TODO: Check column names/types
        }

        protected void CheckCommandSPDescribe(
            string commandText,
            IEnumerable<SqlParameter> sqlParameters)
        {
            string paramsDecl = this.GetSqlDeclaration(sqlParameters);
            using SqlCommand sqlCommand = new("sp_describe_first_result_set", this.sqlConnectionSpDescribe);
            sqlCommand.CommandType = CommandType.StoredProcedure;
            sqlCommand.Parameters.Add(new SqlParameter
            {
                ParameterName = "tsql",
                Size = -1,
                SqlDbType = SqlDbType.NVarChar,
                Value = commandText,
            });
            sqlCommand.Parameters.AddWithValue("params", paramsDecl);

            using SqlDataReader sqlDataReader = sqlCommand.ExecuteReader();
            // TODO: Check column names/types
        }

        protected SqlParameter ConvertParameter(XmlNode parameterNode)
        {
            if (parameterNode == null)
            {
                throw new ArgumentNullException(nameof(parameterNode));
            }

            SqlParameter sqlParameter = new();
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

        private static XmlNamespaceManager CreateNamespaceManager()
        {
            XmlNamespaceManager msDsNsManager = new(new NameTable());
            msDsNsManager.AddNamespace("msds", DataSetConstants.MsDsNamespace);
            return msDsNsManager;
        }

        // FIXME: Is this really not implemented somewhere in ADO.NET?
        private string GetSqlDeclaration(IEnumerable<SqlParameter> parameters)
        {
            if (parameters == null)
            {
                return string.Empty;
            }

            StringBuilder declaration = new();
            foreach (SqlParameter parameter in parameters)
            {
                string parameterName = parameter.ParameterName;
                if (!SqlServerParameterNameRegex.IsMatch(parameterName))
                {
                    // SqlCommand appears to ignore these.
                    this.LogWarning(
                        Invariant($"Ignoring Parameter with invalid ParameterName [{parameterName}]"),
                        null);
                    continue;
                }

                if (parameterName.Length == 0 || parameterName[0] != '@')
                {
                    // SqlCommand behaves as if @ prefix is added when not present.
                    declaration.Append('@');
                }

                declaration.Append(parameterName)
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
            DataSetCheckerEventArgs eventArgs = new(
                XmlSeverityType.Error,
                message,
                node,
                exception);
            this.OnDataSetCheckerEventHandler(eventArgs);
        }

        private void LogWarning(string message, XmlNode node, Exception exception = null)
        {
            // FIXME: Throw if no EventHandler?
            DataSetCheckerEventArgs eventArgs = new(
                XmlSeverityType.Warning,
                message,
                node,
                exception);
            this.OnDataSetCheckerEventHandler(eventArgs);
        }
    }
}
