// <copyright file="XPathFinderTests.cs" company="Kevin Locke">
// Copyright 2017-2025 Kevin Locke.  All rights reserved.
// </copyright>

namespace KevinLocke.DataSetChecker.UnitTests
{
    using System;
    using System.Xml;

    using Xunit;

    public static class XPathFinderTests
    {
        [Fact]
        public static void FindXPath_ThrowsOnNull()
        {
            XPathFinder finder = new();
            Assert.Throws<ArgumentNullException>(() => finder.FindXPath(null!));
        }

        [Fact]
        public static void FindXPath_Root()
        {
            XmlDocument doc = new();
            XmlElement root = doc.CreateElement("root");

            XPathFinder finder = new();
            Assert.Equal(
                "/root",
                finder.FindXPath(root));
        }

        [Fact]
        public static void FindXPath_RootAttribute()
        {
            XmlDocument doc = new();
            XmlElement root = doc.CreateElement("root");
            XmlAttribute attr = doc.CreateAttribute("attr");
            root.Attributes.Append(attr);

            XPathFinder finder = new();
            Assert.Equal(
                "/root/@attr",
                finder.FindXPath(attr));
        }

        [Fact]
        public static void FindXPath_RootComment()
        {
            XmlDocument doc = new();
            XmlElement root = doc.CreateElement("root");
            XmlComment comment = doc.CreateComment("comment");
            root.AppendChild(comment);

            XPathFinder finder = new();
            Assert.Equal(
                "/root/comment()",
                finder.FindXPath(comment));
        }

        [Fact]
        public static void FindXPath_RootText()
        {
            XmlDocument doc = new();
            XmlElement root = doc.CreateElement("root");
            XmlText text = doc.CreateTextNode("text");
            root.AppendChild(text);

            XPathFinder finder = new();
            Assert.Equal(
                "/root/text()",
                finder.FindXPath(text));
        }

        [Fact]
        public static void FindXPath_PathToUniqueChild()
        {
            XmlDocument doc = new();
            XmlElement root = doc.CreateElement("root");
            XmlElement child = doc.CreateElement("child");
            root.AppendChild(child);

            XPathFinder finder = new();
            Assert.Equal(
                "/root/child",
                finder.FindXPath(child));
        }

        [Fact]
        public static void FindXPath_PredicateForSecondChild()
        {
            XmlDocument doc = new();
            XmlElement root = doc.CreateElement("root");
            XmlElement child1 = doc.CreateElement("child");
            root.AppendChild(child1);
            XmlElement child2 = doc.CreateElement("child");
            root.AppendChild(child2);

            XPathFinder finder = new();
            Assert.Equal(
                "/root/child[2]",
                finder.FindXPath(child2));
        }

        [Fact]
        public static void FindXPath_NoPredicateForUniqueTagName()
        {
            XmlDocument doc = new();
            XmlElement sports = doc.CreateElement("sports");
            XmlElement soccer = doc.CreateElement("soccer");
            sports.AppendChild(soccer);
            XmlElement baseball = doc.CreateElement("baseball");
            sports.AppendChild(baseball);

            XPathFinder finder = new();
            Assert.Equal(
                "/sports/baseball",
                finder.FindXPath(baseball));
        }
    }
}
