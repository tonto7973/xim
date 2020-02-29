﻿using System.Collections;
using System.Linq;
using Microsoft.AspNetCore.Http;
using NUnit.Framework;
using Shouldly;

namespace Xim.Simulators.Api.Tests
{
    [TestFixture]
    public class ApiCallCollectionTests
    {
        [Test]
        public void Add_AddsItemToCollection()
        {
            var apiCall = ApiCall.Start("act", new DefaultHttpContext());
            var collection = new ApiCallCollection
            {
                apiCall
            };

            collection.Count.ShouldBe(1);
            collection.ShouldHaveSingleItem().ShouldBeSameAs(apiCall);
        }

        [Test]
        public void Add_AddsItemsInOrder()
        {
            var apiCall1 = ApiCall.Start("foo", new DefaultHttpContext());
            var apiCall2 = ApiCall.Start("bar", new DefaultHttpContext());
            var collection = new ApiCallCollection
            {
                apiCall1,
                apiCall2
            };
            var allCalls = collection.ToList();

            collection.Count.ShouldBe(2);
            allCalls.Count.ShouldBe(2);
            allCalls[0].ShouldBeSameAs(apiCall1);
            allCalls[1].ShouldBeSameAs(apiCall2);
        }

        [Test]
        public void GetEnumerator_IsTheSameAsGetEnumeratorT()
        {
            var collection = new ApiCallCollection
            {
                ApiCall.Start("act0", new DefaultHttpContext()),
                ApiCall.Start("act1", new DefaultHttpContext()),
                ApiCall.Start("act2", new DefaultHttpContext())
            };

            var enumeratorT = collection.GetEnumerator();
            var enumerator = ((IEnumerable)collection).GetEnumerator();

            for (var i = 0; i < 3; i++)
            {
                var canMove = enumerator.MoveNext();
                var canMoveT = enumeratorT.MoveNext();
                canMove.ShouldBe(canMoveT);
                enumerator.Current.ShouldBeSameAs(enumeratorT.Current);
                enumeratorT.Current.Action.ShouldBe($"act{i}");
            }
        }
    }
}
