//
// MockOptionService.cs
//
// Author:
//       Marius Ungureanu <maungu@microsoft.com>
//
// Copyright (c) 2018 Microsoft Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated terms, to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Options;

namespace MonoDevelop.Ide.RoslynServices.Options
{
	// Sustituto local de Microsoft.CodeAnalysis.Options.OptionChangedEventArgs,
	// que es internal en Roslyn 4.8 (el test solo usa Option/Value).
	class MockOptionChangedEventArgs : EventArgs
	{
		public MockOptionChangedEventArgs (OptionKey optionKey, object value)
		{
			Option = optionKey.Option;
			Value = value;
		}

		public IOption Option { get; }
		public object Value { get; }
	}

	class MockOptionService : IOptionService
	{
		// Roslyn 4.8: OptionSet tiene constructor y field changedOptions internos.
		// El proyecto de tests no es friend de Microsoft.CodeAnalysis.Workspaces,
		// así que se accede por reflection (mismo espíritu que MonoRoslynCompat).
		static readonly ConstructorInfo s_optionSetCtor = typeof (OptionSet).GetConstructor (BindingFlags.Instance | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
		static readonly FieldInfo s_optionSetChangedOptionsField = typeof (OptionSet).GetField ("changedOptions", BindingFlags.Instance | BindingFlags.NonPublic);

		static OptionSet CreateOptionSet () => (OptionSet)s_optionSetCtor.Invoke (null);

		static IEnumerable<OptionKey> GetChangedOptionKeys (OptionSet optionSet)
		{
			if (s_optionSetChangedOptionsField.GetValue (optionSet) is ImmutableDictionary<OptionKey, object> changedOptions)
				return changedOptions.Keys;

			return Enumerable.Empty<OptionKey> ();
		}

		IOptionPersister persister;
		object _gate = new object ();
		HashSet<IOption> _options = new HashSet<IOption> ();
		ImmutableDictionary<OptionKey, object> _currentValues = ImmutableDictionary.Create<OptionKey, object> ();

		public void RegisterPersister (IOptionPersister persister) => this.persister = persister;

		public void RegisterOption (IOption option) => _options.Add (option);

		public IEnumerable<IOption> GetRegisteredOptions () => _options;

		public T GetOption<T> (Option<T> option) => (T)GetOption (new OptionKey (option, language: null));

		public T GetOption<T> (PerLanguageOption<T> option, string language) => (T)GetOption (new OptionKey (option, language));

		public bool GetOptionInternal (OptionKey optionKey, out object value)
		{
			return _currentValues.TryGetValue (optionKey, out value);
		}

		public object GetOption (OptionKey optionKey)
		{
			lock (_gate) {
				if (GetOptionInternal (optionKey, out var value))
					return value;

				value = LoadOptionFromSerializerOrGetDefault (optionKey);
				_currentValues = _currentValues.Add (optionKey, value);
				return value;
			}
		}

		object LoadOptionFromSerializerOrGetDefault (OptionKey optionKey)
		{
			if (persister.TryFetch (optionKey, out var deserializedValue))
				return deserializedValue;

			return optionKey.Option.DefaultValue;
		}

		public void SetOptions (OptionSet optionSet)
		{
			var changedOptions = new List<MockOptionChangedEventArgs> ();

			lock (_gate) {
				foreach (var optionKey in GetChangedOptionKeys (optionSet)) {
					var setValue = optionSet.GetOption (optionKey);
					var currentValue = GetOption (optionKey);

					if (object.Equals (currentValue, setValue)) {
						// Identical, so nothing is changing
						continue;
					}

					// The value is actually changing, so update
					changedOptions.Add (new MockOptionChangedEventArgs (optionKey, setValue));

					_currentValues = _currentValues.SetItem (optionKey, setValue);

					persister.TryPersist (optionKey, setValue);
				}
			}

			// Outside of the lock, raise the events on our task queue.
			RaiseEvents (changedOptions);
		}

		public void RefreshOption (OptionKey optionKey, object newValue)
		{
			lock (_gate) {
				if (_currentValues.TryGetValue (optionKey, out var oldValue)) {
					if (object.Equals (oldValue, newValue)) {
						// Value is still the same, no reason to raise events
						return;
					}
				}

				_currentValues = _currentValues.SetItem (optionKey, newValue);
			}

			RaiseEvents (new List<MockOptionChangedEventArgs> { new MockOptionChangedEventArgs (optionKey, newValue) });
		}

		void RaiseEvents (List<MockOptionChangedEventArgs> changedOptions)
		{
			var optionChanged = OptionChanged;
			if (optionChanged != null) {
				foreach (var changedOption in changedOptions) {
					optionChanged (this, changedOption);
				}
			}
		}

		public OptionSet GetOptions () => CreateOptionSet ();

		public void RegisterDocumentOptionsProvider (IDocumentOptionsProvider documentOptionsProvider) => throw new NotImplementedException ();

		public Task<OptionSet> GetUpdatedOptionSetForDocumentAsync (Document document, OptionSet optionSet, CancellationToken cancellationToken) => throw new NotImplementedException ();

		public event EventHandler<MockOptionChangedEventArgs> OptionChanged;
	}
}
