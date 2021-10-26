using System;
using System.Diagnostics;
using FluentAssertions;
using Octostache.Templates;
using Xunit;

namespace Octostache.Tests
{
    public class ItemCacheFixture : BaseFixture
    {   
        [Fact]
        protected void GetWithNullReturnsNull()
        {
            var item = new TestItem { Value = nameof(GetWithNullReturnsNull) };
            var cache = new ItemCache<TestItem>("temp", 1, TimeSpan.FromMinutes(2));
            cache.Add("item", null);

            cache.Get("item").Should().BeNull();
        }
        
        [Fact]
        protected void GetOrAddWithNullReturnsNull()
        {
            var item = new TestItem { Value = nameof(GetOrAddWithNullReturnsNull) };
            var cache = new ItemCache<TestItem>("temp", 1, TimeSpan.FromMinutes(2));
            cache.Add("item", null);

            cache.GetOrAdd("item", () => throw new Exception("Should not re-create null")).Should().BeNull();
        }
        
        [Fact]
        protected void GetOrAddWillAddNullOnce()
        {
            var item = new TestItem { Value = nameof(GetOrAddWillAddNullOnce) };
            var cache = new ItemCache<TestItem>("temp", 1, TimeSpan.FromMinutes(2));
            cache.GetOrAdd("item", () => null);

            cache.GetOrAdd("item", () => throw new Exception("Should not re-create item")).Should().BeNull();
        }
        
        [Fact]
        protected void GetWithItemReturnsItem()
        {
            var item = new TestItem { Value = nameof(GetWithItemReturnsItem) };
            var cache = new ItemCache<TestItem>("temp", 1, TimeSpan.FromMinutes(2));
            cache.Add("item", item);

            cache.Get("item").Should().Be(item);
        }
        
        [Fact]
        protected void GetOrAddWithItemReturnsItem()
        {
            var item = new TestItem { Value = nameof(GetOrAddWithItemReturnsItem) };
            var cache = new ItemCache<TestItem>("temp", 1, TimeSpan.FromMinutes(2));
            cache.Add("item", item);

            cache.GetOrAdd("item", () => throw new Exception("Should not re-create item")).Should().Be(item);
        }
        
        [Fact]
        protected void GetOrAddWillAddItemOnce()
        {
            var item = new TestItem { Value = nameof(GetOrAddWillAddItemOnce) };
            var cache = new ItemCache<TestItem>("temp", 1, TimeSpan.FromMinutes(2));
            cache.GetOrAdd("item", () => item);

            cache.GetOrAdd("item", () => throw new Exception("Should not re-create item")).Should().Be(item);
        }
        
        class TestItem
        {
            public string Value { get; set; }
        }
    }
}