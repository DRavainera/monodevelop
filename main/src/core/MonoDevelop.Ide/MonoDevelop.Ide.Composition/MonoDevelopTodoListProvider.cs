using System;
using System.Composition;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Implementation.TodoComments;

namespace MonoDevelop.Ide.Composition
{
	[Export (typeof (ITodoListProvider))]
	[Shared]
	sealed class MonoDevelopTodoListProvider : ITodoListProvider
	{
		public event EventHandler<TodoItemsUpdatedArgs> TodoListUpdated;
	}
}