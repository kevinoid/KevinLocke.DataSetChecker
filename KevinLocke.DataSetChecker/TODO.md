* Add a README
* Support testing with different `SET` options (e.g. `SET QUOTED_IDENTIFIER`).
* Set exit code based on presence of errors.
* Read connection string from XSD if not give on command line.
* Use `DbConnection` instead of `SqlConnection`
  https://stackoverflow.com/a/185482/503410
  - Can get using [`DbProviderFactories`](https://docs.microsoft.com/en-us/dotnet/api/system.data.common.dbproviderfactories)
    Note: Recent support in .NET Core
    https://github.com/dotnet/corefx/pull/25410
    Workaround:
    https://weblog.west-wind.com/posts/2017/Nov/27/Working-around-the-lack-of-dynamic-DbProviderFactory-loading-in-NET-Core
* Test with different providers (Access, ODBC, SQL Server, etc.)
* Choose DbSource name attribute based on command type
* Add configurable error reporters/formatters.
  * Output in JUnit or other consumable XML format?
* Use [SET FMTONLY
  replacements](https://docs.microsoft.com/en-us/sql/t-sql/statements/set-fmtonly-transact-sql)
  when available.
* Suppress "Invalid object name" errors due to temporary tables. (FMTONLY only?)
  http://greglow.com/2017/04/12/avoiding-invalid-object-name-errors-with-temporary-tables-for-biztalk-reporting-services-and-apps-using-set-fmtonly/
* Suppress "Incorrect syntax" and other errors due to exec of dynamic string
  with bad value due to using default parameter values.
* Check result schema
* Warn about parameters with empty `ParameterName` (ignored by `SqlCommand`)?
* Warn if SQL response schema contains columns not declared in table schema.
* Warn if SQL response schema lacks columns declared in table schema.
* Warn if SQL response schema column type doesn't match table schema.
* Warn if SQL response schema column nullability doesn't match table schema.
* Warn if SQL response schema column max length doesn't match table schema.
* Pass `XmlNode` to `GetSqlDeclaration` for logging.
* Check for unused parameters (parameters not present in SQL)
* Make file reading and SQL calls asynchronous.
* Enable [Asynchronous Processing](https://stackoverflow.com/q/9432647) in
  connection string.
* Enable [MARS](https://docs.microsoft.com/en-us/dotnet/framework/data/adonet/sql/enabling-multiple-active-result-sets)
  in connection string for SQL Server 2005 and later?
* Check for `xs:type`/`msdata:DataType` mismatch which could lead to
  corruption warned about in
  https://docs.microsoft.com/en-us/dotnet/api/system.data.dataset.readxmlschema
* Wrap code for use as Cmdlet
* Package for NuGet/Chocolatey
* Find (or write) a schema to check the TypedDataset XSD for validity (to
  catch errors like invalid attribute names/values).  Does Microsoft document
  these XML Namespaces anywhere?
  urn:schemas-microsoft-com:xml-msdatasource
  urn:schemas-microsoft-com:xml-msdata
  urn:schemas-microsoft-com:xml-msprop
