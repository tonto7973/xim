using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Shouldly;

namespace Xim.Simulators.Api.Tests
{
    [TestFixture]
    public class HeadersTests
    {
        [Test]
        public void SetItem_SetsItem()
        {
            var headers = new Headers
            {
                ["key"] = "value"
            };

            headers.Single().ShouldBe(new KeyValuePair<string, string>("key", "value"));
        }

        [Test]
        public void SetItem_Throws_WhenKeyNull()
        {
            Action action = () => new Headers { [null] = "value" };

            action.ShouldThrow<ArgumentNullException>()
                .ParamName.ShouldBe("name");
        }

        [Test]
        public void SetItem_Throws_WhenKeyEmpty()
        {
            ArgumentException exception = null;
            Action action = () => new Headers { [""] = "a" };

            action.ShouldSatisfyAllConditions(
                () => exception = action.ShouldThrow<ArgumentException>(),
                () => exception.Message.ShouldStartWith(SR.Format(SR.ApiHeaderNameIsEmpty)),
                () => exception.ParamName.ShouldBe("name")
            );
        }

        [Test]
        public void SetItem_Throws_WhenKeyInvalid()
        {
            ArgumentException exception = null;
            Action action = () => new Headers { ["0>32"] = "a" };

            action.ShouldSatisfyAllConditions(
                () => exception = action.ShouldThrow<ArgumentException>(),
                () => exception.Message.ShouldStartWith(SR.Format(SR.ApiHeaderNameIsInvalid, "0\\u003e32", "\\u003e", 1)),
                () => exception.ParamName.ShouldBe("name")
            );
        }

        [Test]
        public void SetItem_Throws_WhenValueInvalid()
        {
            ArgumentException exception = null;
            Action action = () => new Headers { ["abc"] = "abc\n7\"x" };

            action.ShouldSatisfyAllConditions(
                () => exception = action.ShouldThrow<ArgumentException>(),
                () => exception.Message.ShouldStartWith(SR.Format(SR.ApiHeaderValueIsInvalid, "abc\\n7\\\"x", "\\n", 3)),
                () => exception.ParamName.ShouldBe("value")
            );
        }

        [Test]
        public void SetItem_SetsMultipleItems_WhenSameKey()
        {
            var headers = new Headers
            {
                ["key"] = "value",
                ["key"] = "two"
            };

            headers.ToArray().ShouldBe(new[] {
                new KeyValuePair<string, string>("key", "value"),
                new KeyValuePair<string, string>("key", "two")
            });
        }

        [Test]
        public void SetItem_ClearsMultipleItems_WhenSameKeyAndValueNull()
        {
            var headers = new Headers
            {
                ["one"] = "me",
                ["key"] = "value",
                ["key"] = "two",
                ["key"] = null
            };

            headers.ToArray().ShouldBe(new[] {
                new KeyValuePair<string, string>("one", "me")
            });
        }

        [TestCase("room")]
        [TestCase("tWO")]
        public void GetItem_GetsPreviouslySetItem(string value)
        {
            var headers = new Headers
            {
                ["loc"] = value
            };

            headers["loc"].ShouldBe(value);
        }

        [TestCase("tom")]
        [TestCase("BoB")]
        public void GetItem_GetsPreviouslySetItemCaseInsensitive(string value)
        {
            var headers = new Headers
            {
                ["loc"] = value
            };

            headers["Loc"].ShouldBe(value);
        }

        [Test]
        public void GetItem_GetsMultipleItemsJoinedByComma()
        {
            var headers = new Headers
            {
                ["Accept"] = "json",
                ["Accept"] = "xml"
            };

            headers["Accept"].ShouldBe("json,xml");
        }

        [Test]
        public void GetItem_GetsMultipleItemsJoinedByCommaCaseInsensitive()
        {
            var headers = new Headers
            {
                ["X-Num"] = "one",
                ["x-Num"] = "two",
                ["x-num"] = "3"
            };

            headers["x-nUm"].ShouldBe("one,two,3");
        }

        [Test]
        public void GetItem_ReturnsNull_WhenHeaderDoesNotExist()
        {
            var headers = new Headers();

            headers["Empty"].ShouldBeNull();
        }

        [Test]
        public void Count_ReturnsNumberOfHeaders()
        {
            var headers = new Headers
            {
                { "header1", "value1" }
            };
            headers.Count.ShouldBe(1);
        }

        [Test]
        public void Count_IncludesDuplicateHeaders()
        {
            var headers = new Headers
            {
                { "key", "value1" },
                { "key", "value2" },
                { "arg", "foo" }
            };
            headers.Count.ShouldBe(3);
        }

        [Test]
        public void Add_AddsItem()
        {
            var headers = new Headers
            {
                { "key", "value" }
            };

            headers.Single().ShouldBe(new KeyValuePair<string, string>("key", "value"));
        }

        [Test]
        public void Add_AddsMultipleItems_WhenSameKey()
        {
            var headers = new Headers
            {
                { "key", "value" },
                { "key", "two" }
            };

            headers.ToArray().ShouldBe(new[] {
                new KeyValuePair<string, string>("key", "value"),
                new KeyValuePair<string, string>("key", "two")
            });
        }

        [Test]
        public void Add_Throws_WhenKeyNull()
        {
            Action action = () => new Headers { { null, "value" } };

            action.ShouldThrow<ArgumentNullException>()
                .ParamName.ShouldBe("name");
        }

        [Test]
        public void Add_Throws_WhenValueNull()
        {
            Action action = () => new Headers { { "key", null } };

            action.ShouldThrow<ArgumentNullException>()
                .ParamName.ShouldBe("value");
        }

        [Test]
        public void Remove_RemovesItem()
        {
            var headers = new Headers
            {
                { "key", "value" }
            };

            headers.Remove("key");
            headers["key"].ShouldBeNull();
        }

        [Test]
        public void Remove_RemovesAllItemsWithTheSameKey()
        {
            var headers = new Headers
            {
                ["key"] = "value",
                ["key"] = "two",
                ["key"] = "other",
                ["one"] = "me"
            };

            headers.Remove("key");

            headers.ToArray().ShouldBe(new[] {
                new KeyValuePair<string, string>("one", "me")
            });
        }

        [Test]
        public void Remove_RemovesAllItemsWithTheSameKeyCaseInsensitive()
        {
            var headers = new Headers
            {
                ["bob"] = "temp",
                ["Bob"] = "uri",
                ["Two"] = "em"
            };

            headers.Remove("bob");

            headers.ToArray().ShouldBe(new[] {
                new KeyValuePair<string, string>("Two", "em")
            });
        }

        [Test]
        public void Remove_Throws_WhenHeaderNull()
        {
            var headers = new Headers();

            Action action = () => headers.Remove(null);

            action.ShouldThrow<ArgumentNullException>()
                .ParamName.ShouldBe("name");
        }

        [Test]
        public void IEnumerableGetEnumerator_IsSameAsGetEnumerator()
        {
            var headers = new Headers
            {
                ["a"] = "b",
                ["c"] = "d",
            };

            var data1 = new List<KeyValuePair<string, string>>();
            var enum1 = headers.GetEnumerator();
            while (enum1.MoveNext())
            {
                data1.Add(enum1.Current);
            }

            var enum2 = ((IEnumerable)headers).GetEnumerator();
            var data2 = new List<KeyValuePair<string, string>>();
            while (enum2.MoveNext())
            {
                data2.Add((KeyValuePair<string, string>)enum2.Current);
            }

            data1.ShouldBe(data2);
        }

        [Test]
        public void FromString_Throws_WhenStringNull()
        {
            Action action = () => Headers.FromString(null);

            action.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void FromString_ReturnsEmpty_WhenInvalidHeader()
        {
            var headers = Headers.FromString("Some Invalid text");

            headers.ShouldBeEmpty();
        }

        [Test]
        public void FromString_ParsesSingleHeader()
        {
            var headers = Headers.FromString("Action: speak");

            headers.Single().ShouldBe(new KeyValuePair<string, string>("Action", "speak"));
        }

        [Test]
        public void FromString_ParsesMultipleHeadersSeparatedByNewline()
        {
            var expectedHeaders = new[] {
                new KeyValuePair<string, string>("Location", "Home"),
                new KeyValuePair<string, string>("X-Operation", "lock,check"),
                new KeyValuePair<string, string>("ETag", "A634B")
            };

            var headers = Headers.FromString("Location: Home\r\nX-Operation: lock,check\nETag: A634B");

            headers.ShouldBe(expectedHeaders);
        }

        [Test]
        public void ToString_ReturnsEmptyString_WhenNoHeadersPresent()
        {
            var headers = new Headers();

            headers.ToString().ShouldBe(string.Empty);
        }

        [Test]
        public void ToString_ReturnsSingleRfc2616FormattedHeader()
        {
            var headers = new Headers
            {
                ["ETag"] = "789667485"
            };

            headers.ToString().ShouldBe("ETag: 789667485");
        }

        [Test]
        public void ToString_ReturnsMultiRfc2616FormattedHeader()
        {
            var headers = new Headers
            {
                ["ETag"] = "ART454C",
                ["X-Colour"] = "red",
                ["X-Colour"] = "blue",
                ["Name"] = "foo"
            };

            headers.ToString().ShouldBe("ETag: ART454C\r\nX-Colour: red\r\nX-Colour: blue\r\nName: foo");
        }
    }
}
