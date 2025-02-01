// <copyright file="DataSourceModel.cs" company="Kevin Locke">
// Copyright 2017-2018 Kevin Locke &lt;kevin@kevinlocke.name&gt;
// This file is part of KevinLocke.DataSetChecker,
// publicly available under the MIT License.  See LICENSE.txt for details.
// </copyright>

namespace KevinLocke.DataSetChecker.Models
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Reflection;
    using System.Xml.Serialization;

    [XmlRoot(
        Namespace = DataSetConstants.MsDsNamespace,
        ElementName = "DataSource")]
    public class DataSourceModel
    {
        [XmlAttribute]
        public int DefaultConnectionIndex { get; set; }

        [XmlAttribute]
        public string FunctionsComponentName { get; set; }

        // XmlSerializer works with space-separated items, need comma separated
        // Ignore here and provide string property for XML serialization.
        [XmlIgnore]
        public TypeAttributes Modifier { get; set; }

        [XmlAttribute("Modifier")]
        public string ModifierString
        {
            get { return this.Modifier.ToString(); }
            set { this.Modifier = (TypeAttributes)Enum.Parse(typeof(TypeAttributes), value); }
        }

        [XmlAttribute]
        public SchemaSerializationMode SchemaSerializationMode { get; set; }

        public List<ConnectionModel> Connections { get; } =
            new List<ConnectionModel>();
    }
}
