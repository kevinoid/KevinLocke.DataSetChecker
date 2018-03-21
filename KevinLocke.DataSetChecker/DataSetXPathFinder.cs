// <copyright file="DataSetXPathFinder.cs" company="Kevin Locke">
// Copyright 2017-2018 Kevin Locke &lt;kevin@kevinlocke.name&gt;
// This file is part of KevinLocke.DataSetChecker,
// publicly available under the MIT License.  See LICENSE.txt for details.
// </copyright>

namespace KevinLocke.DataSetChecker
{
    using System;
    using System.Collections.Generic;
    using System.Xml;

    /// <summary>
    /// <see cref="XPathFinder"/> specialized for use on Typed DataSet XSDs.
    /// </summary>
    public class DataSetXPathFinder : XPathFinder
    {
        private static readonly Dictionary<string, string[]> UniqueAttrNamesByLocalName = new Dictionary<string, string[]>
        {
            {
                "DbSource",
                new string[]
                {
                    "UserGetMethodName",
                    "GetMethodName",
                    "GeneratorGetMethodName",
                    "UserSourceName",
                    "FillMethodName",
                    "GeneratorSourceName",
                }
            },
            {
                "TableAdapter",
                new string[]
                {
                    "Name",
                    "UserDataComponentName",
                    "DataAccessorName",
                    "GeneratorDataComponentClassName",
                }
            },
        };

        public virtual string FindXPathFromTablesOrRoot(XmlNode node)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            for (XmlNode parentNode = node.ParentNode; parentNode != null; parentNode = parentNode.ParentNode)
            {
                if (parentNode.NamespaceURI == DataSetConstants.MsDsNamespace &&
                    parentNode.LocalName == "Tables")
                {
                    return this.FindXPath(node, parentNode);
                }
            }

            return this.FindXPath(node);
        }

        protected override string FindPredicate(XmlNode node, Predicate<XmlNode> siblingIsMatch)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            if (node.NamespaceURI == DataSetConstants.MsDsNamespace &&
                UniqueAttrNamesByLocalName.TryGetValue(node.LocalName, out string[] uniqueAttrNames))
            {
                foreach (string attrName in uniqueAttrNames)
                {
                    XmlAttribute attr = node.Attributes[attrName];
                    if (attr != null)
                    {
                        string attrValue = attr.Value;
                        if (!string.IsNullOrWhiteSpace(attrValue))
                        {
                            return "[@" + attrName + "='" + attrValue + "']";
                        }
                    }
                }
            }

            return base.FindPredicate(node, siblingIsMatch);
        }
    }
}
