﻿using System;
using System.Collections.Generic;
using FluentAssertions;
using Xunit;

namespace AutoFake.UnitTests
{
    public class ArgTests
    {
        [Fact]
        public void IsNull_ReferenceType_Success()
        {
            var dependency = Arg.IsNull<string>();
            
            var wrapper = dependency.Should().BeOfType<Arg.TypeWrapper>();
            wrapper.Subject.Type.Should().Be(typeof(string));
        }

        [Fact]
        public void IsNull_NullableType_Success()
        {
            var dependency = Arg.IsNull<int?>();

            var wrapper = dependency.Should().BeOfType<Arg.TypeWrapper>();
            wrapper.Subject.Type.Should().Be(typeof(int?));
        }

        [Fact]
        public void IsNull_ValueType_Throws()
        {
            Assert.Throws<NotSupportedException>(() => Arg.IsNull<int>());
        }

        [Fact]
        public void Is_SomeValue_ReturnsDefaultValueOfType()
        {
            Assert.Equal(0, Arg.Is<int>(x => x > 5));
            Assert.Null(Arg.Is<int?>(x => x != 5));
            Assert.Null(Arg.Is<string>(x => !string.IsNullOrEmpty(x)));
        }

        [Fact]
        public void IsAny_SomeValue_ReturnsDefaultValueOfType()
        {
            Assert.Equal(0, Arg.IsAny<int>());
            Assert.Null(Arg.IsAny<int?>());
            Assert.Null(Arg.IsAny<string>());
        }

        [Theory, AutoMoqData]
        public void Is_SomeValueWithComparer_ReturnsDefaultValueOfType(
            IEqualityComparer<int> intCmp, IEqualityComparer<int?> nullIntCmp, IEqualityComparer<string> strCmp)
        {
            Assert.Equal(0, Arg.Is(5, intCmp));
            Assert.Null(Arg.Is<int?>(5, nullIntCmp));
            Assert.Null(Arg.Is("5", strCmp));
        }
    }
}
