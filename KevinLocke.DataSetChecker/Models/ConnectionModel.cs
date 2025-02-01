// <copyright file="ConnectionModel.cs" company="Kevin Locke">
// Copyright 2017-2018 Kevin Locke &lt;kevin@kevinlocke.name&gt;
// This file is part of KevinLocke.DataSetChecker,
// publicly available under the MIT License.  See LICENSE.txt for details.
// </copyright>

namespace KevinLocke.DataSetChecker.Models
{
    using System.CodeDom;
    using System.Xml.Serialization;

    [XmlType(
        Namespace = DataSetConstants.MsDsNamespace,
        TypeName = "Connection")]
    public class ConnectionModel
    {
        [XmlAttribute]
        public string AppSettingsObjectName { get; set; }

        [XmlAttribute]
        public string AppSettingsPropertyName { get; set; }

        [XmlAttribute]
        public string ConnectionStringObject { get; set; }

        [XmlAttribute]
        public bool IsAppSettingsProperty { get; set; }

        [XmlAttribute]
        public MemberAttributes Modifier { get; set; }

        [XmlAttribute]
        public string Name { get; set; }

        [XmlAttribute]
        public string ParameterPrefix { get; set; }

        [XmlAttribute]
        public string PropertyReference { get; set; }

        [XmlAttribute]
        public string Provider { get; set; }
    }
}
