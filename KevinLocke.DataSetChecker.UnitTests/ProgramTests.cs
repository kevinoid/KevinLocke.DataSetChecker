// <copyright file="ProgramTests.cs" company="Kevin Locke">
// Copyright 2019-2025 Kevin Locke.  All rights reserved.
// </copyright>

namespace KevinLocke.DataSetChecker.UnitTests
{
    using Xunit;

    public static class ProgramTests
    {
        [Fact]
        public static void Returns0OnEmpty()
        {
            Assert.Equal(0, Program.Main([]));
        }
    }
}
