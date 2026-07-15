// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

using System.Collections;
using System.Collections.Concurrent;

namespace Pocok.Conversion.Tests;

public sealed class CollectionConversionTests
{
    private readonly IValueConverter _converter = new ValueConverter();

    [Test]
    public void ArrayItemsAreConvertedRecursively()
    {
        string[] source = ["1", "2", "3"];

        ConversionResult<int[]> result = _converter.Convert<int[]>(source);

        result.Value.ShouldBe([1, 2, 3]);
    }

    [Test]
    public void CommonCollectionInterfacesUseAssignableImplementations()
    {
        string[] source = ["1", "1", "2"];
        IReadOnlyList<int> list = _converter.Convert<IReadOnlyList<int>>(new ArrayList { "1", 2, 3m }).Value;
        ISet<int> set = _converter.Convert<ISet<int>>(source).Value;

        list.ShouldBe([1, 2, 3]);
        set.SetEquals([1, 2]).ShouldBeTrue();
    }

    [Test]
    public void ConcurrentCollectionTargetsAreSupported()
    {
        string[] source = ["1", "2", "3"];

        ConversionResult<ConcurrentBag<int>> result = _converter.Convert<ConcurrentBag<int>>(source);

        result.Value.Order().ShouldBe([1, 2, 3]);
    }

    [Test]
    public void NestedCollectionsAreConvertedRecursively()
    {
        string[] first = ["1", "2"];
        string[] second = ["3"];
        object[] source = [first, second];

        ConversionResult<int[][]> result = _converter.Convert<int[][]>(source);

        result.Value.Length.ShouldBe(2);
        result.Value[0].ShouldBe([1, 2]);
        result.Value[1].ShouldBe([3]);
    }

    [Test]
    public void CollectionStopsAtFirstFailedItem()
    {
        string[] source = ["1", "invalid", "3"];

        ConversionResult<int[]> result = _converter.Convert<int[]>(source);

        result.Error!.Code.ShouldBe(ConversionErrorCodes.InvalidFormat);
    }

    [Test]
    public void NullItemsRespectTargetNullPolicy()
    {
        string?[] source = ["1", null];

        _converter.Convert<int?[]>(source).Value.ShouldBe([1, null]);
        _converter.Convert<int[]>(source).Error!.Code.ShouldBe(ConversionErrorCodes.NullNotAllowed);
    }

    [Test]
    public void StringIsNotImplicitlyTreatedAsCharacterCollection()
    {
        _converter.Convert<char[]>("abc").Error!.Code.ShouldBe(ConversionErrorCodes.Unsupported);
    }

    [Test]
    public void KeyValuePairConvertsBothSides()
    {
        var source = new KeyValuePair<string, string>("42", "12.5");

        ConversionResult<KeyValuePair<int, decimal>> result = _converter.Convert<KeyValuePair<int, decimal>>(source);

        result.Value.ShouldBe(new KeyValuePair<int, decimal>(42, 12.5m));
    }

    [Test]
    public void DictionaryEntryAndKeyValuePairInteroperate()
    {
        var entry = new DictionaryEntry("7", "ready");
        KeyValuePair<int, string> pair = _converter.Convert<KeyValuePair<int, string>>(entry).Value;

        pair.ShouldBe(new KeyValuePair<int, string>(7, "ready"));
        _converter.Convert<DictionaryEntry>(pair).Value.ShouldBe(new DictionaryEntry(7, "ready"));
    }

    [Test]
    public void DictionaryConvertsKeysAndValues()
    {
        var source = new Dictionary<string, string> { ["1"] = "1.5", ["2"] = "2.5" };

        ConversionResult<IReadOnlyDictionary<int, decimal>> result =
            _converter.Convert<IReadOnlyDictionary<int, decimal>>(source);

        result.Value.Count.ShouldBe(2);
        result.Value[1].ShouldBe(1.5m);
        result.Value[2].ShouldBe(2.5m);
    }

    [Test]
    public void ConcurrentDictionaryTargetIsSupported()
    {
        var source = new Dictionary<string, string> { ["1"] = "10", ["2"] = "20" };

        ConversionResult<ConcurrentDictionary<int, long>> result =
            _converter.Convert<ConcurrentDictionary<int, long>>(source);

        result.Value.Count.ShouldBe(2);
        result.Value[1].ShouldBe(10);
        result.Value[2].ShouldBe(20);
    }

    [Test]
    public void DuplicateConvertedKeysFailWithoutThrowing()
    {
        KeyValuePair<string, int>[] source =
        [
            new("1", 10),
            new("01", 20)
        ];

        ConversionResult<Dictionary<int, int>> result = _converter.Convert<Dictionary<int, int>>(source);

        result.Error!.Code.ShouldBe(ConversionErrorCodes.DuplicateKey);
    }

    [Test]
    public void NonPairDictionarySourceFailsWithoutReflectionFallback()
    {
        int[] source = [1, 2];

        ConversionResult<Dictionary<int, int>> result = _converter.Convert<Dictionary<int, int>>(source);

        result.Error!.Code.ShouldBe(ConversionErrorCodes.CollectionItem);
    }

    [Test]
    public void CancellationFromCollectionCodePropagates()
    {
        int[] source = [1];

        Should.Throw<OperationCanceledException>(() =>
            _converter.Convert<CancelingCollection<int>>(source));
    }

    [Test]
    public void CustomCollectionInterfaceFailsWithoutReturningAnIncompatibleValue()
    {
        string[] source = ["1"];

        ConversionResult<ICustomCollection<int>> result = _converter.Convert<ICustomCollection<int>>(source);

        result.Error!.Code.ShouldBe(ConversionErrorCodes.Unsupported);
    }

    [Test]
    public void CustomDictionaryInterfaceFailsWithoutReturningAnIncompatibleValue()
    {
        KeyValuePair<string, string>[] source = [new("1", "2")];

        ConversionResult<object?> result = _converter.Convert(source, typeof(ICustomDictionary<int, int>));

        result.Error!.Code.ShouldBe(ConversionErrorCodes.Unsupported);
    }

    [Test]
    public void MultidimensionalArrayTargetFailsWithoutReturningAOneDimensionalArray()
    {
        int[] source = [1, 2];

        ConversionResult<object?> result = _converter.Convert(source, typeof(int[,]));

        result.Error!.Code.ShouldBe(ConversionErrorCodes.Unsupported);
    }

    private interface ICustomCollection<T> : ICollection<T>
    {
    }

    private interface ICustomDictionary<TKey, TValue> : IDictionary<TKey, TValue>
        where TKey : notnull
    {
    }

    private sealed class CancelingCollection<T> : ICollection<T>
    {
        public int Count => 0;
        public bool IsReadOnly => false;

        public void Add(T item)
        {
            throw new OperationCanceledException();
        }

        public void Clear()
        {
        }

        public bool Contains(T item)
        {
            return false;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
        }

        public bool Remove(T item)
        {
            return false;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return Enumerable.Empty<T>().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
