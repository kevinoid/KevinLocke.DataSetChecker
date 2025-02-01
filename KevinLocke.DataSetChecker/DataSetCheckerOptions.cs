// <copyright file="DataSetCheckerOptions.cs" company="Kevin Locke">
// Copyright 2017-2025 Kevin Locke &lt;kevin@kevinlocke.name&gt;
// This file is part of KevinLocke.DataSetChecker,
// publicly available under the MIT License.  See LICENSE.txt for details.
// </copyright>

namespace KevinLocke.DataSetChecker
{
    using System.Collections.Generic;

    /// <summary>
    /// Command-line options for <see cref="DataSetChecker"/>.
    /// </summary>
    public class DataSetCheckerOptions : DataSetCheckerSettings
    {
        public ICollection<string> DataSetFilePaths { get; } =
            [];

        public bool ShowHelp { get; set; }
    }
}
