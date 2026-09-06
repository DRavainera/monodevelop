//
// WorkspaceTaskQueue.cs
//
// Author:
//       Marius Ungureanu <maungu@microsoft.com>
//
// Copyright (c) 2018 Microsoft Inc.
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
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace MonoDevelop.Ide.RoslynServices
{
	// Replaces the editor-layer WorkspaceTaskQueue from Roslyn, which is internal to the
	// Microsoft.VisualStudio.LanguageServices assembly and therefore not available to
	// MonoDevelop. It schedules every task reported by the workspace through the given
	// TaskScheduler (an STA/reentrant scheduler in practice).
	sealed class WorkspaceTaskQueue : IWorkspaceTaskScheduler
	{
		readonly TaskScheduler taskScheduler;

		public WorkspaceTaskQueue (object factory, TaskScheduler taskScheduler)
		{
			this.taskScheduler = taskScheduler;
		}

		public Task ScheduleTask (Action taskAction, string taskName, CancellationToken cancellationToken = default (CancellationToken))
		{
			return Task.Factory.StartNew (taskAction, cancellationToken, TaskCreationOptions.None, taskScheduler);
		}

		public Task<T> ScheduleTask<T> (Func<T> taskFunc, string taskName, CancellationToken cancellationToken = default (CancellationToken))
		{
			return Task.Factory.StartNew (taskFunc, cancellationToken, TaskCreationOptions.None, taskScheduler);
		}

		public Task ScheduleTask (Func<Task> taskFunc, string taskName, CancellationToken cancellationToken = default (CancellationToken))
		{
			return Task.Factory.StartNew (taskFunc, cancellationToken, TaskCreationOptions.None, taskScheduler).Unwrap ();
		}

		public Task<T> ScheduleTask<T> (Func<Task<T>> taskFunc, string taskName, CancellationToken cancellationToken = default (CancellationToken))
		{
			return Task.Factory.StartNew (taskFunc, cancellationToken, TaskCreationOptions.None, taskScheduler).Unwrap ();
		}
	}
}