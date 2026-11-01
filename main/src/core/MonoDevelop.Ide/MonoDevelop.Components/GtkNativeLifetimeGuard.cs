using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

namespace MonoDevelop.Components
{
	// GLibSharp 3.24 tracks native objects with a toggle reference: the C# wrapper
	// does not hold a strong reference, so the native can be freed by GTK without
	// the wrapper ever being disposed (gtk_widget_destroy dropping the last
	// reference, a parent container being destroyed, GC finalization racing with
	// native destruction). The wrapper is then left with a dangling Handle, and a
	// later Destroy()/Dispose()/GC finalization calls g_object_remove_toggle_ref()/
	// g_object_unref()/g_signal_handler_disconnect() on freed memory, which
	// produces "G_IS_OBJECT" criticals and segfaults when the freed region has
	// been reused (observed: closing an options dialog with the window close
	// button killed the process).
	//
	// The fix installs two hooks before the first wrapper exists (must run before
	// Gtk.Init):
	//
	//   1. The binding's toggle-reference callback fires once for every wrapper
	//      right after construction (add_toggle_ref + unref), while the native is
	//      guaranteed alive. A wrapper of that callback uses it as a universal
	//      creation hook to register a g_object_weak_ref() death watch on the
	//      native (idempotently).
	//
	//   2. The weak-ref callback fires during finalization, while the memory is
	//      still valid. It detaches the managed bookkeeping for the wrapper:
	//      removes the registry entry, cancels any GC-queued unref, and zeroes
	//      the wrapper Handle. From that moment on, every Destroy()/Dispose()/
	//      GC finalization on the wrapper is a safe no-op instead of a
	//      use-after-free.
	public static class GtkNativeLifetimeGuard
	{
		[UnmanagedFunctionPointer (CallingConvention.Cdecl)]
		delegate void d_g_weak_notify (IntPtr data, IntPtr where_took_pointer);

		[DllImport ("libgobject-2.0.so.0", CallingConvention = CallingConvention.Cdecl)]
		static extern void g_object_weak_ref (IntPtr instance, d_g_weak_notify weak_notify, IntPtr data);

		// Held in a static field for the process lifetime: the native side keeps
		// a raw pointer to this delegate, so it must never be garbage collected.
		static readonly d_g_weak_notify WeakNotify = OnNativeFinalized;

		class GObjectWatch
		{
			public GObjectWatch (IntPtr handle, object toggleRef, GLib.Object wrapper)
			{
				Handle = handle;
				ToggleRef = toggleRef;
				WrapperRef = wrapper != null ? new WeakReference (wrapper) : null;
			}

			public readonly IntPtr Handle;
			public readonly object ToggleRef;
			public readonly WeakReference WrapperRef;
		}

		static bool installed;
		static bool failed;
		static readonly object Sync = new object ();
		static readonly HashSet<IntPtr> Watched = new HashSet<IntPtr> ();
		static Delegate originalCallback;
		static FieldInfo handleField;
		static FieldInfo referenceField;
		static IDictionary objectRegistry;
		static IList pendingDestroys;

		public static void Install ()
		{
			if (installed || failed)
				return;

			try {
				Type glibObject = typeof (GLib.Object);
				Type toggleRef = glibObject.Assembly.GetType ("GLib.ToggleRef");
				Type callbackType = toggleRef?.GetNestedType ("ToggleNotifyHandler", BindingFlags.NonPublic);
				MethodInfo originalMethod = toggleRef?.GetMethod ("RefToggled", BindingFlags.Static | BindingFlags.NonPublic);
				FieldInfo callbackField = toggleRef?.GetField ("toggle_notify_callback", BindingFlags.Static | BindingFlags.NonPublic);

				objectRegistry = (IDictionary) glibObject.GetField ("Objects", BindingFlags.Static | BindingFlags.NonPublic)?.GetValue (null);
				handleField = glibObject.GetField ("handle", BindingFlags.Instance | BindingFlags.NonPublic);
				referenceField = toggleRef?.GetField ("reference", BindingFlags.Instance | BindingFlags.NonPublic);
				pendingDestroys = (IList) toggleRef?.GetField ("PendingDestroys", BindingFlags.Static | BindingFlags.NonPublic)?.GetValue (null);

				if (callbackType == null || originalMethod == null || callbackField == null || objectRegistry == null || handleField == null || referenceField == null || pendingDestroys == null) {
					Console.Error.WriteLine ("GtkNativeLifetimeGuard: unexpected GLibSharp layout, guard not installed");
					failed = true;
					return;
				}

				// A non-null field means wrappers already registered the original
				// callback natively; replacing it now would leave them unmanaged.
				if (callbackField.GetValue (null) != null) {
					Console.Error.WriteLine ("GtkNativeLifetimeGuard: toggle callback already in use, guard not installed");
					failed = true;
					return;
				}

				originalCallback = Delegate.CreateDelegate (callbackType, null, originalMethod);
				Delegate safeCallback = Delegate.CreateDelegate (callbackType, null, typeof (GtkNativeLifetimeGuard).GetMethod ("SafeToggleNotify", BindingFlags.Static | BindingFlags.NonPublic));

				// GLib.ToggleRef.ToggleNotifyCallback is lazy: it only assigns the
				// default when this field is null, so assigning here makes every
				// future wrapper register SafeToggleNotify with the native.
				callbackField.SetValue (null, safeCallback);
				installed = true;
			} catch (Exception ex) {
				Console.Error.WriteLine ("GtkNativeLifetimeGuard: install failed: " + ex);
				failed = true;
			}
		}

		static void SafeToggleNotify (IntPtr data, IntPtr handle, bool is_last_ref)
		{
			if (originalCallback != null)
				originalCallback.DynamicInvoke (data, handle, is_last_ref);

			EnsureDeathWatch (data, handle);
		}

		static void EnsureDeathWatch (IntPtr data, IntPtr handle)
		{
			if (handle == IntPtr.Zero)
				return;

			try {
				lock (Sync) {
					if (Watched.Contains (handle))
						return;
				}

				object toggleRef = GCHandle.FromIntPtr (data).Target;
				object reference = toggleRef != null ? referenceField.GetValue (toggleRef) : null;
				GLib.Object wrapper = reference as GLib.Object ?? (reference as WeakReference)?.Target as GLib.Object;
				GObjectWatch watch = new GObjectWatch (handle, toggleRef, wrapper);
				GCHandle gch = GCHandle.Alloc (watch);

				try {
					g_object_weak_ref (handle, WeakNotify, (IntPtr) gch);
					lock (Sync)
						Watched.Add (handle);
				} catch {
					gch.Free ();
				}
			} catch {
				// Never propagate out of the native callback
			}
		}

		static void OnNativeFinalized (IntPtr data, IntPtr where_took_pointer)
		{
			GCHandle gch = GCHandle.FromIntPtr (data);
			GObjectWatch watch = gch.Target as GObjectWatch;
			gch.Free ();
			if (watch == null)
				return;

			try {
				lock (Sync)
					Watched.Remove (watch.Handle);

				// Cancel any unref queued by a racing GC finalization so it cannot
				// touch the native after it is freed.
				lock (pendingDestroys)
					pendingDestroys.Remove (watch.ToggleRef);

				// Detach the managed bookkeeping so any later Destroy()/Dispose()/
				// GC finalization on the wrapper is a no-op instead of a
				// use-after-free on the just-freed native.
				lock (objectRegistry)
					objectRegistry.Remove (watch.Handle);

				GLib.Object wrapper = watch.WrapperRef?.Target as GLib.Object;
				if (wrapper != null)
					handleField.SetValue (wrapper, IntPtr.Zero);
			} catch {
				// Never propagate out of the native callback
			}
		}
	}
}
