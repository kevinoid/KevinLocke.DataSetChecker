// <copyright file="DataSetCheckerSettings.cs" company="Kevin Locke">
// Copyright 2017-2018 Kevin Locke &lt;kevin@kevinlocke.name&gt;
// This file is part of KevinLocke.DataSetChecker,
// publicly available under the MIT License.  See LICENSE.txt for details.
// </copyright>

namespace KevinLocke.DataSetChecker
{
    using System.Configuration;

    /// <summary>
    /// Settings for <see cref="DataSetChecker"/>.
    /// </summary>
    /// <remarks>
    /// Consider extending <see cref="ApplicationSettingsBase"/> for loading
    /// from configuration files as part of
    /// https://docs.microsoft.com/en-us/dotnet/framework/winforms/advanced/application-settings-architecture
    /// </remarks>
    public class DataSetCheckerSettings
    {
        public string ConnectionString { get; set; }

        public bool NoWarnings { get; set; }
    }
}
