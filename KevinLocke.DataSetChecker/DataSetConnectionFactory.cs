// <copyright file="DataSetConnectionFactory.cs" company="Kevin Locke">
// Copyright 2017-2018 Kevin Locke &lt;kevin@kevinlocke.name&gt;
// This file is part of KevinLocke.DataSetChecker,
// publicly available under the MIT License.  See LICENSE.txt for details.
// </copyright>

namespace KevinLocke.DataSetChecker
{
    using System.Xml;
    using System.Xml.Serialization;
    using System.Xml.XPath;
    using KevinLocke.DataSetChecker.Models;

    public class DataSetConnectionFactory
    {
        public DataSetConnectionFactory(XmlReader xmlReader)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(ConnectionModel));
            while (xmlSerializer.CanDeserialize(xmlReader))
                xmlSerializer.Deserialize(xmlReader);
        }
    }
}
