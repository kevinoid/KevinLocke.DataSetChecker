// <copyright file="DataSetChecker.cs" company="Kevin Locke">
// Copyright 2017-2025 Kevin Locke &lt;kevin@kevinlocke.name&gt;
// This file is part of KevinLocke.DataSetChecker,
// publicly available under the MIT License.  See LICENSE.txt for details.
// </copyright>

namespace KevinLocke.DataSetChecker
{
    using System;
    using System.CodeDom;
    using System.Data;
    using System.IO;
    using System.Reflection;
    using System.Xml;
    using System.Xml.Serialization;
    using KevinLocke.DataSetChecker.Models;

    public class DataSetChecker
    {
        public static int Main2(string[] args)
        {
            dynamic dataSource =
                typeof(System.Data.Design.TypedDataSetGenerator).Assembly
                    .CreateInstance("System.Data.Design.DesignDataSource");
            string xsdPath = args[args.Length - 1];
            using (FileStream stream = new FileStream(xsdPath, FileMode.Open, FileAccess.Read))
            {
                dataSource.ReadXmlSchema((Stream)stream, xsdPath);
            }

            object connection = dataSource.DefaultConnection;

            return 0;
        }

        public static int Main(string[] args)
        {
            DataSourceModel dataSourceModel = new DataSourceModel
            {
                DefaultConnectionIndex = 2,
                FunctionsComponentName = "hi",
                Modifier = TypeAttributes.Public | TypeAttributes.Sealed,
                SchemaSerializationMode = SchemaSerializationMode.ExcludeSchema,
            };
            dataSourceModel.Connections.Add(new ConnectionModel
            {
                AppSettingsObjectName = "aon",
            });

            XmlSerializer xmlSerializer = new XmlSerializer(typeof(DataSourceModel));
            //xmlSerializer.Serialize(Console.Out, dataSourceModel);

            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.InnerXml = @"
      <DataSource DefaultConnectionIndex=""2"" FunctionsComponentName=""QueriesTableAdapter"" Modifier=""AutoLayout, AnsiClass, Class, Public"" SchemaSerializationMode=""IncludeSchema"" xmlns=""urn:schemas-microsoft-com:xml-msdatasource"">
        <Connections>
          <Connection ConnectionStringObject=""Data Source=localhost;Initial Catalog=HomeCare;Integrated Security=True;MultipleActiveResultSets=True"" IsAppSettingsProperty=""false"" Modifier=""Assembly"" Name=""user-pc.HomeCare.dbo"" ParameterPrefix=""@"" Provider=""System.Data.SqlClient"" />
          <Connection AppSettingsObjectName=""Settings"" AppSettingsPropertyName=""HomeCareConnectionString"" ConnectionStringObject="""" IsAppSettingsProperty=""true"" Modifier=""Assembly"" Name=""HomeCareConnectionString (Settings)"" ParameterPrefix=""@"" PropertyReference=""ApplicationSettings.ConsoleApp1.Properties.Settings.GlobalReference.Default.HomeCareConnectionString"" Provider=""System.Data.SqlClient"" />
          <Connection Extra=""foo"" ConnectionStringObject=""Driver={SQL Server Native Client 11.0};server=localhost;trusted_connection=Yes;app=Microsoft® Visual Studio®;wsid=USER-PC;database=HomeCare"" IsAppSettingsProperty=""false"" Modifier=""Assembly"" Name=""ODBC.USER-PC.HomeCare"" Provider=""System.Data.Odbc"" />
        </Connections>
      </DataSource>
";
            DataSourceModel deserialized =
                (DataSourceModel)xmlSerializer.Deserialize(new XmlNodeReader(xmlDocument));

            return 0;
        }
    }
}
