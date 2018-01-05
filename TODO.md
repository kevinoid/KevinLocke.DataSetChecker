* Add a README
* Support testing with different `SET` options (e.g. `SET QUOTED_IDENTIFIER`).
* Set exit code based on presence of errors.
* Read connection string from XSD if not give on command line.
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
* Pass `XmlNode` to `GetSqlDeclaration` for logging.
* Check for unused parameters (parameters not present in SQL)
* Make file reading and SQL calls asynchronous.
* Enable [Asynchronous Processing](https://stackoverflow.com/q/9432647) in
  connection string.
* Enable [MARS](https://docs.microsoft.com/en-us/dotnet/framework/data/adonet/sql/enabling-multiple-active-result-sets)
  in connection string for SQL Server 2005 and later?
* Wrap code for use as Cmdlet
* Package for Choco
