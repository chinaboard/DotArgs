﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using DotArgs;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotArgsTests
{
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    [TestClass]
    public class CommandLineArgsTest
    {
        [TestMethod]
        public void AliasTest()
        {
            CommandLineArgs args = new CommandLineArgs();
            args.RegisterArgument("flag", new FlagArgument(false));
            args.RegisterAlias("flag", "alias");

            Assert.IsTrue(args.Validate(string.Empty));
            Assert.IsFalse(args.GetValue<bool>("alias"));

            Assert.IsTrue(args.Validate("-alias"));
            Assert.IsTrue(args.GetValue<bool>("alias"));

            ExceptionAssert.Assert<KeyNotFoundException>(() => args.RegisterAlias("nonexisting", "test"));

            args = new CommandLineArgs();
            args.RegisterArgument("flag", new FlagArgument(false, true));
            args.RegisterAlias("flag", "alias");

            Assert.IsTrue(args.Validate("-alias"));
        }

        [TestMethod]
        public void CollectionTest()
        {
            CommandLineArgs args = new CommandLineArgs();
            args.RegisterArgument("option", new CollectionArgument());

            Assert.IsTrue(args.Validate("/option=value1"));
            string[] values = args.GetValue<string[]>("option");
            CollectionAssert.AreEqual(new[] { "value1" }, args.GetValue<string[]>("option"));

            Assert.IsTrue(args.Validate("/option=value1 /option=value2"));
            values = args.GetValue<string[]>("option");
            CollectionAssert.AreEqual(new[] { "value1", "value2" }, values);

            Assert.IsTrue(args.Validate("/option=value1 --option=value2"));
            values = args.GetValue<string[]>("option");
            CollectionAssert.AreEqual(new[] { "value1", "value2" }, values);

            ExceptionAssert.Assert<KeyNotFoundException>(() => args.GetValue<string[]>("nonexisting"));
        }

        [TestMethod]
        public void CustomValidatorTest()
        {
            CommandLineArgs args = new CommandLineArgs();
            OptionArgument option = new OptionArgument("123", true);
            option.Validator = (v) => v.Equals("test");
            args.RegisterArgument("option", option);

            Assert.IsFalse(args.Validate("/option=123"));
            Assert.IsTrue(args.Validate("/option=test"));
        }

        [TestMethod]
        public void DefaultArgumentTest()
        {
            CommandLineArgs args = new CommandLineArgs();
            args.RegisterArgument("default", new OptionArgument(null, true));
            args.SetDefaultArgument("default");

            Assert.IsTrue(args.Validate("value"));
            Assert.AreEqual("value", args.GetValue<string>("default"));

            args.RegisterArgument("flag", new FlagArgument(false, true));

            Assert.IsTrue(args.Validate("value /flag"));
            Assert.AreEqual("value", args.GetValue<string>("default"));
            Assert.AreEqual(true, args.GetValue<bool>("flag"));

            Assert.IsTrue(args.Validate("/flag value"));
            Assert.AreEqual("value", args.GetValue<string>("default"));
            Assert.AreEqual(true, args.GetValue<bool>("flag"));

            Assert.IsTrue(args.Validate("/flag flag"));
            Assert.AreEqual("flag", args.GetValue<string>("default"));
            Assert.AreEqual(true, args.GetValue<bool>("flag"));

            args = new CommandLineArgs();
            args.RegisterArgument("default", new OptionArgument(null, true));

            Assert.IsFalse(args.Validate("value"));

            ExceptionAssert.Assert<ArgumentException>(() => args.SetDefaultArgument("nonexisting"));
        }

        [TestMethod]
        public void DefaultCollectionTest()
        {
            CommandLineArgs args = new CommandLineArgs();
            args.RegisterArgument("default", new CollectionArgument(true));
            args.RegisterArgument("flag", new FlagArgument());
            args.SetDefaultArgument("default");

            Assert.IsTrue(args.Validate("test1 test2"));
            string[] values = args.GetValue<string[]>("default");
            CollectionAssert.AreEqual(new[] { "test1", "test2" }, values);

            Assert.IsTrue(args.Validate("test1 test2 /flag"));
            values = args.GetValue<string[]>("default");
            CollectionAssert.AreEqual(new[] { "test1", "test2" }, values);
            Assert.IsTrue(args.GetValue<bool>("flag"));

            Assert.IsTrue(args.Validate("/flag test1 test2"));
            values = args.GetValue<string[]>("default");
            CollectionAssert.AreEqual(new[] { "test1", "test2" }, values);
            Assert.IsTrue(args.GetValue<bool>("flag"));
        }

        [TestMethod]
        public void FlagTest()
        {
            CommandLineArgs args = new CommandLineArgs();
            args.RegisterArgument("flag", new FlagArgument());

            Assert.IsTrue(args.Validate(string.Empty));
            Assert.IsFalse(args.GetValue<bool>("flag"));
            ExceptionAssert.Assert<KeyNotFoundException>(() => args.GetValue<bool>("nonexisting"));

            Assert.IsTrue(args.Validate("-flag"));
            Assert.IsTrue(args.GetValue<bool>("flag"));

            Assert.IsTrue(args.Validate("--flag"));
            Assert.IsTrue(args.GetValue<bool>("flag"));

            Assert.IsTrue(args.Validate("/flag"));
            Assert.IsTrue(args.GetValue<bool>("flag"));
        }

        [TestMethod]
        public void GetArgNameTest()
        {
            CommandLineArgs args = new CommandLineArgs();
            PrivateObject obj = new PrivateObject(args);

            string name = (string)obj.Invoke("GetArgName", "/arg");
            Assert.AreEqual("arg", name);

            name = (string)obj.Invoke("GetArgName", "-arg");
            Assert.AreEqual("arg", name);

            name = (string)obj.Invoke("GetArgName", "arg");
            Assert.AreEqual("arg", name);

            name = (string)obj.Invoke("GetArgName", "--arg");
            Assert.AreEqual("arg", name);

            name = (string)obj.Invoke("GetArgName", "/arg-");
            Assert.AreEqual("arg-", name);

            name = (string)obj.Invoke("GetArgName", "/arg/");
            Assert.AreEqual("arg/", name);

            name = (string)obj.Invoke("GetArgName", "/arg--");
            Assert.AreEqual("arg--", name);

            name = (string)obj.Invoke("GetArgName", "//arg");
            Assert.AreEqual("arg", name);

            name = (string)obj.Invoke("GetArgName", "//arg--");
            Assert.AreEqual("arg--", name);

            name = (string)obj.Invoke("GetArgName", "/--/-/-//arg");
            Assert.AreEqual("arg", name);

            name = (string)obj.Invoke("GetArgName", "/option=value");
            Assert.AreEqual("option", name);

            name = (string)obj.Invoke("GetArgName", "/option:value");
            Assert.AreEqual("option", name);

            name = (string)obj.Invoke("GetArgName", "--option=value");
            Assert.AreEqual("option", name);

            name = (string)obj.Invoke("GetArgName", "--option:value");
            Assert.AreEqual("option", name);
        }

        [TestMethod]
        public void HelpArgumentTest()
        {
            CommandLineArgs args = new CommandLineArgs();
            args.RegisterHelpArgument();

            Assert.IsTrue(args.Validate("/help"));
            Assert.IsTrue(args.GetValue<bool>("help"));

            using (StringWriter writer = new StringWriter())
            {
                args.OutputWriter = writer;
                args.Process();

                Assert.IsFalse(string.IsNullOrWhiteSpace(writer.ToString()));
            }
        }

        [TestMethod]
        public void HelpPageExampleTest()
        {
            CommandLineArgs args = new CommandLineArgs();
            args.ApplicationInfo = "MyCoolProgram v1.2 Copyright (C) John Smith <smith@example.com>";
            args.AddExample("example 1", "/flag /option=1");
            args.AddExample("great example", "/flag2");
            args.AddExample("example 2", "/option=222");

            using (TextWriter writer = new StringWriter())
            {
                args.OutputWriter = writer;

                args.PrintHelp();

                StringBuilder sb = new StringBuilder();
                sb.AppendLine(args.ApplicationInfo);
                sb.AppendLine();
                sb.AppendLine("Usage:");
                sb.AppendFormat("DotArgsTests ");
                sb.AppendLine();
                sb.AppendLine();
                sb.AppendLine("Examples:");
                sb.AppendLine();
                sb.AppendLine("example 1");
                sb.AppendLine("/flag /option=1");
                sb.AppendLine();
                sb.AppendLine("example 2");
                sb.AppendLine("/option=222");
                sb.AppendLine();
                sb.AppendLine("great example");
                sb.AppendLine("/flag2");

                Assert.AreEqual(sb.ToString(), writer.ToString());
            }
        }

        [TestMethod]
        public void OptionTest()
        {
            CommandLineArgs args = new CommandLineArgs();
            args.RegisterArgument("option", new OptionArgument("123", false));

            Assert.IsTrue(args.Validate(string.Empty));
            Assert.AreEqual("123", args.GetValue<string>("option"));

            Assert.IsTrue(args.Validate("/option=42"));
            Assert.AreEqual("42", args.GetValue<string>("option"));

            Assert.IsTrue(args.Validate("/option:42"));
            Assert.AreEqual("42", args.GetValue<string>("option"));

            Assert.IsTrue(args.Validate("/option 42"));
            Assert.AreEqual("42", args.GetValue<string>("option"));

            Assert.IsTrue(args.Validate("--option=42"));
            Assert.AreEqual("42", args.GetValue<string>("option"));

            Assert.IsTrue(args.Validate("--option:42"));
            Assert.AreEqual("42", args.GetValue<string>("option"));

            Assert.IsTrue(args.Validate("--option 42"));
            Assert.AreEqual("42", args.GetValue<string>("option"));

            Assert.IsTrue(args.Validate("-option 42"));
            Assert.AreEqual("42", args.GetValue<string>("option"));

            Assert.IsTrue(args.Validate("-option=42"));
            Assert.AreEqual("42", args.GetValue<string>("option"));

            Assert.IsTrue(args.Validate("-option:42"));
            Assert.AreEqual("42", args.GetValue<string>("option"));

            ExceptionAssert.Assert<KeyNotFoundException>(() => args.GetValue<string>("nonexisting"));

            Assert.IsTrue(args.Validate("-option:42 /option=444"));
            Assert.AreEqual("444", args.GetValue<string>("option"));
        }

        [TestMethod, TestCategory("RealWorldExamples")]
        public void PingTest()
        {
            CommandLineArgs args = new CommandLineArgs();
            args.RegisterArgument("target_name", new OptionArgument(null, true));
            args.RegisterArgument("t", new FlagArgument(false, false));
            args.RegisterArgument("4", new FlagArgument(false, false));
            args.RegisterArgument("6", new FlagArgument(false, false));
            args.SetDefaultArgument("target_name");

            Assert.IsTrue(args.Validate("localhost"));
            Assert.AreEqual("localhost", args.GetValue<string>("target_name"));
            Assert.IsFalse(args.GetValue<bool>("4"));
            Assert.IsFalse(args.GetValue<bool>("6"));
            Assert.IsFalse(args.GetValue<bool>("t"));

            Assert.IsTrue(args.Validate("localhost -t"));
            Assert.AreEqual("localhost", args.GetValue<string>("target_name"));
            Assert.IsFalse(args.GetValue<bool>("4"));
            Assert.IsFalse(args.GetValue<bool>("6"));
            Assert.IsTrue(args.GetValue<bool>("t"));

            Assert.IsTrue(args.Validate("localhost -4"));
            Assert.AreEqual("localhost", args.GetValue<string>("target_name"));
            Assert.IsTrue(args.GetValue<bool>("4"));
            Assert.IsFalse(args.GetValue<bool>("6"));
            Assert.IsFalse(args.GetValue<bool>("t"));

            Assert.IsTrue(args.Validate("localhost -6"));
            Assert.AreEqual("localhost", args.GetValue<string>("target_name"));
            Assert.IsFalse(args.GetValue<bool>("4"));
            Assert.IsTrue(args.GetValue<bool>("6"));
            Assert.IsFalse(args.GetValue<bool>("t"));

            Assert.IsTrue(args.Validate("localhost -6 -t"));
            Assert.AreEqual("localhost", args.GetValue<string>("target_name"));
            Assert.IsFalse(args.GetValue<bool>("4"));
            Assert.IsTrue(args.GetValue<bool>("6"));
            Assert.IsTrue(args.GetValue<bool>("t"));
        }

        [TestMethod]
        public void PositionalArgumentsTest()
        {
            CommandLineArgs args = new CommandLineArgs();
            args.RegisterArgument("first", new OptionArgument(null, true, 0));
            args.RegisterArgument("second", new OptionArgument(null, true, 1));
            args.RegisterArgument("third", new OptionArgument(null, false, 2));

            Assert.IsFalse(args.Validate("one"));

            Assert.IsTrue(args.Validate("one two"));
            Assert.AreEqual("one", args.GetValue<string>("first"));
            Assert.AreEqual("two", args.GetValue<string>("second"));
            Assert.AreEqual(null, args.GetValue<string>("third"));

            Assert.IsTrue(args.Validate("one two three"));
            Assert.AreEqual("one", args.GetValue<string>("first"));
            Assert.AreEqual("two", args.GetValue<string>("second"));
            Assert.AreEqual("three", args.GetValue<string>("third"));

            args.RegisterArgument("flag", new FlagArgument());
            Assert.IsTrue(args.Validate("one two three"));
            Assert.AreEqual(false, args.GetValue<bool>("flag"));

            Assert.IsTrue(args.Validate("one two three /flag"));
            Assert.AreEqual(true, args.GetValue<bool>("flag"));

            args.RegisterArgument("default", new OptionArgument("def"));
            args.SetDefaultArgument("default");
            Assert.IsTrue(args.Validate("one two three /flag zero"));
            Assert.AreEqual(true, args.GetValue<bool>("flag"));
            Assert.AreEqual("zero", args.GetValue<string>("default"));

            Assert.IsTrue(args.Validate("one two three zero /flag"));
            Assert.AreEqual(true, args.GetValue<bool>("flag"));
            Assert.AreEqual("zero", args.GetValue<string>("default"));
        }

        [TestMethod]
        public void PrintHelpTest()
        {
            CommandLineArgs args = new CommandLineArgs();
            args.ApplicationInfo = "MyCoolProgram v1.2 Copyright (C) John Smith <smith@example.com>";

            args.RegisterArgument("flag", new FlagArgument(true, true) { HelpMessage = "This is a flag." });
            args.RegisterArgument("option", new OptionArgument("123", false) { HelpMessage = "This is an option." });

            using (TextWriter writer = new StringWriter())
            {
                args.OutputWriter = writer;

                args.PrintHelp();

                StringBuilder sb = new StringBuilder();
                sb.AppendLine(args.ApplicationInfo);
                sb.AppendLine();
                sb.AppendLine("Usage:");
                sb.AppendFormat("DotArgsTests </flag> [/option=OPTION, 123]");
                sb.AppendLine();
                sb.AppendLine();
                sb.AppendFormat("{0,-10}{1}", "flag", "This is a flag.");
                sb.AppendLine();
                sb.AppendFormat("{0,-10}Required", "");
                sb.AppendLine();
                sb.AppendLine();
                sb.AppendFormat("{0,-10}{1}", "option", "This is an option.");
                sb.AppendLine();
                sb.AppendFormat("{0,-10}Optional, Default value: 123", "");
                sb.AppendLine();

                Assert.AreEqual(sb.ToString(), writer.ToString());
            }

            using (TextWriter writer = new StringWriter())
            {
                args.OutputWriter = writer;

                args.PrintHelp("This is an error");

                StringBuilder sb = new StringBuilder();
                sb.AppendLine(args.ApplicationInfo);
                sb.AppendLine();
                sb.AppendLine("This is an error");
                sb.AppendLine();
                sb.AppendLine("Usage:");
                sb.AppendFormat("DotArgsTests </flag> [/option=OPTION, 123]");
                sb.AppendLine();
                sb.AppendLine();
                sb.AppendFormat("{0,-10}{1}", "flag", "This is a flag.");
                sb.AppendLine();
                sb.AppendFormat("{0,-10}Required", "");
                sb.AppendLine();
                sb.AppendLine();
                sb.AppendFormat("{0,-10}{1}", "option", "This is an option.");
                sb.AppendLine();
                sb.AppendFormat("{0,-10}Optional, Default value: 123", "");
                sb.AppendLine();

                Assert.AreEqual(sb.ToString(), writer.ToString());
            }
        }

        [TestMethod]
        public void ProcessTest()
        {
            bool flagCalled = false;
            bool optionCalled = false;
            CommandLineArgs args = new CommandLineArgs();
            args.RegisterArgument("flag", new FlagArgument(true)
                {
                    Processor = (value) => flagCalled = true
                });
            args.RegisterArgument("option", new OptionArgument("test")
                {
                    Processor = (value) => optionCalled = true
                });

            Assert.IsTrue(args.Validate("/flag /option=value"));

            Assert.IsFalse(flagCalled);
            Assert.IsFalse(optionCalled);

            args.Process();

            Assert.IsTrue(flagCalled);
            Assert.IsTrue(optionCalled);
        }

        [TestMethod]
        public void SetTest()
        {
            CommandLineArgs args = new CommandLineArgs();
            args.RegisterArgument("set", new SetArgument(new[] { "v1", "v2" }, "v1"));

            Assert.IsTrue(args.Validate("/set=v1"));
            Assert.AreEqual("v1", args.GetValue<string>("set"));

            Assert.IsTrue(args.Validate("/set=v2"));
            Assert.AreEqual("v2", args.GetValue<string>("set"));

            Assert.IsFalse(args.Validate("/set=v3"));
        }

        [TestMethod]
        public void SplitCommandLineTest()
        {
            CommandLineArgs args = new CommandLineArgs();
            PrivateObject obj = new PrivateObject(args);

            List<string> parsed = (List<string>)obj.Invoke("SplitCommandLine", "this is a test");
            CollectionAssert.AreEqual(new[] { "this", "is", "a", "test" }, parsed);

            parsed = (List<string>)obj.Invoke("SplitCommandLine", "this \"is a test\"");
            CollectionAssert.AreEqual(new[] { "this", "is a test" }, parsed);

            parsed = (List<string>)obj.Invoke("SplitCommandLine", "this 'is a test'");
            CollectionAssert.AreEqual(new[] { "this", "is a test" }, parsed);

            parsed = (List<string>)obj.Invoke("SplitCommandLine", "this \"is 'a' test\"");
            CollectionAssert.AreEqual(new[] { "this", "is 'a' test" }, parsed);

            parsed = (List<string>)obj.Invoke("SplitCommandLine", "this 'is \"a\" test'");
            CollectionAssert.AreEqual(new[] { "this", "is \"a\" test" }, parsed);

            parsed = (List<string>)obj.Invoke("SplitCommandLine", "this  is    a  test ");
            CollectionAssert.AreEqual(new[] { "this", "is", "a", "test" }, parsed);
        }

        [TestMethod]
        public void ValidateTest()
        {
            CommandLineArgs args = new CommandLineArgs();
            args.RegisterArgument("flag", new FlagArgument(true, true));

            Assert.IsFalse(args.Validate(string.Empty));

            args = new CommandLineArgs();
            args.RegisterHelpArgument();
            args.RegisterArgument("flag", new FlagArgument());

            Assert.IsTrue(args.Validate(new[] { "/help", "/flag" }));

            Assert.IsTrue(args.GetValue<bool>("help"));
            Assert.IsTrue(args.GetValue<bool>("flag"));

            OptionalOut<string[]> outErrors = new OptionalOut<string[]>();
            Assert.IsFalse(args.Validate("/unknown", outErrors));

            Assert.AreEqual(1, outErrors.Result.Length);
            Assert.AreEqual("Unknown option: 'unknown'", outErrors.Result[0]);

            args.RegisterArgument("option", new OptionArgument(null));
            Assert.IsFalse(args.Validate("/option", outErrors));
            Assert.AreEqual(1, outErrors.Result.Length);
            Assert.AreEqual("Missing value for option 'option'", outErrors.Result[0]);

            Assert.IsFalse(args.Validate("/option /flag", outErrors));
            Assert.AreEqual(1, outErrors.Result.Length);
            Assert.AreEqual("Missing value for option 'option'", outErrors.Result[0]);

            args = new CommandLineArgs();
            args.RegisterArgument("option", new OptionArgument(null, true));

            Assert.IsFalse(args.Validate(""));
            Assert.IsTrue(args.Validate("/option=value"));
        }

        [TestMethod]
        public void TryGetValueTest()
        {
            CommandLineArgs args = new CommandLineArgs();
            args.RegisterArgument("option", new OptionArgument(""));
            args.Validate("option 123");

            string value = string.Empty;

            Assert.IsTrue(args.TryGetValue<string>("option", out value));
            Assert.AreEqual(value, "123");
            Assert.IsFalse(args.TryGetValue<string>("optionTest", out value));
        }
    }
}