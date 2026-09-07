// Copyright (c) Microsoft Corp (https://www.microsoft.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.

// LINUX/SDK shim for the Cocoa-only editor extras.
//
// The canonical definitions live in vs-editor-api's TextUICocoa project, which
// is only built for macOS (see OpenSource.Def.projitems). This file provides
// identical type shapes on every other platform so the MEF imports and call
// sites in this addin compile unchanged. It is excluded when building for
// Xamarin/macOS by a <Compile Remove> in the project file.

namespace Microsoft.VisualStudio.Text.Find
{
	public interface IFindPresenterFactory
	{
		IFindPresenter TryGetFindPresenter (Microsoft.VisualStudio.Text.Editor.ITextView textView);
	}

	public interface IFindPresenter
	{
		void ShowFind (bool usePreviousTerm = false, bool takeFocus = true);
		void ShowReplace ();
		void Hide ();
		bool IsVisible { get; }
		bool IsFocused { get; }
	}
}

namespace Microsoft.VisualStudio.Text.Editor
{
	public interface IInfoBarPresenterFactory
	{
		IInfoBarPresenter TryGetInfoBarPresenter (ITextView textView);
	}

	public interface IInfoBarPresenter
	{
		void Present (InfoBarViewModel viewModel);
		void Dismiss (InfoBarViewModel viewModel);
		void DismissAll ();
	}

	public readonly struct InfoBarAction
	{
		public string Title { get; }
		public System.Action Handler { get; }
		public bool IsDefault { get; }

		public InfoBarAction (string title, System.Action handler, bool isDefault = false)
		{
			Title = title;
			Handler = handler;
			IsDefault = isDefault;
		}

		public void Invoke ()
			=> Handler?.Invoke ();
	}

	public interface ICocoaTextView : ITextView2
	{
		bool IsKeyboardFocused { get; }

		void Focus ();
	}

	public sealed class InfoBarViewModel
	{
		public string PrimaryLabelText { get; }

		public string SecondaryLabelText { get; }

		public System.Collections.Generic.IReadOnlyList<InfoBarAction> Actions { get; }

		public System.Action DismissedHandler { get; }

		public InfoBarViewModel (
			string primaryLabelText,
			string secondaryLabelText,
			System.Collections.Generic.IReadOnlyList<InfoBarAction> actions,
			System.Action dismissedHandler = null)
		{
			PrimaryLabelText = primaryLabelText;
			SecondaryLabelText = secondaryLabelText;
			Actions = actions;
			DismissedHandler = dismissedHandler;
		}

		public void InvokeDismissed ()
			=> DismissedHandler?.Invoke ();
	}
}