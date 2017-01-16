﻿using System;
using System.Data.SqlClient;
using System.IO;
using Xunit;

namespace AutoFake.IntegrationTests.StaticTests
{
    public class FieldMockTests
    {
        [Fact]
        public void OwnStaticTest()
        {
            var fake = new Fake(typeof(TestClass));

            fake.Replace(() => TestClass.DynamicStaticValue).Returns(7);

            Assert.Equal(7, fake.Rewrite(() => TestClass.GetDynamicStaticValue()).Execute());
        }

        [Fact]
        public void ExternalStaticTest()
        {
            var fake = new Fake(typeof(TestClass));

            fake.Replace(() => HelperClass.DynamicStaticValue).Returns(7);

            Assert.Equal(7, fake.Rewrite(() => TestClass.GetHelperDynamicStaticValue()).Execute());
        }

        [Fact]
        public void FrameworkInstanceTest()
        {
            var fake = new Fake(typeof(TestClass));

            var cmd = "select * from Test";
            fake.Replace((SqlCommand c) => c.CommandText).Returns(cmd);

            Assert.Equal(cmd, fake.Rewrite(() => TestClass.GetFrameworkValue()).Execute());
        }

        [Fact]
        public void FrameworkStaticTest()
        {
            var fake = new Fake(typeof(TestClass));

            var textReader = new StringReader(string.Empty);
            fake.Replace(() => TextReader.Null).Returns(textReader);

            var actual = fake.Rewrite(() => TestClass.GetFrameworkStaticValue()).Execute();
            Assert.Equal(textReader, actual);
            Assert.NotEqual(TextReader.Null, actual);
        }

        private static class TestClass
        {
            public static int DynamicStaticValue = 5;

            public static int GetDynamicStaticValue()
            {
                Console.WriteLine("Started");
                var value = DynamicStaticValue;
                Console.WriteLine("Finished");
                return value;
            }

            public static int GetHelperDynamicStaticValue()
            {
                Console.WriteLine("Started");
                var value = HelperClass.DynamicStaticValue;
                Console.WriteLine("Finished");
                return value;
            }

            public static string GetFrameworkValue()
            {
                Console.WriteLine("Started");
                var cmd = new SqlCommand();
                var value = cmd.CommandText;
                Console.WriteLine("Finished");
                return value;
            }
            public static TextReader GetFrameworkStaticValue()
            {
                Console.WriteLine("Started");
                var value = TextReader.Null;
                Console.WriteLine("Finished");
                return value;
            }
        }

        private static class HelperClass
        {
            public static int DynamicStaticValue = 5;
        }
    }
}
