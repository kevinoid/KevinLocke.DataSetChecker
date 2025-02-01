// <copyright file="XPathFinder.cs" company="Kevin Locke">
// Copyright 2017-2025 Kevin Locke &lt;kevin@kevinlocke.name&gt;
// This file is part of KevinLocke.DataSetChecker,
// publicly available under the MIT License.  See LICENSE.txt for details.
// </copyright>

namespace KevinLocke.DataSetChecker
{
    using System;
    using System.Collections.Generic;
    using System.Xml;

    /// <summary>
    /// Finds an XPath expression for a given <see cref="XmlNode"/>.
    /// </summary>
    /// <remarks>
    /// Based on <a href="https://stackoverflow.com/a/241291">How to get xpath
    /// from an XmlNode instance</a>.
    /// </remarks>
    public class XPathFinder
    {
        private static readonly Dictionary<XmlNodeType, string> NodeTypeTest = new()
        {
            { XmlNodeType.Comment, "comment()" },
            { XmlNodeType.ProcessingInstruction, "processing-instruction()" },
            { XmlNodeType.Text, "text()" },
        };

        public string FindXPath(XmlNode node)
        {
            return this.FindXPath(node, null);
        }

        /// <summary>
        /// Finds an XPath expression to a given <see cref="XmlNode"/> from a
        /// given context.
        /// </summary>
        /// <param name="node">Node which the returned XPath will select.</param>
        /// <param name="context">Node from which the XPath will be run.
        /// If <c>null</c>, an absolute XPath is returned.</param>
        /// <returns>An XPath expression which selects <paramref name="node"/>
        /// when run from <paramref name="context"/></returns>
        public virtual string FindXPath(XmlNode node, XmlNode context)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            List<string> steps = [];
            while (node != null && node != context)
            {
                XmlNodeType nodeType = node.NodeType;
#pragma warning disable IDE0010 // Add missing cases
                switch (nodeType)
                {
                    case XmlNodeType.Attribute:
                        steps.Add("@" + node.Name);
                        node = ((XmlAttribute)node).OwnerElement;
                        break;
                    case XmlNodeType.Element:
                        string elemPredicate = this.FindPredicate(
                            node,
                            (sibling) => sibling.Name == node.Name);
                        steps.Add(node.Name + elemPredicate);
                        node = node.ParentNode;
                        break;
                    case XmlNodeType.Document:
                    case XmlNodeType.DocumentFragment:
                        node = null;
                        break;
                    default:
                        if (!NodeTypeTest.TryGetValue(nodeType, out string typeTest))
                        {
                            throw new NotSupportedException(node.NodeType + " node type is not supported");
                        }

                        string textPredicate = this.FindPredicate(
                            node,
                            (sibling) => sibling.NodeType == nodeType);
                        steps.Add(typeTest + textPredicate);
                        node = node.ParentNode;
                        break;
                }
#pragma warning restore IDE0010 // Add missing cases
            }

            if (steps.Count == 0)
            {
                return ".";
            }

            steps.Reverse();
            string xpath = string.Join("/", steps);
            if (context == null)
            {
                xpath = "/" + xpath;
            }

            return xpath;
        }

        /// <summary>
        /// Finds a predicate to apply to the XPath step for a given
        /// <see cref="XmlNode"/> selected by type.
        /// </summary>
        /// <param name="node">Node for which to find a predicate.</param>
        /// <param name="siblingIsMatch">Does a sibling node match the XPath
        /// expression without the returned predicate?</param>
        /// <returns>Predicate which uniquely selects the given node
        /// from its parent node by type.  <see cref="string.Empty"/> if the
        /// type alone is unique.</returns>
        protected virtual string FindPredicate(XmlNode node, Predicate<XmlNode> siblingIsMatch)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            if (siblingIsMatch == null)
            {
                throw new ArgumentNullException(nameof(siblingIsMatch));
            }

            int nodePosition = 0;
            int position = 1;
            foreach (XmlNode siblingNode in node.ParentNode.ChildNodes)
            {
                if (siblingIsMatch(siblingNode))
                {
                    if (siblingNode == node)
                    {
                        nodePosition = position;
                    }

                    // Note: Don't return nodePosition == 1 && position == 1
                    // since "[1]" predicate may be unnecessary.
                    if (nodePosition > 0 && position > 1)
                    {
                        return "[" + nodePosition + "]";
                    }

                    ++position;
                }
            }

            return string.Empty;
        }
    }
}
