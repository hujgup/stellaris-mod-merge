using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Runtime.Serialization;
using JetBrains.Annotations;

namespace Echo.StellarisModMerge {
	public interface IOrderedDictionary<in TKey, TValue> : IOrderedDictionary {
		new TValue this[int index] {
			get;
			set;
		}
		void Insert(int index, TKey key, TValue value);
	}

	public class OrderedDictionary<TKey, TValue> : IOrderedDictionary<TKey, TValue>, IDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>, IDictionary, ISerializable, IDeserializationCallback {
		private class UntypedComparer : IEqualityComparer {
			private readonly IEqualityComparer<TKey> _cmp;
			public UntypedComparer(IEqualityComparer<TKey> cmp) {
				_cmp = cmp;
			}
			bool IEqualityComparer.Equals([NotNull] object x, [NotNull] object y) {
				return x is TKey key1 && y is TKey key2 && _cmp.Equals(key1, key2);
			}
			int IEqualityComparer.GetHashCode(object obj) {
				return obj is TKey key ? _cmp.GetHashCode(key) : obj.GetHashCode();
			}
		}

		private class TypedCollection<T> : ICollection<T>, ICollection {
			private readonly ICollection _col;
			public TypedCollection(ICollection col) {
				if (col.Cast<object>().Any(obj => !(obj is T))) {
					throw new InvalidArgumentTypeException("Collection contained an object that was not of type " + typeof(T).FullName, nameof(col));
				}
				_col = col;
			}
			public int Count {
				get => _col. Count;
			}
			public bool IsReadOnly {
				get => true;
			}
			public bool IsSynchronized {
				get => _col.IsSynchronized;
			}
			public object SyncRoot {
				get => _col.SyncRoot;
			}
			public void Add([CanBeNull] T value) {
				throw new NotSupportedException("Collection is readonly.");
			}
			public void Clear() {
				throw new NotSupportedException("Collection is readonly.");
			}
			public bool Contains([CanBeNull] T value) {
				return _col.Cast<object>().Contains(value);
			}
			public void CopyTo(T[] array, int index) {
				_col.CopyTo(array, index);
			}
			public void CopyTo(Array array, int index) {
				_col.CopyTo(array, index);
			}
			public bool Remove([CanBeNull] T value) {
				throw new NotSupportedException("Collection is readonly.");
			}
			public IEnumerator<T> GetEnumerator() {
				return _col.Cast<T>().GetEnumerator();
			}
			IEnumerator IEnumerable.GetEnumerator() {
				return _col.GetEnumerator();
			}
		}

		private class Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>, IDictionaryEnumerator {
			[NotNull]
			private readonly IDictionaryEnumerator _e;
			public Enumerator(OrderedDictionary dic) {
				_e = dic.GetEnumerator();
			}
			public KeyValuePair<TKey, TValue> Current {
				get {
					// ReSharper disable once PossibleNullReferenceException
					var current = (DictionaryEntry)_e.Current;
					return new KeyValuePair<TKey, TValue>((TKey)current.Key, (TValue)current.Value);
				}
			}
			[NotNull]
			object IEnumerator.Current {
				get => Current;
			}
			[NotNull]
			object IDictionaryEnumerator.Key {
				// ReSharper disable once PossibleNullReferenceException
				get => ((DictionaryEntry)_e.Current).Key;
			}
			[CanBeNull]
			object IDictionaryEnumerator.Value {
				// ReSharper disable once PossibleNullReferenceException
				get => ((DictionaryEntry)_e.Current).Value;
			}
			[NotNull]
			DictionaryEntry IDictionaryEnumerator.Entry {
				// ReSharper disable once PossibleNullReferenceException
				get => (DictionaryEntry)_e.Current;
			}
			public bool MoveNext() {
				return _e.MoveNext();
			}
			public void Reset() {
				_e.Reset();
			}
			void IDisposable.Dispose() {}
		}

		private readonly OrderedDictionary _dic;
		public OrderedDictionary(int capacity, IEqualityComparer<TKey> comparer) {
			_dic = new OrderedDictionary(capacity, new UntypedComparer(comparer));
		}
		public OrderedDictionary(IEqualityComparer<TKey> comparer) {
			_dic = new OrderedDictionary(new UntypedComparer(comparer));
		}
		public OrderedDictionary(int capacity) {
			_dic = new OrderedDictionary();
		}
		public OrderedDictionary() {
			_dic = new OrderedDictionary();
		}
		[CanBeNull]
		public TValue this[int index] {
			get => (TValue)_dic[index];
			set => _dic[index] = value;
		}
		[CanBeNull]
		public TValue this[[NotNull] TKey key] {
			get => (TValue)_dic[key];
			set => _dic[key] = value;
		}
		[CanBeNull]
		object IOrderedDictionary.this[int index] {
			get => _dic[index];
			set {
				if (ValueAssignable(value)) {
					_dic[index] = value;
				} else {
					throw new InvalidArgumentTypeException("Value is not a " + typeof(TValue).FullName, nameof(value));
				}
			}
		}
		[CanBeNull]
		object IDictionary.this[object key] {
			get {
				if (key is TKey) {
					return _dic[key];
				} else {
					throw new InvalidArgumentTypeException("Key is not a " + typeof(TKey).FullName, nameof(key));
				}
			}
			set {
				if (key is TKey) {
					if (ValueAssignable(value)) {
						_dic[key] = value;
					} else {
						throw new InvalidArgumentTypeException("Value is not a " + typeof(TValue).FullName, nameof(value));
					}
				} else {
					throw new InvalidArgumentTypeException("Key is not a " + typeof(TKey).FullName, nameof(key));
				}
			}
		}
		public int Count {
			get => _dic.Count;
		}
		public bool IsReadOnly {
			get => _dic.IsReadOnly;
		}
		public ICollection<TKey> Keys {
			get => new TypedCollection<TKey>(_dic.Keys);
		}
		ICollection IDictionary.Keys {
			get => _dic.Keys;
		}
		IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys {
			get => new TypedCollection<TKey>(_dic.Keys);
		}
		[ItemCanBeNull]
		public ICollection<TValue> Values {
			get => new TypedCollection<TValue>(_dic.Values);
		}
		[ItemCanBeNull]
		ICollection IDictionary.Values {
			get => _dic.Values;
		}
		[ItemCanBeNull]
		IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values {
			get => new TypedCollection<TValue>(_dic.Values);
		}
		bool IDictionary.IsFixedSize {
			get => ((IDictionary)_dic).IsFixedSize;
		}
		bool ICollection.IsSynchronized {
			get => ((ICollection)_dic).IsSynchronized;
		}
		object ICollection.SyncRoot {
			get => ((ICollection)_dic).SyncRoot;
		}
		private static bool ValueAssignable([CanBeNull] object value) {
			return value is null ? !typeof(TValue).IsValueType : value is TValue;
		}
		public void Add(TKey key, [CanBeNull] TValue value) {
			_dic.Add(key, value);
		}
		void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> kvp) {
			_dic.Add(kvp.Key, kvp.Value);
		}
		void IDictionary.Add(object key, [CanBeNull] object value) {
			if (key is TKey) {
				if (ValueAssignable(value)) {
					_dic.Add(key, value);
				} else {
					throw new InvalidArgumentTypeException("Value is not a " + typeof(TValue).FullName, nameof(value));
				}
			} else {
				throw new InvalidArgumentTypeException("Key is not a " + typeof(TKey).FullName, nameof(key));
			}
		}
		public bool ContainsKey([NotNull] TKey key) {
			return _dic.Contains(key);
		}
		bool IDictionary.Contains(object key) {
			if (key is TKey) {
				return _dic.Contains(key);
			} else {
				throw new InvalidArgumentTypeException("Key is not a " + typeof(TKey).FullName, nameof(key));
			}
		}
		public bool ContainsValue([CanBeNull] TValue value) {
			return _dic.Values.Cast<TValue>().Contains(value);
		}
		public void Insert(int index, TKey key, [CanBeNull] TValue value) {
			_dic.Insert(index, key, value);
		}
		void IOrderedDictionary.Insert(int index, object key, [CanBeNull] object value) {
			if (key is TKey) {
				if (ValueAssignable(value)) {
					_dic.Insert(index, key, value);
				} else {
					throw new InvalidArgumentTypeException("Value is not a " + typeof(TValue).FullName, nameof(value));
				}
			} else {
				throw new InvalidArgumentTypeException("Key is not a " + typeof(TKey).FullName, nameof(key));
			}
		}
		public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() {
			return new Enumerator(_dic);
		}
		IDictionaryEnumerator IOrderedDictionary.GetEnumerator() {
			return new Enumerator(_dic);
		}
		IDictionaryEnumerator IDictionary.GetEnumerator() {
			return new Enumerator(_dic);
		}
		IEnumerator IEnumerable.GetEnumerator() {
			return GetEnumerator();
		}
		public void Clear() {
			_dic.Clear();
		}
		public bool Remove(TKey key) {
			bool res = _dic.Contains(key);
			if (res) {
				_dic.Remove(key);
			}
			return res;
		}
		public bool Remove(KeyValuePair<TKey, TValue> kvp) {
			bool res = TryGetValue(kvp.Key, out TValue value);
			if (res) {
				res = value?.Equals(kvp.Value) ?? (kvp.Value == null);
			}
			return res;
		}
		void IDictionary.Remove(object key) {
			if (key is TKey) {
				_dic.Remove(key);
			}
		}
		public bool TryGetValue(TKey key, [CanBeNull] out TValue value) {
			bool res = _dic.Contains(key);
			value = res ? (TValue)_dic[key] : default(TValue);
			return res;
		}
		public bool Contains(KeyValuePair<TKey, TValue> kvp) {
			bool res = _dic.Contains(kvp.Key);
			if (res) {
				res = _dic[kvp.Key]?.Equals(kvp.Value) ?? (kvp.Value == null);
			}
			return res;
		}
		public void CopyTo(KeyValuePair<TKey, TValue>[] array, int index) {
			foreach (KeyValuePair<TKey, TValue> kvp in this) {
				array[index++] = kvp;
			}
		}
		public void CopyTo(Array array, int index) {
			foreach (KeyValuePair<TKey, TValue> kvp in this) {
				array.SetValue(kvp, index++);
			}
		}
		public void RemoveAt(int index) {
			_dic.RemoveAt(index);
		}
		void IDeserializationCallback.OnDeserialization(object sender) {
			((IDeserializationCallback)_dic).OnDeserialization(sender);
		}
		public void GetObjectData(SerializationInfo info, StreamingContext context) {
			_dic.GetObjectData(info, context);
		}
	}
}
