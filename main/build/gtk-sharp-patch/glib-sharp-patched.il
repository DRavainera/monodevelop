.assembly extern System.Core
{
  .ver 4:0:0:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}
.assembly extern System
{
  .ver 4:0:0:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}
.assembly extern mscorlib
{
  .ver 4:0:0:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}
.assembly 'glib-sharp'
{
  .custom instance void class GLib.IgnoreClassInitializersAttribute::'.ctor'() =  (01 00 00 00 ) // ....

  .custom instance void class [mscorlib]System.Reflection.AssemblyKeyFileAttribute::'.ctor'(string) =  (
		01 00 0D 67 74 6B 2D 73 68 61 72 70 2E 73 6E 6B   // ...gtk-sharp.snk
		00 00                                           ) // ..

  .custom instance void class [mscorlib]System.Reflection.AssemblyDelaySignAttribute::'.ctor'(bool) =  (01 00 00 00 00 ) // .....

  .custom instance void class [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::'.ctor'() =  (
		01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
		63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01       ) // ceptionThrows.

  .permissionset reqmin = {[mscorlib]System.Security.Permissions.SecurityPermissionAttribute = {property bool 'SkipVerification' = bool(true)}}
  .hash algorithm 0x00008004
  .ver  2:12:0:0
  .publickey = (
		00 24 00 00 04 80 00 00 94 00 00 00 06 02 00 00   // .$..............
		00 24 00 00 52 53 41 31 00 04 00 00 01 00 01 00   // .$..RSA1........
		71 EB 6C 55 75 52 9C BF 72 44 F7 A6 EA 05 62 84   // q.lUuR..rD....b.
		F9 EA E0 3B CF F2 CC 13 2C 9C 49 0A B3 09 EA B0   // ...;....,.I.....
		B5 6B CE 44 9D F5 03 D9 C0 A8 1E 52 05 85 CD BE   // .k.D.......R....
		70 E2 FB 90 43 4B AC 04 FA 62 22 A8 00 98 B7 A1   // p...CK...b".....
		A7 B3 AF 99 1A 41 23 24 BB 43 25 F6 B8 65 BB 64   // .....A#$.C%..e.d
		EB F6 D1 C2 06 D5 73 2D DF BC 70 A7 38 9E E5 3E   // ......s-..p.8..>
		0C 24 6E 32 79 74 1A D0 05 03 E4 98 42 E1 9B F3   // .$n2yt......B...
		7B 19 8B 40 21 26 CB 36 89 C2 EA 64 96 A4 7C B4 ) // {..@!&.6...d..|.
}
.module 'glib-sharp.dll' // GUID = {2E15C8EC-EDB8-45C4-8C5B-ED02517404B0}

.custom instance void class [mscorlib]System.Security.UnverifiableCodeAttribute::'.ctor'() =  (01 00 00 00 ) // ....

.module extern 'glibsharpglue-2'
.module extern 'libgobject-2.0-0.dll'
.module extern 'libglib-2.0-0.dll'

.namespace GLib
{
  .class public auto ansi beforefieldinit Argv
  	extends [mscorlib]System.Object
  {
    .field  private  native int[] arg_ptrs
    .field  private  native int handle
    .field  private  bool add_progname

    // method line 1
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (string[] args)  cil managed 
    {
        // Method begins at RVA 0x2050
	// Code size 9 (0x9)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldarg.1 
	IL_0002:  ldc.i4.0 
	IL_0003:  call instance void class GLib.Argv::'.ctor'(string[], bool)
	IL_0008:  ret 
    } // end of method Argv::.ctor

    // method line 2
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (string[] args, bool add_program_name)  cil managed 
    {
        // Method begins at RVA 0x205c
	// Code size 190 (0xbe)
	.maxstack 4
	.locals init (
		string[]	V_0,
		int32	V_1,
		int32	V_2)
	IL_0000:  ldarg.0 
	IL_0001:  ldc.i4.0 
	IL_0002:  stfld bool GLib.Argv::add_progname
	IL_0007:  ldarg.0 
	IL_0008:  call instance void object::'.ctor'()
	IL_000d:  ldarg.0 
	IL_000e:  ldarg.2 
	IL_000f:  stfld bool GLib.Argv::add_progname
	IL_0014:  ldarg.0 
	IL_0015:  ldfld bool GLib.Argv::add_progname
	IL_001a:  brfalse IL_003f

	IL_001f:  ldarg.1 
	IL_0020:  ldlen 
	IL_0021:  conv.i4 
	IL_0022:  ldc.i4.1 
	IL_0023:  add 
	IL_0024:  newarr string
	IL_0029:  stloc.0 
	IL_002a:  ldloc.0 
	IL_002b:  ldc.i4.0 
	IL_002c:  call string[] class [mscorlib]System.Environment::GetCommandLineArgs()
	IL_0031:  ldc.i4.0 
	IL_0032:  ldelem.ref 
	IL_0033:  stelem.ref 
	IL_0034:  ldarg.1 
	IL_0035:  ldloc.0 
	IL_0036:  ldc.i4.1 
	IL_0037:  callvirt instance void class [mscorlib]System.Array::CopyTo(class [mscorlib]System.Array, int32)
	IL_003c:  ldloc.0 
	IL_003d:  starg.s 1
	IL_003f:  ldarg.0 
	IL_0040:  ldarg.1 
	IL_0041:  ldlen 
	IL_0042:  conv.i4 
	IL_0043:  newarr native int
	IL_0048:  stfld native int[] GLib.Argv::arg_ptrs
	IL_004d:  ldc.i4.0 
	IL_004e:  stloc.1 
	IL_004f:  br IL_006d

	IL_0054:  ldarg.0 
	IL_0055:  ldfld native int[] GLib.Argv::arg_ptrs
	IL_005a:  ldloc.1 
	IL_005b:  ldelema native int
	IL_0060:  ldarg.1 
	IL_0061:  ldloc.1 
	IL_0062:  ldelem.ref 
	IL_0063:  call native int class GLib.Marshaller::StringToPtrGStrdup(string)
	IL_0068:  stind.i 
	IL_0069:  ldloc.1 
	IL_006a:  ldc.i4.1 
	IL_006b:  add 
	IL_006c:  stloc.1 
	IL_006d:  ldloc.1 
	IL_006e:  ldarg.1 
	IL_006f:  ldlen 
	IL_0070:  conv.i4 
	IL_0071:  blt IL_0054

	IL_0076:  ldarg.0 
	IL_0077:  call int32 native int::get_Size()
	IL_007c:  ldarg.1 
	IL_007d:  ldlen 
	IL_007e:  conv.i4 
	IL_007f:  mul 
	IL_0080:  newobj instance void native int::'.ctor'(int32)
	IL_0085:  call native int class GLib.Argv::g_malloc(native int)
	IL_008a:  stfld native int GLib.Argv::handle
	IL_008f:  ldc.i4.0 
	IL_0090:  stloc.2 
	IL_0091:  br IL_00b4

	IL_0096:  ldarg.0 
	IL_0097:  ldfld native int GLib.Argv::handle
	IL_009c:  ldloc.2 
	IL_009d:  call int32 native int::get_Size()
	IL_00a2:  mul 
	IL_00a3:  ldarg.0 
	IL_00a4:  ldfld native int[] GLib.Argv::arg_ptrs
	IL_00a9:  ldloc.2 
	IL_00aa:  ldelem.i 
	IL_00ab:  call void class [mscorlib]System.Runtime.InteropServices.Marshal::WriteIntPtr(native int, int32, native int)
	IL_00b0:  ldloc.2 
	IL_00b1:  ldc.i4.1 
	IL_00b2:  add 
	IL_00b3:  stloc.2 
	IL_00b4:  ldloc.2 
	IL_00b5:  ldarg.1 
	IL_00b6:  ldlen 
	IL_00b7:  conv.i4 
	IL_00b8:  blt IL_0096

	IL_00bd:  ret 
    } // end of method Argv::.ctor

    // method line 3
    .method private static hidebysig pinvokeimpl ("libglib-2.0-0.dll" as "g_malloc" cdecl )
           default native int g_malloc (native int size)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method Argv::g_malloc

    // method line 4
    .method private static hidebysig pinvokeimpl ("libglib-2.0-0.dll" as "g_free" cdecl )
           default void g_free (native int mem)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method Argv::g_free

    // method line 5
    .method family virtual hidebysig 
           instance default void Finalize ()  cil managed 
    {
        // Method begins at RVA 0x2128
	// Code size 61 (0x3d)
	.maxstack 2
	.locals init (
		native int	V_0,
		native int[]	V_1,
		int32	V_2)
	.try { // 0
	  IL_0000:  ldarg.0 
	  IL_0001:  ldfld native int[] GLib.Argv::arg_ptrs
	  IL_0006:  stloc.1 
	  IL_0007:  ldc.i4.0 
	  IL_0008:  stloc.2 
	  IL_0009:  br IL_001c

	  IL_000e:  ldloc.1 
	  IL_000f:  ldloc.2 
	  IL_0010:  ldelem.i 
	  IL_0011:  stloc.0 
	  IL_0012:  ldloc.0 
	  IL_0013:  call void class GLib.Argv::g_free(native int)
	  IL_0018:  ldloc.2 
	  IL_0019:  ldc.i4.1 
	  IL_001a:  add 
	  IL_001b:  stloc.2 
	  IL_001c:  ldloc.2 
	  IL_001d:  ldloc.1 
	  IL_001e:  ldlen 
	  IL_001f:  conv.i4 
	  IL_0020:  blt IL_000e

	  IL_0025:  ldarg.0 
	  IL_0026:  ldfld native int GLib.Argv::handle
	  IL_002b:  call void class GLib.Argv::g_free(native int)
	  IL_0030:  leave IL_003c

	} // end .try 0
	finally  { // 0
	  IL_0035:  ldarg.0 
	  IL_0036:  call instance void object::Finalize()
	  IL_003b:  endfinally 
	} // end handler 0
	IL_003c:  ret 
    } // end of method Argv::Finalize

    // method line 6
    .method public hidebysig specialname 
           instance default native int get_Handle ()  cil managed 
    {
        // Method begins at RVA 0x2184
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldfld native int GLib.Argv::handle
	IL_0006:  ret 
    } // end of method Argv::get_Handle

    // method line 7
    .method public hidebysig 
           instance default string[] GetArgs (int32 argc)  cil managed 
    {
        // Method begins at RVA 0x218c
	// Code size 97 (0x61)
	.maxstack 5
	.locals init (
		int32	V_0,
		int32	V_1,
		string[]	V_2,
		int32	V_3)
	IL_0000:  ldarg.0 
	IL_0001:  ldfld bool GLib.Argv::add_progname
	IL_0006:  brfalse IL_0013

	IL_000b:  ldarg.1 
	IL_000c:  ldc.i4.1 
	IL_000d:  sub 
	IL_000e:  br IL_0014

	IL_0013:  ldarg.1 
	IL_0014:  stloc.0 
	IL_0015:  ldarg.0 
	IL_0016:  ldfld bool GLib.Argv::add_progname
	IL_001b:  brfalse IL_0026

	IL_0020:  ldc.i4.1 
	IL_0021:  br IL_0027

	IL_0026:  ldc.i4.0 
	IL_0027:  stloc.1 
	IL_0028:  ldloc.0 
	IL_0029:  newarr string
	IL_002e:  stloc.2 
	IL_002f:  ldc.i4.0 
	IL_0030:  stloc.3 
	IL_0031:  br IL_0058

	IL_0036:  ldloc.2 
	IL_0037:  ldloc.3 
	IL_0038:  ldarg.0 
	IL_0039:  ldfld native int GLib.Argv::handle
	IL_003e:  ldloc.1 
	IL_003f:  call int32 native int::get_Size()
	IL_0044:  mul 
	IL_0045:  call native int class [mscorlib]System.Runtime.InteropServices.Marshal::ReadIntPtr(native int, int32)
	IL_004a:  call string class GLib.Marshaller::Utf8PtrToString(native int)
	IL_004f:  stelem.ref 
	IL_0050:  ldloc.3 
	IL_0051:  ldc.i4.1 
	IL_0052:  add 
	IL_0053:  stloc.3 
	IL_0054:  ldloc.1 
	IL_0055:  ldc.i4.1 
	IL_0056:  add 
	IL_0057:  stloc.1 
	IL_0058:  ldloc.3 
	IL_0059:  ldloc.0 
	IL_005a:  blt IL_0036

	IL_005f:  ldloc.2 
	IL_0060:  ret 
    } // end of method Argv::GetArgs

	.property instance native int Handle ()
	{
		.get instance default native int GLib.Argv::get_Handle () 
	}
  } // end of class GLib.Argv
}

.namespace GLib
{
  .class public auto ansi beforefieldinit Boxed
  	extends [mscorlib]System.Object
  {
    .custom instance void class [mscorlib]System.ObsoleteAttribute::'.ctor'() =  (01 00 00 00 ) // ....

    .field  private  object obj
    .field  private  native int raw

    // method line 8
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (object o)  cil managed 
    {
        // Method begins at RVA 0x21fc
	// Code size 14 (0xe)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::'.ctor'()
	IL_0006:  ldarg.0 
	IL_0007:  ldarg.1 
	IL_0008:  stfld object GLib.Boxed::obj
	IL_000d:  ret 
    } // end of method Boxed::.ctor

    // method line 9
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (native int ptr)  cil managed 
    {
        // Method begins at RVA 0x220c
	// Code size 14 (0xe)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::'.ctor'()
	IL_0006:  ldarg.0 
	IL_0007:  ldarg.1 
	IL_0008:  stfld native int GLib.Boxed::raw
	IL_000d:  ret 
    } // end of method Boxed::.ctor

    // method line 10
    .method public virtual hidebysig newslot specialname 
           instance default native int get_Handle ()  cil managed 
    {
        // Method begins at RVA 0x221c
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldfld native int GLib.Boxed::raw
	IL_0006:  ret 
    } // end of method Boxed::get_Handle

    // method line 11
    .method public virtual hidebysig newslot specialname 
           instance default void set_Handle (native int 'value')  cil managed 
    {
        // Method begins at RVA 0x2224
	// Code size 8 (0x8)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldarg.1 
	IL_0002:  stfld native int GLib.Boxed::raw
	IL_0007:  ret 
    } // end of method Boxed::set_Handle

    // method line 12
    .method public static hidebysig specialname 
           default native int op_Explicit (class GLib.Boxed boxed)  cil managed 
    {
        // Method begins at RVA 0x2230
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  callvirt instance native int class GLib.Boxed::get_Handle()
	IL_0006:  ret 
    } // end of method Boxed::op_Explicit

    // method line 13
    .method public virtual hidebysig newslot specialname 
           instance default object get_Obj ()  cil managed 
    {
        // Method begins at RVA 0x2238
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldfld object GLib.Boxed::obj
	IL_0006:  ret 
    } // end of method Boxed::get_Obj

    // method line 14
    .method public virtual hidebysig newslot specialname 
           instance default void set_Obj (object 'value')  cil managed 
    {
        // Method begins at RVA 0x2240
	// Code size 8 (0x8)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldarg.1 
	IL_0002:  stfld object GLib.Boxed::obj
	IL_0007:  ret 
    } // end of method Boxed::set_Obj

	.property instance native int Handle ()
	{
		.get instance default native int GLib.Boxed::get_Handle () 
		.set instance default void GLib.Boxed::set_Handle (native int 'value') 
	}
	.property instance object Obj ()
	{
		.set instance default void GLib.Boxed::set_Obj (object 'value') 
		.get instance default object GLib.Boxed::get_Obj () 
	}
  } // end of class GLib.Boxed
}

.namespace GLib
{
  .class public auto ansi sealed beforefieldinit CDeclCallbackAttribute
  	extends [mscorlib]System.Attribute
  {
    .custom instance void class [mscorlib]System.ObsoleteAttribute::'.ctor'(string) =  (
		01 00 80 97 59 6F 75 20 6D 75 73 74 20 75 73 65   // ....You must use
		20 74 68 65 20 55 6E 6D 61 6E 61 67 65 64 46 75   //  the UnmanagedFu
		6E 63 74 69 6F 6E 50 6F 69 6E 74 65 72 20 61 74   // nctionPointer at
		74 72 69 62 75 74 65 20 77 69 74 68 20 76 61 6C   // tribute with val
		75 65 20 27 43 61 6C 6C 69 6E 67 43 6F 6E 76 65   // ue 'CallingConve
		6E 74 69 6F 6E 2E 43 64 65 63 6C 27 2E 20 54 68   // ntion.Cdecl'. Th
		69 73 20 61 74 74 72 69 62 75 74 65 20 64 6F 65   // is attribute doe
		73 20 6E 6F 74 20 73 65 74 20 74 68 65 20 64 65   // s not set the de
		6C 65 67 61 74 65 20 74 6F 20 27 63 64 65 63 6C   // legate to 'cdecl
		27 20 63 6F 72 72 65 63 74 6C 79 00 00          ) // ' correctly..


    // method line 15
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' ()  cil managed 
    {
        // Method begins at RVA 0x224c
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void class [mscorlib]System.Attribute::'.ctor'()
	IL_0006:  ret 
    } // end of method CDeclCallbackAttribute::.ctor

  } // end of class GLib.CDeclCallbackAttribute
}

.namespace GLib
{
  .class public auto ansi sealed beforefieldinit ClassInitializerAttribute
  	extends [mscorlib]System.Attribute
  {
    .custom instance void class [mscorlib]System.ObsoleteAttribute::'.ctor'(string) =  (
		01 00 24 52 65 70 6C 61 63 65 64 20 62 79 20 54   // ..$Replaced by T
		79 70 65 49 6E 69 74 69 61 6C 69 7A 65 72 41 74   // ypeInitializerAt
		74 72 69 62 75 74 65 00 00                      ) // tribute..


    // method line 16
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' ()  cil managed 
    {
        // Method begins at RVA 0x2254
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void class [mscorlib]System.Attribute::'.ctor'()
	IL_0006:  ret 
    } // end of method ClassInitializerAttribute::.ctor

  } // end of class GLib.ClassInitializerAttribute
}

.namespace GLib
{
  .class public auto ansi sealed beforefieldinit ConnectBeforeAttribute
  	extends [mscorlib]System.Attribute
  {

    // method line 17
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' ()  cil managed 
    {
        // Method begins at RVA 0x225c
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void class [mscorlib]System.Attribute::'.ctor'()
	IL_0006:  ret 
    } // end of method ConnectBeforeAttribute::.ctor

  } // end of class GLib.ConnectBeforeAttribute
}

.namespace GLib
{
  .class public auto ansi sealed beforefieldinit DefaultSignalHandlerAttribute
  	extends [mscorlib]System.Attribute
  {
    .field  private  string 'method'
    .field  private  class [mscorlib]System.Type 'type'

    // method line 18
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' ()  cil managed 
    {
        // Method begins at RVA 0x2264
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void class [mscorlib]System.Attribute::'.ctor'()
	IL_0006:  ret 
    } // end of method DefaultSignalHandlerAttribute::.ctor

    // method line 19
    .method public hidebysig specialname 
           instance default string get_ConnectionMethod ()  cil managed 
    {
        // Method begins at RVA 0x226c
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldfld string GLib.DefaultSignalHandlerAttribute::'method'
	IL_0006:  ret 
    } // end of method DefaultSignalHandlerAttribute::get_ConnectionMethod

    // method line 20
    .method public hidebysig specialname 
           instance default void set_ConnectionMethod (string 'value')  cil managed 
    {
        // Method begins at RVA 0x2274
	// Code size 8 (0x8)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldarg.1 
	IL_0002:  stfld string GLib.DefaultSignalHandlerAttribute::'method'
	IL_0007:  ret 
    } // end of method DefaultSignalHandlerAttribute::set_ConnectionMethod

    // method line 21
    .method public hidebysig specialname 
           instance default class [mscorlib]System.Type get_Type ()  cil managed 
    {
        // Method begins at RVA 0x2280
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldfld class [mscorlib]System.Type GLib.DefaultSignalHandlerAttribute::'type'
	IL_0006:  ret 
    } // end of method DefaultSignalHandlerAttribute::get_Type

    // method line 22
    .method public hidebysig specialname 
           instance default void set_Type (class [mscorlib]System.Type 'value')  cil managed 
    {
        // Method begins at RVA 0x2288
	// Code size 8 (0x8)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldarg.1 
	IL_0002:  stfld class [mscorlib]System.Type GLib.DefaultSignalHandlerAttribute::'type'
	IL_0007:  ret 
    } // end of method DefaultSignalHandlerAttribute::set_Type

	.property instance string ConnectionMethod ()
	{
		.get instance default string GLib.DefaultSignalHandlerAttribute::get_ConnectionMethod () 
		.set instance default void GLib.DefaultSignalHandlerAttribute::set_ConnectionMethod (string 'value') 
	}
	.property instance class [mscorlib]System.Type Type ()
	{
		.set instance default void GLib.DefaultSignalHandlerAttribute::set_Type (class [mscorlib]System.Type 'value') 
		.get instance default class [mscorlib]System.Type GLib.DefaultSignalHandlerAttribute::get_Type () 
	}
  } // end of class GLib.DefaultSignalHandlerAttribute
}

.namespace GLib
{
  .class public auto ansi beforefieldinit DelegateWrapper
  	extends [mscorlib]System.Object
  {
    .field  private static  class [mscorlib]System.Collections.Hashtable instances
    .field  private static  class [mscorlib]System.Collections.ArrayList static_instances
    .field  private static  int32 notify_count

    // method line 23
    .method family hidebysig specialname rtspecialname 
           instance default void '.ctor' (object o)  cil managed 
    {
        .custom instance void class [mscorlib]System.ObsoleteAttribute::'.ctor'(string) =  (
		01 00 3D 43 61 6C 6C 62 61 63 6B 20 77 72 61 70   // ..=Callback wrap
		70 65 72 73 20 73 68 6F 75 6C 64 20 62 65 20 6D   // pers should be m
		61 6E 75 61 6C 6C 79 20 6D 61 6E 61 67 65 64 20   // anually managed 
		66 6F 72 20 70 65 72 73 69 73 74 65 6E 63 65 2E   // for persistence.
		00 00                                           ) // ..

        // Method begins at RVA 0x2294
	// Code size 89 (0x59)
	.maxstack 2
	.locals init (
		object	V_0,
		bool	V_1)
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::'.ctor'()
	IL_0006:  ldarg.1 
	IL_0007:  brfalse IL_0028

	IL_000c:  ldarg.1 
	IL_000d:  isinst GLib.Object
	IL_0012:  brfalse IL_0023

	IL_0017:  ldarg.0 
	IL_0018:  ldarg.1 
	IL_0019:  castclass GLib.Object
	IL_001e:  call instance void class GLib.DelegateWrapper::AddDestroyNotify(class GLib.Object)
	IL_0023:  br IL_0058

	IL_0028:  ldtoken GLib.DelegateWrapper
	IL_002d:  call class [mscorlib]System.Type class [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
	IL_0032:  stloc.0 
	IL_0033:  ldc.i4.0 
	IL_0034:  stloc.1 
	.try { // 0
	  IL_0035:  ldloc.0 
	  IL_0036:  ldloca.s 1
	  IL_0038:  call void class [mscorlib]System.Threading.Monitor::Enter(object, [out] bool&)
	  IL_003d:  ldsfld class [mscorlib]System.Collections.ArrayList GLib.DelegateWrapper::static_instances
	  IL_0042:  ldarg.0 
	  IL_0043:  callvirt instance int32 class [mscorlib]System.Collections.ArrayList::Add(object)
	  IL_0048:  pop 
	  IL_0049:  leave IL_0058

	} // end .try 0
	finally  { // 0
	  IL_004e:  ldloc.1 
	  IL_004f:  brfalse.s IL_0057

	  IL_0051:  ldloc.0 
	  IL_0052:  call void class [mscorlib]System.Threading.Monitor::Exit(object)
	  IL_0057:  endfinally 
	} // end handler 0
	IL_0058:  ret 
    } // end of method DelegateWrapper::.ctor

    // method line 24
    .method private hidebysig 
           instance default void AddDestroyNotify (class GLib.Object o)  cil managed 
    {
        // Method begins at RVA 0x230c
	// Code size 124 (0x7c)
	.maxstack 4
	.locals init (
		native int	V_0,
		class GLib.DelegateWrapper/DestroyNotify	V_1,
		object	V_2,
		bool	V_3)
	IL_0000:  ldstr "_GtkSharpDelegateWrapper_{0}"
	IL_0005:  ldsfld int32 GLib.DelegateWrapper::notify_count
	IL_000a:  box int32
	IL_000f:  call string string::Format(string, object)
	IL_0014:  call native int class GLib.Marshaller::StringToPtrGStrdup(string)
	IL_0019:  stloc.0 
	IL_001a:  ldarg.0 
	IL_001b:  ldftn instance void class GLib.DelegateWrapper::OnDestroy(native int)
	IL_0021:  newobj instance void class GLib.DelegateWrapper/DestroyNotify::'.ctor'(object, native int)
	IL_0026:  stloc.1 
	IL_0027:  ldarg.1 
	IL_0028:  callvirt instance native int class GLib.Object::get_Handle()
	IL_002d:  ldloc.0 
	IL_002e:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_0033:  ldloc.1 
	IL_0034:  call void class GLib.DelegateWrapper::g_object_set_data_full(native int, native int, native int, class GLib.DelegateWrapper/DestroyNotify)
	IL_0039:  ldloc.0 
	IL_003a:  call void class GLib.Marshaller::Free(native int)
	IL_003f:  ldtoken GLib.DelegateWrapper
	IL_0044:  call class [mscorlib]System.Type class [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
	IL_0049:  stloc.2 
	IL_004a:  ldc.i4.0 
	IL_004b:  stloc.3 
	.try { // 0
	  IL_004c:  ldloc.2 
	  IL_004d:  ldloca.s 3
	  IL_004f:  call void class [mscorlib]System.Threading.Monitor::Enter(object, [out] bool&)
	  IL_0054:  ldsfld class [mscorlib]System.Collections.Hashtable GLib.DelegateWrapper::instances
	  IL_0059:  ldarg.0 
	  IL_005a:  ldloc.1 
	  IL_005b:  callvirt instance void class [mscorlib]System.Collections.Hashtable::set_Item(object, object)
	  IL_0060:  ldsfld int32 GLib.DelegateWrapper::notify_count
	  IL_0065:  ldc.i4.1 
	  IL_0066:  add 
	  IL_0067:  stsfld int32 GLib.DelegateWrapper::notify_count
	  IL_006c:  leave IL_007b

	} // end .try 0
	finally  { // 0
	  IL_0071:  ldloc.3 
	  IL_0072:  brfalse.s IL_007a

	  IL_0074:  ldloc.2 
	  IL_0075:  call void class [mscorlib]System.Threading.Monitor::Exit(object)
	  IL_007a:  endfinally 
	} // end handler 0
	IL_007b:  ret 
    } // end of method DelegateWrapper::AddDestroyNotify

    // method line 25
    .method private static hidebysig pinvokeimpl ("libgobject-2.0-0.dll" as "g_object_set_data_full" cdecl )
           default void g_object_set_data_full (native int obj, native int name, native int data, class GLib.DelegateWrapper/DestroyNotify destroy)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method DelegateWrapper::g_object_set_data_full

    // method line 26
    .method private hidebysig 
           instance default void OnDestroy (native int data)  cil managed 
    {
        // Method begins at RVA 0x23a4
	// Code size 82 (0x52)
	.maxstack 2
	.locals init (
		object	V_0,
		bool	V_1,
		class [mscorlib]System.Exception	V_2)
	.try { // 1
	  IL_0000:  ldtoken GLib.DelegateWrapper
	  IL_0005:  call class [mscorlib]System.Type class [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
	  IL_000a:  stloc.0 
	  IL_000b:  ldc.i4.0 
	  IL_000c:  stloc.1 
	  .try { // 0
	    IL_000d:  ldloc.0 
	    IL_000e:  ldloca.s 1
	    IL_0010:  call void class [mscorlib]System.Threading.Monitor::Enter(object, [out] bool&)
	    IL_0015:  ldsfld class [mscorlib]System.Collections.Hashtable GLib.DelegateWrapper::instances
	    IL_001a:  ldarg.0 
	    IL_001b:  callvirt instance bool class [mscorlib]System.Collections.Hashtable::ContainsKey(object)
	    IL_0020:  brfalse IL_0030

	    IL_0025:  ldsfld class [mscorlib]System.Collections.Hashtable GLib.DelegateWrapper::instances
	    IL_002a:  ldarg.0 
	    IL_002b:  callvirt instance void class [mscorlib]System.Collections.Hashtable::Remove(object)
	    IL_0030:  leave IL_003f

	  } // end .try 0
	  finally  { // 0
	    IL_0035:  ldloc.1 
	    IL_0036:  brfalse.s IL_003e

	    IL_0038:  ldloc.0 
	    IL_0039:  call void class [mscorlib]System.Threading.Monitor::Exit(object)
	    IL_003e:  endfinally 
	  } // end handler 0
	  IL_003f:  leave IL_0051

	} // end .try 1
	catch class [mscorlib]System.Exception { // 1
	  IL_0044:  stloc.2 
	  IL_0045:  ldloc.2 
	  IL_0046:  ldc.i4.0 
	  IL_0047:  call void class GLib.ExceptionManager::RaiseUnhandledException(class [mscorlib]System.Exception, bool)
	  IL_004c:  leave IL_0051

	} // end handler 1
	IL_0051:  ret 
    } // end of method DelegateWrapper::OnDestroy

    // method line 27
    .method private static hidebysig specialname rtspecialname 
           default void '.cctor' ()  cil managed 
    {
        // Method begins at RVA 0x2420
	// Code size 27 (0x1b)
	.maxstack 8
	IL_0000:  newobj instance void class [mscorlib]System.Collections.Hashtable::'.ctor'()
	IL_0005:  stsfld class [mscorlib]System.Collections.Hashtable GLib.DelegateWrapper::instances
	IL_000a:  newobj instance void class [mscorlib]System.Collections.ArrayList::'.ctor'()
	IL_000f:  stsfld class [mscorlib]System.Collections.ArrayList GLib.DelegateWrapper::static_instances
	IL_0014:  ldc.i4.0 
	IL_0015:  stsfld int32 GLib.DelegateWrapper::notify_count
	IL_001a:  ret 
    } // end of method DelegateWrapper::.cctor

  .class nested private auto ansi sealed DestroyNotify
  	extends [mscorlib]System.MulticastDelegate
  {
    .custom instance void class [mscorlib]System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute::'.ctor'(valuetype [mscorlib]System.Runtime.InteropServices.CallingConvention) =  (01 00 02 00 00 00 00 00 ) // ........


    // method line 28
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (object 'object', native int 'method')  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method DestroyNotify::.ctor

    // method line 29
    .method public virtual hidebysig newslot 
           instance default void Invoke (native int data)  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method DestroyNotify::Invoke

    // method line 30
    .method public virtual hidebysig newslot 
           instance default class [mscorlib]System.IAsyncResult BeginInvoke (native int data, class [mscorlib]System.AsyncCallback callback, object 'object')  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method DestroyNotify::BeginInvoke

    // method line 31
    .method public virtual hidebysig newslot 
           instance default void EndInvoke (class [mscorlib]System.IAsyncResult result)  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method DestroyNotify::EndInvoke

  } // end of class DestroyNotify

  } // end of class GLib.DelegateWrapper
}

.namespace GLib
{
  .class public auto ansi sealed DestroyNotify
  	extends [mscorlib]System.MulticastDelegate
  {
    .custom instance void class [mscorlib]System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute::'.ctor'(valuetype [mscorlib]System.Runtime.InteropServices.CallingConvention) =  (01 00 02 00 00 00 00 00 ) // ........


    // method line 32
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (object 'object', native int 'method')  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method DestroyNotify::.ctor

    // method line 33
    .method public virtual hidebysig newslot 
           instance default void Invoke (native int data)  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method DestroyNotify::Invoke

    // method line 34
    .method public virtual hidebysig newslot 
           instance default class [mscorlib]System.IAsyncResult BeginInvoke (native int data, class [mscorlib]System.AsyncCallback callback, object 'object')  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method DestroyNotify::BeginInvoke

    // method line 35
    .method public virtual hidebysig newslot 
           instance default void EndInvoke (class [mscorlib]System.IAsyncResult result)  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method DestroyNotify::EndInvoke

  } // end of class GLib.DestroyNotify
}

.namespace GLib
{
  .class public auto ansi beforefieldinit DestroyHelper
  	extends [mscorlib]System.Object
  {
    .field  private static  class GLib.DestroyNotify release_gchandle

    // method line 36
    .method private hidebysig specialname rtspecialname 
           instance default void '.ctor' ()  cil managed 
    {
        // Method begins at RVA 0x243c
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::'.ctor'()
	IL_0006:  ret 
    } // end of method DestroyHelper::.ctor

    // method line 37
    .method private static hidebysig 
           default void ReleaseGCHandle (native int data)  cil managed 
    {
        // Method begins at RVA 0x2444
	// Code size 32 (0x20)
	.maxstack 2
	.locals init (
		valuetype [mscorlib]System.Runtime.InteropServices.GCHandle	V_0)
	IL_0000:  ldarg.0 
	IL_0001:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_0006:  call bool native int::op_Equality(native int, native int)
	IL_000b:  brfalse IL_0011

	IL_0010:  ret 
	IL_0011:  ldarg.0 
	IL_0012:  call valuetype [mscorlib]System.Runtime.InteropServices.GCHandle valuetype [mscorlib]System.Runtime.InteropServices.GCHandle::op_Explicit(native int)
	IL_0017:  stloc.0 
	IL_0018:  ldloca.s 0
	IL_001a:  call instance void valuetype [mscorlib]System.Runtime.InteropServices.GCHandle::Free()
	IL_001f:  ret 
    } // end of method DestroyHelper::ReleaseGCHandle

    // method line 38
    .method public static hidebysig specialname 
           default class GLib.DestroyNotify get_NotifyHandler ()  cil managed 
    {
        // Method begins at RVA 0x2470
	// Code size 33 (0x21)
	.maxstack 8
	IL_0000:  ldsfld class GLib.DestroyNotify GLib.DestroyHelper::release_gchandle
	IL_0005:  brtrue IL_001b

	IL_000a:  ldnull 
	IL_000b:  ldftn void class GLib.DestroyHelper::ReleaseGCHandle(native int)
	IL_0011:  newobj instance void class GLib.DestroyNotify::'.ctor'(object, native int)
	IL_0016:  stsfld class GLib.DestroyNotify GLib.DestroyHelper::release_gchandle
	IL_001b:  ldsfld class GLib.DestroyNotify GLib.DestroyHelper::release_gchandle
	IL_0020:  ret 
    } // end of method DestroyHelper::get_NotifyHandler

	.property class GLib.DestroyNotify NotifyHandler ()
	{
		.get default class GLib.DestroyNotify GLib.DestroyHelper::get_NotifyHandler () 
	}
  } // end of class GLib.DestroyHelper
}

.namespace GLib
{
  .class public auto ansi beforefieldinit EnumWrapper
  	extends [mscorlib]System.Object
  {
    .custom instance void class [mscorlib]System.ObsoleteAttribute::'.ctor'(string) =  (
		01 00 35 52 65 70 6C 61 63 65 64 20 62 79 20 64   // ..5Replaced by d
		69 72 65 63 74 20 65 6E 75 6D 20 74 79 70 65 20   // irect enum type 
		63 61 73 74 73 20 74 6F 2F 66 72 6F 6D 20 47 4C   // casts to/from GL
		69 62 2E 56 61 6C 75 65 00 00                   ) // ib.Value..

    .field  private  int32 val
    .field  public  bool 'flags'

    // method line 39
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (int32 val, bool 'flags')  cil managed 
    {
        // Method begins at RVA 0x2494
	// Code size 21 (0x15)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::'.ctor'()
	IL_0006:  ldarg.0 
	IL_0007:  ldarg.1 
	IL_0008:  stfld int32 GLib.EnumWrapper::val
	IL_000d:  ldarg.0 
	IL_000e:  ldarg.2 
	IL_000f:  stfld bool GLib.EnumWrapper::'flags'
	IL_0014:  ret 
    } // end of method EnumWrapper::.ctor

    // method line 40
    .method public static hidebysig specialname 
           default int32 op_Explicit (class GLib.EnumWrapper wrap)  cil managed 
    {
        // Method begins at RVA 0x24ac
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldfld int32 GLib.EnumWrapper::val
	IL_0006:  ret 
    } // end of method EnumWrapper::op_Explicit

  } // end of class GLib.EnumWrapper
}

.namespace GLib
{
  .class public auto ansi sealed UnhandledExceptionHandler
  	extends [mscorlib]System.MulticastDelegate
  {

    // method line 41
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (object 'object', native int 'method')  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method UnhandledExceptionHandler::.ctor

    // method line 42
    .method public virtual hidebysig newslot 
           instance default void Invoke (class GLib.UnhandledExceptionArgs args)  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method UnhandledExceptionHandler::Invoke

    // method line 43
    .method public virtual hidebysig newslot 
           instance default class [mscorlib]System.IAsyncResult BeginInvoke (class GLib.UnhandledExceptionArgs args, class [mscorlib]System.AsyncCallback callback, object 'object')  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method UnhandledExceptionHandler::BeginInvoke

    // method line 44
    .method public virtual hidebysig newslot 
           instance default void EndInvoke (class [mscorlib]System.IAsyncResult result)  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method UnhandledExceptionHandler::EndInvoke

  } // end of class GLib.UnhandledExceptionHandler
}

.namespace GLib
{
  .class public auto ansi beforefieldinit UnhandledExceptionArgs
  	extends [mscorlib]System.UnhandledExceptionEventArgs
  {
    .field  private  bool exit_app

    // method line 45
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (class [mscorlib]System.Exception e, bool is_terminal)  cil managed 
    {
        // Method begins at RVA 0x24b4
	// Code size 16 (0x10)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldc.i4.0 
	IL_0002:  stfld bool GLib.UnhandledExceptionArgs::exit_app
	IL_0007:  ldarg.0 
	IL_0008:  ldarg.1 
	IL_0009:  ldarg.2 
	IL_000a:  call instance void class [mscorlib]System.UnhandledExceptionEventArgs::'.ctor'(object, bool)
	IL_000f:  ret 
    } // end of method UnhandledExceptionArgs::.ctor

    // method line 46
    .method public hidebysig specialname 
           instance default bool get_ExitApplication ()  cil managed 
    {
        // Method begins at RVA 0x24c8
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldfld bool GLib.UnhandledExceptionArgs::exit_app
	IL_0006:  ret 
    } // end of method UnhandledExceptionArgs::get_ExitApplication

    // method line 47
    .method public hidebysig specialname 
           instance default void set_ExitApplication (bool 'value')  cil managed 
    {
        // Method begins at RVA 0x24d0
	// Code size 14 (0xe)
	.maxstack 8
	IL_0000:  ldarg.1 
	IL_0001:  brfalse IL_000d

	IL_0006:  ldarg.0 
	IL_0007:  ldarg.1 
	IL_0008:  stfld bool GLib.UnhandledExceptionArgs::exit_app
	IL_000d:  ret 
    } // end of method UnhandledExceptionArgs::set_ExitApplication

	.property instance bool ExitApplication ()
	{
		.get instance default bool GLib.UnhandledExceptionArgs::get_ExitApplication () 
		.set instance default void GLib.UnhandledExceptionArgs::set_ExitApplication (bool 'value') 
	}
  } // end of class GLib.UnhandledExceptionArgs
}

.namespace GLib
{
  .class public auto ansi beforefieldinit ExceptionManager
  	extends [mscorlib]System.Object
  {
    .field  private static  class GLib.UnhandledExceptionHandler UnhandledException
    .custom instance void class [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::'.ctor'() =  (01 00 00 00 ) // ....

    .custom instance void class [mscorlib]System.Diagnostics.DebuggerBrowsableAttribute::'.ctor'(valuetype [mscorlib]System.Diagnostics.DebuggerBrowsableState) =  (01 00 00 00 00 00 00 00 ) // ........


    // method line 48
    .method public static hidebysig 
           default void RaiseUnhandledException (class [mscorlib]System.Exception e, bool is_terminal)  cil managed 
    {
        // Method begins at RVA 0x24e0
	// Code size 144 (0x90)
	.maxstack 2
	.locals init (
		class GLib.UnhandledExceptionArgs	V_0,
		class [mscorlib]System.Exception	V_1)
	IL_0000:  ldsfld class GLib.UnhandledExceptionHandler GLib.ExceptionManager::UnhandledException
	IL_0005:  brtrue IL_0049

	IL_000a:  call class [mscorlib]System.IO.TextWriter class [mscorlib]System.Console::get_Error()
	IL_000f:  ldstr "Exception in Gtk# callback delegate"
	IL_0014:  callvirt instance void class [mscorlib]System.IO.TextWriter::WriteLine(string)
	IL_0019:  call class [mscorlib]System.IO.TextWriter class [mscorlib]System.Console::get_Error()
	IL_001e:  ldstr "  Note: Applications can use GLib.ExceptionManager.UnhandledException to handle the exception."
	IL_0023:  callvirt instance void class [mscorlib]System.IO.TextWriter::WriteLine(string)
	IL_0028:  call class [mscorlib]System.IO.TextWriter class [mscorlib]System.Console::get_Error()
	IL_002d:  ldarg.0 
	IL_002e:  callvirt instance void class [mscorlib]System.IO.TextWriter::WriteLine(object)
	IL_0033:  call class [mscorlib]System.IO.TextWriter class [mscorlib]System.Console::get_Error()
	IL_0038:  ldc.i4.1 
	IL_0039:  newobj instance void class [mscorlib]System.Diagnostics.StackTrace::'.ctor'(bool)
	IL_003e:  callvirt instance void class [mscorlib]System.IO.TextWriter::WriteLine(object)
	IL_0043:  ldc.i4.1 
	IL_0044:  call void class [mscorlib]System.Environment::Exit(int32)
	IL_0049:  ldarg.0 
	IL_004a:  ldarg.1 
	IL_004b:  newobj instance void class GLib.UnhandledExceptionArgs::'.ctor'(class [mscorlib]System.Exception, bool)
	IL_0050:  stloc.0 
	.try { // 0
	  IL_0051:  ldsfld class GLib.UnhandledExceptionHandler GLib.ExceptionManager::UnhandledException
	  IL_0056:  ldloc.0 
	  IL_0057:  callvirt instance void class GLib.UnhandledExceptionHandler::Invoke(class GLib.UnhandledExceptionArgs)
	  IL_005c:  leave IL_0078

	} // end .try 0
	catch class [mscorlib]System.Exception { // 0
	  IL_0061:  stloc.1 
	  IL_0062:  call class [mscorlib]System.IO.TextWriter class [mscorlib]System.Console::get_Error()
	  IL_0067:  ldloc.1 
	  IL_0068:  callvirt instance void class [mscorlib]System.IO.TextWriter::WriteLine(object)
	  IL_006d:  ldc.i4.1 
	  IL_006e:  call void class [mscorlib]System.Environment::Exit(int32)
	  IL_0073:  leave IL_0078

	} // end handler 0
	IL_0078:  ldarg.1 
	IL_0079:  brtrue IL_0089

	IL_007e:  ldloc.0 
	IL_007f:  callvirt instance bool class GLib.UnhandledExceptionArgs::get_ExitApplication()
	IL_0084:  brfalse IL_008f

	IL_0089:  ldc.i4.1 
	IL_008a:  call void class [mscorlib]System.Environment::Exit(int32)
	IL_008f:  ret 
    } // end of method ExceptionManager::RaiseUnhandledException

    // method line 49
    .method private hidebysig specialname rtspecialname 
           instance default void '.ctor' ()  cil managed 
    {
        // Method begins at RVA 0x258c
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::'.ctor'()
	IL_0006:  ret 
    } // end of method ExceptionManager::.ctor

    // method line 50
    .method public static hidebysig specialname 
           default void add_UnhandledException (class GLib.UnhandledExceptionHandler 'value')  cil managed 
    {
        // Method begins at RVA 0x2594
	// Code size 40 (0x28)
	.maxstack 3
	.locals init (
		class GLib.UnhandledExceptionHandler	V_0,
		class GLib.UnhandledExceptionHandler	V_1)
	IL_0000:  ldsfld class GLib.UnhandledExceptionHandler GLib.ExceptionManager::UnhandledException
	IL_0005:  stloc.0 
	IL_0006:  ldloc.0 
	IL_0007:  stloc.1 
	IL_0008:  ldsflda class GLib.UnhandledExceptionHandler GLib.ExceptionManager::UnhandledException
	IL_000d:  ldloc.1 
	IL_000e:  ldarg.0 
	IL_000f:  call class [mscorlib]System.Delegate class [mscorlib]System.Delegate::Combine(class [mscorlib]System.Delegate, class [mscorlib]System.Delegate)
	IL_0014:  castclass GLib.UnhandledExceptionHandler
	IL_0019:  ldloc.0 
	IL_001a:  call !!0 class [mscorlib]System.Threading.Interlocked::CompareExchange<class GLib.UnhandledExceptionHandler> ([out] !!0&, !!0, !!0)
	IL_001f:  stloc.0 
	IL_0020:  ldloc.0 
	IL_0021:  ldloc.1 
	IL_0022:  bne.un IL_0006

	IL_0027:  ret 
    } // end of method ExceptionManager::add_UnhandledException

    // method line 51
    .method public static hidebysig specialname 
           default void remove_UnhandledException (class GLib.UnhandledExceptionHandler 'value')  cil managed 
    {
        // Method begins at RVA 0x25c8
	// Code size 40 (0x28)
	.maxstack 3
	.locals init (
		class GLib.UnhandledExceptionHandler	V_0,
		class GLib.UnhandledExceptionHandler	V_1)
	IL_0000:  ldsfld class GLib.UnhandledExceptionHandler GLib.ExceptionManager::UnhandledException
	IL_0005:  stloc.0 
	IL_0006:  ldloc.0 
	IL_0007:  stloc.1 
	IL_0008:  ldsflda class GLib.UnhandledExceptionHandler GLib.ExceptionManager::UnhandledException
	IL_000d:  ldloc.1 
	IL_000e:  ldarg.0 
	IL_000f:  call class [mscorlib]System.Delegate class [mscorlib]System.Delegate::Remove(class [mscorlib]System.Delegate, class [mscorlib]System.Delegate)
	IL_0014:  castclass GLib.UnhandledExceptionHandler
	IL_0019:  ldloc.0 
	IL_001a:  call !!0 class [mscorlib]System.Threading.Interlocked::CompareExchange<class GLib.UnhandledExceptionHandler> ([out] !!0&, !!0, !!0)
	IL_001f:  stloc.0 
	IL_0020:  ldloc.0 
	IL_0021:  ldloc.1 
	IL_0022:  bne.un IL_0006

	IL_0027:  ret 
    } // end of method ExceptionManager::remove_UnhandledException

	.event GLib.UnhandledExceptionHandler UnhandledException
	{
		.removeon default void GLib.ExceptionManager::remove_UnhandledException (class GLib.UnhandledExceptionHandler 'value') 
		.addon default void GLib.ExceptionManager::add_UnhandledException (class GLib.UnhandledExceptionHandler 'value') 
	}
  } // end of class GLib.ExceptionManager
}

.namespace GLib
{
  .class private auto ansi abstract sealed beforefieldinit FastActivator
  	extends [mscorlib]System.Object
  {
    .field private static literal  valuetype [mscorlib]System.Reflection.BindingFlags 'flags' = int32(0x00000234)
    .field  private static initonly  class [mscorlib]System.Threading.ThreadLocal`1<class [mscorlib]System.Collections.Generic.Dictionary`2<class [mscorlib]System.Type,class GLib.FastActivator/FastCreateObjectPtr>> cacheOpaque
    .field  private static initonly  class [mscorlib]System.Threading.ThreadLocal`1<class [mscorlib]System.Collections.Generic.Dictionary`2<class [mscorlib]System.Type,class GLib.FastActivator/FastCreateObjectPtr>> cacheObject
    .field  private static initonly  class [mscorlib]System.Threading.ThreadLocal`1<class [mscorlib]System.Collections.Generic.Dictionary`2<class [mscorlib]System.Type,class GLib.FastActivator/FastCreateObject>> cacheSignalArgs
    .field  private static initonly  class [mscorlib]System.Threading.ThreadLocal`1<class [mscorlib]System.Collections.Generic.Dictionary`2<class [mscorlib]System.Type,class GLib.FastActivator/FastCreateBoxed>> cacheBoxed

    // method line 52
    .method private static hidebysig 
           default class GLib.FastActivator/FastCreateObjectPtr FastCtorPtr (class [mscorlib]System.Type t, class [mscorlib]System.Collections.Generic.Dictionary`2<class [mscorlib]System.Type, class GLib.FastActivator/FastCreateObjectPtr> cache)  cil managed 
    {
        // Method begins at RVA 0x25fc
	// Code size 128 (0x80)
	.maxstack 7
	.locals init (
		class GLib.FastActivator/FastCreateObjectPtr	V_0,
		class [System.Core]System.Linq.Expressions.ParameterExpression	V_1,
		class [mscorlib]System.Reflection.ConstructorInfo	V_2,
		class [System.Core]System.Linq.Expressions.NewExpression	V_3)
	IL_0000:  ldarg.1 
	IL_0001:  ldarg.0 
	IL_0002:  ldloca.s 0
	IL_0004:  callvirt instance bool class [mscorlib]System.Collections.Generic.Dictionary`2<class [mscorlib]System.Type, class GLib.FastActivator/FastCreateObjectPtr>::TryGetValue(!0, [out] !1&)
	IL_0009:  brtrue IL_007e

	IL_000e:  ldtoken native int
	IL_0013:  call class [mscorlib]System.Type class [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
	IL_0018:  call class [System.Core]System.Linq.Expressions.ParameterExpression class [System.Core]System.Linq.Expressions.Expression::Parameter(class [mscorlib]System.Type)
	IL_001d:  stloc.1 
	IL_001e:  ldarg.0 
	IL_001f:  ldc.i4 564
	IL_0024:  ldc.i4.1 
	IL_0025:  newarr [mscorlib]System.Type
	IL_002a:  dup 
	IL_002b:  ldc.i4.0 
	IL_002c:  ldtoken native int
	IL_0031:  call class [mscorlib]System.Type class [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
	IL_0036:  stelem.ref 
	IL_0037:  callvirt instance class [mscorlib]System.Reflection.ConstructorInfo [mscorlib]System.Type::GetConstructor(valuetype [mscorlib]System.Reflection.BindingFlags, class [mscorlib]System.Type[])
	IL_003c:  stloc.2 
	IL_003d:  ldloc.2 
	IL_003e:  ldnull 
	IL_003f:  call bool class [mscorlib]System.Reflection.ConstructorInfo::op_Equality(class [mscorlib]System.Reflection.ConstructorInfo, class [mscorlib]System.Reflection.ConstructorInfo)
	IL_0044:  brfalse IL_004f

	IL_0049:  newobj instance void class [mscorlib]System.MissingMethodException::'.ctor'()
	IL_004e:  throw 
	IL_004f:  ldloc.2 
	IL_0050:  ldc.i4.1 
	IL_0051:  newarr [System.Core]System.Linq.Expressions.Expression
	IL_0056:  dup 
	IL_0057:  ldc.i4.0 
	IL_0058:  ldloc.1 
	IL_0059:  stelem.ref 
	IL_005a:  call class [System.Core]System.Linq.Expressions.NewExpression class [System.Core]System.Linq.Expressions.Expression::New(class [mscorlib]System.Reflection.ConstructorInfo, class [System.Core]System.Linq.Expressions.Expression[])
	IL_005f:  stloc.3 
	IL_0060:  ldarg.1 
	IL_0061:  ldarg.0 
	IL_0062:  ldloc.3 
	IL_0063:  ldc.i4.1 
	IL_0064:  newarr [System.Core]System.Linq.Expressions.ParameterExpression
	IL_0069:  dup 
	IL_006a:  ldc.i4.0 
	IL_006b:  ldloc.1 
	IL_006c:  stelem.ref 
	IL_006d:  call class [System.Core]System.Linq.Expressions.Expression`1<!!0> class [System.Core]System.Linq.Expressions.Expression::Lambda<class GLib.FastActivator/FastCreateObjectPtr> (class [System.Core]System.Linq.Expressions.Expression, class [System.Core]System.Linq.Expressions.ParameterExpression[])
	IL_0072:  callvirt instance !0 class [System.Core]System.Linq.Expressions.Expression`1<class GLib.FastActivator/FastCreateObjectPtr>::Compile()
	IL_0077:  dup 
	IL_0078:  stloc.0 
	IL_0079:  callvirt instance void class [mscorlib]System.Collections.Generic.Dictionary`2<class [mscorlib]System.Type, class GLib.FastActivator/FastCreateObjectPtr>::set_Item(!0, !1)
	IL_007e:  ldloc.0 
	IL_007f:  ret 
    } // end of method FastActivator::FastCtorPtr

    // method line 53
    .method private static hidebysig 
           default class GLib.FastActivator/FastCreateObject FastCtor (class [mscorlib]System.Type t, class [mscorlib]System.Collections.Generic.Dictionary`2<class [mscorlib]System.Type, class GLib.FastActivator/FastCreateObject> cache)  cil managed 
    {
        // Method begins at RVA 0x2688
	// Code size 48 (0x30)
	.maxstack 4
	.locals init (
		class GLib.FastActivator/FastCreateObject	V_0,
		class [System.Core]System.Linq.Expressions.NewExpression	V_1)
	IL_0000:  ldarg.1 
	IL_0001:  ldarg.0 
	IL_0002:  ldloca.s 0
	IL_0004:  callvirt instance bool class [mscorlib]System.Collections.Generic.Dictionary`2<class [mscorlib]System.Type, class GLib.FastActivator/FastCreateObject>::TryGetValue(!0, [out] !1&)
	IL_0009:  brtrue IL_002e

	IL_000e:  ldarg.0 
	IL_000f:  call class [System.Core]System.Linq.Expressions.NewExpression class [System.Core]System.Linq.Expressions.Expression::New(class [mscorlib]System.Type)
	IL_0014:  stloc.1 
	IL_0015:  ldarg.1 
	IL_0016:  ldarg.0 
	IL_0017:  ldloc.1 
	IL_0018:  call !!0[] class [mscorlib]System.Array::Empty<class [System.Core]System.Linq.Expressions.ParameterExpression> ()
	IL_001d:  call class [System.Core]System.Linq.Expressions.Expression`1<!!0> class [System.Core]System.Linq.Expressions.Expression::Lambda<class GLib.FastActivator/FastCreateObject> (class [System.Core]System.Linq.Expressions.Expression, class [System.Core]System.Linq.Expressions.ParameterExpression[])
	IL_0022:  callvirt instance !0 class [System.Core]System.Linq.Expressions.Expression`1<class GLib.FastActivator/FastCreateObject>::Compile()
	IL_0027:  dup 
	IL_0028:  stloc.0 
	IL_0029:  callvirt instance void class [mscorlib]System.Collections.Generic.Dictionary`2<class [mscorlib]System.Type, class GLib.FastActivator/FastCreateObject>::set_Item(!0, !1)
	IL_002e:  ldloc.0 
	IL_002f:  ret 
    } // end of method FastActivator::FastCtor

    // method line 54
    .method private static hidebysig 
           default class GLib.FastActivator/FastCreateBoxed FastBoxed (class [mscorlib]System.Type t, class [mscorlib]System.Collections.Generic.Dictionary`2<class [mscorlib]System.Type, class GLib.FastActivator/FastCreateBoxed> cache)  cil managed 
    {
        // Method begins at RVA 0x26c4
	// Code size 176 (0xb0)
	.maxstack 7
	.locals init (
		class GLib.FastActivator/'<FastBoxed>c__AnonStorey0'	V_0,
		class GLib.FastActivator/FastCreateBoxed	V_1,
		class [mscorlib]System.Reflection.MethodInfo	V_2,
		class [System.Core]System.Linq.Expressions.ParameterExpression	V_3,
		class [System.Core]System.Linq.Expressions.MethodCallExpression	V_4,
		class [System.Core]System.Linq.Expressions.UnaryExpression	V_5)
	IL_0000:  newobj instance void class GLib.FastActivator/'<FastBoxed>c__AnonStorey0'::'.ctor'()
	IL_0005:  stloc.0 
	IL_0006:  ldloc.0 
	IL_0007:  ldarg.0 
	IL_0008:  stfld class [mscorlib]System.Type GLib.FastActivator/'<FastBoxed>c__AnonStorey0'::t
	IL_000d:  ldarg.1 
	IL_000e:  ldloc.0 
	IL_000f:  ldfld class [mscorlib]System.Type GLib.FastActivator/'<FastBoxed>c__AnonStorey0'::t
	IL_0014:  ldloca.s 1
	IL_0016:  callvirt instance bool class [mscorlib]System.Collections.Generic.Dictionary`2<class [mscorlib]System.Type, class GLib.FastActivator/FastCreateBoxed>::TryGetValue(!0, [out] !1&)
	IL_001b:  brtrue IL_00ae

	IL_0020:  ldloc.0 
	IL_0021:  ldfld class [mscorlib]System.Type GLib.FastActivator/'<FastBoxed>c__AnonStorey0'::t
	IL_0026:  ldstr "New"
	IL_002b:  ldc.i4.s 0x58
	IL_002d:  callvirt instance class [mscorlib]System.Reflection.MethodInfo class [mscorlib]System.Type::GetMethod(string, valuetype [mscorlib]System.Reflection.BindingFlags)
	IL_0032:  stloc.2 
	IL_0033:  ldloc.2 
	IL_0034:  ldnull 
	IL_0035:  call bool class [mscorlib]System.Reflection.MethodInfo::op_Inequality(class [mscorlib]System.Reflection.MethodInfo, class [mscorlib]System.Reflection.MethodInfo)
	IL_003a:  brfalse IL_0094

	IL_003f:  ldtoken native int
	IL_0044:  call class [mscorlib]System.Type class [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
	IL_0049:  call class [System.Core]System.Linq.Expressions.ParameterExpression class [System.Core]System.Linq.Expressions.Expression::Parameter(class [mscorlib]System.Type)
	IL_004e:  stloc.3 
	IL_004f:  ldloc.2 
	IL_0050:  ldloc.3 
	IL_0051:  call class [System.Core]System.Linq.Expressions.MethodCallExpression class [System.Core]System.Linq.Expressions.Expression::Call(class [mscorlib]System.Reflection.MethodInfo, class [System.Core]System.Linq.Expressions.Expression)
	IL_0056:  stloc.s 4
	IL_0058:  ldloc.s 4
	IL_005a:  ldtoken object
	IL_005f:  call class [mscorlib]System.Type class [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
	IL_0064:  call class [System.Core]System.Linq.Expressions.UnaryExpression class [System.Core]System.Linq.Expressions.Expression::Convert(class [System.Core]System.Linq.Expressions.Expression, class [mscorlib]System.Type)
	IL_0069:  stloc.s 5
	IL_006b:  ldarg.1 
	IL_006c:  ldloc.0 
	IL_006d:  ldfld class [mscorlib]System.Type GLib.FastActivator/'<FastBoxed>c__AnonStorey0'::t
	IL_0072:  ldloc.s 5
	IL_0074:  ldc.i4.1 
	IL_0075:  newarr [System.Core]System.Linq.Expressions.ParameterExpression
	IL_007a:  dup 
	IL_007b:  ldc.i4.0 
	IL_007c:  ldloc.3 
	IL_007d:  stelem.ref 
	IL_007e:  call class [System.Core]System.Linq.Expressions.Expression`1<!!0> class [System.Core]System.Linq.Expressions.Expression::Lambda<class GLib.FastActivator/FastCreateBoxed> (class [System.Core]System.Linq.Expressions.Expression, class [System.Core]System.Linq.Expressions.ParameterExpression[])
	IL_0083:  callvirt instance !0 class [System.Core]System.Linq.Expressions.Expression`1<class GLib.FastActivator/FastCreateBoxed>::Compile()
	IL_0088:  dup 
	IL_0089:  stloc.1 
	IL_008a:  callvirt instance void class [mscorlib]System.Collections.Generic.Dictionary`2<class [mscorlib]System.Type, class GLib.FastActivator/FastCreateBoxed>::set_Item(!0, !1)
	IL_008f:  br IL_00ae

	IL_0094:  ldarg.1 
	IL_0095:  ldloc.0 
	IL_0096:  ldfld class [mscorlib]System.Type GLib.FastActivator/'<FastBoxed>c__AnonStorey0'::t
	IL_009b:  ldloc.0 
	IL_009c:  ldftn instance object class GLib.FastActivator/'<FastBoxed>c__AnonStorey0'::'<>m__0'(native int)
	IL_00a2:  newobj instance void class GLib.FastActivator/FastCreateBoxed::'.ctor'(object, native int)
	IL_00a7:  dup 
	IL_00a8:  stloc.1 
	IL_00a9:  callvirt instance void class [mscorlib]System.Collections.Generic.Dictionary`2<class [mscorlib]System.Type, class GLib.FastActivator/FastCreateBoxed>::set_Item(!0, !1)
	IL_00ae:  ldloc.1 
	IL_00af:  ret 
    } // end of method FastActivator::FastBoxed

    // method line 55
    .method public static hidebysig 
           default class GLib.Opaque CreateOpaque (native int o, class [mscorlib]System.Type 'type')  cil managed 
    {
        // Method begins at RVA 0x2780
	// Code size 28 (0x1c)
	.maxstack 8
	IL_0000:  ldarg.1 
	IL_0001:  ldsfld class [mscorlib]System.Threading.ThreadLocal`1<class [mscorlib]System.Collections.Generic.Dictionary`2<class [mscorlib]System.Type,class GLib.FastActivator/FastCreateObjectPtr>> GLib.FastActivator::cacheOpaque
	IL_0006:  callvirt instance !0 class [mscorlib]System.Threading.ThreadLocal`1<class [mscorlib]System.Collections.Generic.Dictionary`2<class [mscorlib]System.Type, class GLib.FastActivator/FastCreateObjectPtr>>::get_Value()
	IL_000b:  call class GLib.FastActivator/FastCreateObjectPtr class GLib.FastActivator::FastCtorPtr(class [mscorlib]System.Type, class [mscorlib]System.Collections.Generic.Dictionary`2<class [mscorlib]System.Type,class GLib.FastActivator/FastCreateObjectPtr>)
	IL_0010:  ldarg.0 
	IL_0011:  callvirt instance object class GLib.FastActivator/FastCreateObjectPtr::Invoke(native int)
	IL_0016:  castclass GLib.Opaque
	IL_001b:  ret 
    } // end of method FastActivator::CreateOpaque

    // method line 56
    .method public static hidebysig 
           default class GLib.Object CreateObject (native int o, class [mscorlib]System.Type 'type')  cil managed 
    {
        // Method begins at RVA 0x27a0
	// Code size 28 (0x1c)
	.maxstack 8
	IL_0000:  ldarg.1 
	IL_0001:  ldsfld class [mscorlib]System.Threading.ThreadLocal`1<class [mscorlib]System.Collections.Generic.Dictionary`2<class [mscorlib]System.Type,class GLib.FastActivator/FastCreateObjectPtr>> GLib.FastActivator::cacheObject
	IL_0006:  callvirt instance !0 class [mscorlib]System.Threading.ThreadLocal`1<class [mscorlib]System.Collections.Generic.Dictionary`2<class [mscorlib]System.Type, class GLib.FastActivator/FastCreateObjectPtr>>::get_Value()
	IL_000b:  call class GLib.FastActivator/FastCreateObjectPtr class GLib.FastActivator::FastCtorPtr(class [mscorlib]System.Type, class [mscorlib]System.Collections.Generic.Dictionary`2<class [mscorlib]System.Type,class GLib.FastActivator/FastCreateObjectPtr>)
	IL_0010:  ldarg.0 
	IL_0011:  callvirt instance object class GLib.FastActivator/FastCreateObjectPtr::Invoke(native int)
	IL_0016:  castclass GLib.Object
	IL_001b:  ret 
    } // end of method FastActivator::CreateObject

    // method line 57
    .method public static hidebysig 
           default class GLib.SignalArgs CreateSignalArgs (class [mscorlib]System.Type 'type')  cil managed 
    {
        // Method begins at RVA 0x27c0
	// Code size 27 (0x1b)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldsfld class [mscorlib]System.Threading.ThreadLocal`1<class [mscorlib]System.Collections.Generic.Dictionary`2<class [mscorlib]System.Type,class GLib.FastActivator/FastCreateObject>> GLib.FastActivator::cacheSignalArgs
	IL_0006:  callvirt instance !0 class [mscorlib]System.Threading.ThreadLocal`1<class [mscorlib]System.Collections.Generic.Dictionary`2<class [mscorlib]System.Type, class GLib.FastActivator/FastCreateObject>>::get_Value()
	IL_000b:  call class GLib.FastActivator/FastCreateObject class GLib.FastActivator::FastCtor(class [mscorlib]System.Type, class [mscorlib]System.Collections.Generic.Dictionary`2<class [mscorlib]System.Type,class GLib.FastActivator/FastCreateObject>)
	IL_0010:  callvirt instance object class GLib.FastActivator/FastCreateObject::Invoke()
	IL_0015:  castclass GLib.SignalArgs
	IL_001a:  ret 
    } // end of method FastActivator::CreateSignalArgs

    // method line 58
    .method public static hidebysig 
           default object CreateBoxed (native int o, class [mscorlib]System.Type 'type')  cil managed 
    {
        // Method begins at RVA 0x27dc
	// Code size 23 (0x17)
	.maxstack 8
	IL_0000:  ldarg.1 
	IL_0001:  ldsfld class [mscorlib]System.Threading.ThreadLocal`1<class [mscorlib]System.Collections.Generic.Dictionary`2<class [mscorlib]System.Type,class GLib.FastActivator/FastCreateBoxed>> GLib.FastActivator::cacheBoxed
	IL_0006:  callvirt instance !0 class [mscorlib]System.Threading.ThreadLocal`1<class [mscorlib]System.Collections.Generic.Dictionary`2<class [mscorlib]System.Type, class GLib.FastActivator/FastCreateBoxed>>::get_Value()
	IL_000b:  call class GLib.FastActivator/FastCreateBoxed class GLib.FastActivator::FastBoxed(class [mscorlib]System.Type, class [mscorlib]System.Collections.Generic.Dictionary`2<class [mscorlib]System.Type,class GLib.FastActivator/FastCreateBoxed>)
	IL_0010:  ldarg.0 
	IL_0011:  callvirt instance object class GLib.FastActivator/FastCreateBoxed::Invoke(native int)
	IL_0016:  ret 
    } // end of method FastActivator::CreateBoxed

    // method line 59
    .method private static hidebysig specialname rtspecialname 
           default void '.cctor' ()  cil managed 
    {
        // Method begins at RVA 0x27f4
	// Code size 89 (0x59)
	.maxstack 2
	IL_0000:  ldnull 
	IL_0001:  ldftn class [mscorlib]System.Collections.Generic.Dictionary`2<class [mscorlib]System.Type,class GLib.FastActivator/FastCreateObjectPtr> class GLib.FastActivator::'<cacheOpaque>m__0'()
	IL_0007:  newobj instance void class [mscorlib]System.Func`1<class [mscorlib]System.Collections.Generic.Dictionary`2<class [mscorlib]System.Type, class GLib.FastActivator/FastCreateObjectPtr>>::'.ctor'(object, native int)
	IL_000c:  newobj instance void class [mscorlib]System.Threading.ThreadLocal`1<class [mscorlib]System.Collections.Generic.Dictionary`2<class [mscorlib]System.Type, class GLib.FastActivator/FastCreateObjectPtr>>::'.ctor'(class [mscorlib]System.Func`1<!0>)
	IL_0011:  stsfld class [mscorlib]System.Threading.ThreadLocal`1<class [mscorlib]System.Collections.Generic.Dictionary`2<class [mscorlib]System.Type,class GLib.FastActivator/FastCreateObjectPtr>> GLib.FastActivator::cacheOpaque
	IL_0016:  ldnull 
	IL_0017:  ldftn class [mscorlib]System.Collections.Generic.Dictionary`2<class [mscorlib]System.Type,class GLib.FastActivator/FastCreateObjectPtr> class GLib.FastActivator::'<cacheObject>m__1'()
	IL_001d:  newobj instance void class [mscorlib]System.Func`1<class [mscorlib]System.Collections.Generic.Dictionary`2<class [mscorlib]System.Type, class GLib.FastActivator/FastCreateObjectPtr>>::'.ctor'(object, native int)
	IL_0022:  newobj instance void class [mscorlib]System.Threading.ThreadLocal`1<class [mscorlib]System.Collections.Generic.Dictionary`2<class [mscorlib]System.Type, class GLib.FastActivator/FastCreateObjectPtr>>::'.ctor'(class [mscorlib]System.Func`1<!0>)
	IL_0027:  stsfld class [mscorlib]System.Threading.ThreadLocal`1<class [mscorlib]System.Collections.Generic.Dictionary`2<class [mscorlib]System.Type,class GLib.FastActivator/FastCreateObjectPtr>> GLib.FastActivator::cacheObject
	IL_002c:  ldnull 
	IL_002d:  ldftn class [mscorlib]System.Collections.Generic.Dictionary`2<class [mscorlib]System.Type,class GLib.FastActivator/FastCreateObject> class GLib.FastActivator::'<cacheSignalArgs>m__2'()
	IL_0033:  newobj instance void class [mscorlib]System.Func`1<class [mscorlib]System.Collections.Generic.Dictionary`2<class [mscorlib]System.Type, class GLib.FastActivator/FastCreateObject>>::'.ctor'(object, native int)
	IL_0038:  newobj instance void class [mscorlib]System.Threading.ThreadLocal`1<class [mscorlib]System.Collections.Generic.Dictionary`2<class [mscorlib]System.Type, class GLib.FastActivator/FastCreateObject>>::'.ctor'(class [mscorlib]System.Func`1<!0>)
	IL_003d:  stsfld class [mscorlib]System.Threading.ThreadLocal`1<class [mscorlib]System.Collections.Generic.Dictionary`2<class [mscorlib]System.Type,class GLib.FastActivator/FastCreateObject>> GLib.FastActivator::cacheSignalArgs
	IL_0042:  ldnull 
	IL_0043:  ldftn class [mscorlib]System.Collections.Generic.Dictionary`2<class [mscorlib]System.Type,class GLib.FastActivator/FastCreateBoxed> class GLib.FastActivator::'<cacheBoxed>m__3'()
	IL_0049:  newobj instance void class [mscorlib]System.Func`1<class [mscorlib]System.Collections.Generic.Dictionary`2<class [mscorlib]System.Type, class GLib.FastActivator/FastCreateBoxed>>::'.ctor'(object, native int)
	IL_004e:  newobj instance void class [mscorlib]System.Threading.ThreadLocal`1<class [mscorlib]System.Collections.Generic.Dictionary`2<class [mscorlib]System.Type, class GLib.FastActivator/FastCreateBoxed>>::'.ctor'(class [mscorlib]System.Func`1<!0>)
	IL_0053:  stsfld class [mscorlib]System.Threading.ThreadLocal`1<class [mscorlib]System.Collections.Generic.Dictionary`2<class [mscorlib]System.Type,class GLib.FastActivator/FastCreateBoxed>> GLib.FastActivator::cacheBoxed
	IL_0058:  ret 
    } // end of method FastActivator::.cctor

    // method line 60
    .method private static hidebysig 
           default class [mscorlib]System.Collections.Generic.Dictionary`2<class [mscorlib]System.Type, class GLib.FastActivator/FastCreateObjectPtr> '<cacheOpaque>m__0' ()  cil managed 
    {
        .custom instance void class [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::'.ctor'() =  (01 00 00 00 ) // ....

        // Method begins at RVA 0x285c
	// Code size 11 (0xb)
	.maxstack 8
	IL_0000:  newobj instance void class GLib.FastActivator/TypeEqualityComparer::'.ctor'()
	IL_0005:  newobj instance void class [mscorlib]System.Collections.Generic.Dictionary`2<class [mscorlib]System.Type, class GLib.FastActivator/FastCreateObjectPtr>::'.ctor'(class [mscorlib]System.Collections.Generic.IEqualityComparer`1<!0>)
	IL_000a:  ret 
    } // end of method FastActivator::<cacheOpaque>m__0

    // method line 61
    .method private static hidebysig 
           default class [mscorlib]System.Collections.Generic.Dictionary`2<class [mscorlib]System.Type, class GLib.FastActivator/FastCreateObjectPtr> '<cacheObject>m__1' ()  cil managed 
    {
        .custom instance void class [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::'.ctor'() =  (01 00 00 00 ) // ....

        // Method begins at RVA 0x2868
	// Code size 11 (0xb)
	.maxstack 8
	IL_0000:  newobj instance void class GLib.FastActivator/TypeEqualityComparer::'.ctor'()
	IL_0005:  newobj instance void class [mscorlib]System.Collections.Generic.Dictionary`2<class [mscorlib]System.Type, class GLib.FastActivator/FastCreateObjectPtr>::'.ctor'(class [mscorlib]System.Collections.Generic.IEqualityComparer`1<!0>)
	IL_000a:  ret 
    } // end of method FastActivator::<cacheObject>m__1

    // method line 62
    .method private static hidebysig 
           default class [mscorlib]System.Collections.Generic.Dictionary`2<class [mscorlib]System.Type, class GLib.FastActivator/FastCreateObject> '<cacheSignalArgs>m__2' ()  cil managed 
    {
        .custom instance void class [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::'.ctor'() =  (01 00 00 00 ) // ....

        // Method begins at RVA 0x2874
	// Code size 11 (0xb)
	.maxstack 8
	IL_0000:  newobj instance void class GLib.FastActivator/TypeEqualityComparer::'.ctor'()
	IL_0005:  newobj instance void class [mscorlib]System.Collections.Generic.Dictionary`2<class [mscorlib]System.Type, class GLib.FastActivator/FastCreateObject>::'.ctor'(class [mscorlib]System.Collections.Generic.IEqualityComparer`1<!0>)
	IL_000a:  ret 
    } // end of method FastActivator::<cacheSignalArgs>m__2

    // method line 63
    .method private static hidebysig 
           default class [mscorlib]System.Collections.Generic.Dictionary`2<class [mscorlib]System.Type, class GLib.FastActivator/FastCreateBoxed> '<cacheBoxed>m__3' ()  cil managed 
    {
        .custom instance void class [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::'.ctor'() =  (01 00 00 00 ) // ....

        // Method begins at RVA 0x2880
	// Code size 11 (0xb)
	.maxstack 8
	IL_0000:  newobj instance void class GLib.FastActivator/TypeEqualityComparer::'.ctor'()
	IL_0005:  newobj instance void class [mscorlib]System.Collections.Generic.Dictionary`2<class [mscorlib]System.Type, class GLib.FastActivator/FastCreateBoxed>::'.ctor'(class [mscorlib]System.Collections.Generic.IEqualityComparer`1<!0>)
	IL_000a:  ret 
    } // end of method FastActivator::<cacheBoxed>m__3

  .class nested private auto ansi sealed FastCreateObjectPtr
  	extends [mscorlib]System.MulticastDelegate
  {

    // method line 64
    .method public virtual hidebysig newslot 
           instance default object Invoke (native int ptr)  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method FastCreateObjectPtr::Invoke

    // method line 65
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (object 'object', native int 'method')  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method FastCreateObjectPtr::.ctor

    // method line 66
    .method public virtual hidebysig newslot 
           instance default class [mscorlib]System.IAsyncResult BeginInvoke (native int ptr, class [mscorlib]System.AsyncCallback callback, object 'object')  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method FastCreateObjectPtr::BeginInvoke

    // method line 67
    .method public virtual hidebysig newslot 
           instance default object EndInvoke (class [mscorlib]System.IAsyncResult result)  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method FastCreateObjectPtr::EndInvoke

  } // end of class FastCreateObjectPtr

  .class nested private auto ansi sealed FastCreateObject
  	extends [mscorlib]System.MulticastDelegate
  {

    // method line 68
    .method public virtual hidebysig newslot 
           instance default object Invoke ()  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method FastCreateObject::Invoke

    // method line 69
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (object 'object', native int 'method')  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method FastCreateObject::.ctor

    // method line 70
    .method public virtual hidebysig newslot 
           instance default class [mscorlib]System.IAsyncResult BeginInvoke (class [mscorlib]System.AsyncCallback callback, object 'object')  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method FastCreateObject::BeginInvoke

    // method line 71
    .method public virtual hidebysig newslot 
           instance default object EndInvoke (class [mscorlib]System.IAsyncResult result)  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method FastCreateObject::EndInvoke

  } // end of class FastCreateObject

  .class nested private auto ansi sealed FastCreateBoxed
  	extends [mscorlib]System.MulticastDelegate
  {

    // method line 72
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (object 'object', native int 'method')  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method FastCreateBoxed::.ctor

    // method line 73
    .method public virtual hidebysig newslot 
           instance default object Invoke (native int ptr)  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method FastCreateBoxed::Invoke

    // method line 74
    .method public virtual hidebysig newslot 
           instance default class [mscorlib]System.IAsyncResult BeginInvoke (native int ptr, class [mscorlib]System.AsyncCallback callback, object 'object')  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method FastCreateBoxed::BeginInvoke

    // method line 75
    .method public virtual hidebysig newslot 
           instance default object EndInvoke (class [mscorlib]System.IAsyncResult result)  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method FastCreateBoxed::EndInvoke

  } // end of class FastCreateBoxed

  .class nested private auto ansi beforefieldinit TypeEqualityComparer
  	extends [mscorlib]System.Object
  	implements class [mscorlib]System.Collections.Generic.IEqualityComparer`1<class [mscorlib]System.Type>  {

    // method line 76
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' ()  cil managed 
    {
        // Method begins at RVA 0x288c
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::'.ctor'()
	IL_0006:  ret 
    } // end of method TypeEqualityComparer::.ctor

    // method line 77
    .method public final virtual hidebysig newslot 
           instance default bool Equals (class [mscorlib]System.Type x, class [mscorlib]System.Type y)  cil managed 
    {
        // Method begins at RVA 0x2894
	// Code size 8 (0x8)
	.maxstack 8
	IL_0000:  ldarg.1 
	IL_0001:  ldarg.2 
	IL_0002:  call bool class [mscorlib]System.Type::op_Equality(class [mscorlib]System.Type, class [mscorlib]System.Type)
	IL_0007:  ret 
    } // end of method TypeEqualityComparer::Equals

    // method line 78
    .method public final virtual hidebysig newslot 
           instance default int32 GetHashCode (class [mscorlib]System.Type obj)  cil managed 
    {
        // Method begins at RVA 0x28a0
	// Code size 21 (0x15)
	.maxstack 8
	IL_0000:  ldarg.1 
	IL_0001:  ldnull 
	IL_0002:  call bool class [mscorlib]System.Type::op_Equality(class [mscorlib]System.Type, class [mscorlib]System.Type)
	IL_0007:  brfalse IL_000e

	IL_000c:  ldc.i4.0 
	IL_000d:  ret 
	IL_000e:  ldarg.1 
	IL_000f:  callvirt instance int32 object::GetHashCode()
	IL_0014:  ret 
    } // end of method TypeEqualityComparer::GetHashCode

  } // end of class TypeEqualityComparer

  .class nested private auto ansi sealed beforefieldinit '<FastBoxed>c__AnonStorey0'
  	extends [mscorlib]System.Object
  {
    .custom instance void class [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::'.ctor'() =  (01 00 00 00 ) // ....

    .field  assembly  class [mscorlib]System.Type t

    // method line 79
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' ()  cil managed 
    {
        // Method begins at RVA 0x28b8
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::'.ctor'()
	IL_0006:  ret 
    } // end of method <FastBoxed>c__AnonStorey0::.ctor

    // method line 80
    .method assembly hidebysig 
           instance default object '<>m__0' (native int ptr)  cil managed 
    {
        // Method begins at RVA 0x28c0
	// Code size 13 (0xd)
	.maxstack 8
	IL_0000:  ldarg.1 
	IL_0001:  ldarg.0 
	IL_0002:  ldfld class [mscorlib]System.Type GLib.FastActivator/'<FastBoxed>c__AnonStorey0'::t
	IL_0007:  call object class [mscorlib]System.Runtime.InteropServices.Marshal::PtrToStructure(native int, class [mscorlib]System.Type)
	IL_000c:  ret 
    } // end of method <FastBoxed>c__AnonStorey0::<>m__0

  } // end of class <FastBoxed>c__AnonStorey0

  } // end of class GLib.FastActivator
}

.namespace GLib
{
  .class public auto ansi beforefieldinit FileUtils
  	extends [mscorlib]System.Object
  {

    // method line 81
    .method private hidebysig specialname rtspecialname 
           instance default void '.ctor' ()  cil managed 
    {
        // Method begins at RVA 0x28d0
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::'.ctor'()
	IL_0006:  ret 
    } // end of method FileUtils::.ctor

    // method line 82
    .method private static hidebysig pinvokeimpl ("libglib-2.0-0.dll" as "g_file_get_contents" cdecl )
           default bool g_file_get_contents (native int filename, [out] native int& contents, [out] int32& length, [out] native int& 'error')  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method FileUtils::g_file_get_contents

    // method line 83
    .method private static hidebysig pinvokeimpl ("libglib-2.0-0.dll" as "g_file_get_contents_utf8" cdecl )
           default bool g_file_get_contents_utf8 (native int filename, [out] native int& contents, [out] int32& length, [out] native int& 'error')  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method FileUtils::g_file_get_contents_utf8

    // method line 84
    .method public static hidebysig 
           default string GetFileContents (string filename)  cil managed 
    {
        // Method begins at RVA 0x28d8
	// Code size 83 (0x53)
	.maxstack 4
	.locals init (
		int32	V_0,
		native int	V_1,
		native int	V_2,
		native int	V_3)
	IL_0000:  ldarg.0 
	IL_0001:  call native int class GLib.Marshaller::StringToPtrGStrdup(string)
	IL_0006:  stloc.3 
	IL_0007:  call bool class GLib.Global::get_IsWindowsPlatform()
	IL_000c:  brfalse IL_002e

	IL_0011:  ldloc.3 
	IL_0012:  ldloca.s 1
	IL_0014:  ldloca.s 0
	IL_0016:  ldloca.s 2
	IL_0018:  call bool class GLib.FileUtils::g_file_get_contents_utf8(native int, [out] native int&, [out] int32&, [out] native int&)
	IL_001d:  brtrue IL_0029

	IL_0022:  ldloc.2 
	IL_0023:  newobj instance void class GLib.GException::'.ctor'(native int)
	IL_0028:  throw 
	IL_0029:  br IL_0046

	IL_002e:  ldloc.3 
	IL_002f:  ldloca.s 1
	IL_0031:  ldloca.s 0
	IL_0033:  ldloca.s 2
	IL_0035:  call bool class GLib.FileUtils::g_file_get_contents(native int, [out] native int&, [out] int32&, [out] native int&)
	IL_003a:  brtrue IL_0046

	IL_003f:  ldloc.2 
	IL_0040:  newobj instance void class GLib.GException::'.ctor'(native int)
	IL_0045:  throw 
	IL_0046:  ldloc.3 
	IL_0047:  call void class GLib.Marshaller::Free(native int)
	IL_004c:  ldloc.1 
	IL_004d:  call string class GLib.Marshaller::Utf8PtrToString(native int)
	IL_0052:  ret 
    } // end of method FileUtils::GetFileContents

  } // end of class GLib.FileUtils
}

.namespace GLib
{
  .class public auto ansi beforefieldinit GException
  	extends [mscorlib]System.Exception
  {
    .field  private  native int errptr

    // method line 85
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (native int errptr)  cil managed 
    {
        // Method begins at RVA 0x2938
	// Code size 14 (0xe)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void class [mscorlib]System.Exception::'.ctor'()
	IL_0006:  ldarg.0 
	IL_0007:  ldarg.1 
	IL_0008:  stfld native int GLib.GException::errptr
	IL_000d:  ret 
    } // end of method GException::.ctor

    // method line 86
    .method private static hidebysig pinvokeimpl ("glibsharpglue-2" as "gtksharp_error_get_message" cdecl )
           default native int gtksharp_error_get_message (native int errptr)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method GException::gtksharp_error_get_message

    // method line 87
    .method public virtual hidebysig specialname 
           instance default string get_Message ()  cil managed 
    {
        // Method begins at RVA 0x2948
	// Code size 17 (0x11)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldfld native int GLib.GException::errptr
	IL_0006:  call native int class GLib.GException::gtksharp_error_get_message(native int)
	IL_000b:  call string class GLib.Marshaller::Utf8PtrToString(native int)
	IL_0010:  ret 
    } // end of method GException::get_Message

    // method line 88
    .method private static hidebysig pinvokeimpl ("libglib-2.0-0.dll" as "g_clear_error" cdecl )
           default void g_clear_error (native int& errptr)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method GException::g_clear_error

    // method line 89
    .method family virtual hidebysig 
           instance default void Finalize ()  cil managed 
    {
        // Method begins at RVA 0x295c
	// Code size 24 (0x18)
	.maxstack 1
	.try { // 0
	  IL_0000:  ldarg.0 
	  IL_0001:  ldflda native int GLib.GException::errptr
	  IL_0006:  call void class GLib.GException::g_clear_error([out] native int&)
	  IL_000b:  leave IL_0017

	} // end .try 0
	finally  { // 0
	  IL_0010:  ldarg.0 
	  IL_0011:  call instance void object::Finalize()
	  IL_0016:  endfinally 
	} // end handler 0
	IL_0017:  ret 
    } // end of method GException::Finalize

	.property instance string Message ()
	{
		.get instance default string GLib.GException::get_Message () 
	}
  } // end of class GLib.GException
}

.namespace GLib
{
  .class public auto ansi sealed GInterfaceInitHandler
  	extends [mscorlib]System.MulticastDelegate
  {
    .custom instance void class [mscorlib]System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute::'.ctor'(valuetype [mscorlib]System.Runtime.InteropServices.CallingConvention) =  (01 00 02 00 00 00 00 00 ) // ........


    // method line 90
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (object 'object', native int 'method')  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method GInterfaceInitHandler::.ctor

    // method line 91
    .method public virtual hidebysig newslot 
           instance default void Invoke (native int iface_ptr, native int data)  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method GInterfaceInitHandler::Invoke

    // method line 92
    .method public virtual hidebysig newslot 
           instance default class [mscorlib]System.IAsyncResult BeginInvoke (native int iface_ptr, native int data, class [mscorlib]System.AsyncCallback callback, object 'object')  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method GInterfaceInitHandler::BeginInvoke

    // method line 93
    .method public virtual hidebysig newslot 
           instance default void EndInvoke (class [mscorlib]System.IAsyncResult result)  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method GInterfaceInitHandler::EndInvoke

  } // end of class GLib.GInterfaceInitHandler
}

.namespace GLib
{
  .class private auto ansi sealed GInterfaceFinalizeHandler
  	extends [mscorlib]System.MulticastDelegate
  {
    .custom instance void class [mscorlib]System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute::'.ctor'(valuetype [mscorlib]System.Runtime.InteropServices.CallingConvention) =  (01 00 02 00 00 00 00 00 ) // ........


    // method line 94
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (object 'object', native int 'method')  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method GInterfaceFinalizeHandler::.ctor

    // method line 95
    .method public virtual hidebysig newslot 
           instance default void Invoke (native int iface_ptr, native int data)  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method GInterfaceFinalizeHandler::Invoke

    // method line 96
    .method public virtual hidebysig newslot 
           instance default class [mscorlib]System.IAsyncResult BeginInvoke (native int iface_ptr, native int data, class [mscorlib]System.AsyncCallback callback, object 'object')  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method GInterfaceFinalizeHandler::BeginInvoke

    // method line 97
    .method public virtual hidebysig newslot 
           instance default void EndInvoke (class [mscorlib]System.IAsyncResult result)  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method GInterfaceFinalizeHandler::EndInvoke

  } // end of class GLib.GInterfaceFinalizeHandler
}

.namespace GLib
{
  .class private sequential ansi sealed beforefieldinit GInterfaceInfo
  	extends [mscorlib]System.ValueType
  {
    .field  assembly  class GLib.GInterfaceInitHandler InitHandler
    .field  assembly  class GLib.GInterfaceFinalizeHandler FinalizeHandler
    .field  assembly  native int Data

  } // end of class GLib.GInterfaceInfo
}

.namespace GLib
{
  .class public auto ansi abstract beforefieldinit GInterfaceAdapter
  	extends [mscorlib]System.Object
  {
    .field  private  valuetype GLib.GInterfaceInfo info

    // method line 98
    .method family hidebysig specialname rtspecialname 
           instance default void '.ctor' ()  cil managed 
    {
        // Method begins at RVA 0x2990
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::'.ctor'()
	IL_0006:  ret 
    } // end of method GInterfaceAdapter::.ctor

    // method line 99
    .method family hidebysig specialname 
           instance default void set_InitHandler (class GLib.GInterfaceInitHandler 'value')  cil managed 
    {
        // Method begins at RVA 0x2998
	// Code size 13 (0xd)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldflda valuetype GLib.GInterfaceInfo GLib.GInterfaceAdapter::info
	IL_0006:  ldarg.1 
	IL_0007:  stfld class GLib.GInterfaceInitHandler GLib.GInterfaceInfo::InitHandler
	IL_000c:  ret 
    } // end of method GInterfaceAdapter::set_InitHandler

    // method line 100
    .method public virtual hidebysig newslot abstract specialname 
           instance default valuetype GLib.GType get_GType ()  cil managed 
    {
        // Method begins at RVA 0x0
    } // end of method GInterfaceAdapter::get_GType

    // method line 101
    .method public virtual hidebysig newslot abstract specialname 
           instance default native int get_Handle ()  cil managed 
    {
        // Method begins at RVA 0x0
    } // end of method GInterfaceAdapter::get_Handle

    // method line 102
    .method assembly hidebysig specialname 
           instance default valuetype GLib.GInterfaceInfo get_Info ()  cil managed 
    {
        // Method begins at RVA 0x29a8
	// Code size 55 (0x37)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldflda valuetype GLib.GInterfaceInfo GLib.GInterfaceAdapter::info
	IL_0006:  ldfld native int GLib.GInterfaceInfo::Data
	IL_000b:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_0010:  call bool native int::op_Equality(native int, native int)
	IL_0015:  brfalse IL_0030

	IL_001a:  ldarg.0 
	IL_001b:  ldflda valuetype GLib.GInterfaceInfo GLib.GInterfaceAdapter::info
	IL_0020:  ldarg.0 
	IL_0021:  call valuetype [mscorlib]System.Runtime.InteropServices.GCHandle valuetype [mscorlib]System.Runtime.InteropServices.GCHandle::Alloc(object)
	IL_0026:  call native int valuetype [mscorlib]System.Runtime.InteropServices.GCHandle::op_Explicit(valuetype [mscorlib]System.Runtime.InteropServices.GCHandle)
	IL_002b:  stfld native int GLib.GInterfaceInfo::Data
	IL_0030:  ldarg.0 
	IL_0031:  ldfld valuetype GLib.GInterfaceInfo GLib.GInterfaceAdapter::info
	IL_0036:  ret 
    } // end of method GInterfaceAdapter::get_Info

	.property instance class GLib.GInterfaceInitHandler InitHandler ()
	{
		.set instance default void GLib.GInterfaceAdapter::set_InitHandler (class GLib.GInterfaceInitHandler 'value') 
	}
	.property instance valuetype GLib.GType GType ()
	{
		.get instance default valuetype GLib.GType GLib.GInterfaceAdapter::get_GType () 
	}
	.property instance native int Handle ()
	{
		.get instance default native int GLib.GInterfaceAdapter::get_Handle () 
	}
	.property instance valuetype GLib.GInterfaceInfo Info ()
	{
		.get instance default valuetype GLib.GInterfaceInfo GLib.GInterfaceAdapter::get_Info () 
	}
  } // end of class GLib.GInterfaceAdapter
}

.namespace GLib
{
  .class public auto ansi sealed beforefieldinit GInterfaceAttribute
  	extends [mscorlib]System.Attribute
  {
    .custom instance void class [mscorlib]System.AttributeUsageAttribute::'.ctor'(valuetype [mscorlib]System.AttributeTargets) =  (01 00 00 04 00 00 00 00 ) // ........

    .field  private  class [mscorlib]System.Type adapter_type

    // method line 103
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (class [mscorlib]System.Type adapter_type)  cil managed 
    {
        // Method begins at RVA 0x29e0
	// Code size 14 (0xe)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void class [mscorlib]System.Attribute::'.ctor'()
	IL_0006:  ldarg.0 
	IL_0007:  ldarg.1 
	IL_0008:  stfld class [mscorlib]System.Type GLib.GInterfaceAttribute::adapter_type
	IL_000d:  ret 
    } // end of method GInterfaceAttribute::.ctor

    // method line 104
    .method public hidebysig specialname 
           instance default class [mscorlib]System.Type get_AdapterType ()  cil managed 
    {
        // Method begins at RVA 0x29f0
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldfld class [mscorlib]System.Type GLib.GInterfaceAttribute::adapter_type
	IL_0006:  ret 
    } // end of method GInterfaceAttribute::get_AdapterType

    // method line 105
    .method public hidebysig specialname 
           instance default void set_AdapterType (class [mscorlib]System.Type 'value')  cil managed 
    {
        // Method begins at RVA 0x29f8
	// Code size 8 (0x8)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldarg.1 
	IL_0002:  stfld class [mscorlib]System.Type GLib.GInterfaceAttribute::adapter_type
	IL_0007:  ret 
    } // end of method GInterfaceAttribute::set_AdapterType

	.property instance class [mscorlib]System.Type AdapterType ()
	{
		.get instance default class [mscorlib]System.Type GLib.GInterfaceAttribute::get_AdapterType () 
		.set instance default void GLib.GInterfaceAttribute::set_AdapterType (class [mscorlib]System.Type 'value') 
	}
  } // end of class GLib.GInterfaceAttribute
}

.namespace GLib
{
  .class public auto ansi beforefieldinit Global
  	extends [mscorlib]System.Object
  {

    // method line 106
    .method assembly static hidebysig specialname 
           default bool get_IsWindowsPlatform ()  cil managed 
    {
        // Method begins at RVA 0x2a04
	// Code size 42 (0x2a)
	.maxstack 1
	.locals init (
		valuetype [mscorlib]System.PlatformID	V_0)
	IL_0000:  call class [mscorlib]System.OperatingSystem class [mscorlib]System.Environment::get_OSVersion()
	IL_0005:  callvirt instance valuetype [mscorlib]System.PlatformID class [mscorlib]System.OperatingSystem::get_Platform()
	IL_000a:  stloc.0 
	IL_000b:  ldloc.0 
	IL_000c:  switch (
	  IL_0026,
	  IL_0026,
	  IL_0026,
	  IL_0026)
	IL_0021:  br IL_0028

	IL_0026:  ldc.i4.1 
	IL_0027:  ret 
	IL_0028:  ldc.i4.0 
	IL_0029:  ret 
    } // end of method Global::get_IsWindowsPlatform

    // method line 107
    .method private hidebysig specialname rtspecialname 
           instance default void '.ctor' ()  cil managed 
    {
        // Method begins at RVA 0x2a3c
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::'.ctor'()
	IL_0006:  ret 
    } // end of method Global::.ctor

    // method line 108
    .method public static hidebysig specialname 
           default string get_ProgramName ()  cil managed 
    {
        // Method begins at RVA 0x2a44
	// Code size 11 (0xb)
	.maxstack 8
	IL_0000:  call native int class GLib.Global::g_get_prgname()
	IL_0005:  call string class GLib.Marshaller::Utf8PtrToString(native int)
	IL_000a:  ret 
    } // end of method Global::get_ProgramName

    // method line 109
    .method private static hidebysig pinvokeimpl ("libglib-2.0-0.dll" as "g_get_prgname" cdecl )
           default native int g_get_prgname ()  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method Global::g_get_prgname

    // method line 110
    .method public static hidebysig specialname 
           default void set_ProgramName (string 'value')  cil managed 
    {
        // Method begins at RVA 0x2a50
	// Code size 20 (0x14)
	.maxstack 1
	.locals init (
		native int	V_0)
	IL_0000:  ldarg.0 
	IL_0001:  call native int class GLib.Marshaller::StringToPtrGStrdup(string)
	IL_0006:  stloc.0 
	IL_0007:  ldloc.0 
	IL_0008:  call void class GLib.Global::g_set_prgname(native int)
	IL_000d:  ldloc.0 
	IL_000e:  call void class GLib.Marshaller::Free(native int)
	IL_0013:  ret 
    } // end of method Global::set_ProgramName

    // method line 111
    .method private static hidebysig pinvokeimpl ("libglib-2.0-0.dll" as "g_set_prgname" cdecl )
           default void g_set_prgname (native int name)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method Global::g_set_prgname

    // method line 112
    .method public static hidebysig specialname 
           default string get_ApplicationName ()  cil managed 
    {
        // Method begins at RVA 0x2a70
	// Code size 11 (0xb)
	.maxstack 8
	IL_0000:  call native int class GLib.Global::g_get_application_name()
	IL_0005:  call string class GLib.Marshaller::Utf8PtrToString(native int)
	IL_000a:  ret 
    } // end of method Global::get_ApplicationName

    // method line 113
    .method private static hidebysig pinvokeimpl ("libglib-2.0-0.dll" as "g_get_application_name" cdecl )
           default native int g_get_application_name ()  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method Global::g_get_application_name

    // method line 114
    .method public static hidebysig specialname 
           default void set_ApplicationName (string 'value')  cil managed 
    {
        // Method begins at RVA 0x2a7c
	// Code size 20 (0x14)
	.maxstack 1
	.locals init (
		native int	V_0)
	IL_0000:  ldarg.0 
	IL_0001:  call native int class GLib.Marshaller::StringToPtrGStrdup(string)
	IL_0006:  stloc.0 
	IL_0007:  ldloc.0 
	IL_0008:  call void class GLib.Global::g_set_application_name(native int)
	IL_000d:  ldloc.0 
	IL_000e:  call void class GLib.Marshaller::Free(native int)
	IL_0013:  ret 
    } // end of method Global::set_ApplicationName

    // method line 115
    .method private static hidebysig pinvokeimpl ("libglib-2.0-0.dll" as "g_set_application_name" cdecl )
           default void g_set_application_name (native int name)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method Global::g_set_application_name

	.property bool IsWindowsPlatform ()
	{
		.get default bool GLib.Global::get_IsWindowsPlatform () 
	}
	.property string ProgramName ()
	{
		.set default void GLib.Global::set_ProgramName (string 'value') 
		.get default string GLib.Global::get_ProgramName () 
	}
	.property string ApplicationName ()
	{
		.get default string GLib.Global::get_ApplicationName () 
		.set default void GLib.Global::set_ApplicationName (string 'value') 
	}
  } // end of class GLib.Global
}

.namespace GLib
{
  .class public auto ansi beforefieldinit GString
  	extends [mscorlib]System.Object
  	implements GLib.IWrapper  {
    .field  private  native int handle

    // method line 116
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (string text)  cil managed 
    {
        // Method begins at RVA 0x2a9c
	// Code size 32 (0x20)
	.maxstack 2
	.locals init (
		native int	V_0)
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::'.ctor'()
	IL_0006:  ldarg.1 
	IL_0007:  call native int class GLib.Marshaller::StringToPtrGStrdup(string)
	IL_000c:  stloc.0 
	IL_000d:  ldarg.0 
	IL_000e:  ldloc.0 
	IL_000f:  call native int class GLib.GString::g_string_new(native int)
	IL_0014:  stfld native int GLib.GString::handle
	IL_0019:  ldloc.0 
	IL_001a:  call void class GLib.Marshaller::Free(native int)
	IL_001f:  ret 
    } // end of method GString::.ctor

    // method line 117
    .method private static hidebysig pinvokeimpl ("libglib-2.0-0.dll" as "g_string_new" cdecl )
           default native int g_string_new (native int text)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method GString::g_string_new

    // method line 118
    .method private static hidebysig pinvokeimpl ("libglib-2.0-0.dll" as "g_string_free" cdecl )
           default native int g_string_free (native int mem, bool free_segments)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method GString::g_string_free

    // method line 119
    .method family virtual hidebysig 
           instance default void Finalize ()  cil managed 
    {
        // Method begins at RVA 0x2ac8
	// Code size 26 (0x1a)
	.maxstack 2
	.try { // 0
	  IL_0000:  ldarg.0 
	  IL_0001:  ldfld native int GLib.GString::handle
	  IL_0006:  ldc.i4.1 
	  IL_0007:  call native int class GLib.GString::g_string_free(native int, bool)
	  IL_000c:  pop 
	  IL_000d:  leave IL_0019

	} // end .try 0
	finally  { // 0
	  IL_0012:  ldarg.0 
	  IL_0013:  call instance void object::Finalize()
	  IL_0018:  endfinally 
	} // end handler 0
	IL_0019:  ret 
    } // end of method GString::Finalize

    // method line 120
    .method public final virtual hidebysig newslot specialname 
           instance default native int get_Handle ()  cil managed 
    {
        // Method begins at RVA 0x2b00
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldfld native int GLib.GString::handle
	IL_0006:  ret 
    } // end of method GString::get_Handle

    // method line 121
    .method public static hidebysig 
           default string PtrToString (native int ptr)  cil managed 
    {
        // Method begins at RVA 0x2b08
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call string class GLib.Marshaller::Utf8PtrToString(native int)
	IL_0006:  ret 
    } // end of method GString::PtrToString

	.property instance native int Handle ()
	{
		.get instance default native int GLib.GString::get_Handle () 
	}
  } // end of class GLib.GString
}

.namespace GLib
{
  .class interface public auto ansi abstract IWrapper
  {

    // method line 122
    .method public virtual hidebysig newslot abstract specialname 
           instance default native int get_Handle ()  cil managed 
    {
        // Method begins at RVA 0x0
    } // end of method IWrapper::get_Handle

	.property instance native int Handle ()
	{
		.get instance default native int GLib.IWrapper::get_Handle () 
	}
  } // end of class GLib.IWrapper
}

.namespace GLib
{
  .class public sequential ansi sealed GType
  	extends [mscorlib]System.ValueType
  	implements class [mscorlib]System.IEquatable`1<valuetype GLib.GType>  {
    .field  private  native int val
    .field  public static initonly  valuetype GLib.GType Invalid
    .field  public static initonly  valuetype GLib.GType None
    .field  public static initonly  valuetype GLib.GType Interface
    .field  public static initonly  valuetype GLib.GType Char
    .field  public static initonly  valuetype GLib.GType UChar
    .field  public static initonly  valuetype GLib.GType Boolean
    .field  public static initonly  valuetype GLib.GType Int
    .field  public static initonly  valuetype GLib.GType UInt
    .field  public static initonly  valuetype GLib.GType Long
    .field  public static initonly  valuetype GLib.GType ULong
    .field  public static initonly  valuetype GLib.GType Int64
    .field  public static initonly  valuetype GLib.GType UInt64
    .field  public static initonly  valuetype GLib.GType Enum
    .field  public static initonly  valuetype GLib.GType Flags
    .field  public static initonly  valuetype GLib.GType Float
    .field  public static initonly  valuetype GLib.GType Double
    .field  public static initonly  valuetype GLib.GType String
    .field  public static initonly  valuetype GLib.GType Pointer
    .field  public static initonly  valuetype GLib.GType Boxed
    .field  public static initonly  valuetype GLib.GType Param
    .field  public static initonly  valuetype GLib.GType Object
    .field  private static  class [mscorlib]System.Collections.Generic.Dictionary`2<native int,class [mscorlib]System.Type> types
    .field  private static  class [mscorlib]System.Collections.Generic.Dictionary`2<class [mscorlib]System.Type,valuetype GLib.GType> gtypes

    // method line 123
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (native int val)  cil managed 
    {
        // Method begins at RVA 0x2b10
	// Code size 8 (0x8)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldarg.1 
	IL_0002:  stfld native int GLib.GType::val
	IL_0007:  ret 
    } // end of method GType::.ctor

    // method line 124
    .method private static hidebysig specialname rtspecialname 
           default void '.cctor' ()  cil managed 
    {
        // Method begins at RVA 0x2b1c
	// Code size 665 (0x299)
	.maxstack 3
	IL_0000:  ldc.i4.0 
	IL_0001:  call native int native int::op_Explicit(int32)
	IL_0006:  newobj instance void valuetype GLib.GType::'.ctor'(native int)
	IL_000b:  stsfld valuetype GLib.GType GLib.GType::Invalid
	IL_0010:  ldc.i4.4 
	IL_0011:  call native int native int::op_Explicit(int32)
	IL_0016:  newobj instance void valuetype GLib.GType::'.ctor'(native int)
	IL_001b:  stsfld valuetype GLib.GType GLib.GType::None
	IL_0020:  ldc.i4.8 
	IL_0021:  call native int native int::op_Explicit(int32)
	IL_0026:  newobj instance void valuetype GLib.GType::'.ctor'(native int)
	IL_002b:  stsfld valuetype GLib.GType GLib.GType::Interface
	IL_0030:  ldc.i4.s 0x0c
	IL_0032:  call native int native int::op_Explicit(int32)
	IL_0037:  newobj instance void valuetype GLib.GType::'.ctor'(native int)
	IL_003c:  stsfld valuetype GLib.GType GLib.GType::Char
	IL_0041:  ldc.i4.s 0x10
	IL_0043:  call native int native int::op_Explicit(int32)
	IL_0048:  newobj instance void valuetype GLib.GType::'.ctor'(native int)
	IL_004d:  stsfld valuetype GLib.GType GLib.GType::UChar
	IL_0052:  ldc.i4.s 0x14
	IL_0054:  call native int native int::op_Explicit(int32)
	IL_0059:  newobj instance void valuetype GLib.GType::'.ctor'(native int)
	IL_005e:  stsfld valuetype GLib.GType GLib.GType::Boolean
	IL_0063:  ldc.i4.s 0x18
	IL_0065:  call native int native int::op_Explicit(int32)
	IL_006a:  newobj instance void valuetype GLib.GType::'.ctor'(native int)
	IL_006f:  stsfld valuetype GLib.GType GLib.GType::Int
	IL_0074:  ldc.i4.s 0x1c
	IL_0076:  call native int native int::op_Explicit(int32)
	IL_007b:  newobj instance void valuetype GLib.GType::'.ctor'(native int)
	IL_0080:  stsfld valuetype GLib.GType GLib.GType::UInt
	IL_0085:  ldc.i4.s 0x20
	IL_0087:  call native int native int::op_Explicit(int32)
	IL_008c:  newobj instance void valuetype GLib.GType::'.ctor'(native int)
	IL_0091:  stsfld valuetype GLib.GType GLib.GType::Long
	IL_0096:  ldc.i4.s 0x24
	IL_0098:  call native int native int::op_Explicit(int32)
	IL_009d:  newobj instance void valuetype GLib.GType::'.ctor'(native int)
	IL_00a2:  stsfld valuetype GLib.GType GLib.GType::ULong
	IL_00a7:  ldc.i4.s 0x28
	IL_00a9:  call native int native int::op_Explicit(int32)
	IL_00ae:  newobj instance void valuetype GLib.GType::'.ctor'(native int)
	IL_00b3:  stsfld valuetype GLib.GType GLib.GType::Int64
	IL_00b8:  ldc.i4.s 0x2c
	IL_00ba:  call native int native int::op_Explicit(int32)
	IL_00bf:  newobj instance void valuetype GLib.GType::'.ctor'(native int)
	IL_00c4:  stsfld valuetype GLib.GType GLib.GType::UInt64
	IL_00c9:  ldc.i4.s 0x30
	IL_00cb:  call native int native int::op_Explicit(int32)
	IL_00d0:  newobj instance void valuetype GLib.GType::'.ctor'(native int)
	IL_00d5:  stsfld valuetype GLib.GType GLib.GType::Enum
	IL_00da:  ldc.i4.s 0x34
	IL_00dc:  call native int native int::op_Explicit(int32)
	IL_00e1:  newobj instance void valuetype GLib.GType::'.ctor'(native int)
	IL_00e6:  stsfld valuetype GLib.GType GLib.GType::Flags
	IL_00eb:  ldc.i4.s 0x38
	IL_00ed:  call native int native int::op_Explicit(int32)
	IL_00f2:  newobj instance void valuetype GLib.GType::'.ctor'(native int)
	IL_00f7:  stsfld valuetype GLib.GType GLib.GType::Float
	IL_00fc:  ldc.i4.s 0x3c
	IL_00fe:  call native int native int::op_Explicit(int32)
	IL_0103:  newobj instance void valuetype GLib.GType::'.ctor'(native int)
	IL_0108:  stsfld valuetype GLib.GType GLib.GType::Double
	IL_010d:  ldc.i4.s 0x40
	IL_010f:  call native int native int::op_Explicit(int32)
	IL_0114:  newobj instance void valuetype GLib.GType::'.ctor'(native int)
	IL_0119:  stsfld valuetype GLib.GType GLib.GType::String
	IL_011e:  ldc.i4.s 0x44
	IL_0120:  call native int native int::op_Explicit(int32)
	IL_0125:  newobj instance void valuetype GLib.GType::'.ctor'(native int)
	IL_012a:  stsfld valuetype GLib.GType GLib.GType::Pointer
	IL_012f:  ldc.i4.s 0x48
	IL_0131:  call native int native int::op_Explicit(int32)
	IL_0136:  newobj instance void valuetype GLib.GType::'.ctor'(native int)
	IL_013b:  stsfld valuetype GLib.GType GLib.GType::Boxed
	IL_0140:  ldc.i4.s 0x4c
	IL_0142:  call native int native int::op_Explicit(int32)
	IL_0147:  newobj instance void valuetype GLib.GType::'.ctor'(native int)
	IL_014c:  stsfld valuetype GLib.GType GLib.GType::Param
	IL_0151:  ldc.i4.s 0x50
	IL_0153:  call native int native int::op_Explicit(int32)
	IL_0158:  newobj instance void valuetype GLib.GType::'.ctor'(native int)
	IL_015d:  stsfld valuetype GLib.GType GLib.GType::Object
	IL_0162:  ldsfld class [mscorlib]System.Collections.Generic.IEqualityComparer`1<native int> GLib.IntPtrEqualityComparer::Instance
	IL_0167:  newobj instance void class [mscorlib]System.Collections.Generic.Dictionary`2<native int, class [mscorlib]System.Type>::'.ctor'(class [mscorlib]System.Collections.Generic.IEqualityComparer`1<!0>)
	IL_016c:  stsfld class [mscorlib]System.Collections.Generic.Dictionary`2<native int,class [mscorlib]System.Type> GLib.GType::types
	IL_0171:  newobj instance void class [mscorlib]System.Collections.Generic.Dictionary`2<class [mscorlib]System.Type, valuetype GLib.GType>::'.ctor'()
	IL_0176:  stsfld class [mscorlib]System.Collections.Generic.Dictionary`2<class [mscorlib]System.Type,valuetype GLib.GType> GLib.GType::gtypes
	IL_017b:  call bool class GLib.Thread::get_Supported()
	IL_0180:  brtrue IL_018a

	IL_0185:  call void class GLib.Thread::Init()
	IL_018a:  call void valuetype GLib.GType::g_type_init()
	IL_018f:  ldsfld valuetype GLib.GType GLib.GType::Char
	IL_0194:  ldtoken int8
	IL_0199:  call class [mscorlib]System.Type class [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
	IL_019e:  call void valuetype GLib.GType::Register(valuetype GLib.GType, class [mscorlib]System.Type)
	IL_01a3:  ldsfld valuetype GLib.GType GLib.GType::UChar
	IL_01a8:  ldtoken unsigned int8
	IL_01ad:  call class [mscorlib]System.Type class [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
	IL_01b2:  call void valuetype GLib.GType::Register(valuetype GLib.GType, class [mscorlib]System.Type)
	IL_01b7:  ldsfld valuetype GLib.GType GLib.GType::Boolean
	IL_01bc:  ldtoken bool
	IL_01c1:  call class [mscorlib]System.Type class [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
	IL_01c6:  call void valuetype GLib.GType::Register(valuetype GLib.GType, class [mscorlib]System.Type)
	IL_01cb:  ldsfld valuetype GLib.GType GLib.GType::Int
	IL_01d0:  ldtoken int32
	IL_01d5:  call class [mscorlib]System.Type class [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
	IL_01da:  call void valuetype GLib.GType::Register(valuetype GLib.GType, class [mscorlib]System.Type)
	IL_01df:  ldsfld valuetype GLib.GType GLib.GType::UInt
	IL_01e4:  ldtoken unsigned int32
	IL_01e9:  call class [mscorlib]System.Type class [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
	IL_01ee:  call void valuetype GLib.GType::Register(valuetype GLib.GType, class [mscorlib]System.Type)
	IL_01f3:  ldsfld valuetype GLib.GType GLib.GType::Int64
	IL_01f8:  ldtoken int64
	IL_01fd:  call class [mscorlib]System.Type class [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
	IL_0202:  call void valuetype GLib.GType::Register(valuetype GLib.GType, class [mscorlib]System.Type)
	IL_0207:  ldsfld valuetype GLib.GType GLib.GType::UInt64
	IL_020c:  ldtoken unsigned int64
	IL_0211:  call class [mscorlib]System.Type class [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
	IL_0216:  call void valuetype GLib.GType::Register(valuetype GLib.GType, class [mscorlib]System.Type)
	IL_021b:  ldsfld valuetype GLib.GType GLib.GType::Float
	IL_0220:  ldtoken float32
	IL_0225:  call class [mscorlib]System.Type class [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
	IL_022a:  call void valuetype GLib.GType::Register(valuetype GLib.GType, class [mscorlib]System.Type)
	IL_022f:  ldsfld valuetype GLib.GType GLib.GType::Double
	IL_0234:  ldtoken float64
	IL_0239:  call class [mscorlib]System.Type class [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
	IL_023e:  call void valuetype GLib.GType::Register(valuetype GLib.GType, class [mscorlib]System.Type)
	IL_0243:  ldsfld valuetype GLib.GType GLib.GType::String
	IL_0248:  ldtoken string
	IL_024d:  call class [mscorlib]System.Type class [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
	IL_0252:  call void valuetype GLib.GType::Register(valuetype GLib.GType, class [mscorlib]System.Type)
	IL_0257:  ldsfld valuetype GLib.GType GLib.GType::Pointer
	IL_025c:  ldtoken native int
	IL_0261:  call class [mscorlib]System.Type class [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
	IL_0266:  call void valuetype GLib.GType::Register(valuetype GLib.GType, class [mscorlib]System.Type)
	IL_026b:  ldsfld valuetype GLib.GType GLib.GType::Object
	IL_0270:  ldtoken GLib.Object
	IL_0275:  call class [mscorlib]System.Type class [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
	IL_027a:  call void valuetype GLib.GType::Register(valuetype GLib.GType, class [mscorlib]System.Type)
	IL_027f:  ldsfld class [mscorlib]System.Collections.Generic.Dictionary`2<class [mscorlib]System.Type,valuetype GLib.GType> GLib.GType::gtypes
	IL_0284:  ldtoken char
	IL_0289:  call class [mscorlib]System.Type class [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
	IL_028e:  ldsfld valuetype GLib.GType GLib.GType::UInt
	IL_0293:  callvirt instance void class [mscorlib]System.Collections.Generic.Dictionary`2<class [mscorlib]System.Type, valuetype GLib.GType>::set_Item(!0, !1)
	IL_0298:  ret 
    } // end of method GType::.cctor

    // method line 125
    .method private static hidebysig pinvokeimpl ("libgobject-2.0-0.dll" as "g_type_init" cdecl )
           default void g_type_init ()  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method GType::g_type_init

    // method line 126
    .method public static hidebysig 
           default void Register (valuetype GLib.GType native_type, class [mscorlib]System.Type 'type')  cil managed 
    {
        // Method begins at RVA 0x2dc4
	// Code size 91 (0x5b)
	.maxstack 3
	IL_0000:  ldarg.0 
	IL_0001:  ldsfld valuetype GLib.GType GLib.GType::Pointer
	IL_0006:  call bool valuetype GLib.GType::op_Inequality(valuetype GLib.GType, valuetype GLib.GType)
	IL_000b:  brfalse IL_0042

	IL_0010:  ldarg.0 
	IL_0011:  ldsfld valuetype GLib.GType GLib.GType::Boxed
	IL_0016:  call bool valuetype GLib.GType::op_Inequality(valuetype GLib.GType, valuetype GLib.GType)
	IL_001b:  brfalse IL_0042

	IL_0020:  ldarg.0 
	IL_0021:  call valuetype GLib.GType class GLib.ManagedValue::get_GType()
	IL_0026:  call bool valuetype GLib.GType::op_Inequality(valuetype GLib.GType, valuetype GLib.GType)
	IL_002b:  brfalse IL_0042

	IL_0030:  ldsfld class [mscorlib]System.Collections.Generic.Dictionary`2<native int,class [mscorlib]System.Type> GLib.GType::types
	IL_0035:  ldarga.s 0
	IL_0037:  call instance native int valuetype GLib.GType::get_Val()
	IL_003c:  ldarg.1 
	IL_003d:  callvirt instance void class [mscorlib]System.Collections.Generic.Dictionary`2<native int, class [mscorlib]System.Type>::set_Item(!0, !1)
	IL_0042:  ldarg.1 
	IL_0043:  ldnull 
	IL_0044:  call bool class [mscorlib]System.Type::op_Inequality(class [mscorlib]System.Type, class [mscorlib]System.Type)
	IL_0049:  brfalse IL_005a

	IL_004e:  ldsfld class [mscorlib]System.Collections.Generic.Dictionary`2<class [mscorlib]System.Type,valuetype GLib.GType> GLib.GType::gtypes
	IL_0053:  ldarg.1 
	IL_0054:  ldarg.0 
	IL_0055:  callvirt instance void class [mscorlib]System.Collections.Generic.Dictionary`2<class [mscorlib]System.Type, valuetype GLib.GType>::set_Item(!0, !1)
	IL_005a:  ret 
    } // end of method GType::Register

    // method line 127
    .method public static hidebysig 
           default valuetype GLib.GType FromName (string native_name)  cil managed 
    {
        // Method begins at RVA 0x2e2c
	// Code size 12 (0xc)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call native int valuetype GLib.GType::g_type_from_name(string)
	IL_0006:  newobj instance void valuetype GLib.GType::'.ctor'(native int)
	IL_000b:  ret 
    } // end of method GType::FromName

    // method line 128
    .method private static hidebysig pinvokeimpl ("libgobject-2.0-0.dll" as "g_type_from_name" cdecl )
           default native int g_type_from_name (string name)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method GType::g_type_from_name

    // method line 129
    .method public static hidebysig specialname 
           default bool op_Inequality (valuetype GLib.GType a, valuetype GLib.GType b)  cil managed 
    {
        // Method begins at RVA 0x2e3c
	// Code size 20 (0x14)
	.maxstack 8
	IL_0000:  ldarga.s 0
	IL_0002:  call instance native int valuetype GLib.GType::get_Val()
	IL_0007:  ldarga.s 1
	IL_0009:  call instance native int valuetype GLib.GType::get_Val()
	IL_000e:  call bool native int::op_Inequality(native int, native int)
	IL_0013:  ret 
    } // end of method GType::op_Inequality

    // method line 130
    .method public hidebysig specialname 
           instance default native int get_Val ()  cil managed 
    {
        // Method begins at RVA 0x2e54
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldfld native int GLib.GType::val
	IL_0006:  ret 
    } // end of method GType::get_Val

    // method line 131
    .method public static hidebysig specialname 
           default valuetype GLib.GType op_Explicit (class [mscorlib]System.Type 'type')  cil managed 
    {
        // Method begins at RVA 0x2e5c
	// Code size 324 (0x144)
	.maxstack 3
	.locals init (
		valuetype GLib.GType	V_0,
		class GLib.GTypeTypeAttribute	V_1,
		class GLib.GTypeAttribute	V_2,
		class [mscorlib]System.Reflection.PropertyInfo	V_3,
		class GLib.GTypeTypeAttribute	V_4,
		class [mscorlib]System.Reflection.PropertyInfo	V_5)
	IL_0000:  ldsfld class [mscorlib]System.Collections.Generic.Dictionary`2<class [mscorlib]System.Type,valuetype GLib.GType> GLib.GType::gtypes
	IL_0005:  ldarg.0 
	IL_0006:  ldloca.s 0
	IL_0008:  callvirt instance bool class [mscorlib]System.Collections.Generic.Dictionary`2<class [mscorlib]System.Type, valuetype GLib.GType>::TryGetValue(!0, [out] !1&)
	IL_000d:  brfalse IL_0014

	IL_0012:  ldloc.0 
	IL_0013:  ret 
	IL_0014:  ldarg.0 
	IL_0015:  ldtoken GLib.Object
	IL_001a:  call class [mscorlib]System.Type class [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
	IL_001f:  callvirt instance bool class [mscorlib]System.Type::IsSubclassOf(class [mscorlib]System.Type)
	IL_0024:  brfalse IL_0039

	IL_0029:  ldarg.0 
	IL_002a:  call valuetype GLib.GType class GLib.Object::LookupGType(class [mscorlib]System.Type)
	IL_002f:  stloc.0 
	IL_0030:  ldloc.0 
	IL_0031:  ldarg.0 
	IL_0032:  call void valuetype GLib.GType::Register(valuetype GLib.GType, class [mscorlib]System.Type)
	IL_0037:  ldloc.0 
	IL_0038:  ret 
	IL_0039:  ldarg.0 
	IL_003a:  callvirt instance bool class [mscorlib]System.Type::get_IsEnum()
	IL_003f:  brfalse IL_00bb

	IL_0044:  ldarg.0 
	IL_0045:  ldtoken GLib.GTypeTypeAttribute
	IL_004a:  call class [mscorlib]System.Type class [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
	IL_004f:  ldc.i4.0 
	IL_0050:  call class [mscorlib]System.Attribute class [mscorlib]System.Attribute::GetCustomAttribute(class [mscorlib]System.Reflection.MemberInfo, class [mscorlib]System.Type, bool)
	IL_0055:  castclass GLib.GTypeTypeAttribute
	IL_005a:  dup 
	IL_005b:  stloc.1 
	IL_005c:  brfalse IL_006d

	IL_0061:  ldloc.1 
	IL_0062:  callvirt instance valuetype GLib.GType class GLib.GTypeTypeAttribute::get_Type()
	IL_0067:  stloc.0 
	IL_0068:  br IL_00b6

	IL_006d:  ldarg.0 
	IL_006e:  ldtoken GLib.GTypeAttribute
	IL_0073:  call class [mscorlib]System.Type class [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
	IL_0078:  ldc.i4.0 
	IL_0079:  call class [mscorlib]System.Attribute class [mscorlib]System.Attribute::GetCustomAttribute(class [mscorlib]System.Reflection.MemberInfo, class [mscorlib]System.Type, bool)
	IL_007e:  castclass GLib.GTypeAttribute
	IL_0083:  dup 
	IL_0084:  stloc.2 
	IL_0085:  brfalse IL_00b0

	IL_008a:  ldloc.2 
	IL_008b:  callvirt instance class [mscorlib]System.Type class GLib.GTypeAttribute::get_WrapperType()
	IL_0090:  ldstr "GType"
	IL_0095:  ldc.i4.s 0x18
	IL_0097:  callvirt instance class [mscorlib]System.Reflection.PropertyInfo class [mscorlib]System.Type::GetProperty(string, valuetype [mscorlib]System.Reflection.BindingFlags)
	IL_009c:  stloc.3 
	IL_009d:  ldloc.3 
	IL_009e:  ldnull 
	IL_009f:  ldnull 
	IL_00a0:  callvirt instance object class [mscorlib]System.Reflection.PropertyInfo::GetValue(object, object[])
	IL_00a5:  unbox.any GLib.GType
	IL_00aa:  stloc.0 
	IL_00ab:  br IL_00b6

	IL_00b0:  call valuetype GLib.GType class GLib.ManagedValue::get_GType()
	IL_00b5:  stloc.0 
	IL_00b6:  br IL_013b

	IL_00bb:  ldarg.0 
	IL_00bc:  ldtoken GLib.GTypeTypeAttribute
	IL_00c1:  call class [mscorlib]System.Type class [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
	IL_00c6:  ldc.i4.0 
	IL_00c7:  call class [mscorlib]System.Attribute class [mscorlib]System.Attribute::GetCustomAttribute(class [mscorlib]System.Reflection.MemberInfo, class [mscorlib]System.Type, bool)
	IL_00cc:  castclass GLib.GTypeTypeAttribute
	IL_00d1:  dup 
	IL_00d2:  stloc.s 4
	IL_00d4:  brfalse IL_00e6

	IL_00d9:  ldloc.s 4
	IL_00db:  callvirt instance valuetype GLib.GType class GLib.GTypeTypeAttribute::get_Type()
	IL_00e0:  stloc.0 
	IL_00e1:  br IL_013b

	IL_00e6:  ldarg.0 
	IL_00e7:  ldstr "GType"
	IL_00ec:  ldc.i4.s 0x78
	IL_00ee:  callvirt instance class [mscorlib]System.Reflection.PropertyInfo class [mscorlib]System.Type::GetProperty(string, valuetype [mscorlib]System.Reflection.BindingFlags)
	IL_00f3:  dup 
	IL_00f4:  stloc.s 5
	IL_00f6:  ldnull 
	IL_00f7:  call bool class [mscorlib]System.Reflection.PropertyInfo::op_Inequality(class [mscorlib]System.Reflection.PropertyInfo, class [mscorlib]System.Reflection.PropertyInfo)
	IL_00fc:  brfalse IL_0115

	IL_0101:  ldloc.s 5
	IL_0103:  ldnull 
	IL_0104:  ldnull 
	IL_0105:  callvirt instance object class [mscorlib]System.Reflection.PropertyInfo::GetValue(object, object[])
	IL_010a:  unbox.any GLib.GType
	IL_010f:  stloc.0 
	IL_0110:  br IL_013b

	IL_0115:  ldarg.0 
	IL_0116:  ldtoken GLib.Opaque
	IL_011b:  call class [mscorlib]System.Type class [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
	IL_0120:  callvirt instance bool class [mscorlib]System.Type::IsSubclassOf(class [mscorlib]System.Type)
	IL_0125:  brfalse IL_0135

	IL_012a:  ldsfld valuetype GLib.GType GLib.GType::Pointer
	IL_012f:  stloc.0 
	IL_0130:  br IL_013b

	IL_0135:  call valuetype GLib.GType class GLib.ManagedValue::get_GType()
	IL_013a:  stloc.0 
	IL_013b:  ldloc.0 
	IL_013c:  ldarg.0 
	IL_013d:  call void valuetype GLib.GType::Register(valuetype GLib.GType, class [mscorlib]System.Type)
	IL_0142:  ldloc.0 
	IL_0143:  ret 
    } // end of method GType::op_Explicit

    // method line 132
    .method private static hidebysig 
           default string GetQualifiedName (string cname)  cil managed 
    {
        // Method begins at RVA 0x2fac
	// Code size 122 (0x7a)
	.maxstack 4
	.locals init (
		int32	V_0)
	IL_0000:  ldc.i4.1 
	IL_0001:  stloc.0 
	IL_0002:  br IL_005d

	IL_0007:  ldarg.0 
	IL_0008:  ldloc.0 
	IL_0009:  callvirt instance char string::get_Chars(int32)
	IL_000e:  call bool char::IsUpper(char)
	IL_0013:  brfalse IL_0059

	IL_0018:  ldloc.0 
	IL_0019:  ldc.i4.1 
	IL_001a:  bne.un IL_003f

	IL_001f:  ldarg.0 
	IL_0020:  ldc.i4.0 
	IL_0021:  callvirt instance char string::get_Chars(int32)
	IL_0026:  ldc.i4.s 0x47
	IL_0028:  bne.un IL_003f

	IL_002d:  ldstr "GLib."
	IL_0032:  ldarg.0 
	IL_0033:  ldc.i4.1 
	IL_0034:  callvirt instance string string::Substring(int32)
	IL_0039:  call string string::Concat(string, string)
	IL_003e:  ret 
	IL_003f:  ldarg.0 
	IL_0040:  ldc.i4.0 
	IL_0041:  ldloc.0 
	IL_0042:  callvirt instance string string::Substring(int32, int32)
	IL_0047:  ldstr "."
	IL_004c:  ldarg.0 
	IL_004d:  ldloc.0 
	IL_004e:  callvirt instance string string::Substring(int32)
	IL_0053:  call string string::Concat(string, string, string)
	IL_0058:  ret 
	IL_0059:  ldloc.0 
	IL_005a:  ldc.i4.1 
	IL_005b:  add 
	IL_005c:  stloc.0 
	IL_005d:  ldloc.0 
	IL_005e:  ldarg.0 
	IL_005f:  callvirt instance int32 string::get_Length()
	IL_0064:  blt IL_0007

	IL_0069:  ldstr "cname is not in NamespaceType format. GType.Register should be called directly for "
	IL_006e:  ldarg.0 
	IL_006f:  call string string::Concat(string, string)
	IL_0074:  newobj instance void class [mscorlib]System.ArgumentException::'.ctor'(string)
	IL_0079:  throw 
    } // end of method GType::GetQualifiedName

    // method line 133
    .method public static hidebysig specialname 
           default class [mscorlib]System.Type op_Explicit (valuetype GLib.GType gtype)  cil managed 
    {
        // Method begins at RVA 0x3034
	// Code size 13 (0xd)
	.maxstack 8
	IL_0000:  ldarga.s 0
	IL_0002:  call instance native int valuetype GLib.GType::get_Val()
	IL_0007:  call class [mscorlib]System.Type valuetype GLib.GType::LookupType(native int)
	IL_000c:  ret 
    } // end of method GType::op_Explicit

    // method line 134
    .method public static hidebysig 
           default class [mscorlib]System.Type LookupType (native int typeid)  cil managed 
    {
        // Method begins at RVA 0x3044
	// Code size 439 (0x1b7)
	.maxstack 4
	.locals init (
		class [mscorlib]System.Type	V_0,
		string	V_1,
		string	V_2,
		class [mscorlib]System.Reflection.Assembly[]	V_3,
		class [mscorlib]System.Reflection.Assembly	V_4,
		class [mscorlib]System.Reflection.Assembly[]	V_5,
		int32	V_6,
		string	V_7,
		string	V_8,
		class [mscorlib]System.Reflection.Assembly	V_9,
		class [mscorlib]System.Reflection.Assembly[]	V_10,
		int32	V_11,
		class [mscorlib]System.Reflection.AssemblyName	V_12,
		class [mscorlib]System.Reflection.AssemblyName[]	V_13,
		int32	V_14,
		string	V_15,
		class [mscorlib]System.Reflection.Assembly	V_16)
	IL_0000:  ldsfld class [mscorlib]System.Collections.Generic.Dictionary`2<native int,class [mscorlib]System.Type> GLib.GType::types
	IL_0005:  ldarg.0 
	IL_0006:  ldloca.s 0
	IL_0008:  callvirt instance bool class [mscorlib]System.Collections.Generic.Dictionary`2<native int, class [mscorlib]System.Type>::TryGetValue(!0, [out] !1&)
	IL_000d:  brfalse IL_0014

	IL_0012:  ldloc.0 
	IL_0013:  ret 
	IL_0014:  ldarg.0 
	IL_0015:  call native int valuetype GLib.GType::g_type_name(native int)
	IL_001a:  call string class GLib.Marshaller::Utf8PtrToString(native int)
	IL_001f:  stloc.1 
	IL_0020:  ldloc.1 
	IL_0021:  call string valuetype GLib.GType::GetQualifiedName(string)
	IL_0026:  stloc.2 
	IL_0027:  call class [mscorlib]System.AppDomain class [mscorlib]System.AppDomain::get_CurrentDomain()
	IL_002c:  callvirt instance class [mscorlib]System.Reflection.Assembly[] class [mscorlib]System.AppDomain::GetAssemblies()
	IL_0031:  callvirt instance object class [mscorlib]System.Array::Clone()
	IL_0036:  castclass class [mscorlib]System.Reflection.Assembly[]
	IL_003b:  stloc.3 
	IL_003c:  ldloc.3 
	IL_003d:  stloc.s 5
	IL_003f:  ldc.i4.0 
	IL_0040:  stloc.s 6
	IL_0042:  br IL_006e

	IL_0047:  ldloc.s 5
	IL_0049:  ldloc.s 6
	IL_004b:  ldelem.ref 
	IL_004c:  stloc.s 4
	IL_004e:  ldloc.s 4
	IL_0050:  ldloc.2 
	IL_0051:  callvirt instance class [mscorlib]System.Type class [mscorlib]System.Reflection.Assembly::GetType(string)
	IL_0056:  stloc.0 
	IL_0057:  ldloc.0 
	IL_0058:  ldnull 
	IL_0059:  call bool class [mscorlib]System.Type::op_Inequality(class [mscorlib]System.Type, class [mscorlib]System.Type)
	IL_005e:  brfalse IL_0068

	IL_0063:  br IL_0079

	IL_0068:  ldloc.s 6
	IL_006a:  ldc.i4.1 
	IL_006b:  add 
	IL_006c:  stloc.s 6
	IL_006e:  ldloc.s 6
	IL_0070:  ldloc.s 5
	IL_0072:  ldlen 
	IL_0073:  conv.i4 
	IL_0074:  blt IL_0047

	IL_0079:  ldloc.0 
	IL_007a:  ldnull 
	IL_007b:  call bool class [mscorlib]System.Type::op_Equality(class [mscorlib]System.Type, class [mscorlib]System.Type)
	IL_0080:  brfalse IL_01a9

	IL_0085:  ldloc.2 
	IL_0086:  ldc.i4.0 
	IL_0087:  ldloc.2 
	IL_0088:  ldc.i4.s 0x2e
	IL_008a:  callvirt instance int32 string::LastIndexOf(char)
	IL_008f:  callvirt instance string string::Substring(int32, int32)
	IL_0094:  stloc.s 7
	IL_0096:  ldloc.s 7
	IL_0098:  callvirt instance string string::ToLower()
	IL_009d:  ldc.i4.s 0x2e
	IL_009f:  ldc.i4.s 0x2d
	IL_00a1:  callvirt instance string string::Replace(char, char)
	IL_00a6:  ldstr "-sharp"
	IL_00ab:  call string string::Concat(string, string)
	IL_00b0:  stloc.s 8
	IL_00b2:  ldloc.3 
	IL_00b3:  stloc.s 10
	IL_00b5:  ldc.i4.0 
	IL_00b6:  stloc.s 11
	IL_00b8:  br IL_019e

	IL_00bd:  ldloc.s 10
	IL_00bf:  ldloc.s 11
	IL_00c1:  ldelem.ref 
	IL_00c2:  stloc.s 9
	IL_00c4:  ldloc.s 9
	IL_00c6:  callvirt instance class [mscorlib]System.Reflection.AssemblyName[] class [mscorlib]System.Reflection.Assembly::GetReferencedAssemblies()
	IL_00cb:  stloc.s 13
	IL_00cd:  ldc.i4.0 
	IL_00ce:  stloc.s 14
	IL_00d0:  br IL_017c

	IL_00d5:  ldloc.s 13
	IL_00d7:  ldloc.s 14
	IL_00d9:  ldelem.ref 
	IL_00da:  stloc.s 12
	IL_00dc:  ldloc.s 12
	IL_00de:  callvirt instance string class [mscorlib]System.Reflection.AssemblyName::get_Name()
	IL_00e3:  ldloc.s 8
	IL_00e5:  call bool string::op_Inequality(string, string)
	IL_00ea:  brfalse IL_00f4

	IL_00ef:  br IL_0176

	.try { // 0
	  IL_00f4:  ldloc.s 9
	  IL_00f6:  callvirt instance string class [mscorlib]System.Reflection.Assembly::get_Location()
	  IL_00fb:  call string class [mscorlib]System.IO.Path::GetDirectoryName(string)
	  IL_0100:  stloc.s 15
	  IL_0102:  ldloc.s 15
	  IL_0104:  ldloc.s 12
	  IL_0106:  callvirt instance string class [mscorlib]System.Reflection.AssemblyName::get_Name()
	  IL_010b:  ldstr ".dll"
	  IL_0110:  call string string::Concat(string, string)
	  IL_0115:  call string class [mscorlib]System.IO.Path::Combine(string, string)
	  IL_011a:  call bool class [mscorlib]System.IO.File::Exists(string)
	  IL_011f:  brfalse IL_0148

	  IL_0124:  ldloc.s 15
	  IL_0126:  ldloc.s 12
	  IL_0128:  callvirt instance string class [mscorlib]System.Reflection.AssemblyName::get_Name()
	  IL_012d:  ldstr ".dll"
	  IL_0132:  call string string::Concat(string, string)
	  IL_0137:  call string class [mscorlib]System.IO.Path::Combine(string, string)
	  IL_013c:  call class [mscorlib]System.Reflection.Assembly class [mscorlib]System.Reflection.Assembly::LoadFrom(string)
	  IL_0141:  stloc.s 16
	  IL_0143:  br IL_0151

	  IL_0148:  ldloc.s 12
	  IL_014a:  call class [mscorlib]System.Reflection.Assembly class [mscorlib]System.Reflection.Assembly::Load(class [mscorlib]System.Reflection.AssemblyName)
	  IL_014f:  stloc.s 16
	  IL_0151:  ldloc.s 16
	  IL_0153:  ldloc.2 
	  IL_0154:  callvirt instance class [mscorlib]System.Type class [mscorlib]System.Reflection.Assembly::GetType(string)
	  IL_0159:  stloc.0 
	  IL_015a:  ldloc.0 
	  IL_015b:  ldnull 
	  IL_015c:  call bool class [mscorlib]System.Type::op_Inequality(class [mscorlib]System.Type, class [mscorlib]System.Type)
	  IL_0161:  brfalse IL_016b

	  IL_0166:  leave IL_0187

	  IL_016b:  leave IL_0176

	} // end .try 0
	catch class [mscorlib]System.Exception { // 0
	  IL_0170:  pop 
	  IL_0171:  leave IL_0176

	} // end handler 0
	IL_0176:  ldloc.s 14
	IL_0178:  ldc.i4.1 
	IL_0179:  add 
	IL_017a:  stloc.s 14
	IL_017c:  ldloc.s 14
	IL_017e:  ldloc.s 13
	IL_0180:  ldlen 
	IL_0181:  conv.i4 
	IL_0182:  blt IL_00d5

	IL_0187:  ldloc.0 
	IL_0188:  ldnull 
	IL_0189:  call bool class [mscorlib]System.Type::op_Inequality(class [mscorlib]System.Type, class [mscorlib]System.Type)
	IL_018e:  brfalse IL_0198

	IL_0193:  br IL_01a9

	IL_0198:  ldloc.s 11
	IL_019a:  ldc.i4.1 
	IL_019b:  add 
	IL_019c:  stloc.s 11
	IL_019e:  ldloc.s 11
	IL_01a0:  ldloc.s 10
	IL_01a2:  ldlen 
	IL_01a3:  conv.i4 
	IL_01a4:  blt IL_00bd

	IL_01a9:  ldarg.0 
	IL_01aa:  newobj instance void valuetype GLib.GType::'.ctor'(native int)
	IL_01af:  ldloc.0 
	IL_01b0:  call void valuetype GLib.GType::Register(valuetype GLib.GType, class [mscorlib]System.Type)
	IL_01b5:  ldloc.0 
	IL_01b6:  ret 
    } // end of method GType::LookupType

    // method line 135
    .method public static hidebysig 
           default void Init ()  cil managed 
    {
        // Method begins at RVA 0x3218
	// Code size 1 (0x1)
	.maxstack 8
	IL_0000:  ret 
    } // end of method GType::Init

    // method line 136
    .method private static hidebysig pinvokeimpl ("libgobject-2.0-0.dll" as "g_type_name" cdecl )
           default native int g_type_name (native int raw)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method GType::g_type_name

    // method line 137
    .method public virtual hidebysig 
           instance default bool Equals (object o)  cil managed 
    {
        // Method begins at RVA 0x321c
	// Code size 31 (0x1f)
	.maxstack 8
	IL_0000:  ldarg.1 
	IL_0001:  isinst GLib.GType
	IL_0006:  brtrue IL_000d

	IL_000b:  ldc.i4.0 
	IL_000c:  ret 
	IL_000d:  ldarg.1 
	IL_000e:  unbox.any GLib.GType
	IL_0013:  ldarg.0 
	IL_0014:  ldobj GLib.GType
	IL_0019:  call bool valuetype GLib.GType::op_Equality(valuetype GLib.GType, valuetype GLib.GType)
	IL_001e:  ret 
    } // end of method GType::Equals

    // method line 138
    .method public static hidebysig specialname 
           default bool op_Equality (valuetype GLib.GType a, valuetype GLib.GType b)  cil managed 
    {
        // Method begins at RVA 0x323c
	// Code size 20 (0x14)
	.maxstack 8
	IL_0000:  ldarga.s 0
	IL_0002:  call instance native int valuetype GLib.GType::get_Val()
	IL_0007:  ldarga.s 1
	IL_0009:  call instance native int valuetype GLib.GType::get_Val()
	IL_000e:  call bool native int::op_Equality(native int, native int)
	IL_0013:  ret 
    } // end of method GType::op_Equality

    // method line 139
    .method public final virtual hidebysig newslot 
           instance default bool Equals (valuetype GLib.GType other)  cil managed 
    {
        // Method begins at RVA 0x3254
	// Code size 13 (0xd)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldobj GLib.GType
	IL_0006:  ldarg.1 
	IL_0007:  call bool valuetype GLib.GType::op_Equality(valuetype GLib.GType, valuetype GLib.GType)
	IL_000c:  ret 
    } // end of method GType::Equals

    // method line 140
    .method public virtual hidebysig 
           instance default int32 GetHashCode ()  cil managed 
    {
        // Method begins at RVA 0x3264
	// Code size 18 (0x12)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldflda native int GLib.GType::val
	IL_0006:  constrained. native int
	IL_000c:  callvirt instance int32 object::GetHashCode()
	IL_0011:  ret 
    } // end of method GType::GetHashCode

    // method line 141
    .method public virtual hidebysig 
           instance default string ToString ()  cil managed 
    {
        // Method begins at RVA 0x3278
	// Code size 17 (0x11)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldfld native int GLib.GType::val
	IL_0006:  call native int valuetype GLib.GType::g_type_name(native int)
	IL_000b:  call string class GLib.Marshaller::Utf8PtrToString(native int)
	IL_0010:  ret 
    } // end of method GType::ToString

	.property instance native int Val ()
	{
		.get instance default native int GLib.GType::get_Val () 
	}
  } // end of class GLib.GType
}

.namespace GLib
{
  .class public auto ansi sealed beforefieldinit GTypeAttribute
  	extends [mscorlib]System.Attribute
  {
    .custom instance void class [mscorlib]System.ObsoleteAttribute::'.ctor'(string) =  (
		01 00 1F 55 73 65 20 47 54 79 70 65 54 79 70 65   // ...Use GTypeType
		41 74 74 72 69 62 75 74 65 20 69 6E 73 74 65 61   // Attribute instea
		64 2E 00 00                                     ) // d...

    .custom instance void class [mscorlib]System.AttributeUsageAttribute::'.ctor'(valuetype [mscorlib]System.AttributeTargets) =  (01 00 10 00 00 00 00 00 ) // ........

    .field  private  class [mscorlib]System.Type wrapper_type

    // method line 142
    .method public hidebysig specialname 
           instance default class [mscorlib]System.Type get_WrapperType ()  cil managed 
    {
        // Method begins at RVA 0x328c
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldfld class [mscorlib]System.Type GLib.GTypeAttribute::wrapper_type
	IL_0006:  ret 
    } // end of method GTypeAttribute::get_WrapperType

    // method line 143
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (class [mscorlib]System.Type wrapper_type)  cil managed 
    {
        // Method begins at RVA 0x3294
	// Code size 14 (0xe)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void class [mscorlib]System.Attribute::'.ctor'()
	IL_0006:  ldarg.0 
	IL_0007:  ldarg.1 
	IL_0008:  stfld class [mscorlib]System.Type GLib.GTypeAttribute::wrapper_type
	IL_000d:  ret 
    } // end of method GTypeAttribute::.ctor

    // method line 144
    .method public hidebysig specialname 
           instance default void set_WrapperType (class [mscorlib]System.Type 'value')  cil managed 
    {
        // Method begins at RVA 0x32a4
	// Code size 8 (0x8)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldarg.1 
	IL_0002:  stfld class [mscorlib]System.Type GLib.GTypeAttribute::wrapper_type
	IL_0007:  ret 
    } // end of method GTypeAttribute::set_WrapperType

	.property instance class [mscorlib]System.Type WrapperType ()
	{
		.get instance default class [mscorlib]System.Type GLib.GTypeAttribute::get_WrapperType () 
		.set instance default void GLib.GTypeAttribute::set_WrapperType (class [mscorlib]System.Type 'value') 
	}
  } // end of class GLib.GTypeAttribute
}

.namespace GLib
{
  .class public auto ansi abstract beforefieldinit GTypeTypeAttribute
  	extends [mscorlib]System.Attribute
  {
    .custom instance void class [mscorlib]System.AttributeUsageAttribute::'.ctor'(valuetype [mscorlib]System.AttributeTargets) =  (
		01 00 1C 00 00 00 01 00 54 02 09 49 6E 68 65 72   // ........T..Inher
		69 74 65 64 00                                  ) // ited.


    // method line 145
    .method public virtual hidebysig newslot abstract specialname 
           instance default valuetype GLib.GType get_Type ()  cil managed 
    {
        // Method begins at RVA 0x0
    } // end of method GTypeTypeAttribute::get_Type

    // method line 146
    .method family hidebysig specialname rtspecialname 
           instance default void '.ctor' ()  cil managed 
    {
        // Method begins at RVA 0x32b0
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void class [mscorlib]System.Attribute::'.ctor'()
	IL_0006:  ret 
    } // end of method GTypeTypeAttribute::.ctor

	.property instance valuetype GLib.GType Type ()
	{
		.get instance default valuetype GLib.GType GLib.GTypeTypeAttribute::get_Type () 
	}
  } // end of class GLib.GTypeTypeAttribute
}

.namespace GLib
{
  .class public auto ansi sealed beforefieldinit GTypeOpaqueAttribute
  	extends GLib.GTypeTypeAttribute
  {

    // method line 147
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' ()  cil managed 
    {
        // Method begins at RVA 0x32b8
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void class GLib.GTypeTypeAttribute::'.ctor'()
	IL_0006:  ret 
    } // end of method GTypeOpaqueAttribute::.ctor

    // method line 148
    .method public virtual hidebysig specialname 
           instance default valuetype GLib.GType get_Type ()  cil managed 
    {
        // Method begins at RVA 0x32c0
	// Code size 6 (0x6)
	.maxstack 8
	IL_0000:  ldsfld valuetype GLib.GType GLib.GType::Pointer
	IL_0005:  ret 
    } // end of method GTypeOpaqueAttribute::get_Type

	.property instance valuetype GLib.GType Type ()
	{
		.get instance default valuetype GLib.GType GLib.GTypeOpaqueAttribute::get_Type () 
	}
  } // end of class GLib.GTypeOpaqueAttribute
}

.namespace GLib
{
  .class public auto ansi sealed beforefieldinit GTypeObjectAttribute
  	extends GLib.GTypeTypeAttribute
  {

    // method line 149
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' ()  cil managed 
    {
        // Method begins at RVA 0x32c8
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void class GLib.GTypeTypeAttribute::'.ctor'()
	IL_0006:  ret 
    } // end of method GTypeObjectAttribute::.ctor

    // method line 150
    .method public virtual hidebysig specialname 
           instance default valuetype GLib.GType get_Type ()  cil managed 
    {
        // Method begins at RVA 0x32d0
	// Code size 6 (0x6)
	.maxstack 8
	IL_0000:  ldsfld valuetype GLib.GType GLib.GType::Object
	IL_0005:  ret 
    } // end of method GTypeObjectAttribute::get_Type

	.property instance valuetype GLib.GType Type ()
	{
		.get instance default valuetype GLib.GType GLib.GTypeObjectAttribute::get_Type () 
	}
  } // end of class GLib.GTypeObjectAttribute
}

.namespace GLib
{
  .class public auto ansi sealed beforefieldinit GTypeStructAttribute
  	extends GLib.GTypeTypeAttribute
  {

    // method line 151
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' ()  cil managed 
    {
        // Method begins at RVA 0x32d8
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void class GLib.GTypeTypeAttribute::'.ctor'()
	IL_0006:  ret 
    } // end of method GTypeStructAttribute::.ctor

    // method line 152
    .method public virtual hidebysig specialname 
           instance default valuetype GLib.GType get_Type ()  cil managed 
    {
        // Method begins at RVA 0x32e0
	// Code size 6 (0x6)
	.maxstack 8
	IL_0000:  ldsfld valuetype GLib.GType GLib.GType::Pointer
	IL_0005:  ret 
    } // end of method GTypeStructAttribute::get_Type

	.property instance valuetype GLib.GType Type ()
	{
		.get instance default valuetype GLib.GType GLib.GTypeStructAttribute::get_Type () 
	}
  } // end of class GLib.GTypeStructAttribute
}

.namespace GLib
{
  .class public auto ansi sealed IdleHandler
  	extends [mscorlib]System.MulticastDelegate
  {

    // method line 153
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (object 'object', native int 'method')  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method IdleHandler::.ctor

    // method line 154
    .method public virtual hidebysig newslot 
           instance default bool Invoke ()  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method IdleHandler::Invoke

    // method line 155
    .method public virtual hidebysig newslot 
           instance default class [mscorlib]System.IAsyncResult BeginInvoke (class [mscorlib]System.AsyncCallback callback, object 'object')  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method IdleHandler::BeginInvoke

    // method line 156
    .method public virtual hidebysig newslot 
           instance default bool EndInvoke (class [mscorlib]System.IAsyncResult result)  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method IdleHandler::EndInvoke

  } // end of class GLib.IdleHandler
}

.namespace GLib
{
  .class public auto ansi beforefieldinit Idle
  	extends [mscorlib]System.Object
  {

    // method line 157
    .method private hidebysig specialname rtspecialname 
           instance default void '.ctor' ()  cil managed 
    {
        // Method begins at RVA 0x32e8
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::'.ctor'()
	IL_0006:  ret 
    } // end of method Idle::.ctor

    // method line 158
    .method private static hidebysig pinvokeimpl ("libglib-2.0-0.dll" as "g_idle_add" cdecl )
           default unsigned int32 g_idle_add (class GLib.SourceProxy/GSourceFuncInternal d, native int data)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method Idle::g_idle_add

    // method line 159
    .method public static hidebysig 
           default unsigned int32 Add (class GLib.IdleHandler hndlr)  cil managed 
    {
        // Method begins at RVA 0x32f0
	// Code size 42 (0x2a)
	.maxstack 3
	.locals init (
		class GLib.Idle/IdleProxy	V_0)
	IL_0000:  ldarg.0 
	IL_0001:  newobj instance void class GLib.Idle/IdleProxy::'.ctor'(class GLib.IdleHandler)
	IL_0006:  stloc.0 
	IL_0007:  ldloc.0 
	IL_0008:  ldloc.0 
	IL_0009:  ldfld class GLib.SourceProxy/GSourceFuncInternal GLib.SourceProxy::Handler
	IL_000e:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_0013:  call unsigned int32 class GLib.Idle::g_idle_add(class GLib.SourceProxy/GSourceFuncInternal, native int)
	IL_0018:  stfld unsigned int32 GLib.SourceProxy::ID
	IL_001d:  ldloc.0 
	IL_001e:  call void class GLib.Source::Add(class GLib.SourceProxy)
	IL_0023:  ldloc.0 
	IL_0024:  ldfld unsigned int32 GLib.SourceProxy::ID
	IL_0029:  ret 
    } // end of method Idle::Add

    // method line 160
    .method private static hidebysig pinvokeimpl ("libglib-2.0-0.dll" as "g_source_remove" cdecl )
           default bool g_source_remove (unsigned int32 id)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method Idle::g_source_remove

    // method line 161
    .method public static hidebysig 
           default bool Remove (class GLib.IdleHandler hndlr)  cil managed 
    {
        // Method begins at RVA 0x3328
	// Code size 154 (0x9a)
	.maxstack 2
	.locals init (
		bool	V_0,
		object	V_1,
		bool	V_2,
		valuetype [mscorlib]System.Collections.Generic.KeyValuePair`2<unsigned int32, class GLib.SourceProxy>	V_3,
		valuetype [mscorlib]System.Collections.Generic.Dictionary`2/Enumerator<unsigned int32, class GLib.SourceProxy>	V_4,
		unsigned int32	V_5,
		class GLib.Idle/IdleProxy	V_6)
	IL_0000:  ldc.i4.0 
	IL_0001:  stloc.0 
	IL_0002:  ldsfld class [mscorlib]System.Collections.Generic.Dictionary`2<unsigned int32,class GLib.SourceProxy> GLib.Source::source_handlers
	IL_0007:  stloc.1 
	IL_0008:  ldc.i4.0 
	IL_0009:  stloc.2 
	.try { // 1
	  IL_000a:  ldloc.1 
	  IL_000b:  ldloca.s 2
	  IL_000d:  call void class [mscorlib]System.Threading.Monitor::Enter(object, [out] bool&)
	  IL_0012:  ldsfld class [mscorlib]System.Collections.Generic.Dictionary`2<unsigned int32,class GLib.SourceProxy> GLib.Source::source_handlers
	  IL_0017:  callvirt instance valuetype [mscorlib]System.Collections.Generic.Dictionary`2/Enumerator<!0,!1> class [mscorlib]System.Collections.Generic.Dictionary`2<unsigned int32, class GLib.SourceProxy>::GetEnumerator()
	  IL_001c:  stloc.s 4
	  .try { // 0
	    IL_001e:  br IL_006a

	    IL_0023:  ldloca.s 4
	    IL_0025:  call instance valuetype [mscorlib]System.Collections.Generic.KeyValuePair`2<!0,!1> valuetype [mscorlib]System.Collections.Generic.Dictionary`2/Enumerator<unsigned int32, class GLib.SourceProxy>::get_Current()
	    IL_002a:  stloc.3 
	    IL_002b:  ldloca.s 3
	    IL_002d:  call instance !0 valuetype [mscorlib]System.Collections.Generic.KeyValuePair`2<unsigned int32, class GLib.SourceProxy>::get_Key()
	    IL_0032:  stloc.s 5
	    IL_0034:  ldloca.s 3
	    IL_0036:  call instance !1 valuetype [mscorlib]System.Collections.Generic.KeyValuePair`2<unsigned int32, class GLib.SourceProxy>::get_Value()
	    IL_003b:  isinst GLib.Idle/IdleProxy
	    IL_0040:  stloc.s 6
	    IL_0042:  ldloc.s 6
	    IL_0044:  brfalse IL_006a

	    IL_0049:  ldloc.s 6
	    IL_004b:  ldfld class GLib.IdleHandler GLib.Idle/IdleProxy::real_handler
	    IL_0050:  ldarg.0 
	    IL_0051:  call bool class [mscorlib]System.Delegate::op_Equality(class [mscorlib]System.Delegate, class [mscorlib]System.Delegate)
	    IL_0056:  brfalse IL_006a

	    IL_005b:  ldloc.s 5
	    IL_005d:  call bool class GLib.Idle::g_source_remove(unsigned int32)
	    IL_0062:  stloc.0 
	    IL_0063:  ldloc.s 6
	    IL_0065:  callvirt instance void class GLib.SourceProxy::Remove()
	    IL_006a:  ldloca.s 4
	    IL_006c:  call instance bool valuetype [mscorlib]System.Collections.Generic.Dictionary`2/Enumerator<unsigned int32, class GLib.SourceProxy>::MoveNext()
	    IL_0071:  brtrue IL_0023

	    IL_0076:  leave IL_0089

	  } // end .try 0
	  finally  { // 0
	    IL_007b:  ldloca.s 4
	    IL_007d:  constrained. valuetype [mscorlib]System.Collections.Generic.Dictionary`2/Enumerator<unsigned int32,class GLib.SourceProxy>
	    IL_0083:  callvirt instance void class [mscorlib]System.IDisposable::Dispose()
	    IL_0088:  endfinally 
	  } // end handler 0
	  IL_0089:  leave IL_0098

	} // end .try 1
	finally  { // 1
	  IL_008e:  ldloc.2 
	  IL_008f:  brfalse.s IL_0097

	  IL_0091:  ldloc.1 
	  IL_0092:  call void class [mscorlib]System.Threading.Monitor::Exit(object)
	  IL_0097:  endfinally 
	} // end handler 1
	IL_0098:  ldloc.0 
	IL_0099:  ret 
    } // end of method Idle::Remove

  .class nested assembly auto ansi beforefieldinit IdleProxy
  	extends GLib.SourceProxy
  {
    .field  assembly initonly  class GLib.IdleHandler real_handler

    // method line 166
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (class GLib.IdleHandler real)  cil managed 
    {
        // Method begins at RVA 0x34bc
	// Code size 14 (0xe)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void class GLib.SourceProxy::'.ctor'()
	IL_0006:  ldarg.0 
	IL_0007:  ldarg.1 
	IL_0008:  stfld class GLib.IdleHandler GLib.Idle/IdleProxy::real_handler
	IL_000d:  ret 
    } // end of method IdleProxy::.ctor

    // method line 167
    .method family virtual hidebysig 
           instance default bool Invoke (native int data)  cil managed 
    {
        // Method begins at RVA 0x34cc
	// Code size 12 (0xc)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldfld class GLib.IdleHandler GLib.Idle/IdleProxy::real_handler
	IL_0006:  callvirt instance bool class GLib.IdleHandler::Invoke()
	IL_000b:  ret 
    } // end of method IdleProxy::Invoke

  } // end of class IdleProxy

  } // end of class GLib.Idle
}

.namespace GLib
{
  .class private auto ansi abstract beforefieldinit SourceProxy
  	extends [mscorlib]System.Object
  {
    .field  assembly  unsigned int32 ID
    .field  assembly initonly  class GLib.SourceProxy/GSourceFuncInternal Handler
    .field  assembly  bool needsAdd

    // method line 162
    .method public hidebysig 
           instance default void Remove ()  cil managed 
    {
        // Method begins at RVA 0x33ec
	// Code size 72 (0x48)
	.maxstack 2
	.locals init (
		object	V_0,
		bool	V_1)
	IL_0000:  ldsfld class [mscorlib]System.Collections.Generic.Dictionary`2<unsigned int32,class GLib.SourceProxy> GLib.Source::source_handlers
	IL_0005:  stloc.0 
	IL_0006:  ldc.i4.0 
	IL_0007:  stloc.1 
	.try { // 0
	  IL_0008:  ldloc.0 
	  IL_0009:  ldloca.s 1
	  IL_000b:  call void class [mscorlib]System.Threading.Monitor::Enter(object, [out] bool&)
	  IL_0010:  ldarg.0 
	  IL_0011:  ldfld bool GLib.SourceProxy::needsAdd
	  IL_0016:  brfalse IL_0027

	  IL_001b:  ldarg.0 
	  IL_001c:  ldc.i4.0 
	  IL_001d:  stfld bool GLib.SourceProxy::needsAdd
	  IL_0022:  br IL_0038

	  IL_0027:  ldsfld class [mscorlib]System.Collections.Generic.Dictionary`2<unsigned int32,class GLib.SourceProxy> GLib.Source::source_handlers
	  IL_002c:  ldarg.0 
	  IL_002d:  ldfld unsigned int32 GLib.SourceProxy::ID
	  IL_0032:  callvirt instance bool class [mscorlib]System.Collections.Generic.Dictionary`2<unsigned int32, class GLib.SourceProxy>::Remove(!0)
	  IL_0037:  pop 
	  IL_0038:  leave IL_0047

	} // end .try 0
	finally  { // 0
	  IL_003d:  ldloc.1 
	  IL_003e:  brfalse.s IL_0046

	  IL_0040:  ldloc.0 
	  IL_0041:  call void class [mscorlib]System.Threading.Monitor::Exit(object)
	  IL_0046:  endfinally 
	} // end handler 0
	IL_0047:  ret 
    } // end of method SourceProxy::Remove

    // method line 163
    .method family hidebysig specialname rtspecialname 
           instance default void '.ctor' ()  cil managed 
    {
        // Method begins at RVA 0x3450
	// Code size 32 (0x20)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldc.i4.1 
	IL_0002:  stfld bool GLib.SourceProxy::needsAdd
	IL_0007:  ldarg.0 
	IL_0008:  call instance void object::'.ctor'()
	IL_000d:  ldarg.0 
	IL_000e:  ldarg.0 
	IL_000f:  ldftn instance bool class GLib.SourceProxy::HandlerInternal(native int)
	IL_0015:  newobj instance void class GLib.SourceProxy/GSourceFuncInternal::'.ctor'(object, native int)
	IL_001a:  stfld class GLib.SourceProxy/GSourceFuncInternal GLib.SourceProxy::Handler
	IL_001f:  ret 
    } // end of method SourceProxy::.ctor

    // method line 164
    .method private hidebysig 
           instance default bool HandlerInternal (native int data)  cil managed 
    {
        // Method begins at RVA 0x3474
	// Code size 44 (0x2c)
	.maxstack 2
	.locals init (
		bool	V_0,
		bool	V_1,
		class [mscorlib]System.Exception	V_2)
	.try { // 0
	  IL_0000:  ldarg.0 
	  IL_0001:  ldarg.1 
	  IL_0002:  callvirt instance bool class GLib.SourceProxy::Invoke(native int)
	  IL_0007:  stloc.0 
	  IL_0008:  ldloc.0 
	  IL_0009:  brtrue IL_0014

	  IL_000e:  ldarg.0 
	  IL_000f:  call instance void class GLib.SourceProxy::Remove()
	  IL_0014:  ldloc.0 
	  IL_0015:  stloc.1 
	  IL_0016:  leave IL_002a

	} // end .try 0
	catch class [mscorlib]System.Exception { // 0
	  IL_001b:  stloc.2 
	  IL_001c:  ldloc.2 
	  IL_001d:  ldc.i4.0 
	  IL_001e:  call void class GLib.ExceptionManager::RaiseUnhandledException(class [mscorlib]System.Exception, bool)
	  IL_0023:  leave IL_0028

	} // end handler 0
	IL_0028:  ldc.i4.0 
	IL_0029:  ret 
	IL_002a:  ldloc.1 
	IL_002b:  ret 
    } // end of method SourceProxy::HandlerInternal

    // method line 165
    .method family virtual hidebysig newslot abstract 
           instance default bool Invoke (native int data)  cil managed 
    {
        // Method begins at RVA 0x0
    } // end of method SourceProxy::Invoke

  .class nested assembly auto ansi sealed GSourceFuncInternal
  	extends [mscorlib]System.MulticastDelegate
  {
    .custom instance void class [mscorlib]System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute::'.ctor'(valuetype [mscorlib]System.Runtime.InteropServices.CallingConvention) =  (01 00 02 00 00 00 00 00 ) // ........


    // method line 731
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (object 'object', native int 'method')  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method GSourceFuncInternal::.ctor

    // method line 732
    .method public virtual hidebysig newslot 
           instance default bool Invoke (native int ptr)  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method GSourceFuncInternal::Invoke

    // method line 733
    .method public virtual hidebysig newslot 
           instance default class [mscorlib]System.IAsyncResult BeginInvoke (native int ptr, class [mscorlib]System.AsyncCallback callback, object 'object')  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method GSourceFuncInternal::BeginInvoke

    // method line 734
    .method public virtual hidebysig newslot 
           instance default bool EndInvoke (class [mscorlib]System.IAsyncResult result)  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method GSourceFuncInternal::EndInvoke

  } // end of class GSourceFuncInternal

  } // end of class GLib.SourceProxy
}

.namespace GLib
{
  .class public auto ansi sealed beforefieldinit IgnoreClassInitializersAttribute
  	extends [mscorlib]System.Attribute
  {
    .custom instance void class [mscorlib]System.AttributeUsageAttribute::'.ctor'(valuetype [mscorlib]System.AttributeTargets) =  (01 00 01 00 00 00 00 00 ) // ........


    // method line 168
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' ()  cil managed 
    {
        // Method begins at RVA 0x34dc
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void class [mscorlib]System.Attribute::'.ctor'()
	IL_0006:  ret 
    } // end of method IgnoreClassInitializersAttribute::.ctor

  } // end of class GLib.IgnoreClassInitializersAttribute
}

.namespace GLib
{
  .class public auto ansi Object
  	extends [mscorlib]System.Object
  	implements GLib.IWrapper, [mscorlib]System.IDisposable  {
    .field  private  native int handle
    .field  private  class GLib.ToggleRef tref
    .field  famorassem  bool owned
    .field  private  class [mscorlib]System.Collections.Hashtable data
    .field  private static  class [mscorlib]System.Collections.Generic.Dictionary`2<native int,class GLib.ToggleRef> Objects
    .field  private static  object lockObject
    .field  private static  class [mscorlib]System.Collections.Generic.List`1<class GLib.ToggleRef> PendingDestroys
    .field  private static  bool idle_queued
    .field  private static  class [mscorlib]System.Collections.Generic.Dictionary`2<native int,class [mscorlib]System.Reflection.PropertyInfo> properties
    .field  private static  class GLib.Object/GetPropertyDelegate get_property_handler
    .field  private static  class GLib.Object/SetPropertyDelegate set_property_handler
    .field  private static  int32 type_uid
    .field  private static  class [mscorlib]System.Collections.Generic.Dictionary`2<class [mscorlib]System.Type,valuetype GLib.GType> g_types
    .field  private  class [mscorlib]System.Collections.Hashtable before_signals
    .field  private  class [mscorlib]System.Collections.Hashtable after_signals
    .field  private  class [System]System.ComponentModel.EventHandlerList before_handlers
    .field  private  class [System]System.ComponentModel.EventHandlerList after_handlers
    .field  private  class [mscorlib]System.Collections.Hashtable persistent_data
    .field  private static  class GLib.TimeoutHandler '<>f__mg$cache0'
    .custom instance void class [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::'.ctor'() =  (01 00 00 00 ) // ....


    // method line 169
    .method public final virtual hidebysig newslot specialname 
           instance default native int get_Handle ()  cil managed 
    {
        // Method begins at RVA 0x34e4
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldfld native int GLib.Object::handle
	IL_0006:  ret 
    } // end of method Object::get_Handle

    // method line 170
    .method famorassem static hidebysig 
           default valuetype GLib.GType LookupGType (class [mscorlib]System.Type t)  cil managed 
    {
        // Method begins at RVA 0x34ec
	// Code size 134 (0x86)
	.maxstack 3
	.locals init (
		valuetype GLib.GType	V_0,
		class GLib.GTypeTypeAttribute	V_1,
		valuetype GLib.GType	V_2,
		class [mscorlib]System.Reflection.PropertyInfo	V_3,
		valuetype GLib.GType	V_4)
	IL_0000:  ldsfld class [mscorlib]System.Collections.Generic.Dictionary`2<class [mscorlib]System.Type,valuetype GLib.GType> GLib.Object::g_types
	IL_0005:  ldarg.0 
	IL_0006:  ldloca.s 0
	IL_0008:  callvirt instance bool class [mscorlib]System.Collections.Generic.Dictionary`2<class [mscorlib]System.Type, valuetype GLib.GType>::TryGetValue(!0, [out] !1&)
	IL_000d:  brfalse IL_0014

	IL_0012:  ldloc.0 
	IL_0013:  ret 
	IL_0014:  ldarg.0 
	IL_0015:  ldtoken GLib.GTypeTypeAttribute
	IL_001a:  call class [mscorlib]System.Type class [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
	IL_001f:  ldc.i4.0 
	IL_0020:  call class [mscorlib]System.Attribute class [mscorlib]System.Attribute::GetCustomAttribute(class [mscorlib]System.Reflection.MemberInfo, class [mscorlib]System.Type, bool)
	IL_0025:  castclass GLib.GTypeTypeAttribute
	IL_002a:  dup 
	IL_002b:  stloc.1 
	IL_002c:  brfalse IL_0046

	IL_0031:  ldloc.1 
	IL_0032:  callvirt instance valuetype GLib.GType class GLib.GTypeTypeAttribute::get_Type()
	IL_0037:  stloc.2 
	IL_0038:  ldsfld class [mscorlib]System.Collections.Generic.Dictionary`2<class [mscorlib]System.Type,valuetype GLib.GType> GLib.Object::g_types
	IL_003d:  ldarg.0 
	IL_003e:  ldloc.2 
	IL_003f:  callvirt instance void class [mscorlib]System.Collections.Generic.Dictionary`2<class [mscorlib]System.Type, valuetype GLib.GType>::set_Item(!0, !1)
	IL_0044:  ldloc.2 
	IL_0045:  ret 
	IL_0046:  ldarg.0 
	IL_0047:  ldstr "GType"
	IL_004c:  ldc.i4.s 0x1a
	IL_004e:  callvirt instance class [mscorlib]System.Reflection.PropertyInfo class [mscorlib]System.Type::GetProperty(string, valuetype [mscorlib]System.Reflection.BindingFlags)
	IL_0053:  stloc.3 
	IL_0054:  ldloc.3 
	IL_0055:  ldnull 
	IL_0056:  call bool class [mscorlib]System.Reflection.PropertyInfo::op_Inequality(class [mscorlib]System.Reflection.PropertyInfo, class [mscorlib]System.Reflection.PropertyInfo)
	IL_005b:  brfalse IL_007f

	IL_0060:  ldloc.3 
	IL_0061:  ldnull 
	IL_0062:  ldnull 
	IL_0063:  callvirt instance object class [mscorlib]System.Reflection.PropertyInfo::GetValue(object, object[])
	IL_0068:  unbox.any GLib.GType
	IL_006d:  stloc.s 4
	IL_006f:  ldsfld class [mscorlib]System.Collections.Generic.Dictionary`2<class [mscorlib]System.Type,valuetype GLib.GType> GLib.Object::g_types
	IL_0074:  ldarg.0 
	IL_0075:  ldloc.s 4
	IL_0077:  callvirt instance void class [mscorlib]System.Collections.Generic.Dictionary`2<class [mscorlib]System.Type, valuetype GLib.GType>::set_Item(!0, !1)
	IL_007c:  ldloc.s 4
	IL_007e:  ret 
	IL_007f:  ldarg.0 
	IL_0080:  call valuetype GLib.GType class GLib.Object::RegisterGType(class [mscorlib]System.Type)
	IL_0085:  ret 
    } // end of method Object::LookupGType

    // method line 171
    .method family hidebysig specialname rtspecialname 
           instance default void '.ctor' (native int raw)  cil managed 
    {
        // Method begins at RVA 0x3580
	// Code size 21 (0x15)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldc.i4.0 
	IL_0002:  stfld bool GLib.Object::owned
	IL_0007:  ldarg.0 
	IL_0008:  call instance void object::'.ctor'()
	IL_000d:  ldarg.0 
	IL_000e:  ldarg.1 
	IL_000f:  callvirt instance void class GLib.Object::set_Raw(native int)
	IL_0014:  ret 
    } // end of method Object::.ctor

    // method line 172
    .method family virtual hidebysig newslot specialname 
           instance default void set_Raw (native int 'value')  cil managed 
    {
        // Method begins at RVA 0x3598
	// Code size 169 (0xa9)
	.maxstack 3
	.locals init (
		object	V_0,
		bool	V_1)
	IL_0000:  ldarg.0 
	IL_0001:  ldfld native int GLib.Object::handle
	IL_0006:  ldarg.1 
	IL_0007:  call bool native int::op_Equality(native int, native int)
	IL_000c:  brfalse IL_0012

	IL_0011:  ret 
	IL_0012:  ldsfld class [mscorlib]System.Collections.Generic.Dictionary`2<native int,class GLib.ToggleRef> GLib.Object::Objects
	IL_0017:  stloc.0 
	IL_0018:  ldc.i4.0 
	IL_0019:  stloc.1 
	.try { // 0
	  IL_001a:  ldloc.0 
	  IL_001b:  ldloca.s 1
	  IL_001d:  call void class [mscorlib]System.Threading.Monitor::Enter(object, [out] bool&)
	  IL_0022:  ldarg.0 
	  IL_0023:  ldfld native int GLib.Object::handle
	  IL_0028:  ldsfld native int [mscorlib]System.IntPtr::Zero
	  IL_002d:  call bool native int::op_Inequality(native int, native int)
	  IL_0032:  brfalse IL_0065

	  IL_0037:  ldsfld class [mscorlib]System.Collections.Generic.Dictionary`2<native int,class GLib.ToggleRef> GLib.Object::Objects
	  IL_003c:  ldarg.0 
	  IL_003d:  ldfld native int GLib.Object::handle
	  IL_0042:  callvirt instance bool class [mscorlib]System.Collections.Generic.Dictionary`2<native int, class GLib.ToggleRef>::Remove(!0)
	  IL_0047:  pop 
	  IL_0048:  ldarg.0 
	  IL_0049:  ldfld class GLib.ToggleRef GLib.Object::tref
	  IL_004e:  brfalse IL_0065

	  IL_0053:  ldarg.0 
	  IL_0054:  ldfld class GLib.ToggleRef GLib.Object::tref
	  IL_0059:  callvirt instance void class GLib.ToggleRef::Free()
	  IL_005e:  ldarg.0 
	  IL_005f:  ldnull 
	  IL_0060:  stfld class GLib.ToggleRef GLib.Object::tref
	  IL_0065:  ldarg.0 
	  IL_0066:  ldarg.1 
	  IL_0067:  stfld native int GLib.Object::handle
	  IL_006c:  ldarg.1 
	  IL_006d:  ldsfld native int [mscorlib]System.IntPtr::Zero
	  IL_0072:  call bool native int::op_Inequality(native int, native int)
	  IL_0077:  brfalse IL_0099

	  IL_007c:  ldarg.0 
	  IL_007d:  ldarg.0 
	  IL_007e:  newobj instance void class GLib.ToggleRef::'.ctor'(class GLib.Object)
	  IL_0083:  stfld class GLib.ToggleRef GLib.Object::tref
	  IL_0088:  ldsfld class [mscorlib]System.Collections.Generic.Dictionary`2<native int,class GLib.ToggleRef> GLib.Object::Objects
	  IL_008d:  ldarg.1 
	  IL_008e:  ldarg.0 
	  IL_008f:  ldfld class GLib.ToggleRef GLib.Object::tref
	  IL_0094:  callvirt instance void class [mscorlib]System.Collections.Generic.Dictionary`2<native int, class GLib.ToggleRef>::set_Item(!0, !1)
	  IL_0099:  leave IL_00a8

	} // end .try 0
	finally  { // 0
	  IL_009e:  ldloc.1 
	  IL_009f:  brfalse.s IL_00a7

	  IL_00a1:  ldloc.0 
	  IL_00a2:  call void class [mscorlib]System.Threading.Monitor::Exit(object)
	  IL_00a7:  endfinally 
	} // end handler 0
	IL_00a8:  ret 
    } // end of method Object::set_Raw

    // method line 173
    .method family hidebysig specialname rtspecialname 
           instance default void '.ctor' ()  cil managed 
    {
        // Method begins at RVA 0x3660
	// Code size 32 (0x20)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldc.i4.0 
	IL_0002:  stfld bool GLib.Object::owned
	IL_0007:  ldarg.0 
	IL_0008:  call instance void object::'.ctor'()
	IL_000d:  ldarg.0 
	IL_000e:  ldc.i4.0 
	IL_000f:  newarr string
	IL_0014:  ldc.i4.0 
	IL_0015:  newarr GLib.Value
	IL_001a:  callvirt instance void class GLib.Object::CreateNativeObject(string[], valuetype GLib.Value[])
	IL_001f:  ret 
    } // end of method Object::.ctor

    // method line 174
    .method family virtual hidebysig newslot 
           instance default void CreateNativeObject (string[] names, valuetype GLib.Value[] vals)  cil managed 
    {
        // Method begins at RVA 0x3684
	// Code size 12 (0xc)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldarg.1 
	IL_0002:  ldarg.2 
	IL_0003:  ldarg.1 
	IL_0004:  ldlen 
	IL_0005:  conv.i4 
	IL_0006:  call instance void class GLib.Object::CreateNativeObject(string[], valuetype GLib.Value[], int32)
	IL_000b:  ret 
    } // end of method Object::CreateNativeObject

    // method line 175
    .method family hidebysig specialname rtspecialname 
           instance default void '.ctor' (valuetype GLib.GType gtype)  cil managed 
    {
        .custom instance void class [mscorlib]System.ObsoleteAttribute::'.ctor'() =  (01 00 00 00 ) // ....

        // Method begins at RVA 0x3694
	// Code size 37 (0x25)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldc.i4.0 
	IL_0002:  stfld bool GLib.Object::owned
	IL_0007:  ldarg.0 
	IL_0008:  call instance void object::'.ctor'()
	IL_000d:  ldarg.0 
	IL_000e:  ldarga.s 1
	IL_0010:  call instance native int valuetype GLib.GType::get_Val()
	IL_0015:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_001a:  call native int class GLib.Object::g_object_new(native int, native int)
	IL_001f:  callvirt instance void class GLib.Object::set_Raw(native int)
	IL_0024:  ret 
    } // end of method Object::.ctor

    // method line 176
    .method private static hidebysig pinvokeimpl ("libgobject-2.0-0.dll" as "g_object_new" cdecl )
           default native int g_object_new (native int gtype, native int dummy)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method Object::g_object_new

    // method line 177
    .method private static hidebysig specialname rtspecialname 
           default void '.cctor' ()  cil managed 
    {
        // Method begins at RVA 0x36bc
	// Code size 89 (0x59)
	.maxstack 4
	IL_0000:  ldsfld class [mscorlib]System.Collections.Generic.IEqualityComparer`1<native int> GLib.IntPtrEqualityComparer::Instance
	IL_0005:  newobj instance void class [mscorlib]System.Collections.Generic.Dictionary`2<native int, class GLib.ToggleRef>::'.ctor'(class [mscorlib]System.Collections.Generic.IEqualityComparer`1<!0>)
	IL_000a:  stsfld class [mscorlib]System.Collections.Generic.Dictionary`2<native int,class GLib.ToggleRef> GLib.Object::Objects
	IL_000f:  newobj instance void object::'.ctor'()
	IL_0014:  stsfld object GLib.Object::lockObject
	IL_0019:  newobj instance void class [mscorlib]System.Collections.Generic.List`1<class GLib.ToggleRef>::'.ctor'()
	IL_001e:  stsfld class [mscorlib]System.Collections.Generic.List`1<class GLib.ToggleRef> GLib.Object::PendingDestroys
	IL_0023:  newobj instance void class [mscorlib]System.Collections.Generic.Dictionary`2<class [mscorlib]System.Type, valuetype GLib.GType>::'.ctor'()
	IL_0028:  stsfld class [mscorlib]System.Collections.Generic.Dictionary`2<class [mscorlib]System.Type,valuetype GLib.GType> GLib.Object::g_types
	IL_002d:  ldstr "GTK_SHARP_DEBUG"
	IL_0032:  call string class [mscorlib]System.Environment::GetEnvironmentVariable(string)
	IL_0037:  brfalse IL_0058

	IL_003c:  ldstr "GLib-GObject"
	IL_0041:  ldc.i4 255
	IL_0046:  ldnull 
	IL_0047:  ldftn void class GLib.Log::PrintTraceLogFunction(string, valuetype GLib.LogLevelFlags, string)
	IL_004d:  newobj instance void class GLib.LogFunc::'.ctor'(object, native int)
	IL_0052:  call unsigned int32 class GLib.Log::SetLogHandler(string, valuetype GLib.LogLevelFlags, class GLib.LogFunc)
	IL_0057:  pop 
	IL_0058:  ret 
    } // end of method Object::.cctor

    // method line 178
    .method family virtual hidebysig 
           instance default void Finalize ()  cil managed 
    {
        // Method begins at RVA 0x3724
	// Code size 172 (0xac)
	.maxstack 3
	.locals init (
		object	V_0,
		bool	V_1,
		object	V_2,
		bool	V_3)
	.try { // 2
	  IL_0000:  ldsfld object GLib.Object::lockObject
	  IL_0005:  stloc.0 
	  IL_0006:  ldc.i4.0 
	  IL_0007:  stloc.1 
	  .try { // 1
	    IL_0008:  ldloc.0 
	    IL_0009:  ldloca.s 1
	    IL_000b:  call void class [mscorlib]System.Threading.Monitor::Enter(object, [out] bool&)
	    IL_0010:  ldsfld class [mscorlib]System.Collections.Generic.Dictionary`2<native int,class GLib.ToggleRef> GLib.Object::Objects
	    IL_0015:  stloc.2 
	    IL_0016:  ldc.i4.0 
	    IL_0017:  stloc.3 
	    .try { // 0
	      IL_0018:  ldloc.2 
	      IL_0019:  ldloca.s 3
	      IL_001b:  call void class [mscorlib]System.Threading.Monitor::Enter(object, [out] bool&)
	      IL_0020:  ldarg.0 
	      IL_0021:  ldfld class GLib.ToggleRef GLib.Object::tref
	      IL_0026:  brfalse IL_003b

	      IL_002b:  ldsfld class [mscorlib]System.Collections.Generic.List`1<class GLib.ToggleRef> GLib.Object::PendingDestroys
	      IL_0030:  ldarg.0 
	      IL_0031:  ldfld class GLib.ToggleRef GLib.Object::tref
	      IL_0036:  callvirt instance void class [mscorlib]System.Collections.Generic.List`1<class GLib.ToggleRef>::Add(!0)
	      IL_003b:  ldsfld class [mscorlib]System.Collections.Generic.Dictionary`2<native int,class GLib.ToggleRef> GLib.Object::Objects
	      IL_0040:  ldarg.0 
	      IL_0041:  call instance native int class GLib.Object::get_Handle()
	      IL_0046:  callvirt instance bool class [mscorlib]System.Collections.Generic.Dictionary`2<native int, class GLib.ToggleRef>::Remove(!0)
	      IL_004b:  pop 
	      IL_004c:  leave IL_005b

	    } // end .try 0
	    finally  { // 0
	      IL_0051:  ldloc.3 
	      IL_0052:  brfalse.s IL_005a

	      IL_0054:  ldloc.2 
	      IL_0055:  call void class [mscorlib]System.Threading.Monitor::Exit(object)
	      IL_005a:  endfinally 
	    } // end handler 0
	    IL_005b:  ldsfld bool GLib.Object::idle_queued
	    IL_0060:  brtrue IL_0090

	    IL_0065:  ldc.i4.s 0x32
	    IL_0067:  ldsfld class GLib.TimeoutHandler GLib.Object::'<>f__mg$cache0'
	    IL_006c:  brtrue.s IL_007f

	    IL_006e:  ldnull 
	    IL_006f:  ldftn bool class GLib.Object::PerformQueuedUnrefs()
	    IL_0075:  newobj instance void class GLib.TimeoutHandler::'.ctor'(object, native int)
	    IL_007a:  stsfld class GLib.TimeoutHandler GLib.Object::'<>f__mg$cache0'
	    IL_007f:  ldsfld class GLib.TimeoutHandler GLib.Object::'<>f__mg$cache0'
	    IL_0084:  call unsigned int32 class GLib.Timeout::Add(unsigned int32, class GLib.TimeoutHandler)
	    IL_0089:  pop 
	    IL_008a:  ldc.i4.1 
	    IL_008b:  stsfld bool GLib.Object::idle_queued
	    IL_0090:  leave IL_009f

	  } // end .try 1
	  finally  { // 1
	    IL_0095:  ldloc.1 
	    IL_0096:  brfalse.s IL_009e

	    IL_0098:  ldloc.0 
	    IL_0099:  call void class [mscorlib]System.Threading.Monitor::Exit(object)
	    IL_009e:  endfinally 
	  } // end handler 1
	  IL_009f:  leave IL_00ab

	} // end .try 2
	finally  { // 2
	  IL_00a4:  ldarg.0 
	  IL_00a5:  call instance void object::Finalize()
	  IL_00aa:  endfinally 
	} // end handler 2
	IL_00ab:  ret 
    } // end of method Object::Finalize

    // method line 179
    .method private static hidebysig 
           default bool PerformQueuedUnrefs ()  cil managed 
    {
        // Method begins at RVA 0x3804
	// Code size 113 (0x71)
	.maxstack 2
	.locals init (
		class [mscorlib]System.Collections.Generic.List`1<class GLib.ToggleRef>	V_0,
		object	V_1,
		bool	V_2,
		class GLib.ToggleRef	V_3,
		valuetype [mscorlib]System.Collections.Generic.List`1/Enumerator<class GLib.ToggleRef>	V_4)
	IL_0000:  ldsfld object GLib.Object::lockObject
	IL_0005:  stloc.1 
	IL_0006:  ldc.i4.0 
	IL_0007:  stloc.2 
	.try { // 0
	  IL_0008:  ldloc.1 
	  IL_0009:  ldloca.s 2
	  IL_000b:  call void class [mscorlib]System.Threading.Monitor::Enter(object, [out] bool&)
	  IL_0010:  ldsfld class [mscorlib]System.Collections.Generic.List`1<class GLib.ToggleRef> GLib.Object::PendingDestroys
	  IL_0015:  stloc.0 
	  IL_0016:  newobj instance void class [mscorlib]System.Collections.Generic.List`1<class GLib.ToggleRef>::'.ctor'()
	  IL_001b:  stsfld class [mscorlib]System.Collections.Generic.List`1<class GLib.ToggleRef> GLib.Object::PendingDestroys
	  IL_0020:  ldc.i4.0 
	  IL_0021:  stsfld bool GLib.Object::idle_queued
	  IL_0026:  leave IL_0035

	} // end .try 0
	finally  { // 0
	  IL_002b:  ldloc.2 
	  IL_002c:  brfalse.s IL_0034

	  IL_002e:  ldloc.1 
	  IL_002f:  call void class [mscorlib]System.Threading.Monitor::Exit(object)
	  IL_0034:  endfinally 
	} // end handler 0
	IL_0035:  ldloc.0 
	IL_0036:  callvirt instance valuetype [mscorlib]System.Collections.Generic.List`1/Enumerator<!0> class [mscorlib]System.Collections.Generic.List`1<class GLib.ToggleRef>::GetEnumerator()
	IL_003b:  stloc.s 4
	.try { // 1
	  IL_003d:  br IL_0050

	  IL_0042:  ldloca.s 4
	  IL_0044:  call instance !0 valuetype [mscorlib]System.Collections.Generic.List`1/Enumerator<class GLib.ToggleRef>::get_Current()
	  IL_0049:  stloc.3 
	  IL_004a:  ldloc.3 
	  IL_004b:  callvirt instance void class GLib.ToggleRef::Free()
	  IL_0050:  ldloca.s 4
	  IL_0052:  call instance bool valuetype [mscorlib]System.Collections.Generic.List`1/Enumerator<class GLib.ToggleRef>::MoveNext()
	  IL_0057:  brtrue IL_0042

	  IL_005c:  leave IL_006f

	} // end .try 1
	finally  { // 1
	  IL_0061:  ldloca.s 4
	  IL_0063:  constrained. valuetype [mscorlib]System.Collections.Generic.List`1/Enumerator<class GLib.ToggleRef>
	  IL_0069:  callvirt instance void class [mscorlib]System.IDisposable::Dispose()
	  IL_006e:  endfinally 
	} // end handler 1
	IL_006f:  ldc.i4.0 
	IL_0070:  ret 
    } // end of method Object::PerformQueuedUnrefs

    // method line 180
    .method private static hidebysig pinvokeimpl ("libgobject-2.0-0.dll" as "g_object_unref" cdecl )
           default void g_object_unref (native int raw)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method Object::g_object_unref

    // method line 181
    .method public virtual hidebysig newslot 
           instance default void Dispose ()  cil managed 
    {
        // Method begins at RVA 0x38a0
	// Code size 146 (0x92)
	.maxstack 3
	.locals init (
		object	V_0,
		bool	V_1,
		class [mscorlib]System.Exception	V_2)
	IL_0000:  ldarg.0 
	IL_0001:  call instance native int class GLib.Object::get_Handle()
	IL_0006:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_000b:  call bool native int::op_Equality(native int, native int)
	IL_0010:  brfalse IL_0016

	IL_0015:  ret 
	IL_0016:  ldsfld class [mscorlib]System.Collections.Generic.Dictionary`2<native int,class GLib.ToggleRef> GLib.Object::Objects
	IL_001b:  stloc.0 
	IL_001c:  ldc.i4.0 
	IL_001d:  stloc.1 
	.try { // 1
	  IL_001e:  ldloc.0 
	  IL_001f:  ldloca.s 1
	  IL_0021:  call void class [mscorlib]System.Threading.Monitor::Enter(object, [out] bool&)
	  IL_0026:  ldsfld class [mscorlib]System.Collections.Generic.Dictionary`2<native int,class GLib.ToggleRef> GLib.Object::Objects
	  IL_002b:  ldarg.0 
	  IL_002c:  call instance native int class GLib.Object::get_Handle()
	  IL_0031:  callvirt instance bool class [mscorlib]System.Collections.Generic.Dictionary`2<native int, class GLib.ToggleRef>::Remove(!0)
	  IL_0036:  pop 
	  .try { // 0
	    IL_0037:  ldarg.0 
	    IL_0038:  ldfld class GLib.ToggleRef GLib.Object::tref
	    IL_003d:  brfalse IL_0054

	    IL_0042:  ldarg.0 
	    IL_0043:  ldfld class GLib.ToggleRef GLib.Object::tref
	    IL_0048:  callvirt instance void class GLib.ToggleRef::Free()
	    IL_004d:  ldarg.0 
	    IL_004e:  ldnull 
	    IL_004f:  stfld class GLib.ToggleRef GLib.Object::tref
	    IL_0054:  leave IL_0071

	  } // end .try 0
	  catch class [mscorlib]System.Exception { // 0
	    IL_0059:  stloc.2 
	    IL_005a:  ldstr "Exception while disposing a "
	    IL_005f:  ldarg.0 
	    IL_0060:  ldstr " in Gtk#"
	    IL_0065:  call string string::Concat(object, object, object)
	    IL_006a:  call void class [mscorlib]System.Console::WriteLine(string)
	    IL_006f:  ldloc.2 
	    IL_0070:  throw 
	  } // end handler 0
	  IL_0071:  leave IL_0080

	} // end .try 1
	finally  { // 1
	  IL_0076:  ldloc.1 
	  IL_0077:  brfalse.s IL_007f

	  IL_0079:  ldloc.0 
	  IL_007a:  call void class [mscorlib]System.Threading.Monitor::Exit(object)
	  IL_007f:  endfinally 
	} // end handler 1
	IL_0080:  ldarg.0 
	IL_0081:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_0086:  stfld native int GLib.Object::handle
	IL_008b:  ldarg.0 
	IL_008c:  call void class [mscorlib]System.GC::SuppressFinalize(object)
	IL_0091:  ret 
    } // end of method Object::Dispose

    // method line 182
    .method private static hidebysig pinvokeimpl ("libgobject-2.0-0.dll" as "g_object_ref" cdecl )
           default native int g_object_ref (native int raw)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method Object::g_object_ref

    // method line 183
    .method public static hidebysig 
           default class GLib.Object TryGetObject (native int o)  cil managed 
    {
        // Method begins at RVA 0x395c
	// Code size 78 (0x4e)
	.maxstack 3
	.locals init (
		class GLib.ToggleRef	V_0,
		object	V_1,
		bool	V_2)
	IL_0000:  ldarg.0 
	IL_0001:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_0006:  call bool native int::op_Equality(native int, native int)
	IL_000b:  brfalse IL_0012

	IL_0010:  ldnull 
	IL_0011:  ret 
	IL_0012:  ldsfld class [mscorlib]System.Collections.Generic.Dictionary`2<native int,class GLib.ToggleRef> GLib.Object::Objects
	IL_0017:  stloc.1 
	IL_0018:  ldc.i4.0 
	IL_0019:  stloc.2 
	.try { // 0
	  IL_001a:  ldloc.1 
	  IL_001b:  ldloca.s 2
	  IL_001d:  call void class [mscorlib]System.Threading.Monitor::Enter(object, [out] bool&)
	  IL_0022:  ldsfld class [mscorlib]System.Collections.Generic.Dictionary`2<native int,class GLib.ToggleRef> GLib.Object::Objects
	  IL_0027:  ldarg.0 
	  IL_0028:  ldloca.s 0
	  IL_002a:  callvirt instance bool class [mscorlib]System.Collections.Generic.Dictionary`2<native int, class GLib.ToggleRef>::TryGetValue(!0, [out] !1&)
	  IL_002f:  pop 
	  IL_0030:  leave IL_003f

	} // end .try 0
	finally  { // 0
	  IL_0035:  ldloc.2 
	  IL_0036:  brfalse.s IL_003e

	  IL_0038:  ldloc.1 
	  IL_0039:  call void class [mscorlib]System.Threading.Monitor::Exit(object)
	  IL_003e:  endfinally 
	} // end handler 0
	IL_003f:  ldloc.0 
	IL_0040:  brfalse IL_004c

	IL_0045:  ldloc.0 
	IL_0046:  callvirt instance class GLib.Object class GLib.ToggleRef::get_Target()
	IL_004b:  ret 
	IL_004c:  ldnull 
	IL_004d:  ret 
    } // end of method Object::TryGetObject

    // method line 184
    .method public static hidebysig 
           default class GLib.Object GetObject (native int o, bool owned_ref)  cil managed 
    {
        // Method begins at RVA 0x39c8
	// Code size 140 (0x8c)
	.maxstack 3
	.locals init (
		class GLib.Object	V_0,
		class GLib.ToggleRef	V_1,
		object	V_2,
		bool	V_3)
	IL_0000:  ldarg.0 
	IL_0001:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_0006:  call bool native int::op_Equality(native int, native int)
	IL_000b:  brfalse IL_0012

	IL_0010:  ldnull 
	IL_0011:  ret 
	IL_0012:  ldnull 
	IL_0013:  stloc.0 
	IL_0014:  ldnull 
	IL_0015:  stloc.1 
	IL_0016:  ldsfld class [mscorlib]System.Collections.Generic.Dictionary`2<native int,class GLib.ToggleRef> GLib.Object::Objects
	IL_001b:  stloc.2 
	IL_001c:  ldc.i4.0 
	IL_001d:  stloc.3 
	.try { // 0
	  IL_001e:  ldloc.2 
	  IL_001f:  ldloca.s 3
	  IL_0021:  call void class [mscorlib]System.Threading.Monitor::Enter(object, [out] bool&)
	  IL_0026:  ldsfld class [mscorlib]System.Collections.Generic.Dictionary`2<native int,class GLib.ToggleRef> GLib.Object::Objects
	  IL_002b:  ldarg.0 
	  IL_002c:  ldloca.s 1
	  IL_002e:  callvirt instance bool class [mscorlib]System.Collections.Generic.Dictionary`2<native int, class GLib.ToggleRef>::TryGetValue(!0, [out] !1&)
	  IL_0033:  pop 
	  IL_0034:  leave IL_0043

	} // end .try 0
	finally  { // 0
	  IL_0039:  ldloc.3 
	  IL_003a:  brfalse.s IL_0042

	  IL_003c:  ldloc.2 
	  IL_003d:  call void class [mscorlib]System.Threading.Monitor::Exit(object)
	  IL_0042:  endfinally 
	} // end handler 0
	IL_0043:  ldloc.1 
	IL_0044:  brfalse IL_0050

	IL_0049:  ldloc.1 
	IL_004a:  callvirt instance class GLib.Object class GLib.ToggleRef::get_Target()
	IL_004f:  stloc.0 
	IL_0050:  ldloc.0 
	IL_0051:  brfalse IL_0069

	IL_0056:  ldloc.0 
	IL_0057:  callvirt instance native int class GLib.Object::get_Handle()
	IL_005c:  ldarg.0 
	IL_005d:  call bool native int::op_Equality(native int, native int)
	IL_0062:  brfalse IL_0069

	IL_0067:  ldloc.0 
	IL_0068:  ret 
	IL_0069:  ldarg.0 
	IL_006a:  call class GLib.Object class GLib.ObjectManager::CreateObject(native int)
	IL_006f:  stloc.0 
	IL_0070:  ldloc.0 
	IL_0071:  brtrue IL_007e

	IL_0076:  ldarg.0 
	IL_0077:  call void class GLib.Object::g_object_unref(native int)
	IL_007c:  ldnull 
	IL_007d:  ret 
	IL_007e:  ldarg.1 
	IL_007f:  brfalse IL_008a

	IL_0084:  ldarg.0 
	IL_0085:  call void class GLib.Object::g_object_unref(native int)
	IL_008a:  ldloc.0 
	IL_008b:  ret 
    } // end of method Object::GetObject

    // method line 185
    .method public static hidebysig 
           default class GLib.Object GetObject (native int o)  cil managed 
    {
        // Method begins at RVA 0x3a70
	// Code size 8 (0x8)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldc.i4.0 
	IL_0002:  call class GLib.Object class GLib.Object::GetObject(native int, bool)
	IL_0007:  ret 
    } // end of method Object::GetObject

    // method line 186
    .method private static hidebysig 
           default void ConnectDefaultHandlers (valuetype GLib.GType gtype, class [mscorlib]System.Type t)  cil managed 
    {
        // Method begins at RVA 0x3a7c
	// Code size 175 (0xaf)
	.maxstack 3
	.locals init (
		class [mscorlib]System.Reflection.MethodInfo	V_0,
		class [mscorlib]System.Reflection.MethodInfo[]	V_1,
		int32	V_2,
		class [mscorlib]System.Reflection.MethodInfo	V_3,
		object	V_4,
		object[]	V_5,
		int32	V_6,
		class GLib.DefaultSignalHandlerAttribute	V_7,
		class [mscorlib]System.Reflection.MethodInfo	V_8,
		object[]	V_9)
	IL_0000:  ldarg.1 
	IL_0001:  ldc.i4.s 0x36
	IL_0003:  callvirt instance class [mscorlib]System.Reflection.MethodInfo[] class [mscorlib]System.Type::GetMethods(valuetype [mscorlib]System.Reflection.BindingFlags)
	IL_0008:  stloc.1 
	IL_0009:  ldc.i4.0 
	IL_000a:  stloc.2 
	IL_000b:  br IL_00a5

	IL_0010:  ldloc.1 
	IL_0011:  ldloc.2 
	IL_0012:  ldelem.ref 
	IL_0013:  stloc.0 
	IL_0014:  ldloc.0 
	IL_0015:  callvirt instance class [mscorlib]System.Reflection.MethodInfo class [mscorlib]System.Reflection.MethodInfo::GetBaseDefinition()
	IL_001a:  stloc.3 
	IL_001b:  ldloc.3 
	IL_001c:  ldloc.0 
	IL_001d:  call bool class [mscorlib]System.Reflection.MethodInfo::op_Equality(class [mscorlib]System.Reflection.MethodInfo, class [mscorlib]System.Reflection.MethodInfo)
	IL_0022:  brfalse IL_002c

	IL_0027:  br IL_00a1

	IL_002c:  ldloc.3 
	IL_002d:  ldtoken GLib.DefaultSignalHandlerAttribute
	IL_0032:  call class [mscorlib]System.Type class [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
	IL_0037:  ldc.i4.0 
	IL_0038:  callvirt instance object[] class [mscorlib]System.Reflection.MemberInfo::GetCustomAttributes(class [mscorlib]System.Type, bool)
	IL_003d:  stloc.s 5
	IL_003f:  ldc.i4.0 
	IL_0040:  stloc.s 6
	IL_0042:  br IL_0096

	IL_0047:  ldloc.s 5
	IL_0049:  ldloc.s 6
	IL_004b:  ldelem.ref 
	IL_004c:  stloc.s 4
	IL_004e:  ldloc.s 4
	IL_0050:  isinst GLib.DefaultSignalHandlerAttribute
	IL_0055:  stloc.s 7
	IL_0057:  ldloc.s 7
	IL_0059:  callvirt instance class [mscorlib]System.Type class GLib.DefaultSignalHandlerAttribute::get_Type()
	IL_005e:  ldloc.s 7
	IL_0060:  callvirt instance string class GLib.DefaultSignalHandlerAttribute::get_ConnectionMethod()
	IL_0065:  ldc.i4.s 0x28
	IL_0067:  callvirt instance class [mscorlib]System.Reflection.MethodInfo class [mscorlib]System.Type::GetMethod(string, valuetype [mscorlib]System.Reflection.BindingFlags)
	IL_006c:  stloc.s 8
	IL_006e:  ldc.i4.1 
	IL_006f:  newarr object
	IL_0074:  stloc.s 9
	IL_0076:  ldloc.s 9
	IL_0078:  ldc.i4.0 
	IL_0079:  ldarg.0 
	IL_007a:  box GLib.GType
	IL_007f:  stelem.ref 
	IL_0080:  ldloc.s 8
	IL_0082:  ldnull 
	IL_0083:  ldloc.s 9
	IL_0085:  callvirt instance object class [mscorlib]System.Reflection.MethodBase::Invoke(object, object[])
	IL_008a:  pop 
	IL_008b:  br IL_00a1

	IL_0090:  ldloc.s 6
	IL_0092:  ldc.i4.1 
	IL_0093:  add 
	IL_0094:  stloc.s 6
	IL_0096:  ldloc.s 6
	IL_0098:  ldloc.s 5
	IL_009a:  ldlen 
	IL_009b:  conv.i4 
	IL_009c:  blt IL_0047

	IL_00a1:  ldloc.2 
	IL_00a2:  ldc.i4.1 
	IL_00a3:  add 
	IL_00a4:  stloc.2 
	IL_00a5:  ldloc.2 
	IL_00a6:  ldloc.1 
	IL_00a7:  ldlen 
	IL_00a8:  conv.i4 
	IL_00a9:  blt IL_0010

	IL_00ae:  ret 
    } // end of method Object::ConnectDefaultHandlers

    // method line 187
    .method private static hidebysig 
           default void InvokeClassInitializers (valuetype GLib.GType gtype, class [mscorlib]System.Type t)  cil managed 
    {
        // Method begins at RVA 0x3b38
	// Code size 266 (0x10a)
	.maxstack 4
	.locals init (
		object[]	V_0,
		valuetype [mscorlib]System.Reflection.BindingFlags	V_1,
		class GLib.TypeInitializerAttribute	V_2,
		object[]	V_3,
		int32	V_4,
		class [mscorlib]System.Reflection.MethodInfo	V_5,
		class [mscorlib]System.Type	V_6,
		class [mscorlib]System.Reflection.MethodInfo	V_7,
		class [mscorlib]System.Reflection.MethodInfo[]	V_8,
		int32	V_9)
	IL_0000:  ldc.i4.2 
	IL_0001:  newarr object
	IL_0006:  dup 
	IL_0007:  ldc.i4.0 
	IL_0008:  ldarg.0 
	IL_0009:  box GLib.GType
	IL_000e:  stelem.ref 
	IL_000f:  dup 
	IL_0010:  ldc.i4.1 
	IL_0011:  ldarg.1 
	IL_0012:  stelem.ref 
	IL_0013:  stloc.0 
	IL_0014:  ldc.i4.s 0x28
	IL_0016:  stloc.1 
	IL_0017:  ldarg.1 
	IL_0018:  ldtoken GLib.TypeInitializerAttribute
	IL_001d:  call class [mscorlib]System.Type class [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
	IL_0022:  ldc.i4.1 
	IL_0023:  callvirt instance object[] class [mscorlib]System.Reflection.MemberInfo::GetCustomAttributes(class [mscorlib]System.Type, bool)
	IL_0028:  stloc.3 
	IL_0029:  ldc.i4.0 
	IL_002a:  stloc.s 4
	IL_002c:  br IL_006c

	IL_0031:  ldloc.3 
	IL_0032:  ldloc.s 4
	IL_0034:  ldelem.ref 
	IL_0035:  castclass GLib.TypeInitializerAttribute
	IL_003a:  stloc.2 
	IL_003b:  ldloc.2 
	IL_003c:  callvirt instance class [mscorlib]System.Type class GLib.TypeInitializerAttribute::get_Type()
	IL_0041:  ldloc.2 
	IL_0042:  callvirt instance string class GLib.TypeInitializerAttribute::get_MethodName()
	IL_0047:  ldloc.1 
	IL_0048:  callvirt instance class [mscorlib]System.Reflection.MethodInfo class [mscorlib]System.Type::GetMethod(string, valuetype [mscorlib]System.Reflection.BindingFlags)
	IL_004d:  stloc.s 5
	IL_004f:  ldloc.s 5
	IL_0051:  ldnull 
	IL_0052:  call bool class [mscorlib]System.Reflection.MethodInfo::op_Inequality(class [mscorlib]System.Reflection.MethodInfo, class [mscorlib]System.Reflection.MethodInfo)
	IL_0057:  brfalse IL_0066

	IL_005c:  ldloc.s 5
	IL_005e:  ldnull 
	IL_005f:  ldloc.0 
	IL_0060:  callvirt instance object class [mscorlib]System.Reflection.MethodBase::Invoke(object, object[])
	IL_0065:  pop 
	IL_0066:  ldloc.s 4
	IL_0068:  ldc.i4.1 
	IL_0069:  add 
	IL_006a:  stloc.s 4
	IL_006c:  ldloc.s 4
	IL_006e:  ldloc.3 
	IL_006f:  ldlen 
	IL_0070:  conv.i4 
	IL_0071:  blt IL_0031

	IL_0076:  ldarg.1 
	IL_0077:  stloc.s 6
	IL_0079:  br IL_00f3

	IL_007e:  ldloc.s 6
	IL_0080:  callvirt instance class [mscorlib]System.Reflection.Assembly class [mscorlib]System.Type::get_Assembly()
	IL_0085:  ldtoken GLib.IgnoreClassInitializersAttribute
	IL_008a:  call class [mscorlib]System.Type class [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
	IL_008f:  ldc.i4.0 
	IL_0090:  callvirt instance bool class [mscorlib]System.Reflection.Assembly::IsDefined(class [mscorlib]System.Type, bool)
	IL_0095:  brfalse IL_009f

	IL_009a:  br IL_00ea

	IL_009f:  ldloc.s 6
	IL_00a1:  ldloc.1 
	IL_00a2:  callvirt instance class [mscorlib]System.Reflection.MethodInfo[] class [mscorlib]System.Type::GetMethods(valuetype [mscorlib]System.Reflection.BindingFlags)
	IL_00a7:  stloc.s 8
	IL_00a9:  ldc.i4.0 
	IL_00aa:  stloc.s 9
	IL_00ac:  br IL_00df

	IL_00b1:  ldloc.s 8
	IL_00b3:  ldloc.s 9
	IL_00b5:  ldelem.ref 
	IL_00b6:  stloc.s 7
	IL_00b8:  ldloc.s 7
	IL_00ba:  ldtoken GLib.ClassInitializerAttribute
	IL_00bf:  call class [mscorlib]System.Type class [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
	IL_00c4:  ldc.i4.1 
	IL_00c5:  callvirt instance bool class [mscorlib]System.Reflection.MemberInfo::IsDefined(class [mscorlib]System.Type, bool)
	IL_00ca:  brfalse IL_00d9

	IL_00cf:  ldloc.s 7
	IL_00d1:  ldnull 
	IL_00d2:  ldloc.0 
	IL_00d3:  callvirt instance object class [mscorlib]System.Reflection.MethodBase::Invoke(object, object[])
	IL_00d8:  pop 
	IL_00d9:  ldloc.s 9
	IL_00db:  ldc.i4.1 
	IL_00dc:  add 
	IL_00dd:  stloc.s 9
	IL_00df:  ldloc.s 9
	IL_00e1:  ldloc.s 8
	IL_00e3:  ldlen 
	IL_00e4:  conv.i4 
	IL_00e5:  blt IL_00b1

	IL_00ea:  ldloc.s 6
	IL_00ec:  callvirt instance class [mscorlib]System.Type class [mscorlib]System.Type::get_BaseType()
	IL_00f1:  stloc.s 6
	IL_00f3:  ldloc.s 6
	IL_00f5:  ldtoken GLib.Object
	IL_00fa:  call class [mscorlib]System.Type class [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
	IL_00ff:  call bool class [mscorlib]System.Type::op_Inequality(class [mscorlib]System.Type, class [mscorlib]System.Type)
	IL_0104:  brtrue IL_007e

	IL_0109:  ret 
    } // end of method Object::InvokeClassInitializers

    // method line 188
    .method private static hidebysig specialname 
           default class [mscorlib]System.Collections.Generic.Dictionary`2<native int, class [mscorlib]System.Reflection.PropertyInfo> get_Properties ()  cil managed 
    {
        // Method begins at RVA 0x3c50
	// Code size 31 (0x1f)
	.maxstack 8
	IL_0000:  ldsfld class [mscorlib]System.Collections.Generic.Dictionary`2<native int,class [mscorlib]System.Reflection.PropertyInfo> GLib.Object::properties
	IL_0005:  brtrue IL_0019

	IL_000a:  ldsfld class [mscorlib]System.Collections.Generic.IEqualityComparer`1<native int> GLib.IntPtrEqualityComparer::Instance
	IL_000f:  newobj instance void class [mscorlib]System.Collections.Generic.Dictionary`2<native int, class [mscorlib]System.Reflection.PropertyInfo>::'.ctor'(class [mscorlib]System.Collections.Generic.IEqualityComparer`1<!0>)
	IL_0014:  stsfld class [mscorlib]System.Collections.Generic.Dictionary`2<native int,class [mscorlib]System.Reflection.PropertyInfo> GLib.Object::properties
	IL_0019:  ldsfld class [mscorlib]System.Collections.Generic.Dictionary`2<native int,class [mscorlib]System.Reflection.PropertyInfo> GLib.Object::properties
	IL_001e:  ret 
    } // end of method Object::get_Properties

    // method line 189
    .method private static hidebysig pinvokeimpl ("glibsharpglue-2" as "gtksharp_override_property_handlers" cdecl )
           default void gtksharp_override_property_handlers (native int 'type', class GLib.Object/GetPropertyDelegate get_cb, class GLib.Object/SetPropertyDelegate set_cb)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method Object::gtksharp_override_property_handlers

    // method line 190
    .method private static hidebysig pinvokeimpl ("glibsharpglue-2" as "gtksharp_register_property" cdecl )
           default native int gtksharp_register_property (native int 'type', native int name, native int nick, native int blurb, unsigned int32 property_id, native int property_type, bool can_read, bool can_write)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method Object::gtksharp_register_property

    // method line 191
    .method private static hidebysig 
           default void AddProperties (valuetype GLib.GType gtype, class [mscorlib]System.Type t)  cil managed 
    {
        // Method begins at RVA 0x3c70
	// Code size 353 (0x161)
	.maxstack 8
	.locals init (
		unsigned int32	V_0,
		bool	V_1,
		class [mscorlib]System.Reflection.PropertyInfo	V_2,
		class [mscorlib]System.Reflection.PropertyInfo[]	V_3,
		int32	V_4,
		object	V_5,
		object[]	V_6,
		int32	V_7,
		class GLib.PropertyAttribute	V_8,
		native int	V_9,
		native int	V_10,
		native int	V_11,
		native int	V_12,
		valuetype GLib.GType	V_13)
	IL_0000:  ldc.i4.1 
	IL_0001:  stloc.0 
	IL_0002:  ldc.i4.0 
	IL_0003:  stloc.1 
	IL_0004:  ldarg.1 
	IL_0005:  ldc.i4.s 0x16
	IL_0007:  callvirt instance class [mscorlib]System.Reflection.PropertyInfo[] class [mscorlib]System.Type::GetProperties(valuetype [mscorlib]System.Reflection.BindingFlags)
	IL_000c:  stloc.3 
	IL_000d:  ldc.i4.0 
	IL_000e:  stloc.s 4
	IL_0010:  br IL_0156

	IL_0015:  ldloc.3 
	IL_0016:  ldloc.s 4
	IL_0018:  ldelem.ref 
	IL_0019:  stloc.2 
	IL_001a:  ldloc.2 
	IL_001b:  ldtoken GLib.PropertyAttribute
	IL_0020:  call class [mscorlib]System.Type class [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
	IL_0025:  ldc.i4.0 
	IL_0026:  callvirt instance object[] class [mscorlib]System.Reflection.MemberInfo::GetCustomAttributes(class [mscorlib]System.Type, bool)
	IL_002b:  stloc.s 6
	IL_002d:  ldc.i4.0 
	IL_002e:  stloc.s 7
	IL_0030:  br IL_0145

	IL_0035:  ldloc.s 6
	IL_0037:  ldloc.s 7
	IL_0039:  ldelem.ref 
	IL_003a:  stloc.s 5
	IL_003c:  ldloc.2 
	IL_003d:  callvirt instance class [mscorlib]System.Reflection.ParameterInfo[] class [mscorlib]System.Reflection.PropertyInfo::GetIndexParameters()
	IL_0042:  ldlen 
	IL_0043:  conv.i4 
	IL_0044:  ldc.i4.0 
	IL_0045:  ble IL_0066

	IL_004a:  ldstr "GLib.RegisterPropertyAttribute cannot be applied to property {0} of type {1} because the property expects one or more indexed parameters"
	IL_004f:  ldloc.2 
	IL_0050:  callvirt instance string class [mscorlib]System.Reflection.MemberInfo::get_Name()
	IL_0055:  ldarg.1 
	IL_0056:  callvirt instance string class [mscorlib]System.Type::get_FullName()
	IL_005b:  call string string::Format(string, object, object)
	IL_0060:  newobj instance void class [mscorlib]System.InvalidOperationException::'.ctor'(string)
	IL_0065:  throw 
	IL_0066:  ldloc.s 5
	IL_0068:  isinst GLib.PropertyAttribute
	IL_006d:  stloc.s 8
	IL_006f:  ldloc.1 
	IL_0070:  brtrue IL_008d

	IL_0075:  ldarga.s 0
	IL_0077:  call instance native int valuetype GLib.GType::get_Val()
	IL_007c:  call class GLib.Object/GetPropertyDelegate class GLib.Object::get_GetPropertyHandler()
	IL_0081:  call class GLib.Object/SetPropertyDelegate class GLib.Object::get_SetPropertyHandler()
	IL_0086:  call void class GLib.Object::gtksharp_override_property_handlers(native int, class GLib.Object/GetPropertyDelegate, class GLib.Object/SetPropertyDelegate)
	IL_008b:  ldc.i4.1 
	IL_008c:  stloc.1 
	IL_008d:  ldloc.s 8
	IL_008f:  callvirt instance string class GLib.PropertyAttribute::get_Name()
	IL_0094:  call native int class GLib.Marshaller::StringToPtrGStrdup(string)
	IL_0099:  stloc.s 9
	IL_009b:  ldloc.s 8
	IL_009d:  callvirt instance string class GLib.PropertyAttribute::get_Nickname()
	IL_00a2:  call native int class GLib.Marshaller::StringToPtrGStrdup(string)
	IL_00a7:  stloc.s 10
	IL_00a9:  ldloc.s 8
	IL_00ab:  callvirt instance string class GLib.PropertyAttribute::get_Blurb()
	IL_00b0:  call native int class GLib.Marshaller::StringToPtrGStrdup(string)
	IL_00b5:  stloc.s 11
	IL_00b7:  ldarga.s 0
	IL_00b9:  call instance native int valuetype GLib.GType::get_Val()
	IL_00be:  ldloc.s 9
	IL_00c0:  ldloc.s 10
	IL_00c2:  ldloc.s 11
	IL_00c4:  ldloc.0 
	IL_00c5:  ldloc.2 
	IL_00c6:  callvirt instance class [mscorlib]System.Type class [mscorlib]System.Reflection.PropertyInfo::get_PropertyType()
	IL_00cb:  call valuetype GLib.GType valuetype GLib.GType::op_Explicit(class [mscorlib]System.Type)
	IL_00d0:  stloc.s 13
	IL_00d2:  ldloca.s 13
	IL_00d4:  call instance native int valuetype GLib.GType::get_Val()
	IL_00d9:  ldloc.2 
	IL_00da:  callvirt instance bool class [mscorlib]System.Reflection.PropertyInfo::get_CanRead()
	IL_00df:  ldloc.2 
	IL_00e0:  callvirt instance bool class [mscorlib]System.Reflection.PropertyInfo::get_CanWrite()
	IL_00e5:  call native int class GLib.Object::gtksharp_register_property(native int, native int, native int, native int, unsigned int32, native int, bool, bool)
	IL_00ea:  stloc.s 12
	IL_00ec:  ldloc.s 9
	IL_00ee:  call void class GLib.Marshaller::Free(native int)
	IL_00f3:  ldloc.s 10
	IL_00f5:  call void class GLib.Marshaller::Free(native int)
	IL_00fa:  ldloc.s 11
	IL_00fc:  call void class GLib.Marshaller::Free(native int)
	IL_0101:  ldloc.s 12
	IL_0103:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_0108:  call bool native int::op_Equality(native int, native int)
	IL_010d:  brfalse IL_012e

	IL_0112:  ldstr "GLib.PropertyAttribute cannot be applied to property {0} of type {1} because the return type of the property is not supported"
	IL_0117:  ldloc.2 
	IL_0118:  callvirt instance string class [mscorlib]System.Reflection.MemberInfo::get_Name()
	IL_011d:  ldarg.1 
	IL_011e:  callvirt instance string class [mscorlib]System.Type::get_FullName()
	IL_0123:  call string string::Format(string, object, object)
	IL_0128:  newobj instance void class [mscorlib]System.InvalidOperationException::'.ctor'(string)
	IL_012d:  throw 
	IL_012e:  call class [mscorlib]System.Collections.Generic.Dictionary`2<native int,class [mscorlib]System.Reflection.PropertyInfo> class GLib.Object::get_Properties()
	IL_0133:  ldloc.s 12
	IL_0135:  ldloc.2 
	IL_0136:  callvirt instance void class [mscorlib]System.Collections.Generic.Dictionary`2<native int, class [mscorlib]System.Reflection.PropertyInfo>::Add(!0, !1)
	IL_013b:  ldloc.0 
	IL_013c:  ldc.i4.1 
	IL_013d:  add 
	IL_013e:  stloc.0 
	IL_013f:  ldloc.s 7
	IL_0141:  ldc.i4.1 
	IL_0142:  add 
	IL_0143:  stloc.s 7
	IL_0145:  ldloc.s 7
	IL_0147:  ldloc.s 6
	IL_0149:  ldlen 
	IL_014a:  conv.i4 
	IL_014b:  blt IL_0035

	IL_0150:  ldloc.s 4
	IL_0152:  ldc.i4.1 
	IL_0153:  add 
	IL_0154:  stloc.s 4
	IL_0156:  ldloc.s 4
	IL_0158:  ldloc.3 
	IL_0159:  ldlen 
	IL_015a:  conv.i4 
	IL_015b:  blt IL_0015

	IL_0160:  ret 
    } // end of method Object::AddProperties

    // method line 192
    .method private static hidebysig specialname 
           default class GLib.Object/GetPropertyDelegate get_GetPropertyHandler ()  cil managed 
    {
        // Method begins at RVA 0x3de0
	// Code size 33 (0x21)
	.maxstack 8
	IL_0000:  ldsfld class GLib.Object/GetPropertyDelegate GLib.Object::get_property_handler
	IL_0005:  brtrue IL_001b

	IL_000a:  ldnull 
	IL_000b:  ldftn void class GLib.Object::GetPropertyCallback(native int, unsigned int32, [out] valuetype GLib.Value&, native int)
	IL_0011:  newobj instance void class GLib.Object/GetPropertyDelegate::'.ctor'(object, native int)
	IL_0016:  stsfld class GLib.Object/GetPropertyDelegate GLib.Object::get_property_handler
	IL_001b:  ldsfld class GLib.Object/GetPropertyDelegate GLib.Object::get_property_handler
	IL_0020:  ret 
    } // end of method Object::get_GetPropertyHandler

    // method line 193
    .method private static hidebysig specialname 
           default class GLib.Object/SetPropertyDelegate get_SetPropertyHandler ()  cil managed 
    {
        // Method begins at RVA 0x3e04
	// Code size 33 (0x21)
	.maxstack 8
	IL_0000:  ldsfld class GLib.Object/SetPropertyDelegate GLib.Object::set_property_handler
	IL_0005:  brtrue IL_001b

	IL_000a:  ldnull 
	IL_000b:  ldftn void class GLib.Object::SetPropertyCallback(native int, unsigned int32, [out] valuetype GLib.Value&, native int)
	IL_0011:  newobj instance void class GLib.Object/SetPropertyDelegate::'.ctor'(object, native int)
	IL_0016:  stsfld class GLib.Object/SetPropertyDelegate GLib.Object::set_property_handler
	IL_001b:  ldsfld class GLib.Object/SetPropertyDelegate GLib.Object::set_property_handler
	IL_0020:  ret 
    } // end of method Object::get_SetPropertyHandler

    // method line 194
    .method private static hidebysig 
           default void GetPropertyCallback (native int handle, unsigned int32 property_id, valuetype GLib.Value& 'value', native int param_spec)  cil managed 
    {
        // Method begins at RVA 0x3e28
	// Code size 32 (0x20)
	.maxstack 3
	.locals init (
		class GLib.Object	V_0)
	IL_0000:  ldarg.0 
	IL_0001:  ldc.i4.0 
	IL_0002:  call class GLib.Object class GLib.Object::GetObject(native int, bool)
	IL_0007:  stloc.0 
	IL_0008:  ldarg.2 
	IL_0009:  call class [mscorlib]System.Collections.Generic.Dictionary`2<native int,class [mscorlib]System.Reflection.PropertyInfo> class GLib.Object::get_Properties()
	IL_000e:  ldarg.3 
	IL_000f:  callvirt instance !1 class [mscorlib]System.Collections.Generic.Dictionary`2<native int, class [mscorlib]System.Reflection.PropertyInfo>::get_Item(!0)
	IL_0014:  ldloc.0 
	IL_0015:  callvirt instance object class [mscorlib]System.Reflection.PropertyInfo::GetValue(object)
	IL_001a:  call instance void valuetype GLib.Value::set_Val(object)
	IL_001f:  ret 
    } // end of method Object::GetPropertyCallback

    // method line 195
    .method private static hidebysig 
           default void SetPropertyCallback (native int handle, unsigned int32 property_id, valuetype GLib.Value& 'value', native int param_spec)  cil managed 
    {
        // Method begins at RVA 0x3e54
	// Code size 32 (0x20)
	.maxstack 3
	.locals init (
		class GLib.Object	V_0)
	IL_0000:  ldarg.0 
	IL_0001:  ldc.i4.0 
	IL_0002:  call class GLib.Object class GLib.Object::GetObject(native int, bool)
	IL_0007:  stloc.0 
	IL_0008:  call class [mscorlib]System.Collections.Generic.Dictionary`2<native int,class [mscorlib]System.Reflection.PropertyInfo> class GLib.Object::get_Properties()
	IL_000d:  ldarg.3 
	IL_000e:  callvirt instance !1 class [mscorlib]System.Collections.Generic.Dictionary`2<native int, class [mscorlib]System.Reflection.PropertyInfo>::get_Item(!0)
	IL_0013:  ldloc.0 
	IL_0014:  ldarg.2 
	IL_0015:  call instance object valuetype GLib.Value::get_Val()
	IL_001a:  callvirt instance void class [mscorlib]System.Reflection.PropertyInfo::SetValue(object, object)
	IL_001f:  ret 
    } // end of method Object::SetPropertyCallback

    // method line 196
    .method private static hidebysig pinvokeimpl ("libgobject-2.0-0.dll" as "g_type_add_interface_static" cdecl )
           default void g_type_add_interface_static (native int gtype, native int iface_type, valuetype GLib.GInterfaceInfo& info)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method Object::g_type_add_interface_static

    // method line 197
    .method private static hidebysig 
           default void AddInterfaces (valuetype GLib.GType gtype, class [mscorlib]System.Type t)  cil managed 
    {
        // Method begins at RVA 0x3e80
	// Code size 159 (0x9f)
	.maxstack 3
	.locals init (
		class [mscorlib]System.Type	V_0,
		class [mscorlib]System.Type[]	V_1,
		int32	V_2,
		class GLib.GInterfaceAttribute	V_3,
		class GLib.GInterfaceAdapter	V_4,
		valuetype GLib.GInterfaceInfo	V_5,
		valuetype GLib.GType	V_6)
	IL_0000:  ldarg.1 
	IL_0001:  callvirt instance class [mscorlib]System.Type[] class [mscorlib]System.Type::GetInterfaces()
	IL_0006:  stloc.1 
	IL_0007:  ldc.i4.0 
	IL_0008:  stloc.2 
	IL_0009:  br IL_0095

	IL_000e:  ldloc.1 
	IL_000f:  ldloc.2 
	IL_0010:  ldelem.ref 
	IL_0011:  stloc.0 
	IL_0012:  ldloc.0 
	IL_0013:  ldtoken GLib.GInterfaceAttribute
	IL_0018:  call class [mscorlib]System.Type class [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
	IL_001d:  ldc.i4.1 
	IL_001e:  callvirt instance bool class [mscorlib]System.Reflection.MemberInfo::IsDefined(class [mscorlib]System.Type, bool)
	IL_0023:  brfalse IL_0039

	IL_0028:  ldloc.0 
	IL_0029:  ldarg.1 
	IL_002a:  callvirt instance class [mscorlib]System.Type class [mscorlib]System.Type::get_BaseType()
	IL_002f:  callvirt instance bool class [mscorlib]System.Type::IsAssignableFrom(class [mscorlib]System.Type)
	IL_0034:  brfalse IL_003e

	IL_0039:  br IL_0091

	IL_003e:  ldloc.0 
	IL_003f:  ldtoken GLib.GInterfaceAttribute
	IL_0044:  call class [mscorlib]System.Type class [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
	IL_0049:  ldc.i4.0 
	IL_004a:  callvirt instance object[] class [mscorlib]System.Reflection.MemberInfo::GetCustomAttributes(class [mscorlib]System.Type, bool)
	IL_004f:  ldc.i4.0 
	IL_0050:  ldelem.ref 
	IL_0051:  isinst GLib.GInterfaceAttribute
	IL_0056:  stloc.3 
	IL_0057:  ldloc.3 
	IL_0058:  callvirt instance class [mscorlib]System.Type class GLib.GInterfaceAttribute::get_AdapterType()
	IL_005d:  ldnull 
	IL_005e:  call object class [mscorlib]System.Activator::CreateInstance(class [mscorlib]System.Type, object[])
	IL_0063:  isinst GLib.GInterfaceAdapter
	IL_0068:  stloc.s 4
	IL_006a:  ldloc.s 4
	IL_006c:  callvirt instance valuetype GLib.GInterfaceInfo class GLib.GInterfaceAdapter::get_Info()
	IL_0071:  stloc.s 5
	IL_0073:  ldarga.s 0
	IL_0075:  call instance native int valuetype GLib.GType::get_Val()
	IL_007a:  ldloc.s 4
	IL_007c:  callvirt instance valuetype GLib.GType class GLib.GInterfaceAdapter::get_GType()
	IL_0081:  stloc.s 6
	IL_0083:  ldloca.s 6
	IL_0085:  call instance native int valuetype GLib.GType::get_Val()
	IL_008a:  ldloca.s 5
	IL_008c:  call void class GLib.Object::g_type_add_interface_static(native int, native int, [out] valuetype GLib.GInterfaceInfo&)
	IL_0091:  ldloc.2 
	IL_0092:  ldc.i4.1 
	IL_0093:  add 
	IL_0094:  stloc.2 
	IL_0095:  ldloc.2 
	IL_0096:  ldloc.1 
	IL_0097:  ldlen 
	IL_0098:  conv.i4 
	IL_0099:  blt IL_000e

	IL_009e:  ret 
    } // end of method Object::AddInterfaces

    // method line 198
    .method private static hidebysig pinvokeimpl ("glibsharpglue-2" as "gtksharp_register_type" cdecl )
           default native int gtksharp_register_type (native int name, native int parent_type)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method Object::gtksharp_register_type

    // method line 199
    .method private static hidebysig 
           default string BuildEscapedName (class [mscorlib]System.Type t)  cil managed 
    {
        // Method begins at RVA 0x3f2c
	// Code size 273 (0x111)
	.maxstack 4
	.locals init (
		string	V_0,
		class [mscorlib]System.Text.StringBuilder	V_1,
		char	V_2,
		string	V_3,
		int32	V_4,
		unsigned int8	V_5,
		unsigned int32	V_6)
	IL_0000:  ldarg.0 
	IL_0001:  callvirt instance string class [mscorlib]System.Type::get_FullName()
	IL_0006:  stloc.0 
	IL_0007:  ldc.i4.s 0x14
	IL_0009:  ldloc.0 
	IL_000a:  callvirt instance int32 string::get_Length()
	IL_000f:  add 
	IL_0010:  newobj instance void class [mscorlib]System.Text.StringBuilder::'.ctor'(int32)
	IL_0015:  stloc.1 
	IL_0016:  ldloc.1 
	IL_0017:  ldstr "__gtksharp_"
	IL_001c:  callvirt instance class [mscorlib]System.Text.StringBuilder class [mscorlib]System.Text.StringBuilder::Append(string)
	IL_0021:  pop 
	IL_0022:  ldloc.1 
	IL_0023:  ldsfld int32 GLib.Object::type_uid
	IL_0028:  dup 
	IL_0029:  ldc.i4.1 
	IL_002a:  add 
	IL_002b:  stsfld int32 GLib.Object::type_uid
	IL_0030:  callvirt instance class [mscorlib]System.Text.StringBuilder class [mscorlib]System.Text.StringBuilder::Append(int32)
	IL_0035:  pop 
	IL_0036:  ldloc.1 
	IL_0037:  ldstr "_"
	IL_003c:  callvirt instance class [mscorlib]System.Text.StringBuilder class [mscorlib]System.Text.StringBuilder::Append(string)
	IL_0041:  pop 
	IL_0042:  ldloc.0 
	IL_0043:  stloc.3 
	IL_0044:  ldc.i4.0 
	IL_0045:  stloc.s 4
	IL_0047:  br IL_00fd

	IL_004c:  ldloc.3 
	IL_004d:  ldloc.s 4
	IL_004f:  callvirt instance char string::get_Chars(int32)
	IL_0054:  stloc.2 
	IL_0055:  ldloc.2 
	IL_0056:  ldc.i4.s 0x30
	IL_0058:  blt IL_0065

	IL_005d:  ldloc.2 
	IL_005e:  ldc.i4.s 0x39
	IL_0060:  ble IL_0085

	IL_0065:  ldloc.2 
	IL_0066:  ldc.i4.s 0x61
	IL_0068:  blt IL_0075

	IL_006d:  ldloc.2 
	IL_006e:  ldc.i4.s 0x7a
	IL_0070:  ble IL_0085

	IL_0075:  ldloc.2 
	IL_0076:  ldc.i4.s 0x41
	IL_0078:  blt IL_0092

	IL_007d:  ldloc.2 
	IL_007e:  ldc.i4.s 0x5a
	IL_0080:  bgt IL_0092

	IL_0085:  ldloc.1 
	IL_0086:  ldloc.2 
	IL_0087:  callvirt instance class [mscorlib]System.Text.StringBuilder class [mscorlib]System.Text.StringBuilder::Append(char)
	IL_008c:  pop 
	IL_008d:  br IL_00f7

	IL_0092:  ldloc.2 
	IL_0093:  ldc.i4.s 0x2e
	IL_0095:  bne.un IL_00a8

	IL_009a:  ldloc.1 
	IL_009b:  ldc.i4.s 0x5f
	IL_009d:  callvirt instance class [mscorlib]System.Text.StringBuilder class [mscorlib]System.Text.StringBuilder::Append(char)
	IL_00a2:  pop 
	IL_00a3:  br IL_00f7

	IL_00a8:  ldloc.2 
	IL_00a9:  ldc.i4 255
	IL_00ae:  bgt.un IL_00d8

	IL_00b3:  ldloc.1 
	IL_00b4:  ldc.i4.s 0x2b
	IL_00b6:  callvirt instance class [mscorlib]System.Text.StringBuilder class [mscorlib]System.Text.StringBuilder::Append(char)
	IL_00bb:  pop 
	IL_00bc:  ldloc.1 
	IL_00bd:  ldloc.2 
	IL_00be:  conv.u1 
	IL_00bf:  stloc.s 5
	IL_00c1:  ldloca.s 5
	IL_00c3:  ldstr "x2"
	IL_00c8:  call instance string unsigned int8::ToString(string)
	IL_00cd:  callvirt instance class [mscorlib]System.Text.StringBuilder class [mscorlib]System.Text.StringBuilder::Append(string)
	IL_00d2:  pop 
	IL_00d3:  br IL_00f7

	IL_00d8:  ldloc.1 
	IL_00d9:  ldc.i4.s 0x2d
	IL_00db:  callvirt instance class [mscorlib]System.Text.StringBuilder class [mscorlib]System.Text.StringBuilder::Append(char)
	IL_00e0:  pop 
	IL_00e1:  ldloc.1 
	IL_00e2:  ldloc.2 
	IL_00e3:  stloc.s 6
	IL_00e5:  ldloca.s 6
	IL_00e7:  ldstr "x4"
	IL_00ec:  call instance string unsigned int32::ToString(string)
	IL_00f1:  callvirt instance class [mscorlib]System.Text.StringBuilder class [mscorlib]System.Text.StringBuilder::Append(string)
	IL_00f6:  pop 
	IL_00f7:  ldloc.s 4
	IL_00f9:  ldc.i4.1 
	IL_00fa:  add 
	IL_00fb:  stloc.s 4
	IL_00fd:  ldloc.s 4
	IL_00ff:  ldloc.3 
	IL_0100:  callvirt instance int32 string::get_Length()
	IL_0105:  blt IL_004c

	IL_010a:  ldloc.1 
	IL_010b:  callvirt instance string object::ToString()
	IL_0110:  ret 
    } // end of method Object::BuildEscapedName

    // method line 200
    .method family static hidebysig 
           default valuetype GLib.GType RegisterGType (class [mscorlib]System.Type t)  cil managed 
    {
        // Method begins at RVA 0x404c
	// Code size 101 (0x65)
	.maxstack 3
	.locals init (
		valuetype GLib.GType	V_0,
		string	V_1,
		native int	V_2,
		valuetype GLib.GType	V_3)
	IL_0000:  ldarg.0 
	IL_0001:  callvirt instance class [mscorlib]System.Type class [mscorlib]System.Type::get_BaseType()
	IL_0006:  call valuetype GLib.GType class GLib.Object::LookupGType(class [mscorlib]System.Type)
	IL_000b:  stloc.0 
	IL_000c:  ldarg.0 
	IL_000d:  call string class GLib.Object::BuildEscapedName(class [mscorlib]System.Type)
	IL_0012:  stloc.1 
	IL_0013:  ldloc.1 
	IL_0014:  call native int class GLib.Marshaller::StringToPtrGStrdup(string)
	IL_0019:  stloc.2 
	IL_001a:  ldloca.s 3
	IL_001c:  ldloc.2 
	IL_001d:  ldloca.s 0
	IL_001f:  call instance native int valuetype GLib.GType::get_Val()
	IL_0024:  call native int class GLib.Object::gtksharp_register_type(native int, native int)
	IL_0029:  call instance void valuetype GLib.GType::'.ctor'(native int)
	IL_002e:  ldloc.2 
	IL_002f:  call void class GLib.Marshaller::Free(native int)
	IL_0034:  ldloc.3 
	IL_0035:  ldarg.0 
	IL_0036:  call void valuetype GLib.GType::Register(valuetype GLib.GType, class [mscorlib]System.Type)
	IL_003b:  ldloc.3 
	IL_003c:  ldarg.0 
	IL_003d:  call void class GLib.Object::AddProperties(valuetype GLib.GType, class [mscorlib]System.Type)
	IL_0042:  ldloc.3 
	IL_0043:  ldarg.0 
	IL_0044:  call void class GLib.Object::ConnectDefaultHandlers(valuetype GLib.GType, class [mscorlib]System.Type)
	IL_0049:  ldloc.3 
	IL_004a:  ldarg.0 
	IL_004b:  call void class GLib.Object::InvokeClassInitializers(valuetype GLib.GType, class [mscorlib]System.Type)
	IL_0050:  ldloc.3 
	IL_0051:  ldarg.0 
	IL_0052:  call void class GLib.Object::AddInterfaces(valuetype GLib.GType, class [mscorlib]System.Type)
	IL_0057:  ldsfld class [mscorlib]System.Collections.Generic.Dictionary`2<class [mscorlib]System.Type,valuetype GLib.GType> GLib.Object::g_types
	IL_005c:  ldarg.0 
	IL_005d:  ldloc.3 
	IL_005e:  callvirt instance void class [mscorlib]System.Collections.Generic.Dictionary`2<class [mscorlib]System.Type, valuetype GLib.GType>::set_Item(!0, !1)
	IL_0063:  ldloc.3 
	IL_0064:  ret 
    } // end of method Object::RegisterGType

    // method line 201
    .method family hidebysig 
           instance default valuetype GLib.GType LookupGType ()  cil managed 
    {
        // Method begins at RVA 0x40c0
	// Code size 12 (0xc)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance class [mscorlib]System.Type object::GetType()
	IL_0006:  call valuetype GLib.GType class GLib.Object::LookupGType(class [mscorlib]System.Type)
	IL_000b:  ret 
    } // end of method Object::LookupGType

    // method line 202
    .method private static hidebysig pinvokeimpl ("glibsharpglue-2" as "gtksharp_object_newv" cdecl )
           default native int gtksharp_object_newv (native int gtype, int32 n_params, native int[] names, valuetype GLib.Value[] vals)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method Object::gtksharp_object_newv

    // method line 203
    .method family hidebysig 
           instance default void CreateNativeObject (string[] names, valuetype GLib.Value[] vals, int32 count)  cil managed 
    {
        // Method begins at RVA 0x40d0
	// Code size 51 (0x33)
	.maxstack 4
	.locals init (
		native int[]	V_0,
		int32	V_1)
	IL_0000:  ldarg.3 
	IL_0001:  newarr native int
	IL_0006:  stloc.0 
	IL_0007:  ldc.i4.0 
	IL_0008:  stloc.1 
	IL_0009:  br IL_0022

	IL_000e:  ldloc.0 
	IL_000f:  ldloc.1 
	IL_0010:  ldelema native int
	IL_0015:  ldarg.1 
	IL_0016:  ldloc.1 
	IL_0017:  ldelem.ref 
	IL_0018:  call native int class GLib.Marshaller::StringToPtrGStrdup(string)
	IL_001d:  stind.i 
	IL_001e:  ldloc.1 
	IL_001f:  ldc.i4.1 
	IL_0020:  add 
	IL_0021:  stloc.1 
	IL_0022:  ldloc.1 
	IL_0023:  ldarg.3 
	IL_0024:  blt IL_000e

	IL_0029:  ldarg.0 
	IL_002a:  ldloc.0 
	IL_002b:  ldarg.2 
	IL_002c:  ldarg.3 
	IL_002d:  call instance void class GLib.Object::CreateNativeObject(native int[], valuetype GLib.Value[], int32)
	IL_0032:  ret 
    } // end of method Object::CreateNativeObject

    // method line 204
    .method family hidebysig 
           instance default void CreateNativeObject (native int[] native_names, valuetype GLib.Value[] vals, int32 count)  cil managed 
    {
        // Method begins at RVA 0x4110
	// Code size 74 (0x4a)
	.maxstack 5
	.locals init (
		valuetype GLib.GType	V_0,
		int32	V_1)
	IL_0000:  ldarg.0 
	IL_0001:  ldc.i4.1 
	IL_0002:  stfld bool GLib.Object::owned
	IL_0007:  ldarg.0 
	IL_0008:  ldarg.0 
	IL_0009:  call instance valuetype GLib.GType class GLib.Object::LookupGType()
	IL_000e:  stloc.0 
	IL_000f:  ldloca.s 0
	IL_0011:  call instance native int valuetype GLib.GType::get_Val()
	IL_0016:  ldarg.3 
	IL_0017:  ldarg.1 
	IL_0018:  ldarg.2 
	IL_0019:  call native int class GLib.Object::gtksharp_object_newv(native int, int32, native int[], valuetype GLib.Value[])
	IL_001e:  callvirt instance void class GLib.Object::set_Raw(native int)
	IL_0023:  ldc.i4.0 
	IL_0024:  stloc.1 
	IL_0025:  br IL_0042

	IL_002a:  ldarg.1 
	IL_002b:  ldloc.1 
	IL_002c:  ldelem.i 
	IL_002d:  call void class GLib.Marshaller::Free(native int)
	IL_0032:  ldarg.2 
	IL_0033:  ldloc.1 
	IL_0034:  ldelema GLib.Value
	IL_0039:  call instance void valuetype GLib.Value::Dispose()
	IL_003e:  ldloc.1 
	IL_003f:  ldc.i4.1 
	IL_0040:  add 
	IL_0041:  stloc.1 
	IL_0042:  ldloc.1 
	IL_0043:  ldarg.3 
	IL_0044:  blt IL_002a

	IL_0049:  ret 
    } // end of method Object::CreateNativeObject

    // method line 205
    .method family virtual hidebysig newslot specialname 
           instance default native int get_Raw ()  cil managed 
    {
        // Method begins at RVA 0x4168
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldfld native int GLib.Object::handle
	IL_0006:  ret 
    } // end of method Object::get_Raw

    // method line 206
    .method public static hidebysig specialname 
           default valuetype GLib.GType get_GType ()  cil managed 
    {
        // Method begins at RVA 0x4170
	// Code size 6 (0x6)
	.maxstack 8
	IL_0000:  ldsfld valuetype GLib.GType GLib.GType::Object
	IL_0005:  ret 
    } // end of method Object::get_GType

    // method line 207
    .method private static hidebysig pinvokeimpl ("glibsharpglue-2" as "gtksharp_get_type_name" cdecl )
           default native int gtksharp_get_type_name (native int raw)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method Object::gtksharp_get_type_name

    // method line 208
    .method family hidebysig specialname 
           instance default string get_TypeName ()  cil managed 
    {
        // Method begins at RVA 0x4178
	// Code size 17 (0x11)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  callvirt instance native int class GLib.Object::get_Raw()
	IL_0006:  call native int class GLib.Object::gtksharp_get_type_name(native int)
	IL_000b:  call string class GLib.Marshaller::Utf8PtrToString(native int)
	IL_0010:  ret 
    } // end of method Object::get_TypeName

    // method line 209
    .method assembly hidebysig specialname 
           instance default valuetype GLib.GType get_NativeType ()  cil managed 
    {
        // Method begins at RVA 0x418c
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance valuetype GLib.GType class GLib.Object::LookupGType()
	IL_0006:  ret 
    } // end of method Object::get_NativeType

    // method line 210
    .method assembly hidebysig specialname 
           instance default class GLib.ToggleRef get_ToggleRef ()  cil managed 
    {
        // Method begins at RVA 0x4194
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldfld class GLib.ToggleRef GLib.Object::tref
	IL_0006:  ret 
    } // end of method Object::get_ToggleRef

    // method line 211
    .method public hidebysig specialname 
           instance default native int get_OwnedHandle ()  cil managed 
    {
        // Method begins at RVA 0x419c
	// Code size 12 (0xc)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldfld native int GLib.Object::handle
	IL_0006:  call native int class GLib.Object::g_object_ref(native int)
	IL_000b:  ret 
    } // end of method Object::get_OwnedHandle

    // method line 212
    .method famorassem hidebysig specialname 
           instance default class [mscorlib]System.Collections.Hashtable get_BeforeSignals ()  cil managed 
    {
        // Method begins at RVA 0x41ac
	// Code size 29 (0x1d)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldfld class [mscorlib]System.Collections.Hashtable GLib.Object::before_signals
	IL_0006:  brtrue IL_0016

	IL_000b:  ldarg.0 
	IL_000c:  newobj instance void class [mscorlib]System.Collections.Hashtable::'.ctor'()
	IL_0011:  stfld class [mscorlib]System.Collections.Hashtable GLib.Object::before_signals
	IL_0016:  ldarg.0 
	IL_0017:  ldfld class [mscorlib]System.Collections.Hashtable GLib.Object::before_signals
	IL_001c:  ret 
    } // end of method Object::get_BeforeSignals

    // method line 213
    .method famorassem hidebysig specialname 
           instance default class [mscorlib]System.Collections.Hashtable get_AfterSignals ()  cil managed 
    {
        // Method begins at RVA 0x41cc
	// Code size 29 (0x1d)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldfld class [mscorlib]System.Collections.Hashtable GLib.Object::after_signals
	IL_0006:  brtrue IL_0016

	IL_000b:  ldarg.0 
	IL_000c:  newobj instance void class [mscorlib]System.Collections.Hashtable::'.ctor'()
	IL_0011:  stfld class [mscorlib]System.Collections.Hashtable GLib.Object::after_signals
	IL_0016:  ldarg.0 
	IL_0017:  ldfld class [mscorlib]System.Collections.Hashtable GLib.Object::after_signals
	IL_001c:  ret 
    } // end of method Object::get_AfterSignals

    // method line 214
    .method family hidebysig specialname 
           instance default class [System]System.ComponentModel.EventHandlerList get_BeforeHandlers ()  cil managed 
    {
        // Method begins at RVA 0x41ec
	// Code size 29 (0x1d)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldfld class [System]System.ComponentModel.EventHandlerList GLib.Object::before_handlers
	IL_0006:  brtrue IL_0016

	IL_000b:  ldarg.0 
	IL_000c:  newobj instance void class [System]System.ComponentModel.EventHandlerList::'.ctor'()
	IL_0011:  stfld class [System]System.ComponentModel.EventHandlerList GLib.Object::before_handlers
	IL_0016:  ldarg.0 
	IL_0017:  ldfld class [System]System.ComponentModel.EventHandlerList GLib.Object::before_handlers
	IL_001c:  ret 
    } // end of method Object::get_BeforeHandlers

    // method line 215
    .method family hidebysig specialname 
           instance default class [System]System.ComponentModel.EventHandlerList get_AfterHandlers ()  cil managed 
    {
        // Method begins at RVA 0x420c
	// Code size 29 (0x1d)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldfld class [System]System.ComponentModel.EventHandlerList GLib.Object::after_handlers
	IL_0006:  brtrue IL_0016

	IL_000b:  ldarg.0 
	IL_000c:  newobj instance void class [System]System.ComponentModel.EventHandlerList::'.ctor'()
	IL_0011:  stfld class [System]System.ComponentModel.EventHandlerList GLib.Object::after_handlers
	IL_0016:  ldarg.0 
	IL_0017:  ldfld class [System]System.ComponentModel.EventHandlerList GLib.Object::after_handlers
	IL_001c:  ret 
    } // end of method Object::get_AfterHandlers

    // method line 216
    .method private hidebysig 
           instance default void NotifyCallback (native int handle, native int pspec, native int gch)  cil managed 
    {
        // Method begins at RVA 0x422c
	// Code size 126 (0x7e)
	.maxstack 3
	.locals init (
		class GLib.Signal	V_0,
		valuetype [mscorlib]System.Runtime.InteropServices.GCHandle	V_1,
		class GLib.NotifyArgs	V_2,
		class GLib.NotifyHandler	V_3,
		class [mscorlib]System.Exception	V_4)
	.try { // 0
	  IL_0000:  ldarg.3 
	  IL_0001:  call valuetype [mscorlib]System.Runtime.InteropServices.GCHandle valuetype [mscorlib]System.Runtime.InteropServices.GCHandle::op_Explicit(native int)
	  IL_0006:  stloc.1 
	  IL_0007:  ldloca.s 1
	  IL_0009:  call instance object valuetype [mscorlib]System.Runtime.InteropServices.GCHandle::get_Target()
	  IL_000e:  isinst GLib.Signal
	  IL_0013:  stloc.0 
	  IL_0014:  ldloc.0 
	  IL_0015:  brtrue IL_0030

	  IL_001a:  ldstr "Unknown signal GC handle received "
	  IL_001f:  ldarg.3 
	  IL_0020:  box native int
	  IL_0025:  call string string::Concat(object, object)
	  IL_002a:  newobj instance void class [mscorlib]System.Exception::'.ctor'(string)
	  IL_002f:  throw 
	  IL_0030:  newobj instance void class GLib.NotifyArgs::'.ctor'()
	  IL_0035:  stloc.2 
	  IL_0036:  ldloc.2 
	  IL_0037:  ldc.i4.1 
	  IL_0038:  newarr object
	  IL_003d:  callvirt instance void class GLib.SignalArgs::set_Args(object[])
	  IL_0042:  ldloc.2 
	  IL_0043:  callvirt instance object[] class GLib.SignalArgs::get_Args()
	  IL_0048:  ldc.i4.0 
	  IL_0049:  ldarg.2 
	  IL_004a:  box native int
	  IL_004f:  stelem.ref 
	  IL_0050:  ldloc.0 
	  IL_0051:  callvirt instance class [mscorlib]System.Delegate class GLib.Signal::get_Handler()
	  IL_0056:  castclass GLib.NotifyHandler
	  IL_005b:  stloc.3 
	  IL_005c:  ldloc.3 
	  IL_005d:  ldarg.1 
	  IL_005e:  call class GLib.Object class GLib.Object::GetObject(native int)
	  IL_0063:  ldloc.2 
	  IL_0064:  callvirt instance void class GLib.NotifyHandler::Invoke(object, class GLib.NotifyArgs)
	  IL_0069:  leave IL_007d

	} // end .try 0
	catch class [mscorlib]System.Exception { // 0
	  IL_006e:  stloc.s 4
	  IL_0070:  ldloc.s 4
	  IL_0072:  ldc.i4.0 
	  IL_0073:  call void class GLib.ExceptionManager::RaiseUnhandledException(class [mscorlib]System.Exception, bool)
	  IL_0078:  leave IL_007d

	} // end handler 0
	IL_007d:  ret 
    } // end of method Object::NotifyCallback

    // method line 217
    .method private hidebysig 
           instance default void ConnectNotification (string signal, class GLib.NotifyHandler 'handler')  cil managed 
    {
        // Method begins at RVA 0x42c8
	// Code size 28 (0x1c)
	.maxstack 4
	.locals init (
		class GLib.Signal	V_0)
	IL_0000:  ldarg.0 
	IL_0001:  ldarg.1 
	IL_0002:  ldarg.0 
	IL_0003:  ldftn instance void class GLib.Object::NotifyCallback(native int, native int, native int)
	IL_0009:  newobj instance void class GLib.Object/NotifyDelegate::'.ctor'(object, native int)
	IL_000e:  call class GLib.Signal class GLib.Signal::Lookup(class GLib.Object, string, class [mscorlib]System.Delegate)
	IL_0013:  stloc.0 
	IL_0014:  ldloc.0 
	IL_0015:  ldarg.2 
	IL_0016:  callvirt instance void class GLib.Signal::AddDelegate(class [mscorlib]System.Delegate)
	IL_001b:  ret 
    } // end of method Object::ConnectNotification

    // method line 218
    .method public hidebysig 
           instance default void AddNotification (string 'property', class GLib.NotifyHandler 'handler')  cil managed 
    {
        // Method begins at RVA 0x42f0
	// Code size 19 (0x13)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldstr "notify::"
	IL_0006:  ldarg.1 
	IL_0007:  call string string::Concat(string, string)
	IL_000c:  ldarg.2 
	IL_000d:  call instance void class GLib.Object::ConnectNotification(string, class GLib.NotifyHandler)
	IL_0012:  ret 
    } // end of method Object::AddNotification

    // method line 219
    .method public hidebysig 
           instance default void AddNotification (class GLib.NotifyHandler 'handler')  cil managed 
    {
        // Method begins at RVA 0x4304
	// Code size 13 (0xd)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldstr "notify"
	IL_0006:  ldarg.1 
	IL_0007:  call instance void class GLib.Object::ConnectNotification(string, class GLib.NotifyHandler)
	IL_000c:  ret 
    } // end of method Object::AddNotification

    // method line 220
    .method private hidebysig 
           instance default void DisconnectNotification (string signal, class GLib.NotifyHandler 'handler')  cil managed 
    {
        // Method begins at RVA 0x4314
	// Code size 28 (0x1c)
	.maxstack 4
	.locals init (
		class GLib.Signal	V_0)
	IL_0000:  ldarg.0 
	IL_0001:  ldarg.1 
	IL_0002:  ldarg.0 
	IL_0003:  ldftn instance void class GLib.Object::NotifyCallback(native int, native int, native int)
	IL_0009:  newobj instance void class GLib.Object/NotifyDelegate::'.ctor'(object, native int)
	IL_000e:  call class GLib.Signal class GLib.Signal::Lookup(class GLib.Object, string, class [mscorlib]System.Delegate)
	IL_0013:  stloc.0 
	IL_0014:  ldloc.0 
	IL_0015:  ldarg.2 
	IL_0016:  callvirt instance void class GLib.Signal::RemoveDelegate(class [mscorlib]System.Delegate)
	IL_001b:  ret 
    } // end of method Object::DisconnectNotification

    // method line 221
    .method public hidebysig 
           instance default void RemoveNotification (string 'property', class GLib.NotifyHandler 'handler')  cil managed 
    {
        // Method begins at RVA 0x433c
	// Code size 19 (0x13)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldstr "notify::"
	IL_0006:  ldarg.1 
	IL_0007:  call string string::Concat(string, string)
	IL_000c:  ldarg.2 
	IL_000d:  call instance void class GLib.Object::DisconnectNotification(string, class GLib.NotifyHandler)
	IL_0012:  ret 
    } // end of method Object::RemoveNotification

    // method line 222
    .method public hidebysig 
           instance default void RemoveNotification (class GLib.NotifyHandler 'handler')  cil managed 
    {
        // Method begins at RVA 0x4350
	// Code size 13 (0xd)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldstr "notify"
	IL_0006:  ldarg.1 
	IL_0007:  call instance void class GLib.Object::DisconnectNotification(string, class GLib.NotifyHandler)
	IL_000c:  ret 
    } // end of method Object::RemoveNotification

    // method line 223
    .method public virtual hidebysig 
           instance default int32 GetHashCode ()  cil managed 
    {
        // Method begins at RVA 0x4360
	// Code size 21 (0x15)
	.maxstack 1
	.locals init (
		native int	V_0)
	IL_0000:  ldarg.0 
	IL_0001:  call instance native int class GLib.Object::get_Handle()
	IL_0006:  stloc.0 
	IL_0007:  ldloca.s 0
	IL_0009:  constrained. native int
	IL_000f:  callvirt instance int32 object::GetHashCode()
	IL_0014:  ret 
    } // end of method Object::GetHashCode

    // method line 224
    .method public hidebysig specialname 
           instance default class [mscorlib]System.Collections.Hashtable get_Data ()  cil managed 
    {
        // Method begins at RVA 0x4384
	// Code size 29 (0x1d)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldfld class [mscorlib]System.Collections.Hashtable GLib.Object::data
	IL_0006:  brtrue IL_0016

	IL_000b:  ldarg.0 
	IL_000c:  newobj instance void class [mscorlib]System.Collections.Hashtable::'.ctor'()
	IL_0011:  stfld class [mscorlib]System.Collections.Hashtable GLib.Object::data
	IL_0016:  ldarg.0 
	IL_0017:  ldfld class [mscorlib]System.Collections.Hashtable GLib.Object::data
	IL_001c:  ret 
    } // end of method Object::get_Data

    // method line 225
    .method family hidebysig specialname 
           instance default class [mscorlib]System.Collections.Hashtable get_PersistentData ()  cil managed 
    {
        // Method begins at RVA 0x43a4
	// Code size 29 (0x1d)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldfld class [mscorlib]System.Collections.Hashtable GLib.Object::persistent_data
	IL_0006:  brtrue IL_0016

	IL_000b:  ldarg.0 
	IL_000c:  newobj instance void class [mscorlib]System.Collections.Hashtable::'.ctor'()
	IL_0011:  stfld class [mscorlib]System.Collections.Hashtable GLib.Object::persistent_data
	IL_0016:  ldarg.0 
	IL_0017:  ldfld class [mscorlib]System.Collections.Hashtable GLib.Object::persistent_data
	IL_001c:  ret 
    } // end of method Object::get_PersistentData

    // method line 226
    .method private static hidebysig pinvokeimpl ("libgobject-2.0-0.dll" as "g_object_get_property" cdecl )
           default void g_object_get_property (native int obj, native int name, valuetype GLib.Value& val)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method Object::g_object_get_property

    // method line 227
    .method family hidebysig 
           instance default valuetype GLib.Value GetProperty (string name)  cil managed 
    {
        // Method begins at RVA 0x43c4
	// Code size 38 (0x26)
	.maxstack 3
	.locals init (
		native int	V_0,
		valuetype GLib.Value	V_1)
	IL_0000:  ldarg.1 
	IL_0001:  call native int class GLib.Marshaller::StringToPtrGStrdup(string)
	IL_0006:  stloc.0 
	IL_0007:  ldloca.s 1
	IL_0009:  ldarg.0 
	IL_000a:  ldloc.0 
	IL_000b:  call instance void valuetype GLib.Value::'.ctor'(class GLib.Object, native int)
	IL_0010:  ldarg.0 
	IL_0011:  callvirt instance native int class GLib.Object::get_Raw()
	IL_0016:  ldloc.0 
	IL_0017:  ldloca.s 1
	IL_0019:  call void class GLib.Object::g_object_get_property(native int, native int, [out] valuetype GLib.Value&)
	IL_001e:  ldloc.0 
	IL_001f:  call void class GLib.Marshaller::Free(native int)
	IL_0024:  ldloc.1 
	IL_0025:  ret 
    } // end of method Object::GetProperty

    // method line 228
    .method private static hidebysig pinvokeimpl ("libgobject-2.0-0.dll" as "g_object_set_property" cdecl )
           default void g_object_set_property (native int obj, native int name, valuetype GLib.Value& val)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method Object::g_object_set_property

    // method line 229
    .method family hidebysig 
           instance default void SetProperty (string name, valuetype GLib.Value val)  cil managed 
    {
        // Method begins at RVA 0x43f8
	// Code size 28 (0x1c)
	.maxstack 3
	.locals init (
		native int	V_0)
	IL_0000:  ldarg.1 
	IL_0001:  call native int class GLib.Marshaller::StringToPtrGStrdup(string)
	IL_0006:  stloc.0 
	IL_0007:  ldarg.0 
	IL_0008:  callvirt instance native int class GLib.Object::get_Raw()
	IL_000d:  ldloc.0 
	IL_000e:  ldarga.s 2
	IL_0010:  call void class GLib.Object::g_object_set_property(native int, native int, [out] valuetype GLib.Value&)
	IL_0015:  ldloc.0 
	IL_0016:  call void class GLib.Marshaller::Free(native int)
	IL_001b:  ret 
    } // end of method Object::SetProperty

    // method line 230
    .method private static hidebysig pinvokeimpl ("libgobject-2.0-0.dll" as "g_object_notify" cdecl )
           default void g_object_notify (native int obj, native int property_name)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method Object::g_object_notify

    // method line 231
    .method family hidebysig 
           instance default void Notify (string property_name)  cil managed 
    {
        // Method begins at RVA 0x4420
	// Code size 26 (0x1a)
	.maxstack 2
	.locals init (
		native int	V_0)
	IL_0000:  ldarg.1 
	IL_0001:  call native int class GLib.Marshaller::StringToPtrGStrdup(string)
	IL_0006:  stloc.0 
	IL_0007:  ldarg.0 
	IL_0008:  call instance native int class GLib.Object::get_Handle()
	IL_000d:  ldloc.0 
	IL_000e:  call void class GLib.Object::g_object_notify(native int, native int)
	IL_0013:  ldloc.0 
	IL_0014:  call void class GLib.Marshaller::Free(native int)
	IL_0019:  ret 
    } // end of method Object::Notify

    // method line 232
    .method private static hidebysig pinvokeimpl ("glibsharpglue-2" as "gtksharp_override_virtual_method" cdecl )
           default void gtksharp_override_virtual_method (native int gtype, native int name, class [mscorlib]System.Delegate cb)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method Object::gtksharp_override_virtual_method

    // method line 233
    .method family static hidebysig 
           default void OverrideVirtualMethod (valuetype GLib.GType gtype, string name, class [mscorlib]System.Delegate cb)  cil managed 
    {
        // Method begins at RVA 0x4448
	// Code size 28 (0x1c)
	.maxstack 3
	.locals init (
		native int	V_0)
	IL_0000:  ldarg.1 
	IL_0001:  call native int class GLib.Marshaller::StringToPtrGStrdup(string)
	IL_0006:  stloc.0 
	IL_0007:  ldarga.s 0
	IL_0009:  call instance native int valuetype GLib.GType::get_Val()
	IL_000e:  ldloc.0 
	IL_000f:  ldarg.2 
	IL_0010:  call void class GLib.Object::gtksharp_override_virtual_method(native int, native int, class [mscorlib]System.Delegate)
	IL_0015:  ldloc.0 
	IL_0016:  call void class GLib.Marshaller::Free(native int)
	IL_001b:  ret 
    } // end of method Object::OverrideVirtualMethod

    // method line 234
    .method family static hidebysig pinvokeimpl ("libgobject-2.0-0.dll" as "g_signal_chain_from_overridden" cdecl )
           default void g_signal_chain_from_overridden (native int args, valuetype GLib.Value& 'retval')  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method Object::g_signal_chain_from_overridden

    // method line 235
    .method private static hidebysig pinvokeimpl ("glibsharpglue-2" as "gtksharp_is_object" cdecl )
           default bool gtksharp_is_object (native int obj)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method Object::gtksharp_is_object

    // method line 236
    .method assembly static hidebysig 
           default bool IsObject (native int obj)  cil managed 
    {
        // Method begins at RVA 0x4470
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call bool class GLib.Object::gtksharp_is_object(native int)
	IL_0006:  ret 
    } // end of method Object::IsObject

    // method line 237
    .method private static hidebysig pinvokeimpl ("glibsharpglue-2" as "gtksharp_object_get_ref_count" cdecl )
           default int32 gtksharp_object_get_ref_count (native int obj)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method Object::gtksharp_object_get_ref_count

    // method line 238
    .method family hidebysig specialname 
           instance default int32 get_RefCount ()  cil managed 
    {
        // Method begins at RVA 0x4478
	// Code size 12 (0xc)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance native int class GLib.Object::get_Handle()
	IL_0006:  call int32 class GLib.Object::gtksharp_object_get_ref_count(native int)
	IL_000b:  ret 
    } // end of method Object::get_RefCount

    // method line 239
    .method assembly hidebysig 
           instance default void Harden ()  cil managed 
    {
        // Method begins at RVA 0x4488
	// Code size 12 (0xc)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldfld class GLib.ToggleRef GLib.Object::tref
	IL_0006:  callvirt instance void class GLib.ToggleRef::Harden()
	IL_000b:  ret 
    } // end of method Object::Harden

	.property class [mscorlib]System.Collections.Generic.Dictionary`2<native int, class [mscorlib]System.Reflection.PropertyInfo> Properties ()
	{
		.get default class [mscorlib]System.Collections.Generic.Dictionary`2<native int, class [mscorlib]System.Reflection.PropertyInfo> GLib.Object::get_Properties () 
	}
	.property class GLib.Object/GetPropertyDelegate GetPropertyHandler ()
	{
		.get default class GLib.Object/GetPropertyDelegate GLib.Object::get_GetPropertyHandler () 
	}
	.property class GLib.Object/SetPropertyDelegate SetPropertyHandler ()
	{
		.get default class GLib.Object/SetPropertyDelegate GLib.Object::get_SetPropertyHandler () 
	}
	.property instance native int Raw ()
	{
		.get instance default native int GLib.Object::get_Raw () 
		.set instance default void GLib.Object::set_Raw (native int 'value') 
	}
	.property valuetype GLib.GType GType ()
	{
		.get default valuetype GLib.GType GLib.Object::get_GType () 
	}
	.property instance string TypeName ()
	{
		.get instance default string GLib.Object::get_TypeName () 
	}
	.property instance valuetype GLib.GType NativeType ()
	{
		.get instance default valuetype GLib.GType GLib.Object::get_NativeType () 
	}
	.property instance class GLib.ToggleRef ToggleRef ()
	{
		.get instance default class GLib.ToggleRef GLib.Object::get_ToggleRef () 
	}
	.property instance native int Handle ()
	{
		.get instance default native int GLib.Object::get_Handle () 
	}
	.property instance native int OwnedHandle ()
	{
		.custom instance void class [mscorlib]System.Diagnostics.DebuggerBrowsableAttribute::'.ctor'(valuetype [mscorlib]System.Diagnostics.DebuggerBrowsableState) =  (01 00 00 00 00 00 00 00 ) // ........

		.get instance default native int GLib.Object::get_OwnedHandle () 
	}
	.property instance class [mscorlib]System.Collections.Hashtable BeforeSignals ()
	{
		.custom instance void class [mscorlib]System.ObsoleteAttribute::'.ctor'(string) =  (
		01 00 2D 52 65 70 6C 61 63 65 64 20 62 79 20 47   // ..-Replaced by G
		4C 69 62 2E 53 69 67 6E 61 6C 20 6D 61 72 73 68   // Lib.Signal marsh
		61 6C 69 6E 67 20 6D 65 63 68 61 6E 69 73 6D 2E   // aling mechanism.
		00 00                                           ) // ..

		.get instance default class [mscorlib]System.Collections.Hashtable GLib.Object::get_BeforeSignals () 
	}
	.property instance class [mscorlib]System.Collections.Hashtable AfterSignals ()
	{
		.custom instance void class [mscorlib]System.ObsoleteAttribute::'.ctor'(string) =  (
		01 00 2D 52 65 70 6C 61 63 65 64 20 62 79 20 47   // ..-Replaced by G
		4C 69 62 2E 53 69 67 6E 61 6C 20 6D 61 72 73 68   // Lib.Signal marsh
		61 6C 69 6E 67 20 6D 65 63 68 61 6E 69 73 6D 2E   // aling mechanism.
		00 00                                           ) // ..

		.get instance default class [mscorlib]System.Collections.Hashtable GLib.Object::get_AfterSignals () 
	}
	.property instance class [System]System.ComponentModel.EventHandlerList BeforeHandlers ()
	{
		.custom instance void class [mscorlib]System.ObsoleteAttribute::'.ctor'(string) =  (
		01 00 2D 52 65 70 6C 61 63 65 64 20 62 79 20 47   // ..-Replaced by G
		4C 69 62 2E 53 69 67 6E 61 6C 20 6D 61 72 73 68   // Lib.Signal marsh
		61 6C 69 6E 67 20 6D 65 63 68 61 6E 69 73 6D 2E   // aling mechanism.
		00 00                                           ) // ..

		.get instance default class [System]System.ComponentModel.EventHandlerList GLib.Object::get_BeforeHandlers () 
	}
	.property instance class [System]System.ComponentModel.EventHandlerList AfterHandlers ()
	{
		.custom instance void class [mscorlib]System.ObsoleteAttribute::'.ctor'(string) =  (
		01 00 2D 52 65 70 6C 61 63 65 64 20 62 79 20 47   // ..-Replaced by G
		4C 69 62 2E 53 69 67 6E 61 6C 20 6D 61 72 73 68   // Lib.Signal marsh
		61 6C 69 6E 67 20 6D 65 63 68 61 6E 69 73 6D 2E   // aling mechanism.
		00 00                                           ) // ..

		.get instance default class [System]System.ComponentModel.EventHandlerList GLib.Object::get_AfterHandlers () 
	}
	.property instance class [mscorlib]System.Collections.Hashtable Data ()
	{
		.get instance default class [mscorlib]System.Collections.Hashtable GLib.Object::get_Data () 
	}
	.property instance class [mscorlib]System.Collections.Hashtable PersistentData ()
	{
		.get instance default class [mscorlib]System.Collections.Hashtable GLib.Object::get_PersistentData () 
	}
	.property instance int32 RefCount ()
	{
		.get instance default int32 GLib.Object::get_RefCount () 
	}
  .class nested private auto ansi sealed GetPropertyDelegate
  	extends [mscorlib]System.MulticastDelegate
  {
    .custom instance void class [mscorlib]System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute::'.ctor'(valuetype [mscorlib]System.Runtime.InteropServices.CallingConvention) =  (01 00 02 00 00 00 00 00 ) // ........


    // method line 528
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (object 'object', native int 'method')  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method GetPropertyDelegate::.ctor

    // method line 529
    .method public virtual hidebysig newslot 
           instance default void Invoke (native int GObject, unsigned int32 property_id, valuetype GLib.Value& 'value', native int pspec)  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method GetPropertyDelegate::Invoke

    // method line 530
    .method public virtual hidebysig newslot 
           instance default class [mscorlib]System.IAsyncResult BeginInvoke (native int GObject, unsigned int32 property_id, valuetype GLib.Value& 'value', native int pspec, class [mscorlib]System.AsyncCallback callback, object 'object')  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method GetPropertyDelegate::BeginInvoke

    // method line 531
    .method public virtual hidebysig newslot 
           instance default void EndInvoke (valuetype GLib.Value& 'value', class [mscorlib]System.IAsyncResult result)  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method GetPropertyDelegate::EndInvoke

  } // end of class GetPropertyDelegate

  .class nested private auto ansi sealed SetPropertyDelegate
  	extends [mscorlib]System.MulticastDelegate
  {
    .custom instance void class [mscorlib]System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute::'.ctor'(valuetype [mscorlib]System.Runtime.InteropServices.CallingConvention) =  (01 00 02 00 00 00 00 00 ) // ........


    // method line 532
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (object 'object', native int 'method')  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method SetPropertyDelegate::.ctor

    // method line 533
    .method public virtual hidebysig newslot 
           instance default void Invoke (native int GObject, unsigned int32 property_id, valuetype GLib.Value& 'value', native int pspec)  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method SetPropertyDelegate::Invoke

    // method line 534
    .method public virtual hidebysig newslot 
           instance default class [mscorlib]System.IAsyncResult BeginInvoke (native int GObject, unsigned int32 property_id, valuetype GLib.Value& 'value', native int pspec, class [mscorlib]System.AsyncCallback callback, object 'object')  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method SetPropertyDelegate::BeginInvoke

    // method line 535
    .method public virtual hidebysig newslot 
           instance default void EndInvoke (valuetype GLib.Value& 'value', class [mscorlib]System.IAsyncResult result)  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method SetPropertyDelegate::EndInvoke

  } // end of class SetPropertyDelegate

  .class nested private auto ansi sealed NotifyDelegate
  	extends [mscorlib]System.MulticastDelegate
  {
    .custom instance void class [mscorlib]System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute::'.ctor'(valuetype [mscorlib]System.Runtime.InteropServices.CallingConvention) =  (01 00 02 00 00 00 00 00 ) // ........


    // method line 536
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (object 'object', native int 'method')  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method NotifyDelegate::.ctor

    // method line 537
    .method public virtual hidebysig newslot 
           instance default void Invoke (native int handle, native int pspec, native int gch)  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method NotifyDelegate::Invoke

    // method line 538
    .method public virtual hidebysig newslot 
           instance default class [mscorlib]System.IAsyncResult BeginInvoke (native int handle, native int pspec, native int gch, class [mscorlib]System.AsyncCallback callback, object 'object')  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method NotifyDelegate::BeginInvoke

    // method line 539
    .method public virtual hidebysig newslot 
           instance default void EndInvoke (class [mscorlib]System.IAsyncResult result)  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method NotifyDelegate::EndInvoke

  } // end of class NotifyDelegate

  } // end of class GLib.Object
}

.namespace GLib
{
  .class public auto ansi beforefieldinit InitiallyUnowned
  	extends GLib.Object
  {

    // method line 240
    .method family hidebysig specialname rtspecialname 
           instance default void '.ctor' (native int raw)  cil managed 
    {
        // Method begins at RVA 0x4498
	// Code size 8 (0x8)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldarg.1 
	IL_0002:  call instance void class GLib.Object::'.ctor'(native int)
	IL_0007:  ret 
    } // end of method InitiallyUnowned::.ctor

    // method line 241
    .method family hidebysig specialname rtspecialname 
           instance default void '.ctor' (valuetype GLib.GType gtype)  cil managed 
    {
        .custom instance void class [mscorlib]System.ObsoleteAttribute::'.ctor'() =  (01 00 00 00 ) // ....

        // Method begins at RVA 0x44a4
	// Code size 8 (0x8)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldarg.1 
	IL_0002:  call instance void class GLib.Object::'.ctor'(valuetype GLib.GType)
	IL_0007:  ret 
    } // end of method InitiallyUnowned::.ctor

    // method line 242
    .method public static hidebysig specialname 
           default valuetype GLib.GType get_GType ()  cil managed 
    {
        // Method begins at RVA 0x44b0
	// Code size 6 (0x6)
	.maxstack 8
	IL_0000:  ldsfld valuetype GLib.GType GLib.GType::Object
	IL_0005:  ret 
    } // end of method InitiallyUnowned::get_GType

	.property valuetype GLib.GType GType ()
	{
		.get default valuetype GLib.GType GLib.InitiallyUnowned::get_GType () 
	}
  } // end of class GLib.InitiallyUnowned
}

.namespace GLib
{
  .class private auto ansi beforefieldinit IntPtrEqualityComparer
  	extends [mscorlib]System.Object
  	implements class [mscorlib]System.Collections.Generic.IEqualityComparer`1<native int>  {
    .field  public static initonly  class [mscorlib]System.Collections.Generic.IEqualityComparer`1<native int> Instance

    // method line 243
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' ()  cil managed 
    {
        // Method begins at RVA 0x44b8
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::'.ctor'()
	IL_0006:  ret 
    } // end of method IntPtrEqualityComparer::.ctor

    // method line 244
    .method public final virtual hidebysig newslot 
           instance default bool Equals (native int x, native int y)  cil managed 
    {
        // Method begins at RVA 0x44c0
	// Code size 8 (0x8)
	.maxstack 8
	IL_0000:  ldarg.1 
	IL_0001:  ldarg.2 
	IL_0002:  call bool native int::op_Equality(native int, native int)
	IL_0007:  ret 
    } // end of method IntPtrEqualityComparer::Equals

    // method line 245
    .method public final virtual hidebysig newslot 
           instance default int32 GetHashCode (native int obj)  cil managed 
    {
        // Method begins at RVA 0x44cc
	// Code size 14 (0xe)
	.maxstack 8
	IL_0000:  ldarga.s 1
	IL_0002:  constrained. native int
	IL_0008:  callvirt instance int32 object::GetHashCode()
	IL_000d:  ret 
    } // end of method IntPtrEqualityComparer::GetHashCode

    // method line 246
    .method private static hidebysig specialname rtspecialname 
           default void '.cctor' ()  cil managed 
    {
        // Method begins at RVA 0x44dc
	// Code size 11 (0xb)
	.maxstack 8
	IL_0000:  newobj instance void class GLib.IntPtrEqualityComparer::'.ctor'()
	IL_0005:  stsfld class [mscorlib]System.Collections.Generic.IEqualityComparer`1<native int> GLib.IntPtrEqualityComparer::Instance
	IL_000a:  ret 
    } // end of method IntPtrEqualityComparer::.cctor

  } // end of class GLib.IntPtrEqualityComparer
}

.namespace GLibSharp
{
  .class private auto ansi sealed IOFuncNative
  	extends [mscorlib]System.MulticastDelegate
  {
    .custom instance void class [mscorlib]System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute::'.ctor'(valuetype [mscorlib]System.Runtime.InteropServices.CallingConvention) =  (01 00 02 00 00 00 00 00 ) // ........


    // method line 247
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (object 'object', native int 'method')  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method IOFuncNative::.ctor

    // method line 248
    .method public virtual hidebysig newslot 
           instance default bool Invoke (native int source, int32 condition, native int data)  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method IOFuncNative::Invoke

    // method line 249
    .method public virtual hidebysig newslot 
           instance default class [mscorlib]System.IAsyncResult BeginInvoke (native int source, int32 condition, native int data, class [mscorlib]System.AsyncCallback callback, object 'object')  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method IOFuncNative::BeginInvoke

    // method line 250
    .method public virtual hidebysig newslot 
           instance default bool EndInvoke (class [mscorlib]System.IAsyncResult result)  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method IOFuncNative::EndInvoke

  } // end of class GLibSharp.IOFuncNative
}

.namespace GLibSharp
{
  .class private auto ansi beforefieldinit IOFuncWrapper
  	extends [mscorlib]System.Object
  {
    .field  private  class GLib.IOFunc 'managed'
    .field  public  class GLibSharp.IOFuncNative NativeDelegate

    // method line 251
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (class GLib.IOFunc 'managed')  cil managed 
    {
        // Method begins at RVA 0x44e8
	// Code size 32 (0x20)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::'.ctor'()
	IL_0006:  ldarg.0 
	IL_0007:  ldarg.1 
	IL_0008:  stfld class GLib.IOFunc GLibSharp.IOFuncWrapper::'managed'
	IL_000d:  ldarg.0 
	IL_000e:  ldarg.0 
	IL_000f:  ldftn instance bool class GLibSharp.IOFuncWrapper::NativeCallback(native int, int32, native int)
	IL_0015:  newobj instance void class GLibSharp.IOFuncNative::'.ctor'(object, native int)
	IL_001a:  stfld class GLibSharp.IOFuncNative GLibSharp.IOFuncWrapper::NativeDelegate
	IL_001f:  ret 
    } // end of method IOFuncWrapper::.ctor

    // method line 252
    .method private hidebysig 
           instance default bool NativeCallback (native int source, int32 condition, native int data)  cil managed 
    {
        // Method begins at RVA 0x450c
	// Code size 41 (0x29)
	.maxstack 3
	.locals init (
		bool	V_0,
		class [mscorlib]System.Exception	V_1)
	.try { // 0
	  IL_0000:  ldarg.0 
	  IL_0001:  ldfld class GLib.IOFunc GLibSharp.IOFuncWrapper::'managed'
	  IL_0006:  ldarg.1 
	  IL_0007:  call class GLib.IOChannel class GLib.IOChannel::FromHandle(native int)
	  IL_000c:  ldarg.2 
	  IL_000d:  callvirt instance bool class GLib.IOFunc::Invoke(class GLib.IOChannel, valuetype GLib.IOCondition)
	  IL_0012:  stloc.0 
	  IL_0013:  leave IL_0027

	} // end .try 0
	catch class [mscorlib]System.Exception { // 0
	  IL_0018:  stloc.1 
	  IL_0019:  ldloc.1 
	  IL_001a:  ldc.i4.0 
	  IL_001b:  call void class GLib.ExceptionManager::RaiseUnhandledException(class [mscorlib]System.Exception, bool)
	  IL_0020:  ldc.i4.0 
	  IL_0021:  stloc.0 
	  IL_0022:  leave IL_0027

	} // end handler 0
	IL_0027:  ldloc.0 
	IL_0028:  ret 
    } // end of method IOFuncWrapper::NativeCallback

  } // end of class GLibSharp.IOFuncWrapper
}

.namespace GLib
{
  .class public auto ansi beforefieldinit IOChannel
  	extends [mscorlib]System.Object
  	implements [mscorlib]System.IDisposable, GLib.IWrapper  {
    .field  private  native int handle
    .field private static literal  string libname = "libglib-2.0-0.dll"

    // method line 253
    .method public static hidebysig 
           default class GLib.IOChannel FromHandle (native int handle)  cil managed 
    {
        // Method begins at RVA 0x4554
	// Code size 32 (0x20)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_0006:  call bool native int::op_Equality(native int, native int)
	IL_000b:  brfalse IL_0012

	IL_0010:  ldnull 
	IL_0011:  ret 
	IL_0012:  ldarg.0 
	IL_0013:  call native int class GLib.IOChannel::g_io_channel_ref(native int)
	IL_0018:  pop 
	IL_0019:  ldarg.0 
	IL_001a:  newobj instance void class GLib.IOChannel::'.ctor'(native int)
	IL_001f:  ret 
    } // end of method IOChannel::FromHandle

    // method line 254
    .method private hidebysig specialname rtspecialname 
           instance default void '.ctor' (native int handle)  cil managed 
    {
        // Method begins at RVA 0x4578
	// Code size 14 (0xe)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::'.ctor'()
	IL_0006:  ldarg.0 
	IL_0007:  ldarg.1 
	IL_0008:  stfld native int GLib.IOChannel::handle
	IL_000d:  ret 
    } // end of method IOChannel::.ctor

    // method line 255
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (int32 fd)  cil managed 
    {
        // Method begins at RVA 0x4588
	// Code size 13 (0xd)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldarg.1 
	IL_0002:  call native int class GLib.IOChannel::g_io_channel_unix_new(int32)
	IL_0007:  call instance void class GLib.IOChannel::'.ctor'(native int)
	IL_000c:  ret 
    } // end of method IOChannel::.ctor

    // method line 256
    .method private static hidebysig pinvokeimpl ("libglib-2.0-0.dll" as "g_io_channel_unix_new" cdecl )
           default native int g_io_channel_unix_new (int32 fd)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method IOChannel::g_io_channel_unix_new

    // method line 257
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (string filename, string mode)  cil managed 
    {
        // Method begins at RVA 0x4598
	// Code size 101 (0x65)
	.maxstack 4
	.locals init (
		native int	V_0,
		native int	V_1,
		native int	V_2)
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::'.ctor'()
	IL_0006:  ldarg.1 
	IL_0007:  call native int class GLib.Marshaller::StringToPtrGStrdup(string)
	IL_000c:  stloc.0 
	IL_000d:  ldarg.2 
	IL_000e:  call native int class GLib.Marshaller::StringToPtrGStrdup(string)
	IL_0013:  stloc.1 
	IL_0014:  call bool class GLib.Global::get_IsWindowsPlatform()
	IL_0019:  brfalse IL_0032

	IL_001e:  ldarg.0 
	IL_001f:  ldloc.0 
	IL_0020:  ldloc.1 
	IL_0021:  ldloca.s 2
	IL_0023:  call native int class GLib.IOChannel::g_io_channel_new_file_utf8(native int, native int, [out] native int&)
	IL_0028:  stfld native int GLib.IOChannel::handle
	IL_002d:  br IL_0041

	IL_0032:  ldarg.0 
	IL_0033:  ldloc.0 
	IL_0034:  ldloc.1 
	IL_0035:  ldloca.s 2
	IL_0037:  call native int class GLib.IOChannel::g_io_channel_new_file(native int, native int, [out] native int&)
	IL_003c:  stfld native int GLib.IOChannel::handle
	IL_0041:  ldloc.0 
	IL_0042:  call void class GLib.Marshaller::Free(native int)
	IL_0047:  ldloc.1 
	IL_0048:  call void class GLib.Marshaller::Free(native int)
	IL_004d:  ldloc.2 
	IL_004e:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_0053:  call bool native int::op_Inequality(native int, native int)
	IL_0058:  brfalse IL_0064

	IL_005d:  ldloc.2 
	IL_005e:  newobj instance void class GLib.GException::'.ctor'(native int)
	IL_0063:  throw 
	IL_0064:  ret 
    } // end of method IOChannel::.ctor

    // method line 258
    .method private static hidebysig pinvokeimpl ("libglib-2.0-0.dll" as "g_io_channel_new_file_utf8" cdecl )
           default native int g_io_channel_new_file_utf8 (native int filename, native int mode, [out] native int& 'error')  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method IOChannel::g_io_channel_new_file_utf8

    // method line 259
    .method private static hidebysig pinvokeimpl ("libglib-2.0-0.dll" as "g_io_channel_new_file" cdecl )
           default native int g_io_channel_new_file (native int filename, native int mode, [out] native int& 'error')  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method IOChannel::g_io_channel_new_file

    // method line 260
    .method public hidebysig specialname 
           instance default valuetype GLib.IOCondition get_BufferCondition ()  cil managed 
    {
        // Method begins at RVA 0x460c
	// Code size 12 (0xc)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance native int class GLib.IOChannel::get_Handle()
	IL_0006:  call int32 class GLib.IOChannel::g_io_channel_get_buffer_condition(native int)
	IL_000b:  ret 
    } // end of method IOChannel::get_BufferCondition

    // method line 261
    .method public final virtual hidebysig newslot specialname 
           instance default native int get_Handle ()  cil managed 
    {
        // Method begins at RVA 0x461c
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldfld native int GLib.IOChannel::handle
	IL_0006:  ret 
    } // end of method IOChannel::get_Handle

    // method line 262
    .method private static hidebysig pinvokeimpl ("libglib-2.0-0.dll" as "g_io_channel_get_buffer_condition" cdecl )
           default int32 g_io_channel_get_buffer_condition (native int raw)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method IOChannel::g_io_channel_get_buffer_condition

    // method line 263
    .method public hidebysig specialname 
           instance default bool get_Buffered ()  cil managed 
    {
        // Method begins at RVA 0x4624
	// Code size 12 (0xc)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance native int class GLib.IOChannel::get_Handle()
	IL_0006:  call bool class GLib.IOChannel::g_io_channel_get_buffered(native int)
	IL_000b:  ret 
    } // end of method IOChannel::get_Buffered

    // method line 264
    .method private static hidebysig pinvokeimpl ("libglib-2.0-0.dll" as "g_io_channel_get_buffered" cdecl )
           default bool g_io_channel_get_buffered (native int raw)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method IOChannel::g_io_channel_get_buffered

    // method line 265
    .method public hidebysig specialname 
           instance default void set_Buffered (bool 'value')  cil managed 
    {
        // Method begins at RVA 0x4634
	// Code size 13 (0xd)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance native int class GLib.IOChannel::get_Handle()
	IL_0006:  ldarg.1 
	IL_0007:  call void class GLib.IOChannel::g_io_channel_set_buffered(native int, bool)
	IL_000c:  ret 
    } // end of method IOChannel::set_Buffered

    // method line 266
    .method private static hidebysig pinvokeimpl ("libglib-2.0-0.dll" as "g_io_channel_set_buffered" cdecl )
           default void g_io_channel_set_buffered (native int raw, bool buffered)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method IOChannel::g_io_channel_set_buffered

    // method line 267
    .method public hidebysig specialname 
           instance default unsigned int64 get_BufferSize ()  cil managed 
    {
        // Method begins at RVA 0x4644
	// Code size 17 (0x11)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance native int class GLib.IOChannel::get_Handle()
	IL_0006:  call native unsigned int class GLib.IOChannel::g_io_channel_get_buffer_size(native int)
	IL_000b:  call unsigned int64 native unsigned int::op_Explicit(native unsigned int)
	IL_0010:  ret 
    } // end of method IOChannel::get_BufferSize

    // method line 268
    .method private static hidebysig pinvokeimpl ("libglib-2.0-0.dll" as "g_io_channel_get_buffer_size" cdecl )
           default native unsigned int g_io_channel_get_buffer_size (native int raw)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method IOChannel::g_io_channel_get_buffer_size

    // method line 269
    .method public hidebysig specialname 
           instance default void set_BufferSize (unsigned int64 'value')  cil managed 
    {
        // Method begins at RVA 0x4658
	// Code size 18 (0x12)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance native int class GLib.IOChannel::get_Handle()
	IL_0006:  ldarg.1 
	IL_0007:  newobj instance void native unsigned int::'.ctor'(unsigned int64)
	IL_000c:  call void class GLib.IOChannel::g_io_channel_set_buffer_size(native int, native unsigned int)
	IL_0011:  ret 
    } // end of method IOChannel::set_BufferSize

    // method line 270
    .method private static hidebysig pinvokeimpl ("libglib-2.0-0.dll" as "g_io_channel_set_buffer_size" cdecl )
           default void g_io_channel_set_buffer_size (native int raw, native unsigned int size)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method IOChannel::g_io_channel_set_buffer_size

    // method line 271
    .method public hidebysig specialname 
           instance default bool get_CloseOnUnref ()  cil managed 
    {
        // Method begins at RVA 0x466c
	// Code size 12 (0xc)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance native int class GLib.IOChannel::get_Handle()
	IL_0006:  call bool class GLib.IOChannel::g_io_channel_get_close_on_unref(native int)
	IL_000b:  ret 
    } // end of method IOChannel::get_CloseOnUnref

    // method line 272
    .method private static hidebysig pinvokeimpl ("libglib-2.0-0.dll" as "g_io_channel_get_close_on_unref" cdecl )
           default bool g_io_channel_get_close_on_unref (native int raw)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method IOChannel::g_io_channel_get_close_on_unref

    // method line 273
    .method public hidebysig specialname 
           instance default void set_CloseOnUnref (bool 'value')  cil managed 
    {
        // Method begins at RVA 0x467c
	// Code size 13 (0xd)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance native int class GLib.IOChannel::get_Handle()
	IL_0006:  ldarg.1 
	IL_0007:  call void class GLib.IOChannel::g_io_channel_set_close_on_unref(native int, bool)
	IL_000c:  ret 
    } // end of method IOChannel::set_CloseOnUnref

    // method line 274
    .method private static hidebysig pinvokeimpl ("libglib-2.0-0.dll" as "g_io_channel_set_close_on_unref" cdecl )
           default void g_io_channel_set_close_on_unref (native int raw, bool do_close)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method IOChannel::g_io_channel_set_close_on_unref

    // method line 275
    .method public hidebysig specialname 
           instance default string get_Encoding ()  cil managed 
    {
        // Method begins at RVA 0x468c
	// Code size 17 (0x11)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance native int class GLib.IOChannel::get_Handle()
	IL_0006:  call native int class GLib.IOChannel::g_io_channel_get_encoding(native int)
	IL_000b:  call string class GLib.Marshaller::Utf8PtrToString(native int)
	IL_0010:  ret 
    } // end of method IOChannel::get_Encoding

    // method line 276
    .method private static hidebysig pinvokeimpl ("libglib-2.0-0.dll" as "g_io_channel_get_encoding" cdecl )
           default native int g_io_channel_get_encoding (native int raw)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method IOChannel::g_io_channel_get_encoding

    // method line 277
    .method public hidebysig specialname 
           instance default void set_Encoding (string 'value')  cil managed 
    {
        // Method begins at RVA 0x46a0
	// Code size 52 (0x34)
	.maxstack 3
	.locals init (
		native int	V_0,
		native int	V_1)
	IL_0000:  ldarg.1 
	IL_0001:  call native int class GLib.Marshaller::StringToPtrGStrdup(string)
	IL_0006:  stloc.0 
	IL_0007:  ldarg.0 
	IL_0008:  call instance native int class GLib.IOChannel::get_Handle()
	IL_000d:  ldloc.0 
	IL_000e:  ldloca.s 1
	IL_0010:  call int32 class GLib.IOChannel::g_io_channel_set_encoding(native int, native int, [out] native int&)
	IL_0015:  pop 
	IL_0016:  ldloc.0 
	IL_0017:  call void class GLib.Marshaller::Free(native int)
	IL_001c:  ldloc.1 
	IL_001d:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_0022:  call bool native int::op_Inequality(native int, native int)
	IL_0027:  brfalse IL_0033

	IL_002c:  ldloc.1 
	IL_002d:  newobj instance void class GLib.GException::'.ctor'(native int)
	IL_0032:  throw 
	IL_0033:  ret 
    } // end of method IOChannel::set_Encoding

    // method line 278
    .method private static hidebysig pinvokeimpl ("libglib-2.0-0.dll" as "g_io_channel_set_encoding" cdecl )
           default int32 g_io_channel_set_encoding (native int raw, native int encoding, [out] native int& 'error')  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method IOChannel::g_io_channel_set_encoding

    // method line 279
    .method public hidebysig specialname 
           instance default valuetype GLib.IOFlags get_Flags ()  cil managed 
    {
        // Method begins at RVA 0x46e0
	// Code size 12 (0xc)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance native int class GLib.IOChannel::get_Handle()
	IL_0006:  call int32 class GLib.IOChannel::g_io_channel_get_flags(native int)
	IL_000b:  ret 
    } // end of method IOChannel::get_Flags

    // method line 280
    .method private static hidebysig pinvokeimpl ("libglib-2.0-0.dll" as "g_io_channel_get_flags" cdecl )
           default int32 g_io_channel_get_flags (native int raw)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method IOChannel::g_io_channel_get_flags

    // method line 281
    .method public hidebysig specialname 
           instance default void set_Flags (valuetype GLib.IOFlags 'value')  cil managed 
    {
        // Method begins at RVA 0x46f0
	// Code size 39 (0x27)
	.maxstack 3
	.locals init (
		native int	V_0)
	IL_0000:  ldarg.0 
	IL_0001:  call instance native int class GLib.IOChannel::get_Handle()
	IL_0006:  ldarg.1 
	IL_0007:  ldloca.s 0
	IL_0009:  call int32 class GLib.IOChannel::g_io_channel_set_flags(native int, int32, [out] native int&)
	IL_000e:  pop 
	IL_000f:  ldloc.0 
	IL_0010:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_0015:  call bool native int::op_Inequality(native int, native int)
	IL_001a:  brfalse IL_0026

	IL_001f:  ldloc.0 
	IL_0020:  newobj instance void class GLib.GException::'.ctor'(native int)
	IL_0025:  throw 
	IL_0026:  ret 
    } // end of method IOChannel::set_Flags

    // method line 282
    .method private static hidebysig pinvokeimpl ("libglib-2.0-0.dll" as "g_io_channel_set_flags" cdecl )
           default int32 g_io_channel_set_flags (native int raw, int32 'flags', [out] native int& 'error')  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method IOChannel::g_io_channel_set_flags

    // method line 283
    .method public hidebysig specialname 
           instance default char[] get_LineTerminator ()  cil managed 
    {
        // Method begins at RVA 0x4724
	// Code size 52 (0x34)
	.maxstack 2
	.locals init (
		int32	V_0,
		native int	V_1,
		unsigned int8[]	V_2)
	IL_0000:  ldarg.0 
	IL_0001:  call instance native int class GLib.IOChannel::get_Handle()
	IL_0006:  ldloca.s 0
	IL_0008:  call native int class GLib.IOChannel::g_io_channel_get_line_term(native int, [out] int32&)
	IL_000d:  stloc.1 
	IL_000e:  ldloc.0 
	IL_000f:  ldc.i4.m1 
	IL_0010:  bne.un IL_0021

	IL_0015:  ldloc.1 
	IL_0016:  call string class GLib.Marshaller::Utf8PtrToString(native int)
	IL_001b:  callvirt instance char[] string::ToCharArray()
	IL_0020:  ret 
	IL_0021:  ldloc.0 
	IL_0022:  newarr unsigned int8
	IL_0027:  stloc.2 
	IL_0028:  call class [mscorlib]System.Text.Encoding class [mscorlib]System.Text.Encoding::get_UTF8()
	IL_002d:  ldloc.2 
	IL_002e:  callvirt instance char[] class [mscorlib]System.Text.Encoding::GetChars(unsigned int8[])
	IL_0033:  ret 
    } // end of method IOChannel::get_LineTerminator

    // method line 284
    .method private static hidebysig pinvokeimpl ("libglib-2.0-0.dll" as "g_io_channel_get_line_term" cdecl )
           default native int g_io_channel_get_line_term (native int raw, [out] int32& length)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method IOChannel::g_io_channel_get_line_term

    // method line 285
    .method public hidebysig specialname 
           instance default void set_LineTerminator (char[] 'value')  cil managed 
    {
        // Method begins at RVA 0x4764
	// Code size 28 (0x1c)
	.maxstack 3
	.locals init (
		unsigned int8[]	V_0)
	IL_0000:  call class [mscorlib]System.Text.Encoding class [mscorlib]System.Text.Encoding::get_UTF8()
	IL_0005:  ldarg.1 
	IL_0006:  callvirt instance unsigned int8[] class [mscorlib]System.Text.Encoding::GetBytes(char[])
	IL_000b:  stloc.0 
	IL_000c:  ldarg.0 
	IL_000d:  call instance native int class GLib.IOChannel::get_Handle()
	IL_0012:  ldloc.0 
	IL_0013:  ldloc.0 
	IL_0014:  ldlen 
	IL_0015:  conv.i4 
	IL_0016:  call void class GLib.IOChannel::g_io_channel_set_line_term(native int, unsigned int8[], int32)
	IL_001b:  ret 
    } // end of method IOChannel::set_LineTerminator

    // method line 286
    .method private static hidebysig pinvokeimpl ("libglib-2.0-0.dll" as "g_io_channel_set_line_term" cdecl )
           default void g_io_channel_set_line_term (native int raw, unsigned int8[] term, int32 length)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method IOChannel::g_io_channel_set_line_term

    // method line 287
    .method public hidebysig specialname 
           instance default int32 get_UnixFd ()  cil managed 
    {
        // Method begins at RVA 0x478c
	// Code size 12 (0xc)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance native int class GLib.IOChannel::get_Handle()
	IL_0006:  call int32 class GLib.IOChannel::g_io_channel_unix_get_fd(native int)
	IL_000b:  ret 
    } // end of method IOChannel::get_UnixFd

    // method line 288
    .method private static hidebysig pinvokeimpl ("libglib-2.0-0.dll" as "g_io_channel_unix_get_fd" cdecl )
           default int32 g_io_channel_unix_get_fd (native int raw)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method IOChannel::g_io_channel_unix_get_fd

    // method line 289
    .method family hidebysig 
           instance default void Init ()  cil managed 
    {
        // Method begins at RVA 0x479c
	// Code size 12 (0xc)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance native int class GLib.IOChannel::get_Handle()
	IL_0006:  call void class GLib.IOChannel::g_io_channel_init(native int)
	IL_000b:  ret 
    } // end of method IOChannel::Init

    // method line 290
    .method private static hidebysig pinvokeimpl ("libglib-2.0-0.dll" as "g_io_channel_init" cdecl )
           default void g_io_channel_init (native int raw)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method IOChannel::g_io_channel_init

    // method line 291
    .method public final virtual hidebysig newslot 
           instance default void Dispose ()  cil managed 
    {
        // Method begins at RVA 0x47ac
	// Code size 12 (0xc)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance native int class GLib.IOChannel::get_Handle()
	IL_0006:  call void class GLib.IOChannel::g_io_channel_unref(native int)
	IL_000b:  ret 
    } // end of method IOChannel::Dispose

    // method line 292
    .method private static hidebysig pinvokeimpl ("libglib-2.0-0.dll" as "g_io_channel_unref" cdecl )
           default void g_io_channel_unref (native int raw)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method IOChannel::g_io_channel_unref

    // method line 293
    .method public hidebysig 
           instance default unsigned int32 AddWatch (int32 priority, valuetype GLib.IOCondition condition, class GLib.IOFunc func)  cil managed 
    {
        // Method begins at RVA 0x47bc
	// Code size 63 (0x3f)
	.maxstack 6
	.locals init (
		class GLibSharp.IOFuncWrapper	V_0,
		native int	V_1,
		class GLib.DestroyNotify	V_2)
	IL_0000:  ldnull 
	IL_0001:  stloc.0 
	IL_0002:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_0007:  stloc.1 
	IL_0008:  ldnull 
	IL_0009:  stloc.2 
	IL_000a:  ldarg.3 
	IL_000b:  brfalse IL_0029

	IL_0010:  ldarg.3 
	IL_0011:  newobj instance void class GLibSharp.IOFuncWrapper::'.ctor'(class GLib.IOFunc)
	IL_0016:  stloc.0 
	IL_0017:  ldloc.0 
	IL_0018:  call valuetype [mscorlib]System.Runtime.InteropServices.GCHandle valuetype [mscorlib]System.Runtime.InteropServices.GCHandle::Alloc(object)
	IL_001d:  call native int valuetype [mscorlib]System.Runtime.InteropServices.GCHandle::op_Explicit(valuetype [mscorlib]System.Runtime.InteropServices.GCHandle)
	IL_0022:  stloc.1 
	IL_0023:  call class GLib.DestroyNotify class GLib.DestroyHelper::get_NotifyHandler()
	IL_0028:  stloc.2 
	IL_0029:  ldarg.0 
	IL_002a:  call instance native int class GLib.IOChannel::get_Handle()
	IL_002f:  ldarg.1 
	IL_0030:  ldarg.2 
	IL_0031:  ldloc.0 
	IL_0032:  ldfld class GLibSharp.IOFuncNative GLibSharp.IOFuncWrapper::NativeDelegate
	IL_0037:  ldloc.1 
	IL_0038:  ldloc.2 
	IL_0039:  call unsigned int32 class GLib.IOChannel::g_io_add_watch_full(native int, int32, int32, class GLibSharp.IOFuncNative, native int, class GLib.DestroyNotify)
	IL_003e:  ret 
    } // end of method IOChannel::AddWatch

    // method line 294
    .method private static hidebysig pinvokeimpl ("libglib-2.0-0.dll" as "g_io_add_watch_full" cdecl )
           default unsigned int32 g_io_add_watch_full (native int raw, int32 priority, int32 condition, class GLibSharp.IOFuncNative func, native int user_data, class GLib.DestroyNotify notify)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method IOChannel::g_io_add_watch_full

    // method line 295
    .method public hidebysig 
           instance default valuetype GLib.IOStatus Flush ()  cil managed 
    {
        // Method begins at RVA 0x4808
	// Code size 39 (0x27)
	.maxstack 2
	.locals init (
		native int	V_0,
		valuetype GLib.IOStatus	V_1)
	IL_0000:  ldarg.0 
	IL_0001:  call instance native int class GLib.IOChannel::get_Handle()
	IL_0006:  ldloca.s 0
	IL_0008:  call int32 class GLib.IOChannel::g_io_channel_flush(native int, [out] native int&)
	IL_000d:  stloc.1 
	IL_000e:  ldloc.0 
	IL_000f:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_0014:  call bool native int::op_Inequality(native int, native int)
	IL_0019:  brfalse IL_0025

	IL_001e:  ldloc.0 
	IL_001f:  newobj instance void class GLib.GException::'.ctor'(native int)
	IL_0024:  throw 
	IL_0025:  ldloc.1 
	IL_0026:  ret 
    } // end of method IOChannel::Flush

    // method line 296
    .method private static hidebysig pinvokeimpl ("libglib-2.0-0.dll" as "g_io_channel_flush" cdecl )
           default int32 g_io_channel_flush (native int raw, [out] native int& 'error')  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method IOChannel::g_io_channel_flush

    // method line 297
    .method public hidebysig 
           instance default valuetype GLib.IOStatus ReadChars (unsigned int8[] buf, [out] unsigned int64& bytes_read)  cil managed 
    {
        // Method begins at RVA 0x483c
	// Code size 59 (0x3b)
	.maxstack 5
	.locals init (
		native unsigned int	V_0,
		native int	V_1,
		valuetype GLib.IOStatus	V_2)
	IL_0000:  ldarg.0 
	IL_0001:  call instance native int class GLib.IOChannel::get_Handle()
	IL_0006:  ldarg.1 
	IL_0007:  ldarg.1 
	IL_0008:  ldlen 
	IL_0009:  conv.i4 
	IL_000a:  conv.i8 
	IL_000b:  newobj instance void native unsigned int::'.ctor'(unsigned int64)
	IL_0010:  ldloca.s 0
	IL_0012:  ldloca.s 1
	IL_0014:  call int32 class GLib.IOChannel::g_io_channel_read_chars(native int, unsigned int8[], native unsigned int, [out] native unsigned int&, [out] native int&)
	IL_0019:  stloc.2 
	IL_001a:  ldarg.2 
	IL_001b:  ldloc.0 
	IL_001c:  call unsigned int64 native unsigned int::op_Explicit(native unsigned int)
	IL_0021:  stind.i8 
	IL_0022:  ldloc.1 
	IL_0023:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_0028:  call bool native int::op_Inequality(native int, native int)
	IL_002d:  brfalse IL_0039

	IL_0032:  ldloc.1 
	IL_0033:  newobj instance void class GLib.GException::'.ctor'(native int)
	IL_0038:  throw 
	IL_0039:  ldloc.2 
	IL_003a:  ret 
    } // end of method IOChannel::ReadChars

    // method line 298
    .method private static hidebysig pinvokeimpl ("libglib-2.0-0.dll" as "g_io_channel_read_chars" cdecl )
           default int32 g_io_channel_read_chars (native int raw, unsigned int8[] buf, native unsigned int count, [out] native unsigned int& bytes_read, [out] native int& 'error')  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method IOChannel::g_io_channel_read_chars

    // method line 299
    .method public hidebysig 
           instance default valuetype GLib.IOStatus ReadLine ([out] string& str_return)  cil managed 
    {
        // Method begins at RVA 0x4884
	// Code size 10 (0xa)
	.maxstack 3
	.locals init (
		unsigned int64	V_0)
	IL_0000:  ldarg.0 
	IL_0001:  ldarg.1 
	IL_0002:  ldloca.s 0
	IL_0004:  call instance valuetype GLib.IOStatus class GLib.IOChannel::ReadLine([out] string&, [out] unsigned int64&)
	IL_0009:  ret 
    } // end of method IOChannel::ReadLine

    // method line 300
    .method public hidebysig 
           instance default valuetype GLib.IOStatus ReadLine ([out] string& str_return, [out] unsigned int64& terminator_pos)  cil managed 
    {
        // Method begins at RVA 0x489c
	// Code size 74 (0x4a)
	.maxstack 5
	.locals init (
		native int	V_0,
		native unsigned int	V_1,
		native int	V_2,
		valuetype GLib.IOStatus	V_3)
	IL_0000:  ldarg.0 
	IL_0001:  call instance native int class GLib.IOChannel::get_Handle()
	IL_0006:  ldloca.s 0
	IL_0008:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_000d:  ldloca.s 1
	IL_000f:  ldloca.s 2
	IL_0011:  call int32 class GLib.IOChannel::g_io_channel_read_line(native int, [out] native int&, native int, [out] native unsigned int&, [out] native int&)
	IL_0016:  stloc.3 
	IL_0017:  ldarg.2 
	IL_0018:  ldloc.1 
	IL_0019:  call unsigned int64 native unsigned int::op_Explicit(native unsigned int)
	IL_001e:  stind.i8 
	IL_001f:  ldarg.1 
	IL_0020:  ldnull 
	IL_0021:  stind.ref 
	IL_0022:  ldloc.3 
	IL_0023:  ldc.i4.1 
	IL_0024:  bne.un IL_0031

	IL_0029:  ldarg.1 
	IL_002a:  ldloc.0 
	IL_002b:  call string class GLib.Marshaller::PtrToStringGFree(native int)
	IL_0030:  stind.ref 
	IL_0031:  ldloc.2 
	IL_0032:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_0037:  call bool native int::op_Inequality(native int, native int)
	IL_003c:  brfalse IL_0048

	IL_0041:  ldloc.2 
	IL_0042:  newobj instance void class GLib.GException::'.ctor'(native int)
	IL_0047:  throw 
	IL_0048:  ldloc.3 
	IL_0049:  ret 
    } // end of method IOChannel::ReadLine

    // method line 301
    .method private static hidebysig pinvokeimpl ("libglib-2.0-0.dll" as "g_io_channel_read_line" cdecl )
           default int32 g_io_channel_read_line (native int raw, [out] native int& str_return, native int length, [out] native unsigned int& terminator_pos, [out] native int& 'error')  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method IOChannel::g_io_channel_read_line

    // method line 302
    .method public hidebysig 
           instance default valuetype GLib.IOStatus ReadToEnd ([out] string& str_return)  cil managed 
    {
        // Method begins at RVA 0x48f4
	// Code size 103 (0x67)
	.maxstack 4
	.locals init (
		native int	V_0,
		native unsigned int	V_1,
		native int	V_2,
		valuetype GLib.IOStatus	V_3,
		unsigned int8[]	V_4)
	IL_0000:  ldarg.0 
	IL_0001:  call instance native int class GLib.IOChannel::get_Handle()
	IL_0006:  ldloca.s 0
	IL_0008:  ldloca.s 1
	IL_000a:  ldloca.s 2
	IL_000c:  call int32 class GLib.IOChannel::g_io_channel_read_to_end(native int, [out] native int&, [out] native unsigned int&, [out] native int&)
	IL_0011:  stloc.3 
	IL_0012:  ldarg.1 
	IL_0013:  ldnull 
	IL_0014:  stind.ref 
	IL_0015:  ldloc.3 
	IL_0016:  ldc.i4.1 
	IL_0017:  bne.un IL_0048

	IL_001c:  ldloc.1 
	IL_001d:  call unsigned int64 native unsigned int::op_Explicit(native unsigned int)
	IL_0022:  conv.ovf.i.un 
	IL_0023:  newarr unsigned int8
	IL_0028:  stloc.s 4
	IL_002a:  ldloc.0 
	IL_002b:  ldloc.s 4
	IL_002d:  ldc.i4.0 
	IL_002e:  ldloc.1 
	IL_002f:  call unsigned int64 native unsigned int::op_Explicit(native unsigned int)
	IL_0034:  conv.i4 
	IL_0035:  call void class [mscorlib]System.Runtime.InteropServices.Marshal::Copy(native int, unsigned int8[], int32, int32)
	IL_003a:  ldarg.1 
	IL_003b:  call class [mscorlib]System.Text.Encoding class [mscorlib]System.Text.Encoding::get_UTF8()
	IL_0040:  ldloc.s 4
	IL_0042:  callvirt instance string class [mscorlib]System.Text.Encoding::GetString(unsigned int8[])
	IL_0047:  stind.ref 
	IL_0048:  ldloc.0 
	IL_0049:  call void class GLib.Marshaller::Free(native int)
	IL_004e:  ldloc.2 
	IL_004f:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_0054:  call bool native int::op_Inequality(native int, native int)
	IL_0059:  brfalse IL_0065

	IL_005e:  ldloc.2 
	IL_005f:  newobj instance void class GLib.GException::'.ctor'(native int)
	IL_0064:  throw 
	IL_0065:  ldloc.3 
	IL_0066:  ret 
    } // end of method IOChannel::ReadToEnd

    // method line 303
    .method private static hidebysig pinvokeimpl ("libglib-2.0-0.dll" as "g_io_channel_read_to_end" cdecl )
           default int32 g_io_channel_read_to_end (native int raw, [out] native int& str_return, [out] native unsigned int& length, [out] native int& 'error')  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method IOChannel::g_io_channel_read_to_end

    // method line 304
    .method public hidebysig 
           instance default valuetype GLib.IOStatus ReadUnichar ([out] unsigned int32& thechar)  cil managed 
    {
        // Method begins at RVA 0x4968
	// Code size 40 (0x28)
	.maxstack 3
	.locals init (
		native int	V_0,
		valuetype GLib.IOStatus	V_1)
	IL_0000:  ldarg.0 
	IL_0001:  call instance native int class GLib.IOChannel::get_Handle()
	IL_0006:  ldarg.1 
	IL_0007:  ldloca.s 0
	IL_0009:  call int32 class GLib.IOChannel::g_io_channel_read_unichar(native int, [out] unsigned int32&, [out] native int&)
	IL_000e:  stloc.1 
	IL_000f:  ldloc.0 
	IL_0010:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_0015:  call bool native int::op_Inequality(native int, native int)
	IL_001a:  brfalse IL_0026

	IL_001f:  ldloc.0 
	IL_0020:  newobj instance void class GLib.GException::'.ctor'(native int)
	IL_0025:  throw 
	IL_0026:  ldloc.1 
	IL_0027:  ret 
    } // end of method IOChannel::ReadUnichar

    // method line 305
    .method private static hidebysig pinvokeimpl ("libglib-2.0-0.dll" as "g_io_channel_read_unichar" cdecl )
           default int32 g_io_channel_read_unichar (native int raw, [out] unsigned int32& thechar, [out] native int& 'error')  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method IOChannel::g_io_channel_read_unichar

    // method line 306
    .method public hidebysig 
           instance default valuetype GLib.IOStatus SeekPosition (int64 offset, valuetype GLib.SeekType 'type')  cil managed 
    {
        // Method begins at RVA 0x499c
	// Code size 41 (0x29)
	.maxstack 4
	.locals init (
		native int	V_0,
		valuetype GLib.IOStatus	V_1)
	IL_0000:  ldarg.0 
	IL_0001:  call instance native int class GLib.IOChannel::get_Handle()
	IL_0006:  ldarg.1 
	IL_0007:  ldarg.2 
	IL_0008:  ldloca.s 0
	IL_000a:  call int32 class GLib.IOChannel::g_io_channel_seek_position(native int, int64, int32, [out] native int&)
	IL_000f:  stloc.1 
	IL_0010:  ldloc.0 
	IL_0011:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_0016:  call bool native int::op_Inequality(native int, native int)
	IL_001b:  brfalse IL_0027

	IL_0020:  ldloc.0 
	IL_0021:  newobj instance void class GLib.GException::'.ctor'(native int)
	IL_0026:  throw 
	IL_0027:  ldloc.1 
	IL_0028:  ret 
    } // end of method IOChannel::SeekPosition

    // method line 307
    .method private static hidebysig pinvokeimpl ("libglib-2.0-0.dll" as "g_io_channel_seek_position" cdecl )
           default int32 g_io_channel_seek_position (native int raw, int64 offset, int32 'type', [out] native int& 'error')  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method IOChannel::g_io_channel_seek_position

    // method line 308
    .method public hidebysig 
           instance default valuetype GLib.IOStatus Shutdown (bool flush)  cil managed 
    {
        // Method begins at RVA 0x49d4
	// Code size 40 (0x28)
	.maxstack 3
	.locals init (
		native int	V_0,
		valuetype GLib.IOStatus	V_1)
	IL_0000:  ldarg.0 
	IL_0001:  call instance native int class GLib.IOChannel::get_Handle()
	IL_0006:  ldarg.1 
	IL_0007:  ldloca.s 0
	IL_0009:  call int32 class GLib.IOChannel::g_io_channel_shutdown(native int, bool, [out] native int&)
	IL_000e:  stloc.1 
	IL_000f:  ldloc.0 
	IL_0010:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_0015:  call bool native int::op_Inequality(native int, native int)
	IL_001a:  brfalse IL_0026

	IL_001f:  ldloc.0 
	IL_0020:  newobj instance void class GLib.GException::'.ctor'(native int)
	IL_0025:  throw 
	IL_0026:  ldloc.1 
	IL_0027:  ret 
    } // end of method IOChannel::Shutdown

    // method line 309
    .method private static hidebysig pinvokeimpl ("libglib-2.0-0.dll" as "g_io_channel_shutdown" cdecl )
           default int32 g_io_channel_shutdown (native int raw, bool flush, [out] native int& err)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method IOChannel::g_io_channel_shutdown

    // method line 310
    .method public hidebysig 
           instance default valuetype GLib.IOStatus WriteChars (string str, [out] string& remainder)  cil managed 
    {
        // Method begins at RVA 0x4a08
	// Code size 81 (0x51)
	.maxstack 5
	.locals init (
		unsigned int64	V_0,
		class [mscorlib]System.Text.Encoding	V_1,
		unsigned int8[]	V_2,
		valuetype GLib.IOStatus	V_3,
		int32	V_4,
		unsigned int8[]	V_5)
	IL_0000:  call class [mscorlib]System.Text.Encoding class [mscorlib]System.Text.Encoding::get_UTF8()
	IL_0005:  stloc.1 
	IL_0006:  ldloc.1 
	IL_0007:  ldarg.1 
	IL_0008:  callvirt instance unsigned int8[] class [mscorlib]System.Text.Encoding::GetBytes(string)
	IL_000d:  stloc.2 
	IL_000e:  ldarg.0 
	IL_000f:  ldloc.2 
	IL_0010:  ldloca.s 0
	IL_0012:  call instance valuetype GLib.IOStatus class GLib.IOChannel::WriteChars(unsigned int8[], [out] unsigned int64&)
	IL_0017:  stloc.3 
	IL_0018:  ldarg.2 
	IL_0019:  ldnull 
	IL_001a:  stind.ref 
	IL_001b:  ldloc.0 
	IL_001c:  conv.i4 
	IL_001d:  ldloc.2 
	IL_001e:  ldlen 
	IL_001f:  conv.i4 
	IL_0020:  bne.un IL_0027

	IL_0025:  ldloc.3 
	IL_0026:  ret 
	IL_0027:  ldloc.2 
	IL_0028:  ldlen 
	IL_0029:  conv.i4 
	IL_002a:  ldloc.0 
	IL_002b:  conv.i4 
	IL_002c:  sub 
	IL_002d:  stloc.s 4
	IL_002f:  ldloc.s 4
	IL_0031:  newarr unsigned int8
	IL_0036:  stloc.s 5
	IL_0038:  ldloc.2 
	IL_0039:  ldloc.0 
	IL_003a:  conv.i4 
	IL_003b:  ldloc.s 5
	IL_003d:  ldc.i4.0 
	IL_003e:  ldloc.s 4
	IL_0040:  call void class [mscorlib]System.Array::Copy(class [mscorlib]System.Array, int32, class [mscorlib]System.Array, int32, int32)
	IL_0045:  ldarg.2 
	IL_0046:  ldloc.1 
	IL_0047:  ldloc.s 5
	IL_0049:  callvirt instance string class [mscorlib]System.Text.Encoding::GetString(unsigned int8[])
	IL_004e:  stind.ref 
	IL_004f:  ldloc.3 
	IL_0050:  ret 
    } // end of method IOChannel::WriteChars

    // method line 311
    .method public hidebysig 
           instance default valuetype GLib.IOStatus WriteChars (unsigned int8[] buf, [out] unsigned int64& bytes_written)  cil managed 
    {
        // Method begins at RVA 0x4a68
	// Code size 58 (0x3a)
	.maxstack 5
	.locals init (
		native unsigned int	V_0,
		native int	V_1,
		valuetype GLib.IOStatus	V_2)
	IL_0000:  ldarg.0 
	IL_0001:  call instance native int class GLib.IOChannel::get_Handle()
	IL_0006:  ldarg.1 
	IL_0007:  ldarg.1 
	IL_0008:  ldlen 
	IL_0009:  conv.i4 
	IL_000a:  newobj instance void native int::'.ctor'(int32)
	IL_000f:  ldloca.s 0
	IL_0011:  ldloca.s 1
	IL_0013:  call int32 class GLib.IOChannel::g_io_channel_write_chars(native int, unsigned int8[], native int, [out] native unsigned int&, [out] native int&)
	IL_0018:  stloc.2 
	IL_0019:  ldarg.2 
	IL_001a:  ldloc.0 
	IL_001b:  call unsigned int64 native unsigned int::op_Explicit(native unsigned int)
	IL_0020:  stind.i8 
	IL_0021:  ldloc.1 
	IL_0022:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_0027:  call bool native int::op_Inequality(native int, native int)
	IL_002c:  brfalse IL_0038

	IL_0031:  ldloc.1 
	IL_0032:  newobj instance void class GLib.GException::'.ctor'(native int)
	IL_0037:  throw 
	IL_0038:  ldloc.2 
	IL_0039:  ret 
    } // end of method IOChannel::WriteChars

    // method line 312
    .method private static hidebysig pinvokeimpl ("libglib-2.0-0.dll" as "g_io_channel_write_chars" cdecl )
           default int32 g_io_channel_write_chars (native int raw, unsigned int8[] buf, native int count, [out] native unsigned int& bytes_written, [out] native int& 'error')  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method IOChannel::g_io_channel_write_chars

    // method line 313
    .method public hidebysig 
           instance default valuetype GLib.IOStatus WriteUnichar (unsigned int32 thechar)  cil managed 
    {
        // Method begins at RVA 0x4ab0
	// Code size 40 (0x28)
	.maxstack 3
	.locals init (
		native int	V_0,
		valuetype GLib.IOStatus	V_1)
	IL_0000:  ldarg.0 
	IL_0001:  call instance native int class GLib.IOChannel::get_Handle()
	IL_0006:  ldarg.1 
	IL_0007:  ldloca.s 0
	IL_0009:  call int32 class GLib.IOChannel::g_io_channel_write_unichar(native int, unsigned int32, [out] native int&)
	IL_000e:  stloc.1 
	IL_000f:  ldloc.0 
	IL_0010:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_0015:  call bool native int::op_Inequality(native int, native int)
	IL_001a:  brfalse IL_0026

	IL_001f:  ldloc.0 
	IL_0020:  newobj instance void class GLib.GException::'.ctor'(native int)
	IL_0025:  throw 
	IL_0026:  ldloc.1 
	IL_0027:  ret 
    } // end of method IOChannel::WriteUnichar

    // method line 314
    .method private static hidebysig pinvokeimpl ("libglib-2.0-0.dll" as "g_io_channel_write_unichar" cdecl )
           default int32 g_io_channel_write_unichar (native int raw, unsigned int32 thechar, [out] native int& 'error')  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method IOChannel::g_io_channel_write_unichar

    // method line 315
    .method private static hidebysig pinvokeimpl ("libglib-2.0-0.dll" as "g_io_channel_ref" cdecl )
           default native int g_io_channel_ref (native int raw)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method IOChannel::g_io_channel_ref

    // method line 316
    .method public static hidebysig 
           default valuetype GLib.IOChannelError ErrorFromErrno (int32 en)  cil managed 
    {
        // Method begins at RVA 0x4ae4
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call int32 class GLib.IOChannel::g_io_channel_error_from_errno(int32)
	IL_0006:  ret 
    } // end of method IOChannel::ErrorFromErrno

    // method line 317
    .method private static hidebysig pinvokeimpl ("libglib-2.0-0.dll" as "g_io_channel_error_from_errno" cdecl )
           default int32 g_io_channel_error_from_errno (int32 en)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method IOChannel::g_io_channel_error_from_errno

    // method line 318
    .method private static hidebysig pinvokeimpl ("libglib-2.0-0.dll" as "g_io_channel_error_quark" cdecl )
           default int32 g_io_channel_error_quark ()  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method IOChannel::g_io_channel_error_quark

    // method line 319
    .method private static hidebysig pinvokeimpl ("libglib-2.0-0.dll" as "g_io_create_watch" cdecl )
           default native int g_io_create_watch (native int raw, int32 condition)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method IOChannel::g_io_create_watch

	.property instance valuetype GLib.IOCondition BufferCondition ()
	{
		.get instance default valuetype GLib.IOCondition GLib.IOChannel::get_BufferCondition () 
	}
	.property instance bool Buffered ()
	{
		.get instance default bool GLib.IOChannel::get_Buffered () 
		.set instance default void GLib.IOChannel::set_Buffered (bool 'value') 
	}
	.property instance unsigned int64 BufferSize ()
	{
		.get instance default unsigned int64 GLib.IOChannel::get_BufferSize () 
		.set instance default void GLib.IOChannel::set_BufferSize (unsigned int64 'value') 
	}
	.property instance bool CloseOnUnref ()
	{
		.set instance default void GLib.IOChannel::set_CloseOnUnref (bool 'value') 
		.get instance default bool GLib.IOChannel::get_CloseOnUnref () 
	}
	.property instance string Encoding ()
	{
		.get instance default string GLib.IOChannel::get_Encoding () 
		.set instance default void GLib.IOChannel::set_Encoding (string 'value') 
	}
	.property instance valuetype GLib.IOFlags Flags ()
	{
		.get instance default valuetype GLib.IOFlags GLib.IOChannel::get_Flags () 
		.set instance default void GLib.IOChannel::set_Flags (valuetype GLib.IOFlags 'value') 
	}
	.property instance char[] LineTerminator ()
	{
		.get instance default char[] GLib.IOChannel::get_LineTerminator () 
		.set instance default void GLib.IOChannel::set_LineTerminator (char[] 'value') 
	}
	.property instance native int Handle ()
	{
		.get instance default native int GLib.IOChannel::get_Handle () 
	}
	.property instance int32 UnixFd ()
	{
		.get instance default int32 GLib.IOChannel::get_UnixFd () 
	}
  } // end of class GLib.IOChannel
}

.namespace GLib
{
  .class public auto ansi sealed IOFunc
  	extends [mscorlib]System.MulticastDelegate
  {

    // method line 320
    .method public virtual hidebysig newslot 
           instance default bool Invoke (class GLib.IOChannel source, valuetype GLib.IOCondition condition)  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method IOFunc::Invoke

    // method line 321
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (object 'object', native int 'method')  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method IOFunc::.ctor

    // method line 322
    .method public virtual hidebysig newslot 
           instance default class [mscorlib]System.IAsyncResult BeginInvoke (class GLib.IOChannel source, valuetype GLib.IOCondition condition, class [mscorlib]System.AsyncCallback callback, object 'object')  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method IOFunc::BeginInvoke

    // method line 323
    .method public virtual hidebysig newslot 
           instance default bool EndInvoke (class [mscorlib]System.IAsyncResult result)  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method IOFunc::EndInvoke

  } // end of class GLib.IOFunc
}

.namespace GLib
{
  .class public auto ansi sealed IOChannelError
  	extends [mscorlib]System.Enum
  {
    .field  public specialname rtspecialname  int32 value__
    .field public static literal  valuetype GLib.IOChannelError FileTooBig = int32(0x00000000)
    .field public static literal  valuetype GLib.IOChannelError Inval = int32(0x00000001)
    .field public static literal  valuetype GLib.IOChannelError IO = int32(0x00000002)
    .field public static literal  valuetype GLib.IOChannelError IsDir = int32(0x00000003)
    .field public static literal  valuetype GLib.IOChannelError NoSpace = int32(0x00000004)
    .field public static literal  valuetype GLib.IOChannelError Nxio = int32(0x00000005)
    .field public static literal  valuetype GLib.IOChannelError Overflow = int32(0x00000006)
    .field public static literal  valuetype GLib.IOChannelError Pipe = int32(0x00000007)
    .field public static literal  valuetype GLib.IOChannelError Failed = int32(0x00000008)

  } // end of class GLib.IOChannelError
}

.namespace GLib
{
  .class public auto ansi sealed IOCondition
  	extends [mscorlib]System.Enum
  {
    .custom instance void class [mscorlib]System.FlagsAttribute::'.ctor'() =  (01 00 00 00 ) // ....

    .field  public specialname rtspecialname  int32 value__
    .field public static literal  valuetype GLib.IOCondition In = int32(0x00000001)
    .field public static literal  valuetype GLib.IOCondition Out = int32(0x00000004)
    .field public static literal  valuetype GLib.IOCondition Pri = int32(0x00000002)
    .field public static literal  valuetype GLib.IOCondition Err = int32(0x00000008)
    .field public static literal  valuetype GLib.IOCondition Hup = int32(0x00000010)
    .field public static literal  valuetype GLib.IOCondition Nval = int32(0x00000020)

  } // end of class GLib.IOCondition
}

.namespace GLib
{
  .class public auto ansi sealed IOFlags
  	extends [mscorlib]System.Enum
  {
    .custom instance void class [mscorlib]System.FlagsAttribute::'.ctor'() =  (01 00 00 00 ) // ....

    .field  public specialname rtspecialname  int32 value__
    .field public static literal  valuetype GLib.IOFlags Append = int32(0x00000001)
    .field public static literal  valuetype GLib.IOFlags Nonblock = int32(0x00000002)
    .field public static literal  valuetype GLib.IOFlags IsReadable = int32(0x00000004)
    .field public static literal  valuetype GLib.IOFlags IsWriteable = int32(0x00000008)
    .field public static literal  valuetype GLib.IOFlags IsSeekable = int32(0x00000010)
    .field public static literal  valuetype GLib.IOFlags Mask = int32(0x00000010)
    .field public static literal  valuetype GLib.IOFlags GetMask = int32(0x00000010)
    .field public static literal  valuetype GLib.IOFlags SetMask = int32(0x00000003)

  } // end of class GLib.IOFlags
}

.namespace GLib
{
  .class public auto ansi sealed IOStatus
  	extends [mscorlib]System.Enum
  {
    .field  public specialname rtspecialname  int32 value__
    .field public static literal  valuetype GLib.IOStatus Error = int32(0x00000000)
    .field public static literal  valuetype GLib.IOStatus Normal = int32(0x00000001)
    .field public static literal  valuetype GLib.IOStatus Eof = int32(0x00000002)
    .field public static literal  valuetype GLib.IOStatus Again = int32(0x00000003)

  } // end of class GLib.IOStatus
}

.namespace GLib
{
  .class public auto ansi sealed SeekType
  	extends [mscorlib]System.Enum
  {
    .field  public specialname rtspecialname  int32 value__
    .field public static literal  valuetype GLib.SeekType Cur = int32(0x00000000)
    .field public static literal  valuetype GLib.SeekType Set = int32(0x00000001)
    .field public static literal  valuetype GLib.SeekType End = int32(0x00000002)

  } // end of class GLib.SeekType
}

.namespace GLib
{
  .class public auto ansi sealed ListElementFree
  	extends [mscorlib]System.MulticastDelegate
  {

    // method line 324
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (object 'object', native int 'method')  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method ListElementFree::.ctor

    // method line 325
    .method public virtual hidebysig newslot 
           instance default void Invoke (native int ptr)  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method ListElementFree::Invoke

    // method line 326
    .method public virtual hidebysig newslot 
           instance default class [mscorlib]System.IAsyncResult BeginInvoke (native int ptr, class [mscorlib]System.AsyncCallback callback, object 'object')  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method ListElementFree::BeginInvoke

    // method line 327
    .method public virtual hidebysig newslot 
           instance default void EndInvoke (class [mscorlib]System.IAsyncResult result)  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method ListElementFree::EndInvoke

  } // end of class GLib.ListElementFree
}

.namespace GLib
{
  .class public auto ansi abstract beforefieldinit ListBase
  	extends [mscorlib]System.Object
  	implements [mscorlib]System.IDisposable, [mscorlib]System.Collections.ICollection, GLib.IWrapper, [mscorlib]System.ICloneable, [mscorlib]System.Collections.IEnumerable  {
    .custom instance void class [mscorlib]System.Reflection.DefaultMemberAttribute::'.ctor'(string) =  (01 00 04 49 74 65 6D 00 00 ) // ...Item..

    .field  private  native int list_ptr
    .field  private  bool 'managed'
    .field  assembly  bool elements_owned
    .field  family  class [mscorlib]System.Type element_type
    .field  private  class GLib.ListElementFree free_func

    // method line 328
    .method assembly hidebysig specialname rtspecialname 
           instance default void '.ctor' (native int list, class [mscorlib]System.Type element_type, bool owned, bool elements_owned, class GLib.ListElementFree free_func)  cil managed 
    {
        // Method begins at RVA 0x4aec
	// Code size 76 (0x4c)
	.maxstack 2
	IL_0000:  ldarg.0 
	IL_0001:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_0006:  stfld native int GLib.ListBase::list_ptr
	IL_000b:  ldarg.0 
	IL_000c:  ldc.i4.0 
	IL_000d:  stfld bool GLib.ListBase::'managed'
	IL_0012:  ldarg.0 
	IL_0013:  ldc.i4.0 
	IL_0014:  stfld bool GLib.ListBase::elements_owned
	IL_0019:  ldarg.0 
	IL_001a:  ldnull 
	IL_001b:  stfld class [mscorlib]System.Type GLib.ListBase::element_type
	IL_0020:  ldarg.0 
	IL_0021:  call instance void object::'.ctor'()
	IL_0026:  ldarg.0 
	IL_0027:  ldarg.1 
	IL_0028:  stfld native int GLib.ListBase::list_ptr
	IL_002d:  ldarg.0 
	IL_002e:  ldarg.2 
	IL_002f:  stfld class [mscorlib]System.Type GLib.ListBase::element_type
	IL_0034:  ldarg.0 
	IL_0035:  ldarg.3 
	IL_0036:  stfld bool GLib.ListBase::'managed'
	IL_003b:  ldarg.0 
	IL_003c:  ldarg.s 4
	IL_003e:  stfld bool GLib.ListBase::elements_owned
	IL_0043:  ldarg.0 
	IL_0044:  ldarg.s 5
	IL_0046:  stfld class GLib.ListElementFree GLib.ListBase::free_func
	IL_004b:  ret 
    } // end of method ListBase::.ctor

    // method line 329
    .method assembly virtual hidebysig newslot abstract 
           instance default native int NthData (unsigned int32 index)  cil managed 
    {
        // Method begins at RVA 0x0
    } // end of method ListBase::NthData

    // method line 330
    .method assembly virtual hidebysig newslot abstract 
           instance default int32 Length (native int list)  cil managed 
    {
        // Method begins at RVA 0x0
    } // end of method ListBase::Length

    // method line 331
    .method assembly virtual hidebysig newslot abstract 
           instance default void Free (native int list)  cil managed 
    {
        // Method begins at RVA 0x0
    } // end of method ListBase::Free

    // method line 332
    .method assembly virtual hidebysig newslot abstract 
           instance default native int Append (native int current, native int raw)  cil managed 
    {
        // Method begins at RVA 0x0
    } // end of method ListBase::Append

    // method line 333
    .method assembly virtual hidebysig newslot abstract 
           instance default native int Prepend (native int current, native int raw)  cil managed 
    {
        // Method begins at RVA 0x0
    } // end of method ListBase::Prepend

    // method line 334
    .method family virtual hidebysig 
           instance default void Finalize ()  cil managed 
    {
        // Method begins at RVA 0x4b44
	// Code size 20 (0x14)
	.maxstack 2
	.try { // 0
	  IL_0000:  ldarg.0 
	  IL_0001:  ldc.i4.0 
	  IL_0002:  callvirt instance void class GLib.ListBase::Dispose(bool)
	  IL_0007:  leave IL_0013

	} // end .try 0
	finally  { // 0
	  IL_000c:  ldarg.0 
	  IL_000d:  call instance void object::Finalize()
	  IL_0012:  endfinally 
	} // end handler 0
	IL_0013:  ret 
    } // end of method ListBase::Finalize

    // method line 335
    .method family virtual hidebysig newslot 
           instance default void Dispose (bool disposing)  cil managed 
    {
        // Method begins at RVA 0x4b74
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void class GLib.ListBase::Empty()
	IL_0006:  ret 
    } // end of method ListBase::Dispose

    // method line 336
    .method public hidebysig specialname 
           instance default void set_Managed (bool 'value')  cil managed 
    {
        // Method begins at RVA 0x4b7c
	// Code size 8 (0x8)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldarg.1 
	IL_0002:  stfld bool GLib.ListBase::'managed'
	IL_0007:  ret 
    } // end of method ListBase::set_Managed

    // method line 337
    .method public final virtual hidebysig newslot specialname 
           instance default native int get_Handle ()  cil managed 
    {
        // Method begins at RVA 0x4b88
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldfld native int GLib.ListBase::list_ptr
	IL_0006:  ret 
    } // end of method ListBase::get_Handle

    // method line 338
    .method public hidebysig 
           instance default void Append (native int raw)  cil managed 
    {
        // Method begins at RVA 0x4b90
	// Code size 20 (0x14)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldarg.0 
	IL_0002:  ldarg.0 
	IL_0003:  ldfld native int GLib.ListBase::list_ptr
	IL_0008:  ldarg.1 
	IL_0009:  callvirt instance native int class GLib.ListBase::Append(native int, native int)
	IL_000e:  stfld native int GLib.ListBase::list_ptr
	IL_0013:  ret 
    } // end of method ListBase::Append

    // method line 339
    .method public hidebysig 
           instance default void Append (string item)  cil managed 
    {
        // Method begins at RVA 0x4ba8
	// Code size 13 (0xd)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldarg.1 
	IL_0002:  call native int class GLib.Marshaller::StringToPtrGStrdup(string)
	IL_0007:  call instance void class GLib.ListBase::Append(native int)
	IL_000c:  ret 
    } // end of method ListBase::Append

    // method line 340
    .method public hidebysig 
           instance default void Append (object item)  cil managed 
    {
        // Method begins at RVA 0x4bb8
	// Code size 14 (0xe)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldarg.0 
	IL_0002:  ldarg.1 
	IL_0003:  call instance native int class GLib.ListBase::AllocNativeElement(object)
	IL_0008:  call instance void class GLib.ListBase::Append(native int)
	IL_000d:  ret 
    } // end of method ListBase::Append

    // method line 341
    .method private hidebysig 
           instance default native int AllocNativeElement (object element)  cil managed 
    {
        // Method begins at RVA 0x4bc8
	// Code size 271 (0x10f)
	.maxstack 2
	IL_0000:  ldarg.0 
	IL_0001:  ldfld class [mscorlib]System.Type GLib.ListBase::element_type
	IL_0006:  ldnull 
	IL_0007:  call bool class [mscorlib]System.Type::op_Equality(class [mscorlib]System.Type, class [mscorlib]System.Type)
	IL_000c:  brfalse IL_0034

	IL_0011:  ldarg.1 
	IL_0012:  isinst GLib.IWrapper
	IL_0017:  brfalse IL_0028

	IL_001c:  ldarg.1 
	IL_001d:  isinst GLib.IWrapper
	IL_0022:  callvirt instance native int class GLib.IWrapper::get_Handle()
	IL_0027:  ret 
	IL_0028:  ldarg.1 
	IL_0029:  call valuetype [mscorlib]System.Runtime.InteropServices.GCHandle valuetype [mscorlib]System.Runtime.InteropServices.GCHandle::Alloc(object)
	IL_002e:  call native int valuetype [mscorlib]System.Runtime.InteropServices.GCHandle::op_Explicit(valuetype [mscorlib]System.Runtime.InteropServices.GCHandle)
	IL_0033:  ret 
	IL_0034:  ldarg.0 
	IL_0035:  ldfld class [mscorlib]System.Type GLib.ListBase::element_type
	IL_003a:  ldtoken string
	IL_003f:  call class [mscorlib]System.Type class [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
	IL_0044:  call bool class [mscorlib]System.Type::op_Equality(class [mscorlib]System.Type, class [mscorlib]System.Type)
	IL_0049:  brfalse IL_005a

	IL_004e:  ldarg.1 
	IL_004f:  isinst string
	IL_0054:  call native int class GLib.Marshaller::StringToPtrGStrdup(string)
	IL_0059:  ret 
	IL_005a:  ldarg.0 
	IL_005b:  ldfld class [mscorlib]System.Type GLib.ListBase::element_type
	IL_0060:  ldtoken GLib.ListBase/FilenameString
	IL_0065:  call class [mscorlib]System.Type class [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
	IL_006a:  call bool class [mscorlib]System.Type::op_Equality(class [mscorlib]System.Type, class [mscorlib]System.Type)
	IL_006f:  brfalse IL_0080

	IL_0074:  ldarg.1 
	IL_0075:  isinst string
	IL_007a:  call native int class GLib.Marshaller::StringToFilenamePtr(string)
	IL_007f:  ret 
	IL_0080:  ldarg.0 
	IL_0081:  ldfld class [mscorlib]System.Type GLib.ListBase::element_type
	IL_0086:  ldtoken native int
	IL_008b:  call class [mscorlib]System.Type class [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
	IL_0090:  call bool class [mscorlib]System.Type::op_Equality(class [mscorlib]System.Type, class [mscorlib]System.Type)
	IL_0095:  brfalse IL_00a6

	IL_009a:  ldarg.1 
	IL_009b:  call valuetype [mscorlib]System.Runtime.InteropServices.GCHandle valuetype [mscorlib]System.Runtime.InteropServices.GCHandle::Alloc(object)
	IL_00a0:  call native int valuetype [mscorlib]System.Runtime.InteropServices.GCHandle::op_Explicit(valuetype [mscorlib]System.Runtime.InteropServices.GCHandle)
	IL_00a5:  ret 
	IL_00a6:  ldtoken GLib.IWrapper
	IL_00ab:  call class [mscorlib]System.Type class [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
	IL_00b0:  ldarg.0 
	IL_00b1:  ldfld class [mscorlib]System.Type GLib.ListBase::element_type
	IL_00b6:  callvirt instance bool class [mscorlib]System.Type::IsAssignableFrom(class [mscorlib]System.Type)
	IL_00bb:  brfalse IL_00cc

	IL_00c0:  ldarg.1 
	IL_00c1:  isinst GLib.IWrapper
	IL_00c6:  callvirt instance native int class GLib.IWrapper::get_Handle()
	IL_00cb:  ret 
	IL_00cc:  ldarg.0 
	IL_00cd:  ldfld class [mscorlib]System.Type GLib.ListBase::element_type
	IL_00d2:  ldtoken int32
	IL_00d7:  call class [mscorlib]System.Type class [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
	IL_00dc:  call bool class [mscorlib]System.Type::op_Equality(class [mscorlib]System.Type, class [mscorlib]System.Type)
	IL_00e1:  brfalse IL_00f2

	IL_00e6:  ldarg.1 
	IL_00e7:  unbox.any int32
	IL_00ec:  newobj instance void native int::'.ctor'(int32)
	IL_00f1:  ret 
	IL_00f2:  ldarg.0 
	IL_00f3:  ldfld class [mscorlib]System.Type GLib.ListBase::element_type
	IL_00f8:  callvirt instance bool class [mscorlib]System.Type::get_IsValueType()
	IL_00fd:  brfalse IL_0109

	IL_0102:  ldarg.1 
	IL_0103:  call native int class GLib.Marshaller::StructureToPtrAlloc(object)
	IL_0108:  ret 
	IL_0109:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_010e:  ret 
    } // end of method ListBase::AllocNativeElement

    // method line 342
    .method public hidebysig 
           instance default void Prepend (native int raw)  cil managed 
    {
        // Method begins at RVA 0x4ce4
	// Code size 20 (0x14)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldarg.0 
	IL_0002:  ldarg.0 
	IL_0003:  ldfld native int GLib.ListBase::list_ptr
	IL_0008:  ldarg.1 
	IL_0009:  callvirt instance native int class GLib.ListBase::Prepend(native int, native int)
	IL_000e:  stfld native int GLib.ListBase::list_ptr
	IL_0013:  ret 
    } // end of method ListBase::Prepend

    // method line 343
    .method public final virtual hidebysig newslot specialname 
           instance default int32 get_Count ()  cil managed 
    {
        // Method begins at RVA 0x4cfc
	// Code size 13 (0xd)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldarg.0 
	IL_0002:  ldfld native int GLib.ListBase::list_ptr
	IL_0007:  callvirt instance int32 class GLib.ListBase::Length(native int)
	IL_000c:  ret 
    } // end of method ListBase::get_Count

    // method line 344
    .method public hidebysig specialname 
           instance default object get_Item (int32 index)  cil managed 
    {
        // Method begins at RVA 0x4d0c
	// Code size 20 (0x14)
	.maxstack 2
	.locals init (
		native int	V_0,
		object	V_1)
	IL_0000:  ldarg.0 
	IL_0001:  ldarg.1 
	IL_0002:  callvirt instance native int class GLib.ListBase::NthData(unsigned int32)
	IL_0007:  stloc.0 
	IL_0008:  ldnull 
	IL_0009:  stloc.1 
	IL_000a:  ldarg.0 
	IL_000b:  ldloc.0 
	IL_000c:  call instance object class GLib.ListBase::DataMarshal(native int)
	IL_0011:  stloc.1 
	IL_0012:  ldloc.1 
	IL_0013:  ret 
    } // end of method ListBase::get_Item

    // method line 345
    .method assembly hidebysig 
           instance default object DataMarshal (native int data)  cil managed 
    {
        // Method begins at RVA 0x4d2c
	// Code size 487 (0x1e7)
	.maxstack 6
	.locals init (
		object	V_0,
		class [mscorlib]System.Type	V_1,
		class [mscorlib]System.Reflection.MethodInfo	V_2)
	IL_0000:  ldnull 
	IL_0001:  stloc.0 
	IL_0002:  ldarg.0 
	IL_0003:  ldfld class [mscorlib]System.Type GLib.ListBase::element_type
	IL_0008:  ldnull 
	IL_0009:  call bool class [mscorlib]System.Type::op_Inequality(class [mscorlib]System.Type, class [mscorlib]System.Type)
	IL_000e:  brfalse IL_01d2

	IL_0013:  ldarg.0 
	IL_0014:  ldfld class [mscorlib]System.Type GLib.ListBase::element_type
	IL_0019:  ldtoken string
	IL_001e:  call class [mscorlib]System.Type class [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
	IL_0023:  call bool class [mscorlib]System.Type::op_Equality(class [mscorlib]System.Type, class [mscorlib]System.Type)
	IL_0028:  brfalse IL_0039

	IL_002d:  ldarg.1 
	IL_002e:  call string class GLib.Marshaller::Utf8PtrToString(native int)
	IL_0033:  stloc.0 
	IL_0034:  br IL_01cd

	IL_0039:  ldarg.0 
	IL_003a:  ldfld class [mscorlib]System.Type GLib.ListBase::element_type
	IL_003f:  ldtoken GLib.ListBase/FilenameString
	IL_0044:  call class [mscorlib]System.Type class [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
	IL_0049:  call bool class [mscorlib]System.Type::op_Equality(class [mscorlib]System.Type, class [mscorlib]System.Type)
	IL_004e:  brfalse IL_005f

	IL_0053:  ldarg.1 
	IL_0054:  call string class GLib.Marshaller::FilenamePtrToString(native int)
	IL_0059:  stloc.0 
	IL_005a:  br IL_01cd

	IL_005f:  ldarg.0 
	IL_0060:  ldfld class [mscorlib]System.Type GLib.ListBase::element_type
	IL_0065:  ldtoken native int
	IL_006a:  call class [mscorlib]System.Type class [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
	IL_006f:  call bool class [mscorlib]System.Type::op_Equality(class [mscorlib]System.Type, class [mscorlib]System.Type)
	IL_0074:  brfalse IL_0085

	IL_0079:  ldarg.1 
	IL_007a:  box native int
	IL_007f:  stloc.0 
	IL_0080:  br IL_01cd

	IL_0085:  ldarg.0 
	IL_0086:  ldfld class [mscorlib]System.Type GLib.ListBase::element_type
	IL_008b:  ldtoken GLib.Object
	IL_0090:  call class [mscorlib]System.Type class [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
	IL_0095:  callvirt instance bool class [mscorlib]System.Type::IsSubclassOf(class [mscorlib]System.Type)
	IL_009a:  brfalse IL_00ac

	IL_009f:  ldarg.1 
	IL_00a0:  ldc.i4.0 
	IL_00a1:  call class GLib.Object class GLib.Object::GetObject(native int, bool)
	IL_00a6:  stloc.0 
	IL_00a7:  br IL_01cd

	IL_00ac:  ldarg.0 
	IL_00ad:  ldfld class [mscorlib]System.Type GLib.ListBase::element_type
	IL_00b2:  ldtoken GLib.Opaque
	IL_00b7:  call class [mscorlib]System.Type class [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
	IL_00bc:  callvirt instance bool class [mscorlib]System.Type::IsSubclassOf(class [mscorlib]System.Type)
	IL_00c1:  brfalse IL_00de

	IL_00c6:  ldarg.1 
	IL_00c7:  ldarg.0 
	IL_00c8:  ldfld class [mscorlib]System.Type GLib.ListBase::element_type
	IL_00cd:  ldarg.0 
	IL_00ce:  ldfld bool GLib.ListBase::elements_owned
	IL_00d3:  call class GLib.Opaque class GLib.Opaque::GetOpaque(native int, class [mscorlib]System.Type, bool)
	IL_00d8:  stloc.0 
	IL_00d9:  br IL_01cd

	IL_00de:  ldarg.0 
	IL_00df:  ldfld class [mscorlib]System.Type GLib.ListBase::element_type
	IL_00e4:  ldtoken int32
	IL_00e9:  call class [mscorlib]System.Type class [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
	IL_00ee:  call bool class [mscorlib]System.Type::op_Equality(class [mscorlib]System.Type, class [mscorlib]System.Type)
	IL_00f3:  brfalse IL_0109

	IL_00f8:  ldarg.1 
	IL_00f9:  call int32 native int::op_Explicit(native int)
	IL_00fe:  box int32
	IL_0103:  stloc.0 
	IL_0104:  br IL_01cd

	IL_0109:  ldarg.0 
	IL_010a:  ldfld class [mscorlib]System.Type GLib.ListBase::element_type
	IL_010f:  callvirt instance bool class [mscorlib]System.Type::get_IsValueType()
	IL_0114:  brfalse IL_012b

	IL_0119:  ldarg.1 
	IL_011a:  ldarg.0 
	IL_011b:  ldfld class [mscorlib]System.Type GLib.ListBase::element_type
	IL_0120:  call object class [mscorlib]System.Runtime.InteropServices.Marshal::PtrToStructure(native int, class [mscorlib]System.Type)
	IL_0125:  stloc.0 
	IL_0126:  br IL_01cd

	IL_012b:  ldarg.0 
	IL_012c:  ldfld class [mscorlib]System.Type GLib.ListBase::element_type
	IL_0131:  callvirt instance bool class [mscorlib]System.Type::get_IsInterface()
	IL_0136:  brfalse IL_01b2

	IL_013b:  ldarg.0 
	IL_013c:  ldfld class [mscorlib]System.Type GLib.ListBase::element_type
	IL_0141:  callvirt instance class [mscorlib]System.Reflection.Assembly class [mscorlib]System.Type::get_Assembly()
	IL_0146:  ldarg.0 
	IL_0147:  ldfld class [mscorlib]System.Type GLib.ListBase::element_type
	IL_014c:  callvirt instance string class [mscorlib]System.Type::get_FullName()
	IL_0151:  ldstr "Adapter"
	IL_0156:  call string string::Concat(string, string)
	IL_015b:  callvirt instance class [mscorlib]System.Type class [mscorlib]System.Reflection.Assembly::GetType(string)
	IL_0160:  stloc.1 
	IL_0161:  ldloc.1 
	IL_0162:  ldstr "GetObject"
	IL_0167:  ldc.i4.2 
	IL_0168:  newarr [mscorlib]System.Type
	IL_016d:  dup 
	IL_016e:  ldc.i4.0 
	IL_016f:  ldtoken native int
	IL_0174:  call class [mscorlib]System.Type class [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
	IL_0179:  stelem.ref 
	IL_017a:  dup 
	IL_017b:  ldc.i4.1 
	IL_017c:  ldtoken bool
	IL_0181:  call class [mscorlib]System.Type class [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
	IL_0186:  stelem.ref 
	IL_0187:  callvirt instance class [mscorlib]System.Reflection.MethodInfo class [mscorlib]System.Type::GetMethod(string, class [mscorlib]System.Type[])
	IL_018c:  stloc.2 
	IL_018d:  ldloc.2 
	IL_018e:  ldnull 
	IL_018f:  ldc.i4.2 
	IL_0190:  newarr object
	IL_0195:  dup 
	IL_0196:  ldc.i4.0 
	IL_0197:  ldarg.1 
	IL_0198:  box native int
	IL_019d:  stelem.ref 
	IL_019e:  dup 
	IL_019f:  ldc.i4.1 
	IL_01a0:  ldc.i4.0 
	IL_01a1:  box bool
	IL_01a6:  stelem.ref 
	IL_01a7:  callvirt instance object class [mscorlib]System.Reflection.MethodBase::Invoke(object, object[])
	IL_01ac:  stloc.0 
	IL_01ad:  br IL_01cd

	IL_01b2:  ldarg.0 
	IL_01b3:  ldfld class [mscorlib]System.Type GLib.ListBase::element_type
	IL_01b8:  ldc.i4.1 
	IL_01b9:  newarr object
	IL_01be:  dup 
	IL_01bf:  ldc.i4.0 
	IL_01c0:  ldarg.1 
	IL_01c1:  box native int
	IL_01c6:  stelem.ref 
	IL_01c7:  call object class [mscorlib]System.Activator::CreateInstance(class [mscorlib]System.Type, object[])
	IL_01cc:  stloc.0 
	IL_01cd:  br IL_01e5

	IL_01d2:  ldarg.1 
	IL_01d3:  call bool class GLib.Object::IsObject(native int)
	IL_01d8:  brfalse IL_01e5

	IL_01dd:  ldarg.1 
	IL_01de:  ldc.i4.0 
	IL_01df:  call class GLib.Object class GLib.Object::GetObject(native int, bool)
	IL_01e4:  stloc.0 
	IL_01e5:  ldloc.0 
	IL_01e6:  ret 
    } // end of method ListBase::DataMarshal

    // method line 346
    .method public final virtual hidebysig newslot specialname 
           instance default bool get_IsSynchronized ()  cil managed 
    {
        // Method begins at RVA 0x4f20
	// Code size 2 (0x2)
	.maxstack 8
	IL_0000:  ldc.i4.0 
	IL_0001:  ret 
    } // end of method ListBase::get_IsSynchronized

    // method line 347
    .method public final virtual hidebysig newslot specialname 
           instance default object get_SyncRoot ()  cil managed 
    {
        // Method begins at RVA 0x4f24
	// Code size 2 (0x2)
	.maxstack 8
	IL_0000:  ldnull 
	IL_0001:  ret 
    } // end of method ListBase::get_SyncRoot

    // method line 348
    .method public final virtual hidebysig newslot 
           instance default void CopyTo (class [mscorlib]System.Array 'array', int32 index)  cil managed 
    {
        // Method begins at RVA 0x4f28
	// Code size 15 (0xf)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldarg.1 
	IL_0002:  ldarg.2 
	IL_0003:  ldarg.0 
	IL_0004:  call instance int32 class GLib.ListBase::get_Count()
	IL_0009:  call instance void class GLib.ListBase::CopyTo(class [mscorlib]System.Array, int32, int32)
	IL_000e:  ret 
    } // end of method ListBase::CopyTo

    // method line 349
    .method assembly hidebysig 
           instance default void CopyTo (class [mscorlib]System.Array 'array', int32 index, int32 count)  cil managed 
    {
        // Method begins at RVA 0x4f38
	// Code size 83 (0x53)
	.maxstack 4
	.locals init (
		object[]	V_0,
		int32	V_1,
		object	V_2,
		class [mscorlib]System.Collections.IEnumerator	V_3,
		class [mscorlib]System.IDisposable	V_4)
	IL_0000:  ldarg.3 
	IL_0001:  newarr object
	IL_0006:  stloc.0 
	IL_0007:  ldc.i4.0 
	IL_0008:  stloc.1 
	IL_0009:  ldarg.0 
	IL_000a:  call instance class [mscorlib]System.Collections.IEnumerator class GLib.ListBase::GetEnumerator()
	IL_000f:  stloc.3 
	.try { // 0
	  IL_0010:  br IL_0024

	  IL_0015:  ldloc.3 
	  IL_0016:  callvirt instance object class [mscorlib]System.Collections.IEnumerator::get_Current()
	  IL_001b:  stloc.2 
	  IL_001c:  ldloc.0 
	  IL_001d:  ldloc.1 
	  IL_001e:  dup 
	  IL_001f:  ldc.i4.1 
	  IL_0020:  add 
	  IL_0021:  stloc.1 
	  IL_0022:  ldloc.2 
	  IL_0023:  stelem.ref 
	  IL_0024:  ldloc.3 
	  IL_0025:  callvirt instance bool class [mscorlib]System.Collections.IEnumerator::MoveNext()
	  IL_002a:  brtrue IL_0015

	  IL_002f:  leave IL_004a

	} // end .try 0
	finally  { // 0
	  IL_0034:  ldloc.3 
	  IL_0035:  isinst [mscorlib]System.IDisposable
	  IL_003a:  dup 
	  IL_003b:  stloc.s 4
	  IL_003d:  brfalse IL_0049

	  IL_0042:  ldloc.s 4
	  IL_0044:  callvirt instance void class [mscorlib]System.IDisposable::Dispose()
	  IL_0049:  endfinally 
	} // end handler 0
	IL_004a:  ldloc.0 
	IL_004b:  ldarg.1 
	IL_004c:  ldarg.2 
	IL_004d:  callvirt instance void class [mscorlib]System.Array::CopyTo(class [mscorlib]System.Array, int32)
	IL_0052:  ret 
    } // end of method ListBase::CopyTo

    // method line 350
    .method public final virtual hidebysig newslot 
           instance default class [mscorlib]System.Collections.IEnumerator GetEnumerator ()  cil managed 
    {
        // Method begins at RVA 0x4fa8
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  newobj instance void class GLib.ListBase/ListEnumerator::'.ctor'(class GLib.ListBase)
	IL_0006:  ret 
    } // end of method ListBase::GetEnumerator

    // method line 351
    .method public hidebysig 
           instance default void CopyTo<T> (!!T[] 'array', int32 index)  cil managed 
    {
        // Method begins at RVA 0x4fb0
	// Code size 75 (0x4b)
	.maxstack 4
	.locals init (
		int32	V_0,
		!!T	V_1,
		class [mscorlib]System.Collections.IEnumerator	V_2,
		class [mscorlib]System.IDisposable	V_3)
	IL_0000:  ldarg.2 
	IL_0001:  stloc.0 
	IL_0002:  ldarg.0 
	IL_0003:  call instance class [mscorlib]System.Collections.IEnumerator class GLib.ListBase::GetEnumerator()
	IL_0008:  stloc.2 
	.try { // 0
	  IL_0009:  br IL_0026

	  IL_000e:  ldloc.2 
	  IL_000f:  callvirt instance object class [mscorlib]System.Collections.IEnumerator::get_Current()
	  IL_0014:  unbox.any !!0
	  IL_0019:  stloc.1 
	  IL_001a:  ldarg.1 
	  IL_001b:  ldloc.0 
	  IL_001c:  dup 
	  IL_001d:  ldc.i4.1 
	  IL_001e:  add 
	  IL_001f:  stloc.0 
	  IL_0020:  ldloc.1 
	  IL_0021:  stelem !!0
	  IL_0026:  ldloc.2 
	  IL_0027:  callvirt instance bool class [mscorlib]System.Collections.IEnumerator::MoveNext()
	  IL_002c:  brtrue IL_000e

	  IL_0031:  leave IL_004a

	} // end .try 0
	finally  { // 0
	  IL_0036:  ldloc.2 
	  IL_0037:  isinst [mscorlib]System.IDisposable
	  IL_003c:  dup 
	  IL_003d:  stloc.3 
	  IL_003e:  brfalse IL_0049

	  IL_0043:  ldloc.3 
	  IL_0044:  callvirt instance void class [mscorlib]System.IDisposable::Dispose()
	  IL_0049:  endfinally 
	} // end handler 0
	IL_004a:  ret 
    } // end of method ListBase::CopyTo

    // method line 352
    .method private static hidebysig pinvokeimpl ("libgobject-2.0-0.dll" as "g_object_unref" cdecl )
           default void g_object_unref (native int item)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method ListBase::g_object_unref

    // method line 353
    .method public hidebysig 
           instance default void Empty ()  cil managed 
    {
        // Method begins at RVA 0x5018
	// Code size 245 (0xf5)
	.maxstack 3
	.locals init (
		native int	V_0,
		native int	V_1,
		native int	V_2,
		native int	V_3)
	IL_0000:  ldarg.0 
	IL_0001:  ldfld bool GLib.ListBase::elements_owned
	IL_0006:  brfalse IL_00e3

	IL_000b:  ldarg.0 
	IL_000c:  ldfld native int GLib.ListBase::list_ptr
	IL_0011:  stloc.0 
	IL_0012:  ldtoken GLib.Object
	IL_0017:  call class [mscorlib]System.Type class [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
	IL_001c:  ldarg.0 
	IL_001d:  ldfld class [mscorlib]System.Type GLib.ListBase::element_type
	IL_0022:  callvirt instance bool class [mscorlib]System.Type::IsAssignableFrom(class [mscorlib]System.Type)
	IL_0027:  brfalse IL_005c

	IL_002c:  br IL_0047

	IL_0031:  ldloc.0 
	IL_0032:  stloc.1 
	IL_0033:  ldarg.0 
	IL_0034:  ldloc.1 
	IL_0035:  call instance native int class GLib.ListBase::Next(native int)
	IL_003a:  stloc.0 
	IL_003b:  ldarg.0 
	IL_003c:  ldloc.1 
	IL_003d:  call instance native int class GLib.ListBase::GetData(native int)
	IL_0042:  call void class GLib.ListBase::g_object_unref(native int)
	IL_0047:  ldloc.0 
	IL_0048:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_004d:  call bool native int::op_Inequality(native int, native int)
	IL_0052:  brtrue IL_0031

	IL_0057:  br IL_00e3

	IL_005c:  ldtoken GLib.Opaque
	IL_0061:  call class [mscorlib]System.Type class [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
	IL_0066:  ldarg.0 
	IL_0067:  ldfld class [mscorlib]System.Type GLib.ListBase::element_type
	IL_006c:  callvirt instance bool class [mscorlib]System.Type::IsAssignableFrom(class [mscorlib]System.Type)
	IL_0071:  brfalse IL_00b2

	IL_0076:  br IL_009d

	IL_007b:  ldloc.0 
	IL_007c:  stloc.2 
	IL_007d:  ldarg.0 
	IL_007e:  ldloc.2 
	IL_007f:  call instance native int class GLib.ListBase::Next(native int)
	IL_0084:  stloc.0 
	IL_0085:  ldarg.0 
	IL_0086:  ldloc.2 
	IL_0087:  call instance native int class GLib.ListBase::GetData(native int)
	IL_008c:  ldarg.0 
	IL_008d:  ldfld class [mscorlib]System.Type GLib.ListBase::element_type
	IL_0092:  ldc.i4.1 
	IL_0093:  call class GLib.Opaque class GLib.Opaque::GetOpaque(native int, class [mscorlib]System.Type, bool)
	IL_0098:  callvirt instance void class GLib.Opaque::Dispose()
	IL_009d:  ldloc.0 
	IL_009e:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_00a3:  call bool native int::op_Inequality(native int, native int)
	IL_00a8:  brtrue IL_007b

	IL_00ad:  br IL_00e3

	IL_00b2:  br IL_00d3

	IL_00b7:  ldloc.0 
	IL_00b8:  stloc.3 
	IL_00b9:  ldarg.0 
	IL_00ba:  ldloc.3 
	IL_00bb:  call instance native int class GLib.ListBase::Next(native int)
	IL_00c0:  stloc.0 
	IL_00c1:  ldarg.0 
	IL_00c2:  ldfld class GLib.ListElementFree GLib.ListBase::free_func
	IL_00c7:  ldarg.0 
	IL_00c8:  ldloc.3 
	IL_00c9:  call instance native int class GLib.ListBase::GetData(native int)
	IL_00ce:  callvirt instance void class GLib.ListElementFree::Invoke(native int)
	IL_00d3:  ldloc.0 
	IL_00d4:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_00d9:  call bool native int::op_Inequality(native int, native int)
	IL_00de:  brtrue IL_00b7

	IL_00e3:  ldarg.0 
	IL_00e4:  ldfld bool GLib.ListBase::'managed'
	IL_00e9:  brfalse IL_00f4

	IL_00ee:  ldarg.0 
	IL_00ef:  call instance void class GLib.ListBase::FreeList()
	IL_00f4:  ret 
    } // end of method ListBase::Empty

    // method line 354
    .method private hidebysig 
           instance default native int Next (native int current)  cil managed 
    {
        // Method begins at RVA 0x511c
	// Code size 12 (0xc)
	.maxstack 8
	IL_0000:  ldarg.1 
	IL_0001:  call int32 native int::get_Size()
	IL_0006:  call native int class [mscorlib]System.Runtime.InteropServices.Marshal::ReadIntPtr(native int, int32)
	IL_000b:  ret 
    } // end of method ListBase::Next

    // method line 355
    .method private hidebysig 
           instance default native int GetData (native int current)  cil managed 
    {
        // Method begins at RVA 0x512c
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.1 
	IL_0001:  call native int class [mscorlib]System.Runtime.InteropServices.Marshal::ReadIntPtr(native int)
	IL_0006:  ret 
    } // end of method ListBase::GetData

    // method line 356
    .method private hidebysig 
           instance default void FreeList ()  cil managed 
    {
        // Method begins at RVA 0x5134
	// Code size 45 (0x2d)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldfld native int GLib.ListBase::list_ptr
	IL_0006:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_000b:  call bool native int::op_Inequality(native int, native int)
	IL_0010:  brfalse IL_0021

	IL_0015:  ldarg.0 
	IL_0016:  ldarg.0 
	IL_0017:  ldfld native int GLib.ListBase::list_ptr
	IL_001c:  callvirt instance void class GLib.ListBase::Free(native int)
	IL_0021:  ldarg.0 
	IL_0022:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_0027:  stfld native int GLib.ListBase::list_ptr
	IL_002c:  ret 
    } // end of method ListBase::FreeList

    // method line 357
    .method public final virtual hidebysig newslot 
           instance default void Dispose ()  cil managed 
    {
        // Method begins at RVA 0x5164
	// Code size 14 (0xe)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldc.i4.1 
	IL_0002:  callvirt instance void class GLib.ListBase::Dispose(bool)
	IL_0007:  ldarg.0 
	IL_0008:  call void class [mscorlib]System.GC::SuppressFinalize(object)
	IL_000d:  ret 
    } // end of method ListBase::Dispose

    // method line 358
    .method public virtual hidebysig newslot abstract 
           instance default object Clone ()  cil managed 
    {
        // Method begins at RVA 0x0
    } // end of method ListBase::Clone

	.property instance bool Managed ()
	{
		.custom instance void class [mscorlib]System.ObsoleteAttribute::'.ctor'(string) =  (
		01 00 24 52 65 70 6C 61 63 65 64 20 62 79 20 6F   // ..$Replaced by o
		77 6E 65 64 20 70 61 72 61 6D 65 74 65 72 20 6F   // wned parameter o
		6E 20 63 74 6F 72 2E 00 00                      ) // n ctor...

		.set instance default void GLib.ListBase::set_Managed (bool 'value') 
	}
	.property instance native int Handle ()
	{
		.get instance default native int GLib.ListBase::get_Handle () 
	}
	.property instance int32 Count ()
	{
		.get instance default int32 GLib.ListBase::get_Count () 
	}
	.property instance object Item (int32)
	{
		.get instance default object GLib.ListBase::get_Item (int32 index) 
	}
	.property instance bool IsSynchronized ()
	{
		.get instance default bool GLib.ListBase::get_IsSynchronized () 
	}
	.property instance object SyncRoot ()
	{
		.get instance default object GLib.ListBase::get_SyncRoot () 
	}
  .class nested public auto ansi beforefieldinit FilenameString
  	extends [mscorlib]System.Object
  {

    // method line 359
    .method private hidebysig specialname rtspecialname 
           instance default void '.ctor' ()  cil managed 
    {
        // Method begins at RVA 0x5174
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::'.ctor'()
	IL_0006:  ret 
    } // end of method FilenameString::.ctor

  } // end of class FilenameString

  .class nested private auto ansi beforefieldinit ListEnumerator
  	extends [mscorlib]System.Object
  	implements [mscorlib]System.Collections.IEnumerator  {
    .field  private  native int current
    .field  private  class GLib.ListBase list

    // method line 360
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (class GLib.ListBase list)  cil managed 
    {
        // Method begins at RVA 0x517c
	// Code size 25 (0x19)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_0006:  stfld native int GLib.ListBase/ListEnumerator::current
	IL_000b:  ldarg.0 
	IL_000c:  call instance void object::'.ctor'()
	IL_0011:  ldarg.0 
	IL_0012:  ldarg.1 
	IL_0013:  stfld class GLib.ListBase GLib.ListBase/ListEnumerator::list
	IL_0018:  ret 
    } // end of method ListEnumerator::.ctor

    // method line 361
    .method public final virtual hidebysig newslot specialname 
           instance default object get_Current ()  cil managed 
    {
        // Method begins at RVA 0x5198
	// Code size 35 (0x23)
	.maxstack 2
	.locals init (
		native int	V_0,
		object	V_1)
	IL_0000:  ldarg.0 
	IL_0001:  ldfld class GLib.ListBase GLib.ListBase/ListEnumerator::list
	IL_0006:  ldarg.0 
	IL_0007:  ldfld native int GLib.ListBase/ListEnumerator::current
	IL_000c:  callvirt instance native int class GLib.ListBase::GetData(native int)
	IL_0011:  stloc.0 
	IL_0012:  ldnull 
	IL_0013:  stloc.1 
	IL_0014:  ldarg.0 
	IL_0015:  ldfld class GLib.ListBase GLib.ListBase/ListEnumerator::list
	IL_001a:  ldloc.0 
	IL_001b:  callvirt instance object class GLib.ListBase::DataMarshal(native int)
	IL_0020:  stloc.1 
	IL_0021:  ldloc.1 
	IL_0022:  ret 
    } // end of method ListEnumerator::get_Current

    // method line 362
    .method public final virtual hidebysig newslot 
           instance default bool MoveNext ()  cil managed 
    {
        // Method begins at RVA 0x51c8
	// Code size 83 (0x53)
	.maxstack 3
	IL_0000:  ldarg.0 
	IL_0001:  ldfld native int GLib.ListBase/ListEnumerator::current
	IL_0006:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_000b:  call bool native int::op_Equality(native int, native int)
	IL_0010:  brfalse IL_002b

	IL_0015:  ldarg.0 
	IL_0016:  ldarg.0 
	IL_0017:  ldfld class GLib.ListBase GLib.ListBase/ListEnumerator::list
	IL_001c:  ldfld native int GLib.ListBase::list_ptr
	IL_0021:  stfld native int GLib.ListBase/ListEnumerator::current
	IL_0026:  br IL_0042

	IL_002b:  ldarg.0 
	IL_002c:  ldarg.0 
	IL_002d:  ldfld class GLib.ListBase GLib.ListBase/ListEnumerator::list
	IL_0032:  ldarg.0 
	IL_0033:  ldfld native int GLib.ListBase/ListEnumerator::current
	IL_0038:  callvirt instance native int class GLib.ListBase::Next(native int)
	IL_003d:  stfld native int GLib.ListBase/ListEnumerator::current
	IL_0042:  ldarg.0 
	IL_0043:  ldfld native int GLib.ListBase/ListEnumerator::current
	IL_0048:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_004d:  call bool native int::op_Inequality(native int, native int)
	IL_0052:  ret 
    } // end of method ListEnumerator::MoveNext

    // method line 363
    .method public final virtual hidebysig newslot 
           instance default void Reset ()  cil managed 
    {
        // Method begins at RVA 0x5228
	// Code size 12 (0xc)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_0006:  stfld native int GLib.ListBase/ListEnumerator::current
	IL_000b:  ret 
    } // end of method ListEnumerator::Reset

	.property instance object Current ()
	{
		.get instance default object GLib.ListBase/ListEnumerator::get_Current () 
	}
  } // end of class ListEnumerator

  } // end of class GLib.ListBase
}

.namespace GLib
{
  .class public auto ansi beforefieldinit List
  	extends GLib.ListBase
  {
    .field  private static  class GLib.ListElementFree '<>f__mg$cache0'
    .custom instance void class [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::'.ctor'() =  (01 00 00 00 ) // ....


    // method line 364
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (native int raw)  cil managed 
    {
        // Method begins at RVA 0x5238
	// Code size 9 (0x9)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldarg.1 
	IL_0002:  ldnull 
	IL_0003:  call instance void class GLib.List::'.ctor'(native int, class [mscorlib]System.Type)
	IL_0008:  ret 
    } // end of method List::.ctor

    // method line 365
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (native int raw, class [mscorlib]System.Type element_type)  cil managed 
    {
        // Method begins at RVA 0x5244
	// Code size 11 (0xb)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldarg.1 
	IL_0002:  ldarg.2 
	IL_0003:  ldc.i4.0 
	IL_0004:  ldc.i4.0 
	IL_0005:  call instance void class GLib.List::'.ctor'(native int, class [mscorlib]System.Type, bool, bool)
	IL_000a:  ret 
    } // end of method List::.ctor

    // method line 366
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (class [mscorlib]System.Type element_type)  cil managed 
    {
        // Method begins at RVA 0x5250
	// Code size 13 (0xd)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_0006:  ldarg.1 
	IL_0007:  call instance void class GLib.List::'.ctor'(native int, class [mscorlib]System.Type)
	IL_000c:  ret 
    } // end of method List::.ctor

    // method line 367
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (native int raw, class [mscorlib]System.Type element_type, bool owned, bool elements_owned)  cil managed 
    {
        // Method begins at RVA 0x5260
	// Code size 41 (0x29)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldarg.1 
	IL_0002:  ldarg.2 
	IL_0003:  ldarg.3 
	IL_0004:  ldarg.s 4
	IL_0006:  ldsfld class GLib.ListElementFree GLib.List::'<>f__mg$cache0'
	IL_000b:  brtrue.s IL_001e

	IL_000d:  ldnull 
	IL_000e:  ldftn void class GLib.List::g_free(native int)
	IL_0014:  newobj instance void class GLib.ListElementFree::'.ctor'(object, native int)
	IL_0019:  stsfld class GLib.ListElementFree GLib.List::'<>f__mg$cache0'
	IL_001e:  ldsfld class GLib.ListElementFree GLib.List::'<>f__mg$cache0'
	IL_0023:  call instance void class GLib.ListBase::'.ctor'(native int, class [mscorlib]System.Type, bool, bool, class GLib.ListElementFree)
	IL_0028:  ret 
    } // end of method List::.ctor

    // method line 368
    .method private static hidebysig pinvokeimpl ("libglib-2.0-0.dll" as "g_free" cdecl )
           default void g_free (native int item)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method List::g_free

    // method line 369
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (native int raw, class [mscorlib]System.Type element_type, bool owned, bool elements_owned, class GLib.ListElementFree free_func)  cil managed 
    {
        // Method begins at RVA 0x528c
	// Code size 14 (0xe)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldarg.1 
	IL_0002:  ldarg.2 
	IL_0003:  ldarg.3 
	IL_0004:  ldarg.s 4
	IL_0006:  ldarg.s 5
	IL_0008:  call instance void class GLib.ListBase::'.ctor'(native int, class [mscorlib]System.Type, bool, bool, class GLib.ListElementFree)
	IL_000d:  ret 
    } // end of method List::.ctor

    // method line 370
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (object[] elements, class [mscorlib]System.Type element_type, bool owned, bool elements_owned)  cil managed 
    {
        // Method begins at RVA 0x529c
	// Code size 49 (0x31)
	.maxstack 5
	.locals init (
		object	V_0,
		object[]	V_1,
		int32	V_2)
	IL_0000:  ldarg.0 
	IL_0001:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_0006:  ldarg.2 
	IL_0007:  ldarg.3 
	IL_0008:  ldarg.s 4
	IL_000a:  call instance void class GLib.List::'.ctor'(native int, class [mscorlib]System.Type, bool, bool)
	IL_000f:  ldarg.1 
	IL_0010:  stloc.1 
	IL_0011:  ldc.i4.0 
	IL_0012:  stloc.2 
	IL_0013:  br IL_0027

	IL_0018:  ldloc.1 
	IL_0019:  ldloc.2 
	IL_001a:  ldelem.ref 
	IL_001b:  stloc.0 
	IL_001c:  ldarg.0 
	IL_001d:  ldloc.0 
	IL_001e:  call instance void class GLib.ListBase::Append(object)
	IL_0023:  ldloc.2 
	IL_0024:  ldc.i4.1 
	IL_0025:  add 
	IL_0026:  stloc.2 
	IL_0027:  ldloc.2 
	IL_0028:  ldloc.1 
	IL_0029:  ldlen 
	IL_002a:  conv.i4 
	IL_002b:  blt IL_0018

	IL_0030:  ret 
    } // end of method List::.ctor

    // method line 371
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (class [mscorlib]System.Array elements, class [mscorlib]System.Type element_type, bool owned, bool elements_owned)  cil managed 
    {
        // Method begins at RVA 0x52dc
	// Code size 78 (0x4e)
	.maxstack 5
	.locals init (
		object	V_0,
		class [mscorlib]System.Collections.IEnumerator	V_1,
		class [mscorlib]System.IDisposable	V_2)
	IL_0000:  ldarg.0 
	IL_0001:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_0006:  ldarg.2 
	IL_0007:  ldarg.3 
	IL_0008:  ldarg.s 4
	IL_000a:  call instance void class GLib.List::'.ctor'(native int, class [mscorlib]System.Type, bool, bool)
	IL_000f:  ldarg.1 
	IL_0010:  callvirt instance class [mscorlib]System.Collections.IEnumerator class [mscorlib]System.Array::GetEnumerator()
	IL_0015:  stloc.1 
	.try { // 0
	  IL_0016:  br IL_0029

	  IL_001b:  ldloc.1 
	  IL_001c:  callvirt instance object class [mscorlib]System.Collections.IEnumerator::get_Current()
	  IL_0021:  stloc.0 
	  IL_0022:  ldarg.0 
	  IL_0023:  ldloc.0 
	  IL_0024:  call instance void class GLib.ListBase::Append(object)
	  IL_0029:  ldloc.1 
	  IL_002a:  callvirt instance bool class [mscorlib]System.Collections.IEnumerator::MoveNext()
	  IL_002f:  brtrue IL_001b

	  IL_0034:  leave IL_004d

	} // end .try 0
	finally  { // 0
	  IL_0039:  ldloc.1 
	  IL_003a:  isinst [mscorlib]System.IDisposable
	  IL_003f:  dup 
	  IL_0040:  stloc.2 
	  IL_0041:  brfalse IL_004c

	  IL_0046:  ldloc.2 
	  IL_0047:  callvirt instance void class [mscorlib]System.IDisposable::Dispose()
	  IL_004c:  endfinally 
	} // end handler 0
	IL_004d:  ret 
    } // end of method List::.ctor

    // method line 372
    .method private static hidebysig pinvokeimpl ("libglib-2.0-0.dll" as "g_list_copy" cdecl )
           default native int g_list_copy (native int l)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method List::g_list_copy

    // method line 373
    .method public virtual hidebysig 
           instance default object Clone ()  cil managed 
    {
        // Method begins at RVA 0x5348
	// Code size 17 (0x11)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance native int class GLib.ListBase::get_Handle()
	IL_0006:  call native int class GLib.List::g_list_copy(native int)
	IL_000b:  newobj instance void class GLib.List::'.ctor'(native int)
	IL_0010:  ret 
    } // end of method List::Clone

    // method line 374
    .method private static hidebysig pinvokeimpl ("libglib-2.0-0.dll" as "g_list_length" cdecl )
           default int32 g_list_length (native int l)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method List::g_list_length

    // method line 375
    .method assembly virtual hidebysig 
           instance default int32 Length (native int list)  cil managed 
    {
        // Method begins at RVA 0x535c
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.1 
	IL_0001:  call int32 class GLib.List::g_list_length(native int)
	IL_0006:  ret 
    } // end of method List::Length

    // method line 376
    .method private static hidebysig pinvokeimpl ("libglib-2.0-0.dll" as "g_list_free" cdecl )
           default void g_list_free (native int l)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method List::g_list_free

    // method line 377
    .method assembly virtual hidebysig 
           instance default void Free (native int list)  cil managed 
    {
        // Method begins at RVA 0x5364
	// Code size 23 (0x17)
	.maxstack 8
	IL_0000:  ldarg.1 
	IL_0001:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_0006:  call bool native int::op_Inequality(native int, native int)
	IL_000b:  brfalse IL_0016

	IL_0010:  ldarg.1 
	IL_0011:  call void class GLib.List::g_list_free(native int)
	IL_0016:  ret 
    } // end of method List::Free

    // method line 378
    .method private static hidebysig pinvokeimpl ("libglib-2.0-0.dll" as "g_list_append" cdecl )
           default native int g_list_append (native int l, native int raw)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method List::g_list_append

    // method line 379
    .method assembly virtual hidebysig 
           instance default native int Append (native int list, native int raw)  cil managed 
    {
        // Method begins at RVA 0x537c
	// Code size 8 (0x8)
	.maxstack 8
	IL_0000:  ldarg.1 
	IL_0001:  ldarg.2 
	IL_0002:  call native int class GLib.List::g_list_append(native int, native int)
	IL_0007:  ret 
    } // end of method List::Append

    // method line 380
    .method private static hidebysig pinvokeimpl ("libglib-2.0-0.dll" as "g_list_prepend" cdecl )
           default native int g_list_prepend (native int l, native int raw)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method List::g_list_prepend

    // method line 381
    .method assembly virtual hidebysig 
           instance default native int Prepend (native int list, native int raw)  cil managed 
    {
        // Method begins at RVA 0x5388
	// Code size 8 (0x8)
	.maxstack 8
	IL_0000:  ldarg.1 
	IL_0001:  ldarg.2 
	IL_0002:  call native int class GLib.List::g_list_prepend(native int, native int)
	IL_0007:  ret 
    } // end of method List::Prepend

    // method line 382
    .method private static hidebysig pinvokeimpl ("libglib-2.0-0.dll" as "g_list_nth_data" cdecl )
           default native int g_list_nth_data (native int l, unsigned int32 n)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method List::g_list_nth_data

    // method line 383
    .method assembly virtual hidebysig 
           instance default native int NthData (unsigned int32 n)  cil managed 
    {
        // Method begins at RVA 0x5394
	// Code size 13 (0xd)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance native int class GLib.ListBase::get_Handle()
	IL_0006:  ldarg.1 
	IL_0007:  call native int class GLib.List::g_list_nth_data(native int, unsigned int32)
	IL_000c:  ret 
    } // end of method List::NthData

  } // end of class GLib.List
}

.namespace GLib
{
  .class public auto ansi sealed LogFunc
  	extends [mscorlib]System.MulticastDelegate
  {

    // method line 384
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (object 'object', native int 'method')  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method LogFunc::.ctor

    // method line 385
    .method public virtual hidebysig newslot 
           instance default void Invoke (string log_domain, valuetype GLib.LogLevelFlags log_level, string message)  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method LogFunc::Invoke

    // method line 386
    .method public virtual hidebysig newslot 
           instance default class [mscorlib]System.IAsyncResult BeginInvoke (string log_domain, valuetype GLib.LogLevelFlags log_level, string message, class [mscorlib]System.AsyncCallback callback, object 'object')  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method LogFunc::BeginInvoke

    // method line 387
    .method public virtual hidebysig newslot 
           instance default void EndInvoke (class [mscorlib]System.IAsyncResult result)  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method LogFunc::EndInvoke

  } // end of class GLib.LogFunc
}

.namespace GLib
{
  .class private auto ansi sealed LogFunc2
  	extends [mscorlib]System.MulticastDelegate
  {
    .custom instance void class [mscorlib]System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute::'.ctor'(valuetype [mscorlib]System.Runtime.InteropServices.CallingConvention) =  (01 00 02 00 00 00 00 00 ) // ........


    // method line 388
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (object 'object', native int 'method')  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method LogFunc2::.ctor

    // method line 389
    .method public virtual hidebysig newslot 
           instance default void Invoke (string log_domain, valuetype GLib.LogLevelFlags log_level, string message, class GLib.LogFunc user_data)  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method LogFunc2::Invoke

    // method line 390
    .method public virtual hidebysig newslot 
           instance default class [mscorlib]System.IAsyncResult BeginInvoke (string log_domain, valuetype GLib.LogLevelFlags log_level, string message, class GLib.LogFunc user_data, class [mscorlib]System.AsyncCallback callback, object 'object')  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method LogFunc2::BeginInvoke

    // method line 391
    .method public virtual hidebysig newslot 
           instance default void EndInvoke (class [mscorlib]System.IAsyncResult result)  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method LogFunc2::EndInvoke

  } // end of class GLib.LogFunc2
}

.namespace GLib
{
  .class public auto ansi sealed PrintFunc
  	extends [mscorlib]System.MulticastDelegate
  {
    .custom instance void class [mscorlib]System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute::'.ctor'(valuetype [mscorlib]System.Runtime.InteropServices.CallingConvention) =  (01 00 02 00 00 00 00 00 ) // ........


    // method line 392
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (object 'object', native int 'method')  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method PrintFunc::.ctor

    // method line 393
    .method public virtual hidebysig newslot 
           instance default void Invoke (string message)  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method PrintFunc::Invoke

    // method line 394
    .method public virtual hidebysig newslot 
           instance default class [mscorlib]System.IAsyncResult BeginInvoke (string message, class [mscorlib]System.AsyncCallback callback, object 'object')  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method PrintFunc::BeginInvoke

    // method line 395
    .method public virtual hidebysig newslot 
           instance default void EndInvoke (class [mscorlib]System.IAsyncResult result)  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method PrintFunc::EndInvoke

  } // end of class GLib.PrintFunc
}

.namespace GLib
{
  .class public auto ansi sealed LogLevelFlags
  	extends [mscorlib]System.Enum
  {
    .custom instance void class [mscorlib]System.FlagsAttribute::'.ctor'() =  (01 00 00 00 ) // ....

    .field  public specialname rtspecialname  int32 value__
    .field public static literal  valuetype GLib.LogLevelFlags FlagRecursion = int32(0x00000001)
    .field public static literal  valuetype GLib.LogLevelFlags FlagFatal = int32(0x00000002)
    .field public static literal  valuetype GLib.LogLevelFlags Error = int32(0x00000004)
    .field public static literal  valuetype GLib.LogLevelFlags Critical = int32(0x00000008)
    .field public static literal  valuetype GLib.LogLevelFlags Warning = int32(0x00000010)
    .field public static literal  valuetype GLib.LogLevelFlags Message = int32(0x00000020)
    .field public static literal  valuetype GLib.LogLevelFlags Info = int32(0x00000040)
    .field public static literal  valuetype GLib.LogLevelFlags Debug = int32(0x00000080)
    .field public static literal  valuetype GLib.LogLevelFlags AllButFatal = int32(0x000000fd)
    .field public static literal  valuetype GLib.LogLevelFlags AllButRecursion = int32(0x000000fe)
    .field public static literal  valuetype GLib.LogLevelFlags All = int32(0x000000ff)
    .field public static literal  valuetype GLib.LogLevelFlags FlagMask = int32(0x00000003)
    .field public static literal  valuetype GLib.LogLevelFlags LevelMask = int32(0xfffffffc)

  } // end of class GLib.LogLevelFlags
}

.namespace GLib
{
  .class public auto ansi beforefieldinit Log
  	extends [mscorlib]System.Object
  {
    .field  private static  class [mscorlib]System.Collections.Hashtable handlers
    .field  private static initonly  class GLib.LogFunc2 LogFuncTrampoline

    // method line 396
    .method public static hidebysig 
           default void PrintTraceLogFunction (string domain, valuetype GLib.LogLevelFlags level, string message)  cil managed 
    {
        // Method begins at RVA 0x53a4
	// Code size 24 (0x18)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldarg.1 
	IL_0002:  ldarg.2 
	IL_0003:  call void class GLib.Log::PrintLogFunction(string, valuetype GLib.LogLevelFlags, string)
	IL_0008:  ldstr "Trace follows:\n{0}"
	IL_000d:  newobj instance void class [mscorlib]System.Diagnostics.StackTrace::'.ctor'()
	IL_0012:  call void class [mscorlib]System.Console::WriteLine(string, object)
	IL_0017:  ret 
    } // end of method Log::PrintTraceLogFunction

    // method line 397
    .method public static hidebysig 
           default unsigned int32 SetLogHandler (string logDomain, valuetype GLib.LogLevelFlags 'flags', class GLib.LogFunc logFunc)  cil managed 
    {
        // Method begins at RVA 0x53c0
	// Code size 51 (0x33)
	.maxstack 4
	.locals init (
		native int	V_0,
		unsigned int32	V_1)
	IL_0000:  ldarg.0 
	IL_0001:  call native int class GLib.Marshaller::StringToPtrGStrdup(string)
	IL_0006:  stloc.0 
	IL_0007:  ldloc.0 
	IL_0008:  ldarg.1 
	IL_0009:  ldsfld class GLib.LogFunc2 GLib.Log::LogFuncTrampoline
	IL_000e:  ldarg.2 
	IL_000f:  call unsigned int32 class GLib.Log::g_log_set_handler(native int, valuetype GLib.LogLevelFlags, class GLib.LogFunc2, class GLib.LogFunc)
	IL_0014:  stloc.1 
	IL_0015:  ldloc.0 
	IL_0016:  call void class GLib.Marshaller::Free(native int)
	IL_001b:  call void class GLib.Log::EnsureHash()
	IL_0020:  ldsfld class [mscorlib]System.Collections.Hashtable GLib.Log::handlers
	IL_0025:  ldloc.1 
	IL_0026:  box unsigned int32
	IL_002b:  ldarg.2 
	IL_002c:  callvirt instance void class [mscorlib]System.Collections.Hashtable::set_Item(object, object)
	IL_0031:  ldloc.1 
	IL_0032:  ret 
    } // end of method Log::SetLogHandler

    // method line 398
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' ()  cil managed 
    {
        // Method begins at RVA 0x5400
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::'.ctor'()
	IL_0006:  ret 
    } // end of method Log::.ctor

    // method line 399
    .method private static hidebysig 
           default void EnsureHash ()  cil managed 
    {
        // Method begins at RVA 0x5408
	// Code size 21 (0x15)
	.maxstack 8
	IL_0000:  ldsfld class [mscorlib]System.Collections.Hashtable GLib.Log::handlers
	IL_0005:  brtrue IL_0014

	IL_000a:  newobj instance void class [mscorlib]System.Collections.Hashtable::'.ctor'()
	IL_000f:  stsfld class [mscorlib]System.Collections.Hashtable GLib.Log::handlers
	IL_0014:  ret 
    } // end of method Log::EnsureHash

    // method line 400
    .method private static hidebysig pinvokeimpl ("libglib-2.0-0.dll" as "g_logv" cdecl )
           default void g_logv (native int log_domain, valuetype GLib.LogLevelFlags 'flags', native int message)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method Log::g_logv

    // method line 401
    .method public static hidebysig 
           default void Write (string logDomain, valuetype GLib.LogLevelFlags 'flags', string format, object[] args)  cil managed 
    {
	.param [4]
	.custom instance void class [mscorlib]System.ParamArrayAttribute::'.ctor'() =  (01 00 00 00 ) // ....

        // Method begins at RVA 0x5420
	// Code size 41 (0x29)
	.maxstack 3
	.locals init (
		native int	V_0,
		native int	V_1)
	IL_0000:  ldarg.0 
	IL_0001:  call native int class GLib.Marshaller::StringToPtrGStrdup(string)
	IL_0006:  stloc.0 
	IL_0007:  ldarg.2 
	IL_0008:  ldarg.3 
	IL_0009:  call string string::Format(string, object[])
	IL_000e:  call native int class GLib.Marshaller::StringToPtrGStrdup(string)
	IL_0013:  stloc.1 
	IL_0014:  ldloc.0 
	IL_0015:  ldarg.1 
	IL_0016:  ldloc.1 
	IL_0017:  call void class GLib.Log::g_logv(native int, valuetype GLib.LogLevelFlags, native int)
	IL_001c:  ldloc.0 
	IL_001d:  call void class GLib.Marshaller::Free(native int)
	IL_0022:  ldloc.1 
	IL_0023:  call void class GLib.Marshaller::Free(native int)
	IL_0028:  ret 
    } // end of method Log::Write

    // method line 402
    .method public hidebysig 
           instance default void WriteLog (string logDomain, valuetype GLib.LogLevelFlags 'flags', string format, object[] args)  cil managed 
    {
        .custom instance void class [mscorlib]System.ObsoleteAttribute::'.ctor'(string) =  (
		01 00 1B 55 73 65 20 74 68 65 20 73 74 61 74 69   // ...Use the stati
		63 20 6D 65 6D 62 65 72 20 57 72 69 74 65 00 00 ) // c member Write..

	.param [4]
	.custom instance void class [mscorlib]System.ParamArrayAttribute::'.ctor'() =  (01 00 00 00 ) // ....

        // Method begins at RVA 0x5458
	// Code size 42 (0x2a)
	.maxstack 3
	.locals init (
		native int	V_0,
		native int	V_1)
	IL_0000:  ldarg.1 
	IL_0001:  call native int class GLib.Marshaller::StringToPtrGStrdup(string)
	IL_0006:  stloc.0 
	IL_0007:  ldarg.3 
	IL_0008:  ldarg.s 4
	IL_000a:  call string string::Format(string, object[])
	IL_000f:  call native int class GLib.Marshaller::StringToPtrGStrdup(string)
	IL_0014:  stloc.1 
	IL_0015:  ldloc.0 
	IL_0016:  ldarg.2 
	IL_0017:  ldloc.1 
	IL_0018:  call void class GLib.Log::g_logv(native int, valuetype GLib.LogLevelFlags, native int)
	IL_001d:  ldloc.0 
	IL_001e:  call void class GLib.Marshaller::Free(native int)
	IL_0023:  ldloc.1 
	IL_0024:  call void class GLib.Marshaller::Free(native int)
	IL_0029:  ret 
    } // end of method Log::WriteLog

    // method line 403
    .method private static hidebysig pinvokeimpl ("libglib-2.0-0.dll" as "g_log_set_handler" cdecl )
           default unsigned int32 g_log_set_handler (native int log_domain, valuetype GLib.LogLevelFlags 'flags', class GLib.LogFunc2 log_func, class GLib.LogFunc user_data)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method Log::g_log_set_handler

    // method line 404
    .method private static hidebysig pinvokeimpl ("libglib-2.0-0.dll" as "g_log_remove_handler" cdecl )
           default unsigned int32 g_log_remove_handler (native int log_domain, unsigned int32 handler_id)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method Log::g_log_remove_handler

    // method line 405
    .method public static hidebysig 
           default void RemoveLogHandler (string logDomain, unsigned int32 handlerID)  cil managed 
    {
        // Method begins at RVA 0x5490
	// Code size 69 (0x45)
	.maxstack 2
	.locals init (
		native int	V_0)
	IL_0000:  ldsfld class [mscorlib]System.Collections.Hashtable GLib.Log::handlers
	IL_0005:  brfalse IL_002f

	IL_000a:  ldsfld class [mscorlib]System.Collections.Hashtable GLib.Log::handlers
	IL_000f:  ldarg.1 
	IL_0010:  box unsigned int32
	IL_0015:  callvirt instance bool class [mscorlib]System.Collections.Hashtable::ContainsKey(object)
	IL_001a:  brfalse IL_002f

	IL_001f:  ldsfld class [mscorlib]System.Collections.Hashtable GLib.Log::handlers
	IL_0024:  ldarg.1 
	IL_0025:  box unsigned int32
	IL_002a:  callvirt instance void class [mscorlib]System.Collections.Hashtable::Remove(object)
	IL_002f:  ldarg.0 
	IL_0030:  call native int class GLib.Marshaller::StringToPtrGStrdup(string)
	IL_0035:  stloc.0 
	IL_0036:  ldloc.0 
	IL_0037:  ldarg.1 
	IL_0038:  call unsigned int32 class GLib.Log::g_log_remove_handler(native int, unsigned int32)
	IL_003d:  pop 
	IL_003e:  ldloc.0 
	IL_003f:  call void class GLib.Marshaller::Free(native int)
	IL_0044:  ret 
    } // end of method Log::RemoveLogHandler

    // method line 406
    .method private static hidebysig pinvokeimpl ("libglib-2.0-0.dll" as "g_set_print_handler" cdecl )
           default class GLib.PrintFunc g_set_print_handler (class GLib.PrintFunc 'handler')  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method Log::g_set_print_handler

    // method line 407
    .method public static hidebysig 
           default class GLib.PrintFunc SetPrintHandler (class GLib.PrintFunc 'handler')  cil managed 
    {
        // Method begins at RVA 0x54e4
	// Code size 28 (0x1c)
	.maxstack 8
	IL_0000:  call void class GLib.Log::EnsureHash()
	IL_0005:  ldsfld class [mscorlib]System.Collections.Hashtable GLib.Log::handlers
	IL_000a:  ldstr "PrintHandler"
	IL_000f:  ldarg.0 
	IL_0010:  callvirt instance void class [mscorlib]System.Collections.Hashtable::set_Item(object, object)
	IL_0015:  ldarg.0 
	IL_0016:  call class GLib.PrintFunc class GLib.Log::g_set_print_handler(class GLib.PrintFunc)
	IL_001b:  ret 
    } // end of method Log::SetPrintHandler

    // method line 408
    .method private static hidebysig pinvokeimpl ("libglib-2.0-0.dll" as "g_set_printerr_handler" cdecl )
           default class GLib.PrintFunc g_set_printerr_handler (class GLib.PrintFunc 'handler')  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method Log::g_set_printerr_handler

    // method line 409
    .method public static hidebysig 
           default class GLib.PrintFunc SetPrintErrorHandler (class GLib.PrintFunc 'handler')  cil managed 
    {
        // Method begins at RVA 0x5504
	// Code size 28 (0x1c)
	.maxstack 8
	IL_0000:  call void class GLib.Log::EnsureHash()
	IL_0005:  ldsfld class [mscorlib]System.Collections.Hashtable GLib.Log::handlers
	IL_000a:  ldstr "PrintErrorHandler"
	IL_000f:  ldarg.0 
	IL_0010:  callvirt instance void class [mscorlib]System.Collections.Hashtable::set_Item(object, object)
	IL_0015:  ldarg.0 
	IL_0016:  call class GLib.PrintFunc class GLib.Log::g_set_printerr_handler(class GLib.PrintFunc)
	IL_001b:  ret 
    } // end of method Log::SetPrintErrorHandler

    // method line 410
    .method private static hidebysig pinvokeimpl ("libglib-2.0-0.dll" as "g_log_default_handler" cdecl )
           default void g_log_default_handler (native int log_domain, valuetype GLib.LogLevelFlags log_level, native int message, native int unused_data)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method Log::g_log_default_handler

    // method line 411
    .method public static hidebysig 
           default void DefaultHandler (string logDomain, valuetype GLib.LogLevelFlags logLevel, string message)  cil managed 
    {
        // Method begins at RVA 0x5524
	// Code size 40 (0x28)
	.maxstack 4
	.locals init (
		native int	V_0,
		native int	V_1)
	IL_0000:  ldarg.0 
	IL_0001:  call native int class GLib.Marshaller::StringToPtrGStrdup(string)
	IL_0006:  stloc.0 
	IL_0007:  ldarg.2 
	IL_0008:  call native int class GLib.Marshaller::StringToPtrGStrdup(string)
	IL_000d:  stloc.1 
	IL_000e:  ldloc.0 
	IL_000f:  ldarg.1 
	IL_0010:  ldloc.1 
	IL_0011:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_0016:  call void class GLib.Log::g_log_default_handler(native int, valuetype GLib.LogLevelFlags, native int, native int)
	IL_001b:  ldloc.0 
	IL_001c:  call void class GLib.Marshaller::Free(native int)
	IL_0021:  ldloc.1 
	IL_0022:  call void class GLib.Marshaller::Free(native int)
	IL_0027:  ret 
    } // end of method Log::DefaultHandler

    // method line 412
    .method private static hidebysig pinvokeimpl ("libglib-2.0-0.dll" as "g_log_set_always_fatal" cdecl )
           default valuetype GLib.LogLevelFlags g_log_set_always_fatal (valuetype GLib.LogLevelFlags fatal_mask)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method Log::g_log_set_always_fatal

    // method line 413
    .method public static hidebysig 
           default valuetype GLib.LogLevelFlags SetAlwaysFatal (valuetype GLib.LogLevelFlags fatalMask)  cil managed 
    {
        // Method begins at RVA 0x5558
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call valuetype GLib.LogLevelFlags class GLib.Log::g_log_set_always_fatal(valuetype GLib.LogLevelFlags)
	IL_0006:  ret 
    } // end of method Log::SetAlwaysFatal

    // method line 414
    .method private static hidebysig pinvokeimpl ("libglib-2.0-0.dll" as "g_log_set_fatal_mask" cdecl )
           default valuetype GLib.LogLevelFlags g_log_set_fatal_mask (native int log_domain, valuetype GLib.LogLevelFlags fatal_mask)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method Log::g_log_set_fatal_mask

    // method line 415
    .method public static hidebysig 
           default valuetype GLib.LogLevelFlags SetAlwaysFatal (string logDomain, valuetype GLib.LogLevelFlags fatalMask)  cil managed 
    {
        // Method begins at RVA 0x5560
	// Code size 23 (0x17)
	.maxstack 2
	.locals init (
		native int	V_0,
		valuetype GLib.LogLevelFlags	V_1)
	IL_0000:  ldarg.0 
	IL_0001:  call native int class GLib.Marshaller::StringToPtrGStrdup(string)
	IL_0006:  stloc.0 
	IL_0007:  ldloc.0 
	IL_0008:  ldarg.1 
	IL_0009:  call valuetype GLib.LogLevelFlags class GLib.Log::g_log_set_fatal_mask(native int, valuetype GLib.LogLevelFlags)
	IL_000e:  stloc.1 
	IL_000f:  ldloc.0 
	IL_0010:  call void class GLib.Marshaller::Free(native int)
	IL_0015:  ldloc.1 
	IL_0016:  ret 
    } // end of method Log::SetAlwaysFatal

    // method line 416
    .method public static hidebysig 
           default void PrintLogFunction (string domain, valuetype GLib.LogLevelFlags level, string message)  cil managed 
    {
        // Method begins at RVA 0x5584
	// Code size 29 (0x1d)
	.maxstack 8
	IL_0000:  ldstr "Domain: '{0}' Level: {1}"
	IL_0005:  ldarg.0 
	IL_0006:  ldarg.1 
	IL_0007:  box GLib.LogLevelFlags
	IL_000c:  call void class [mscorlib]System.Console::WriteLine(string, object, object)
	IL_0011:  ldstr "Message: {0}"
	IL_0016:  ldarg.2 
	IL_0017:  call void class [mscorlib]System.Console::WriteLine(string, object)
	IL_001c:  ret 
    } // end of method Log::PrintLogFunction

    // method line 417
    .method private static hidebysig specialname rtspecialname 
           default void '.cctor' ()  cil managed 
    {
        // Method begins at RVA 0x55a4
	// Code size 18 (0x12)
	.maxstack 8
	IL_0000:  ldnull 
	IL_0001:  ldftn void class GLib.Log::'<LogFuncTrampoline>m__0'(string, valuetype GLib.LogLevelFlags, string, class GLib.LogFunc)
	IL_0007:  newobj instance void class GLib.LogFunc2::'.ctor'(object, native int)
	IL_000c:  stsfld class GLib.LogFunc2 GLib.Log::LogFuncTrampoline
	IL_0011:  ret 
    } // end of method Log::.cctor

    // method line 418
    .method private static hidebysig 
           default void '<LogFuncTrampoline>m__0' (string domain, valuetype GLib.LogLevelFlags level, string message, class GLib.LogFunc user_data)  cil managed 
    {
        .custom instance void class [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::'.ctor'() =  (01 00 00 00 ) // ....

        // Method begins at RVA 0x55b8
	// Code size 10 (0xa)
	.maxstack 8
	IL_0000:  ldarg.3 
	IL_0001:  ldarg.0 
	IL_0002:  ldarg.1 
	IL_0003:  ldarg.2 
	IL_0004:  callvirt instance void class GLib.LogFunc::Invoke(string, valuetype GLib.LogLevelFlags, string)
	IL_0009:  ret 
    } // end of method Log::<LogFuncTrampoline>m__0

  } // end of class GLib.Log
}

.namespace GLib
{
  .class public auto ansi beforefieldinit MainContext
  	extends [mscorlib]System.Object
  {

    // method line 419
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' ()  cil managed 
    {
        // Method begins at RVA 0x55c4
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::'.ctor'()
	IL_0006:  ret 
    } // end of method MainContext::.ctor

    // method line 420
    .method private static hidebysig pinvokeimpl ("libglib-2.0-0.dll" as "g_main_depth" cdecl )
           default int32 g_main_depth ()  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method MainContext::g_main_depth

    // method line 421
    .method public static hidebysig specialname 
           default int32 get_Depth ()  cil managed 
    {
        // Method begins at RVA 0x55cc
	// Code size 6 (0x6)
	.maxstack 8
	IL_0000:  call int32 class GLib.MainContext::g_main_depth()
	IL_0005:  ret 
    } // end of method MainContext::get_Depth

    // method line 422
    .method private static hidebysig pinvokeimpl ("libglib-2.0-0.dll" as "g_main_context_iteration" cdecl )
           default bool g_main_context_iteration (native int Raw, bool MayBlock)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method MainContext::g_main_context_iteration

    // method line 423
    .method public static hidebysig 
           default bool Iteration ()  cil managed 
    {
        // Method begins at RVA 0x55d4
	// Code size 12 (0xc)
	.maxstack 8
	IL_0000:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_0005:  ldc.i4.0 
	IL_0006:  call bool class GLib.MainContext::g_main_context_iteration(native int, bool)
	IL_000b:  ret 
    } // end of method MainContext::Iteration

    // method line 424
    .method public static hidebysig 
           default bool Iteration (bool MayBlock)  cil managed 
    {
        // Method begins at RVA 0x55e4
	// Code size 12 (0xc)
	.maxstack 8
	IL_0000:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_0005:  ldarg.0 
	IL_0006:  call bool class GLib.MainContext::g_main_context_iteration(native int, bool)
	IL_000b:  ret 
    } // end of method MainContext::Iteration

    // method line 425
    .method private static hidebysig pinvokeimpl ("libglib-2.0-0.dll" as "g_main_context_pending" cdecl )
           default bool g_main_context_pending (native int Raw)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method MainContext::g_main_context_pending

    // method line 426
    .method public static hidebysig 
           default bool Pending ()  cil managed 
    {
        // Method begins at RVA 0x55f4
	// Code size 11 (0xb)
	.maxstack 8
	IL_0000:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_0005:  call bool class GLib.MainContext::g_main_context_pending(native int)
	IL_000a:  ret 
    } // end of method MainContext::Pending

	.property int32 Depth ()
	{
		.get default int32 GLib.MainContext::get_Depth () 
	}
  } // end of class GLib.MainContext
}

.namespace GLib
{
  .class public auto ansi beforefieldinit MainLoop
  	extends [mscorlib]System.Object
  {
    .field  private  native int handle

    // method line 427
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' ()  cil managed 
    {
        // Method begins at RVA 0x5600
	// Code size 24 (0x18)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::'.ctor'()
	IL_0006:  ldarg.0 
	IL_0007:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_000c:  ldc.i4.0 
	IL_000d:  call native int class GLib.MainLoop::g_main_loop_new(native int, bool)
	IL_0012:  stfld native int GLib.MainLoop::handle
	IL_0017:  ret 
    } // end of method MainLoop::.ctor

    // method line 428
    .method private static hidebysig pinvokeimpl ("libglib-2.0-0.dll" as "g_main_loop_new" cdecl )
           default native int g_main_loop_new (native int context, bool isRunning)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method MainLoop::g_main_loop_new

    // method line 429
    .method private static hidebysig pinvokeimpl ("libglib-2.0-0.dll" as "g_main_loop_unref" cdecl )
           default void g_main_loop_unref (native int loop)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method MainLoop::g_main_loop_unref

    // method line 430
    .method family virtual hidebysig 
           instance default void Finalize ()  cil managed 
    {
        // Method begins at RVA 0x561c
	// Code size 35 (0x23)
	.maxstack 2
	.try { // 0
	  IL_0000:  ldarg.0 
	  IL_0001:  ldfld native int GLib.MainLoop::handle
	  IL_0006:  call void class GLib.MainLoop::g_main_loop_unref(native int)
	  IL_000b:  ldarg.0 
	  IL_000c:  ldsfld native int [mscorlib]System.IntPtr::Zero
	  IL_0011:  stfld native int GLib.MainLoop::handle
	  IL_0016:  leave IL_0022

	} // end .try 0
	finally  { // 0
	  IL_001b:  ldarg.0 
	  IL_001c:  call instance void object::Finalize()
	  IL_0021:  endfinally 
	} // end handler 0
	IL_0022:  ret 
    } // end of method MainLoop::Finalize

    // method line 431
    .method private static hidebysig pinvokeimpl ("libglib-2.0-0.dll" as "g_main_loop_is_running" cdecl )
           default bool g_main_loop_is_running (native int loop)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method MainLoop::g_main_loop_is_running

    // method line 432
    .method public hidebysig specialname 
           instance default bool get_IsRunning ()  cil managed 
    {
        // Method begins at RVA 0x565c
	// Code size 12 (0xc)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldfld native int GLib.MainLoop::handle
	IL_0006:  call bool class GLib.MainLoop::g_main_loop_is_running(native int)
	IL_000b:  ret 
    } // end of method MainLoop::get_IsRunning

    // method line 433
    .method private static hidebysig pinvokeimpl ("libglib-2.0-0.dll" as "g_main_loop_run" cdecl )
           default void g_main_loop_run (native int loop)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method MainLoop::g_main_loop_run

    // method line 434
    .method public hidebysig 
           instance default void Run ()  cil managed 
    {
        // Method begins at RVA 0x566c
	// Code size 12 (0xc)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldfld native int GLib.MainLoop::handle
	IL_0006:  call void class GLib.MainLoop::g_main_loop_run(native int)
	IL_000b:  ret 
    } // end of method MainLoop::Run

    // method line 435
    .method private static hidebysig pinvokeimpl ("libglib-2.0-0.dll" as "g_main_loop_quit" cdecl )
           default void g_main_loop_quit (native int loop)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method MainLoop::g_main_loop_quit

    // method line 436
    .method public hidebysig 
           instance default void Quit ()  cil managed 
    {
        // Method begins at RVA 0x567c
	// Code size 12 (0xc)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldfld native int GLib.MainLoop::handle
	IL_0006:  call void class GLib.MainLoop::g_main_loop_quit(native int)
	IL_000b:  ret 
    } // end of method MainLoop::Quit

	.property instance bool IsRunning ()
	{
		.get instance default bool GLib.MainLoop::get_IsRunning () 
	}
  } // end of class GLib.MainLoop
}

.namespace GLib
{
  .class private auto ansi beforefieldinit ManagedValue
  	extends [mscorlib]System.Object
  {
    .field  private  valuetype [mscorlib]System.Runtime.InteropServices.GCHandle gch
    .field  private  object 'instance'
    .field  private  int32 ref_count
    .field  private static  class GLib.ManagedValue/CopyFunc copy
    .field  private static  class GLib.ManagedValue/FreeFunc free
    .field  private static  valuetype GLib.GType boxed_type

    // method line 437
    .method public static hidebysig specialname 
           default valuetype GLib.GType get_GType ()  cil managed 
    {
        // Method begins at RVA 0x568c
	// Code size 103 (0x67)
	.maxstack 3
	.locals init (
		native int	V_0)
	IL_0000:  ldsfld valuetype GLib.GType GLib.ManagedValue::boxed_type
	IL_0005:  ldsfld valuetype GLib.GType GLib.GType::Invalid
	IL_000a:  call bool valuetype GLib.GType::op_Equality(valuetype GLib.GType, valuetype GLib.GType)
	IL_000f:  brfalse IL_0061

	IL_0014:  ldnull 
	IL_0015:  ldftn native int class GLib.ManagedValue::Copy(native int)
	IL_001b:  newobj instance void class GLib.ManagedValue/CopyFunc::'.ctor'(object, native int)
	IL_0020:  stsfld class GLib.ManagedValue/CopyFunc GLib.ManagedValue::copy
	IL_0025:  ldnull 
	IL_0026:  ldftn void class GLib.ManagedValue::Free(native int)
	IL_002c:  newobj instance void class GLib.ManagedValue/FreeFunc::'.ctor'(object, native int)
	IL_0031:  stsfld class GLib.ManagedValue/FreeFunc GLib.ManagedValue::free
	IL_0036:  ldstr "GtkSharpValue"
	IL_003b:  call native int class GLib.Marshaller::StringToPtrGStrdup(string)
	IL_0040:  stloc.0 
	IL_0041:  ldloc.0 
	IL_0042:  ldsfld class GLib.ManagedValue/CopyFunc GLib.ManagedValue::copy
	IL_0047:  ldsfld class GLib.ManagedValue/FreeFunc GLib.ManagedValue::free
	IL_004c:  call native int class GLib.ManagedValue::g_boxed_type_register_static(native int, class GLib.ManagedValue/CopyFunc, class GLib.ManagedValue/FreeFunc)
	IL_0051:  newobj instance void valuetype GLib.GType::'.ctor'(native int)
	IL_0056:  stsfld valuetype GLib.GType GLib.ManagedValue::boxed_type
	IL_005b:  ldloc.0 
	IL_005c:  call void class GLib.Marshaller::Free(native int)
	IL_0061:  ldsfld valuetype GLib.GType GLib.ManagedValue::boxed_type
	IL_0066:  ret 
    } // end of method ManagedValue::get_GType

    // method line 438
    .method private hidebysig specialname rtspecialname 
           instance default void '.ctor' (object 'instance')  cil managed 
    {
        // Method begins at RVA 0x5700
	// Code size 33 (0x21)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldc.i4.1 
	IL_0002:  stfld int32 GLib.ManagedValue::ref_count
	IL_0007:  ldarg.0 
	IL_0008:  call instance void object::'.ctor'()
	IL_000d:  ldarg.0 
	IL_000e:  ldarg.1 
	IL_000f:  stfld object GLib.ManagedValue::'instance'
	IL_0014:  ldarg.0 
	IL_0015:  ldarg.0 
	IL_0016:  call valuetype [mscorlib]System.Runtime.InteropServices.GCHandle valuetype [mscorlib]System.Runtime.InteropServices.GCHandle::Alloc(object)
	IL_001b:  stfld valuetype [mscorlib]System.Runtime.InteropServices.GCHandle GLib.ManagedValue::gch
	IL_0020:  ret 
    } // end of method ManagedValue::.ctor

    // method line 439
    .method private hidebysig specialname 
           instance default native int get_Handle ()  cil managed 
    {
        // Method begins at RVA 0x5724
	// Code size 12 (0xc)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldfld valuetype [mscorlib]System.Runtime.InteropServices.GCHandle GLib.ManagedValue::gch
	IL_0006:  call native int valuetype [mscorlib]System.Runtime.InteropServices.GCHandle::op_Explicit(valuetype [mscorlib]System.Runtime.InteropServices.GCHandle)
	IL_000b:  ret 
    } // end of method ManagedValue::get_Handle

    // method line 440
    .method private hidebysig specialname 
           instance default object get_Instance ()  cil managed 
    {
        // Method begins at RVA 0x5734
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldfld object GLib.ManagedValue::'instance'
	IL_0006:  ret 
    } // end of method ManagedValue::get_Instance

    // method line 441
    .method private hidebysig 
           instance default void Ref ()  cil managed 
    {
        // Method begins at RVA 0x573c
	// Code size 15 (0xf)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  dup 
	IL_0002:  ldfld int32 GLib.ManagedValue::ref_count
	IL_0007:  ldc.i4.1 
	IL_0008:  add 
	IL_0009:  stfld int32 GLib.ManagedValue::ref_count
	IL_000e:  ret 
    } // end of method ManagedValue::Ref

    // method line 442
    .method private hidebysig 
           instance default void Unref ()  cil managed 
    {
        // Method begins at RVA 0x574c
	// Code size 41 (0x29)
	.maxstack 3
	.locals init (
		int32	V_0)
	IL_0000:  ldarg.0 
	IL_0001:  dup 
	IL_0002:  ldfld int32 GLib.ManagedValue::ref_count
	IL_0007:  ldc.i4.1 
	IL_0008:  sub 
	IL_0009:  dup 
	IL_000a:  stloc.0 
	IL_000b:  stfld int32 GLib.ManagedValue::ref_count
	IL_0010:  ldloc.0 
	IL_0011:  brtrue IL_0028

	IL_0016:  ldarg.0 
	IL_0017:  ldnull 
	IL_0018:  stfld object GLib.ManagedValue::'instance'
	IL_001d:  ldarg.0 
	IL_001e:  ldflda valuetype [mscorlib]System.Runtime.InteropServices.GCHandle GLib.ManagedValue::gch
	IL_0023:  call instance void valuetype [mscorlib]System.Runtime.InteropServices.GCHandle::Free()
	IL_0028:  ret 
    } // end of method ManagedValue::Unref

    // method line 443
    .method private static hidebysig pinvokeimpl ("libgobject-2.0-0.dll" as "g_boxed_type_register_static" cdecl )
           default native int g_boxed_type_register_static (native int typename, class GLib.ManagedValue/CopyFunc copy_func, class GLib.ManagedValue/FreeFunc free_func)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method ManagedValue::g_boxed_type_register_static

    // method line 444
    .method private static hidebysig 
           default native int Copy (native int ptr)  cil managed 
    {
        // Method begins at RVA 0x5784
	// Code size 64 (0x40)
	.maxstack 2
	.locals init (
		native int	V_0,
		class GLib.ManagedValue	V_1,
		class [mscorlib]System.Exception	V_2)
	.try { // 0
	  IL_0000:  ldarg.0 
	  IL_0001:  ldsfld native int [mscorlib]System.IntPtr::Zero
	  IL_0006:  call bool native int::op_Equality(native int, native int)
	  IL_000b:  brfalse IL_0017

	  IL_0010:  ldarg.0 
	  IL_0011:  stloc.0 
	  IL_0012:  leave IL_003e

	  IL_0017:  ldarg.0 
	  IL_0018:  call class GLib.ManagedValue class GLib.ManagedValue::FromHandle(native int)
	  IL_001d:  stloc.1 
	  IL_001e:  ldloc.1 
	  IL_001f:  callvirt instance void class GLib.ManagedValue::Ref()
	  IL_0024:  ldarg.0 
	  IL_0025:  stloc.0 
	  IL_0026:  leave IL_003e

	} // end .try 0
	catch class [mscorlib]System.Exception { // 0
	  IL_002b:  stloc.2 
	  IL_002c:  ldloc.2 
	  IL_002d:  ldc.i4.0 
	  IL_002e:  call void class GLib.ExceptionManager::RaiseUnhandledException(class [mscorlib]System.Exception, bool)
	  IL_0033:  leave IL_0038

	} // end handler 0
	IL_0038:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_003d:  ret 
	IL_003e:  ldloc.0 
	IL_003f:  ret 
    } // end of method ManagedValue::Copy

    // method line 445
    .method private static hidebysig 
           default void Free (native int ptr)  cil managed 
    {
        // Method begins at RVA 0x57e0
	// Code size 53 (0x35)
	.maxstack 2
	.locals init (
		class GLib.ManagedValue	V_0,
		class [mscorlib]System.Exception	V_1)
	.try { // 0
	  IL_0000:  ldarg.0 
	  IL_0001:  ldsfld native int [mscorlib]System.IntPtr::Zero
	  IL_0006:  call bool native int::op_Equality(native int, native int)
	  IL_000b:  brfalse IL_0015

	  IL_0010:  leave IL_0034

	  IL_0015:  ldarg.0 
	  IL_0016:  call class GLib.ManagedValue class GLib.ManagedValue::FromHandle(native int)
	  IL_001b:  stloc.0 
	  IL_001c:  ldloc.0 
	  IL_001d:  callvirt instance void class GLib.ManagedValue::Unref()
	  IL_0022:  leave IL_0034

	} // end .try 0
	catch class [mscorlib]System.Exception { // 0
	  IL_0027:  stloc.1 
	  IL_0028:  ldloc.1 
	  IL_0029:  ldc.i4.0 
	  IL_002a:  call void class GLib.ExceptionManager::RaiseUnhandledException(class [mscorlib]System.Exception, bool)
	  IL_002f:  leave IL_0034

	} // end handler 0
	IL_0034:  ret 
    } // end of method ManagedValue::Free

    // method line 446
    .method private static hidebysig 
           default class GLib.ManagedValue FromHandle (native int ptr)  cil managed 
    {
        // Method begins at RVA 0x5834
	// Code size 39 (0x27)
	.maxstack 1
	.locals init (
		valuetype [mscorlib]System.Runtime.InteropServices.GCHandle	V_0,
		class GLib.ManagedValue	V_1)
	IL_0000:  ldarg.0 
	IL_0001:  call valuetype [mscorlib]System.Runtime.InteropServices.GCHandle valuetype [mscorlib]System.Runtime.InteropServices.GCHandle::op_Explicit(native int)
	IL_0006:  stloc.0 
	IL_0007:  ldloca.s 0
	IL_0009:  call instance object valuetype [mscorlib]System.Runtime.InteropServices.GCHandle::get_Target()
	IL_000e:  isinst GLib.ManagedValue
	IL_0013:  stloc.1 
	IL_0014:  ldloc.1 
	IL_0015:  brtrue IL_0025

	IL_001a:  ldstr "Unexpected GCHandle received."
	IL_001f:  newobj instance void class [mscorlib]System.Exception::'.ctor'(string)
	IL_0024:  throw 
	IL_0025:  ldloc.1 
	IL_0026:  ret 
    } // end of method ManagedValue::FromHandle

    // method line 447
    .method public static hidebysig 
           default native int WrapObject (object obj)  cil managed 
    {
        // Method begins at RVA 0x5868
	// Code size 24 (0x18)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  brtrue IL_000c

	IL_0006:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_000b:  ret 
	IL_000c:  ldarg.0 
	IL_000d:  newobj instance void class GLib.ManagedValue::'.ctor'(object)
	IL_0012:  call instance native int class GLib.ManagedValue::get_Handle()
	IL_0017:  ret 
    } // end of method ManagedValue::WrapObject

    // method line 448
    .method public static hidebysig 
           default object ObjectForWrapper (native int ptr)  cil managed 
    {
        // Method begins at RVA 0x5884
	// Code size 44 (0x2c)
	.maxstack 2
	.locals init (
		class GLib.ManagedValue	V_0)
	IL_0000:  ldarg.0 
	IL_0001:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_0006:  call bool native int::op_Equality(native int, native int)
	IL_000b:  brfalse IL_0012

	IL_0010:  ldnull 
	IL_0011:  ret 
	IL_0012:  ldarg.0 
	IL_0013:  call class GLib.ManagedValue class GLib.ManagedValue::FromHandle(native int)
	IL_0018:  stloc.0 
	IL_0019:  ldloc.0 
	IL_001a:  brtrue IL_0025

	IL_001f:  ldnull 
	IL_0020:  br IL_002b

	IL_0025:  ldloc.0 
	IL_0026:  callvirt instance object class GLib.ManagedValue::get_Instance()
	IL_002b:  ret 
    } // end of method ManagedValue::ObjectForWrapper

    // method line 449
    .method public static hidebysig 
           default void ReleaseWrapper (native int ptr)  cil managed 
    {
        // Method begins at RVA 0x58bc
	// Code size 31 (0x1f)
	.maxstack 2
	.locals init (
		class GLib.ManagedValue	V_0)
	IL_0000:  ldarg.0 
	IL_0001:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_0006:  call bool native int::op_Equality(native int, native int)
	IL_000b:  brfalse IL_0011

	IL_0010:  ret 
	IL_0011:  ldarg.0 
	IL_0012:  call class GLib.ManagedValue class GLib.ManagedValue::FromHandle(native int)
	IL_0017:  stloc.0 
	IL_0018:  ldloc.0 
	IL_0019:  callvirt instance void class GLib.ManagedValue::Unref()
	IL_001e:  ret 
    } // end of method ManagedValue::ReleaseWrapper

    // method line 450
    .method private static hidebysig specialname rtspecialname 
           default void '.cctor' ()  cil managed 
    {
        // Method begins at RVA 0x58e8
	// Code size 11 (0xb)
	.maxstack 8
	IL_0000:  ldsfld valuetype GLib.GType GLib.GType::Invalid
	IL_0005:  stsfld valuetype GLib.GType GLib.ManagedValue::boxed_type
	IL_000a:  ret 
    } // end of method ManagedValue::.cctor

	.property instance native int Handle ()
	{
		.get instance default native int GLib.ManagedValue::get_Handle () 
	}
	.property instance object Instance ()
	{
		.get instance default object GLib.ManagedValue::get_Instance () 
	}
	.property valuetype GLib.GType GType ()
	{
		.get default valuetype GLib.GType GLib.ManagedValue::get_GType () 
	}
  .class nested private auto ansi sealed CopyFunc
  	extends [mscorlib]System.MulticastDelegate
  {
    .custom instance void class [mscorlib]System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute::'.ctor'(valuetype [mscorlib]System.Runtime.InteropServices.CallingConvention) =  (01 00 02 00 00 00 00 00 ) // ........


    // method line 451
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (object 'object', native int 'method')  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method CopyFunc::.ctor

    // method line 452
    .method public virtual hidebysig newslot 
           instance default native int Invoke (native int gch)  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method CopyFunc::Invoke

    // method line 453
    .method public virtual hidebysig newslot 
           instance default class [mscorlib]System.IAsyncResult BeginInvoke (native int gch, class [mscorlib]System.AsyncCallback callback, object 'object')  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method CopyFunc::BeginInvoke

    // method line 454
    .method public virtual hidebysig newslot 
           instance default native int EndInvoke (class [mscorlib]System.IAsyncResult result)  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method CopyFunc::EndInvoke

  } // end of class CopyFunc

  .class nested private auto ansi sealed FreeFunc
  	extends [mscorlib]System.MulticastDelegate
  {
    .custom instance void class [mscorlib]System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute::'.ctor'(valuetype [mscorlib]System.Runtime.InteropServices.CallingConvention) =  (01 00 02 00 00 00 00 00 ) // ........


    // method line 455
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (object 'object', native int 'method')  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method FreeFunc::.ctor

    // method line 456
    .method public virtual hidebysig newslot 
           instance default void Invoke (native int gch)  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method FreeFunc::Invoke

    // method line 457
    .method public virtual hidebysig newslot 
           instance default class [mscorlib]System.IAsyncResult BeginInvoke (native int gch, class [mscorlib]System.AsyncCallback callback, object 'object')  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method FreeFunc::BeginInvoke

    // method line 458
    .method public virtual hidebysig newslot 
           instance default void EndInvoke (class [mscorlib]System.IAsyncResult result)  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method FreeFunc::EndInvoke

  } // end of class FreeFunc

  } // end of class GLib.ManagedValue
}

.namespace GLib
{
  .class public auto ansi beforefieldinit Markup
  	extends [mscorlib]System.Object
  {

    // method line 459
    .method private hidebysig specialname rtspecialname 
           instance default void '.ctor' ()  cil managed 
    {
        // Method begins at RVA 0x58f4
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::'.ctor'()
	IL_0006:  ret 
    } // end of method Markup::.ctor

    // method line 460
    .method private static hidebysig pinvokeimpl ("libglib-2.0-0.dll" as "g_markup_escape_text" cdecl )
           default native int g_markup_escape_text (native int text, int64 len)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method Markup::g_markup_escape_text

    // method line 461
    .method public static hidebysig 
           default string EscapeText (string s)  cil managed 
    {
        // Method begins at RVA 0x58fc
	// Code size 41 (0x29)
	.maxstack 2
	.locals init (
		native int	V_0,
		string	V_1)
	IL_0000:  ldarg.0 
	IL_0001:  brtrue IL_000c

	IL_0006:  ldsfld string [mscorlib]System.String::Empty
	IL_000b:  ret 
	IL_000c:  ldarg.0 
	IL_000d:  call native int class GLib.Marshaller::StringToPtrGStrdup(string)
	IL_0012:  stloc.0 
	IL_0013:  ldloc.0 
	IL_0014:  ldc.i4.m1 
	IL_0015:  conv.i8 
	IL_0016:  call native int class GLib.Markup::g_markup_escape_text(native int, int64)
	IL_001b:  call string class GLib.Marshaller::PtrToStringGFree(native int)
	IL_0020:  stloc.1 
	IL_0021:  ldloc.0 
	IL_0022:  call void class GLib.Marshaller::Free(native int)
	IL_0027:  ldloc.1 
	IL_0028:  ret 
    } // end of method Markup::EscapeText

  } // end of class GLib.Markup
}

.namespace GLib
{
  .class public auto ansi beforefieldinit Marshaller
  	extends [mscorlib]System.Object
  {
    .field  private static  valuetype [mscorlib]System.DateTime local_epoch
    .field  private static  int32 utc_offset

    // method line 462
    .method public static hidebysig 
           default native int StringToPtrGStrdup (string str)  cil managed 
    {
        // Method begins at RVA 0x5934
	// Code size 83 (0x53)
	.maxstack 5
	.locals init (
		char*	V_0,
		string	V_1,
		native int	V_2,
		native int	V_3)
	IL_0000:  ldarg.0 
	IL_0001:  brtrue IL_000c

	IL_0006:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_000b:  ret 
	IL_000c:  ldarg.0 
	IL_000d:  stloc.1 
	IL_000e:  ldloc.1 
	IL_000f:  conv.u 
	IL_0010:  stloc.0 
	IL_0011:  ldloc.0 
	IL_0012:  brfalse.s IL_001c

	IL_0014:  ldloc.0 
	IL_0015:  call int32 class [mscorlib]System.Runtime.CompilerServices.RuntimeHelpers::get_OffsetToStringData()
	IL_001a:  add 
	IL_001b:  stloc.0 
	IL_001c:  ldloc.0 
	IL_001d:  ldarg.0 
	IL_001e:  callvirt instance int32 string::get_Length()
	IL_0023:  newobj instance void native int::'.ctor'(int32)
	IL_0028:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_002d:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_0032:  ldloca.s 2
	IL_0034:  call native int class GLib.Marshaller::g_utf16_to_utf8(char*, native int, native int, native int, [out] native int&)
	IL_0039:  stloc.3 
	IL_003a:  ldloc.2 
	IL_003b:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_0040:  call bool native int::op_Inequality(native int, native int)
	IL_0045:  brfalse IL_0051

	IL_004a:  ldloc.2 
	IL_004b:  newobj instance void class GLib.GException::'.ctor'(native int)
	IL_0050:  throw 
	IL_0051:  ldloc.3 
	IL_0052:  ret 
    } // end of method Marshaller::StringToPtrGStrdup

    // method line 463
    .method public static hidebysig 
           default string Utf8PtrToString (native int ptr)  cil managed 
    {
        // Method begins at RVA 0x5994
	// Code size 96 (0x60)
	.maxstack 5
	.locals init (
		native int	V_0,
		native int	V_1,
		char*	V_2,
		string	V_3)
	IL_0000:  ldarg.0 
	IL_0001:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_0006:  call bool native int::op_Equality(native int, native int)
	IL_000b:  brfalse IL_0012

	IL_0010:  ldnull 
	IL_0011:  ret 
	IL_0012:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_0017:  stloc.0 
	IL_0018:  ldarg.0 
	IL_0019:  ldc.i4.m1 
	IL_001a:  newobj instance void native int::'.ctor'(int32)
	IL_001f:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_0024:  ldloca.s 0
	IL_0026:  ldloca.s 1
	IL_0028:  call char* class GLib.Marshaller::g_utf8_to_utf16(native int, native int, native int, [out] native int&, [out] native int&)
	IL_002d:  stloc.2 
	IL_002e:  ldloc.1 
	IL_002f:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_0034:  call bool native int::op_Inequality(native int, native int)
	IL_0039:  brfalse IL_0045

	IL_003e:  ldloc.1 
	IL_003f:  newobj instance void class GLib.GException::'.ctor'(native int)
	IL_0044:  throw 
	IL_0045:  ldloc.2 
	IL_0046:  ldc.i4.0 
	IL_0047:  ldloc.0 
	IL_0048:  call int32 native int::op_Explicit(native int)
	IL_004d:  newobj instance void string::'.ctor'(char*, int32, int32)
	IL_0052:  stloc.3 
	IL_0053:  ldloc.2 
	IL_0054:  call native int native int::op_Explicit(void*)
	IL_0059:  call void class GLib.Marshaller::g_free(native int)
	IL_005e:  ldloc.3 
	IL_005f:  ret 
    } // end of method Marshaller::Utf8PtrToString

    // method line 464
    .method public static hidebysig 
           default void Free (native int ptr)  cil managed 
    {
        // Method begins at RVA 0x5a00
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call void class GLib.Marshaller::g_free(native int)
	IL_0006:  ret 
    } // end of method Marshaller::Free

    // method line 465
    .method public static hidebysig 
           default string PtrToStringGFree (native int ptr)  cil managed 
    {
        // Method begins at RVA 0x5a08
	// Code size 15 (0xf)
	.maxstack 1
	.locals init (
		string	V_0)
	IL_0000:  ldarg.0 
	IL_0001:  call string class GLib.Marshaller::Utf8PtrToString(native int)
	IL_0006:  stloc.0 
	IL_0007:  ldarg.0 
	IL_0008:  call void class GLib.Marshaller::g_free(native int)
	IL_000d:  ldloc.0 
	IL_000e:  ret 
    } // end of method Marshaller::PtrToStringGFree

    // method line 466
    .method public static hidebysig 
           default native int StringToFilenamePtr (string str)  cil managed 
    {
        // Method begins at RVA 0x5a24
	// Code size 99 (0x63)
	.maxstack 5
	.locals init (
		native int	V_0,
		native int	V_1,
		native int	V_2,
		native int	V_3)
	IL_0000:  ldarg.0 
	IL_0001:  brtrue IL_000c

	IL_0006:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_000b:  ret 
	IL_000c:  ldarg.0 
	IL_000d:  call native int class GLib.Marshaller::StringToPtrGStrdup(string)
	IL_0012:  stloc.2 
	IL_0013:  call bool class GLib.Global::get_IsWindowsPlatform()
	IL_0018:  brfalse IL_0033

	IL_001d:  ldloc.2 
	IL_001e:  ldc.i4.m1 
	IL_001f:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_0024:  ldloca.s 0
	IL_0026:  ldloca.s 1
	IL_0028:  call native int class GLib.Marshaller::g_filename_from_utf8_utf8(native int, int32, native int, [out] native int&, [out] native int&)
	IL_002d:  stloc.3 
	IL_002e:  br IL_0044

	IL_0033:  ldloc.2 
	IL_0034:  ldc.i4.m1 
	IL_0035:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_003a:  ldloca.s 0
	IL_003c:  ldloca.s 1
	IL_003e:  call native int class GLib.Marshaller::g_filename_from_utf8(native int, int32, native int, [out] native int&, [out] native int&)
	IL_0043:  stloc.3 
	IL_0044:  ldloc.2 
	IL_0045:  call void class GLib.Marshaller::g_free(native int)
	IL_004a:  ldloc.1 
	IL_004b:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_0050:  call bool native int::op_Inequality(native int, native int)
	IL_0055:  brfalse IL_0061

	IL_005a:  ldloc.1 
	IL_005b:  newobj instance void class GLib.GException::'.ctor'(native int)
	IL_0060:  throw 
	IL_0061:  ldloc.3 
	IL_0062:  ret 
    } // end of method Marshaller::StringToFilenamePtr

    // method line 467
    .method public static hidebysig 
           default native int StructureToPtrAlloc (object o)  cil managed 
    {
        // Method begins at RVA 0x5a94
	// Code size 22 (0x16)
	.maxstack 3
	.locals init (
		native int	V_0)
	IL_0000:  ldarg.0 
	IL_0001:  call int32 class [mscorlib]System.Runtime.InteropServices.Marshal::SizeOf(object)
	IL_0006:  call native int class [mscorlib]System.Runtime.InteropServices.Marshal::AllocHGlobal(int32)
	IL_000b:  stloc.0 
	IL_000c:  ldarg.0 
	IL_000d:  ldloc.0 
	IL_000e:  ldc.i4.0 
	IL_000f:  call void class [mscorlib]System.Runtime.InteropServices.Marshal::StructureToPtr(object, native int, bool)
	IL_0014:  ldloc.0 
	IL_0015:  ret 
    } // end of method Marshaller::StructureToPtrAlloc

    // method line 468
    .method public static hidebysig 
           default string FilenamePtrToString (native int ptr)  cil managed 
    {
        // Method begins at RVA 0x5ab8
	// Code size 97 (0x61)
	.maxstack 5
	.locals init (
		native int	V_0,
		native int	V_1,
		native int	V_2)
	IL_0000:  ldarg.0 
	IL_0001:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_0006:  call bool native int::op_Equality(native int, native int)
	IL_000b:  brfalse IL_0012

	IL_0010:  ldnull 
	IL_0011:  ret 
	IL_0012:  call bool class GLib.Global::get_IsWindowsPlatform()
	IL_0017:  brfalse IL_0032

	IL_001c:  ldarg.0 
	IL_001d:  ldc.i4.m1 
	IL_001e:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_0023:  ldloca.s 0
	IL_0025:  ldloca.s 1
	IL_0027:  call native int class GLib.Marshaller::g_filename_to_utf8_utf8(native int, int32, native int, [out] native int&, [out] native int&)
	IL_002c:  stloc.2 
	IL_002d:  br IL_0043

	IL_0032:  ldarg.0 
	IL_0033:  ldc.i4.m1 
	IL_0034:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_0039:  ldloca.s 0
	IL_003b:  ldloca.s 1
	IL_003d:  call native int class GLib.Marshaller::g_filename_to_utf8(native int, int32, native int, [out] native int&, [out] native int&)
	IL_0042:  stloc.2 
	IL_0043:  ldloc.1 
	IL_0044:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_0049:  call bool native int::op_Inequality(native int, native int)
	IL_004e:  brfalse IL_005a

	IL_0053:  ldloc.1 
	IL_0054:  newobj instance void class GLib.GException::'.ctor'(native int)
	IL_0059:  throw 
	IL_005a:  ldloc.2 
	IL_005b:  call string class GLib.Marshaller::Utf8PtrToString(native int)
	IL_0060:  ret 
    } // end of method Marshaller::FilenamePtrToString

    // method line 469
    .method private hidebysig specialname rtspecialname 
           instance default void '.ctor' ()  cil managed 
    {
        // Method begins at RVA 0x5b28
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::'.ctor'()
	IL_0006:  ret 
    } // end of method Marshaller::.ctor

    // method line 470
    .method private static hidebysig pinvokeimpl ("libglib-2.0-0.dll" as "g_free" cdecl )
           default void g_free (native int mem)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method Marshaller::g_free

    // method line 471
    .method public static hidebysig 
           default void Free (native int[] ptrs)  cil managed 
    {
        // Method begins at RVA 0x5b30
	// Code size 36 (0x24)
	.maxstack 2
	.locals init (
		int32	V_0)
	IL_0000:  ldarg.0 
	IL_0001:  brtrue IL_0007

	IL_0006:  ret 
	IL_0007:  ldc.i4.0 
	IL_0008:  stloc.0 
	IL_0009:  br IL_001a

	IL_000e:  ldarg.0 
	IL_000f:  ldloc.0 
	IL_0010:  ldelem.i 
	IL_0011:  call void class GLib.Marshaller::g_free(native int)
	IL_0016:  ldloc.0 
	IL_0017:  ldc.i4.1 
	IL_0018:  add 
	IL_0019:  stloc.0 
	IL_001a:  ldloc.0 
	IL_001b:  ldarg.0 
	IL_001c:  ldlen 
	IL_001d:  conv.i4 
	IL_001e:  blt IL_000e

	IL_0023:  ret 
    } // end of method Marshaller::Free

    // method line 472
    .method private static hidebysig pinvokeimpl ("libglib-2.0-0.dll" as "g_filename_to_utf8" cdecl )
           default native int g_filename_to_utf8 (native int mem, int32 len, native int read, [out] native int& written, [out] native int& 'error')  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method Marshaller::g_filename_to_utf8

    // method line 473
    .method private static hidebysig pinvokeimpl ("libglib-2.0-0.dll" as "g_filename_to_utf8_utf8" cdecl )
           default native int g_filename_to_utf8_utf8 (native int mem, int32 len, native int read, [out] native int& written, [out] native int& 'error')  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method Marshaller::g_filename_to_utf8_utf8

    // method line 474
    .method public static hidebysig 
           default string FilenamePtrToStringGFree (native int ptr)  cil managed 
    {
        // Method begins at RVA 0x5b60
	// Code size 15 (0xf)
	.maxstack 1
	.locals init (
		string	V_0)
	IL_0000:  ldarg.0 
	IL_0001:  call string class GLib.Marshaller::FilenamePtrToString(native int)
	IL_0006:  stloc.0 
	IL_0007:  ldarg.0 
	IL_0008:  call void class GLib.Marshaller::g_free(native int)
	IL_000d:  ldloc.0 
	IL_000e:  ret 
    } // end of method Marshaller::FilenamePtrToStringGFree

    // method line 475
    .method private static hidebysig pinvokeimpl ("libglib-2.0-0.dll" as "g_utf8_to_utf16" cdecl )
           default char* g_utf8_to_utf16 (native int native_str, native int len, native int items_read, native int& items_written, [out] native int& 'error')  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method Marshaller::g_utf8_to_utf16

    // method line 476
    .method private static hidebysig pinvokeimpl ("libglib-2.0-0.dll" as "g_utf16_to_utf8" cdecl )
           default native int g_utf16_to_utf8 (char* native_str, native int len, native int items_read, native int items_written, [out] native int& 'error')  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method Marshaller::g_utf16_to_utf8

    // method line 477
    .method private static hidebysig pinvokeimpl ("glibsharpglue-2" as "glibsharp_strlen" cdecl )
           default native unsigned int glibsharp_strlen (native int mem)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method Marshaller::glibsharp_strlen

    // method line 478
    .method public static hidebysig 
           default string[] Utf8PtrToString (native int[] ptrs)  cil managed 
    {
        // Method begins at RVA 0x5b7c
	// Code size 44 (0x2c)
	.maxstack 4
	.locals init (
		string[]	V_0,
		int32	V_1)
	IL_0000:  ldarg.0 
	IL_0001:  ldlen 
	IL_0002:  conv.i4 
	IL_0003:  ldc.i4.1 
	IL_0004:  sub 
	IL_0005:  newarr string
	IL_000a:  stloc.0 
	IL_000b:  ldc.i4.0 
	IL_000c:  stloc.1 
	IL_000d:  br IL_0021

	IL_0012:  ldloc.0 
	IL_0013:  ldloc.1 
	IL_0014:  ldarg.0 
	IL_0015:  ldloc.1 
	IL_0016:  ldelem.i 
	IL_0017:  call string class GLib.Marshaller::Utf8PtrToString(native int)
	IL_001c:  stelem.ref 
	IL_001d:  ldloc.1 
	IL_001e:  ldc.i4.1 
	IL_001f:  add 
	IL_0020:  stloc.1 
	IL_0021:  ldloc.1 
	IL_0022:  ldloc.0 
	IL_0023:  ldlen 
	IL_0024:  conv.i4 
	IL_0025:  blt IL_0012

	IL_002a:  ldloc.0 
	IL_002b:  ret 
    } // end of method Marshaller::Utf8PtrToString

    // method line 479
    .method public static hidebysig 
           default string[] PtrToStringGFree (native int[] ptrs)  cil managed 
    {
        // Method begins at RVA 0x5bb4
	// Code size 52 (0x34)
	.maxstack 4
	.locals init (
		string[]	V_0,
		int32	V_1)
	IL_0000:  ldarg.0 
	IL_0001:  ldlen 
	IL_0002:  conv.i4 
	IL_0003:  ldc.i4.1 
	IL_0004:  sub 
	IL_0005:  newarr string
	IL_000a:  stloc.0 
	IL_000b:  ldc.i4.0 
	IL_000c:  stloc.1 
	IL_000d:  br IL_0029

	IL_0012:  ldloc.0 
	IL_0013:  ldloc.1 
	IL_0014:  ldarg.0 
	IL_0015:  ldloc.1 
	IL_0016:  ldelem.i 
	IL_0017:  call string class GLib.Marshaller::Utf8PtrToString(native int)
	IL_001c:  stelem.ref 
	IL_001d:  ldarg.0 
	IL_001e:  ldloc.1 
	IL_001f:  ldelem.i 
	IL_0020:  call void class GLib.Marshaller::g_free(native int)
	IL_0025:  ldloc.1 
	IL_0026:  ldc.i4.1 
	IL_0027:  add 
	IL_0028:  stloc.1 
	IL_0029:  ldloc.1 
	IL_002a:  ldloc.0 
	IL_002b:  ldlen 
	IL_002c:  conv.i4 
	IL_002d:  blt IL_0012

	IL_0032:  ldloc.0 
	IL_0033:  ret 
    } // end of method Marshaller::PtrToStringGFree

    // method line 480
    .method private static hidebysig pinvokeimpl ("libglib-2.0-0.dll" as "g_filename_from_utf8" cdecl )
           default native int g_filename_from_utf8 (native int mem, int32 len, native int read, [out] native int& written, [out] native int& 'error')  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method Marshaller::g_filename_from_utf8

    // method line 481
    .method private static hidebysig pinvokeimpl ("libglib-2.0-0.dll" as "g_filename_from_utf8_utf8" cdecl )
           default native int g_filename_from_utf8_utf8 (native int mem, int32 len, native int read, [out] native int& written, [out] native int& 'error')  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method Marshaller::g_filename_from_utf8_utf8

    // method line 482
    .method public static hidebysig 
           default string StringFormat (string format, object[] args)  cil managed 
    {
	.param [2]
	.custom instance void class [mscorlib]System.ParamArrayAttribute::'.ctor'() =  (01 00 00 00 ) // ....

        // Method begins at RVA 0x5bf4
	// Code size 41 (0x29)
	.maxstack 3
	.locals init (
		string	V_0)
	IL_0000:  ldarg.0 
	IL_0001:  ldarg.1 
	IL_0002:  call string string::Format(string, object[])
	IL_0007:  stloc.0 
	IL_0008:  ldloc.0 
	IL_0009:  ldc.i4.s 0x25
	IL_000b:  callvirt instance int32 string::IndexOf(char)
	IL_0010:  ldc.i4.m1 
	IL_0011:  bne.un IL_0018

	IL_0016:  ldloc.0 
	IL_0017:  ret 
	IL_0018:  ldloc.0 
	IL_0019:  ldstr "%"
	IL_001e:  ldstr "%%"
	IL_0023:  callvirt instance string string::Replace(string, string)
	IL_0028:  ret 
    } // end of method Marshaller::StringFormat

    // method line 483
    .method public static hidebysig 
           default native int[] StringArrayToNullTermPointer (string[] strs)  cil managed 
    {
        // Method begins at RVA 0x5c2c
	// Code size 72 (0x48)
	.maxstack 3
	.locals init (
		native int[]	V_0,
		int32	V_1)
	IL_0000:  ldarg.0 
	IL_0001:  brtrue IL_0008

	IL_0006:  ldnull 
	IL_0007:  ret 
	IL_0008:  ldarg.0 
	IL_0009:  ldlen 
	IL_000a:  conv.i4 
	IL_000b:  ldc.i4.1 
	IL_000c:  add 
	IL_000d:  newarr native int
	IL_0012:  stloc.0 
	IL_0013:  ldc.i4.0 
	IL_0014:  stloc.1 
	IL_0015:  br IL_002e

	IL_001a:  ldloc.0 
	IL_001b:  ldloc.1 
	IL_001c:  ldelema native int
	IL_0021:  ldarg.0 
	IL_0022:  ldloc.1 
	IL_0023:  ldelem.ref 
	IL_0024:  call native int class GLib.Marshaller::StringToPtrGStrdup(string)
	IL_0029:  stind.i 
	IL_002a:  ldloc.1 
	IL_002b:  ldc.i4.1 
	IL_002c:  add 
	IL_002d:  stloc.1 
	IL_002e:  ldloc.1 
	IL_002f:  ldarg.0 
	IL_0030:  ldlen 
	IL_0031:  conv.i4 
	IL_0032:  blt IL_001a

	IL_0037:  ldloc.0 
	IL_0038:  ldarg.0 
	IL_0039:  ldlen 
	IL_003a:  conv.i4 
	IL_003b:  ldelema native int
	IL_0040:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_0045:  stind.i 
	IL_0046:  ldloc.0 
	IL_0047:  ret 
    } // end of method Marshaller::StringArrayToNullTermPointer

    // method line 484
    .method private static hidebysig pinvokeimpl ("libglib-2.0-0.dll" as "g_strfreev" cdecl )
           default void g_strfreev (native int mem)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method Marshaller::g_strfreev

    // method line 485
    .method public static hidebysig 
           default void StrFreeV (native int null_term_array)  cil managed 
    {
        // Method begins at RVA 0x5c80
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call void class GLib.Marshaller::g_strfreev(native int)
	IL_0006:  ret 
    } // end of method Marshaller::StrFreeV

    // method line 486
    .method public static hidebysig 
           default string[] NullTermPtrToStringArray (native int null_term_array, bool owned)  cil managed 
    {
        // Method begins at RVA 0x5c88
	// Code size 119 (0x77)
	.maxstack 4
	.locals init (
		int32	V_0,
		class [mscorlib]System.Collections.Generic.List`1<string>	V_1,
		native int	V_2)
	IL_0000:  ldarg.0 
	IL_0001:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_0006:  call bool native int::op_Equality(native int, native int)
	IL_000b:  brfalse IL_0017

	IL_0010:  ldc.i4.0 
	IL_0011:  newarr string
	IL_0016:  ret 
	IL_0017:  ldc.i4.0 
	IL_0018:  stloc.0 
	IL_0019:  newobj instance void class [mscorlib]System.Collections.Generic.List`1<string>::'.ctor'()
	IL_001e:  stloc.1 
	IL_001f:  ldarg.0 
	IL_0020:  ldloc.0 
	IL_0021:  dup 
	IL_0022:  ldc.i4.1 
	IL_0023:  add 
	IL_0024:  stloc.0 
	IL_0025:  call int32 native int::get_Size()
	IL_002a:  mul 
	IL_002b:  call native int class [mscorlib]System.Runtime.InteropServices.Marshal::ReadIntPtr(native int, int32)
	IL_0030:  stloc.2 
	IL_0031:  br IL_0054

	IL_0036:  ldloc.1 
	IL_0037:  ldloc.2 
	IL_0038:  call string class GLib.Marshaller::Utf8PtrToString(native int)
	IL_003d:  callvirt instance void class [mscorlib]System.Collections.Generic.List`1<string>::Add(!0)
	IL_0042:  ldarg.0 
	IL_0043:  ldloc.0 
	IL_0044:  dup 
	IL_0045:  ldc.i4.1 
	IL_0046:  add 
	IL_0047:  stloc.0 
	IL_0048:  call int32 native int::get_Size()
	IL_004d:  mul 
	IL_004e:  call native int class [mscorlib]System.Runtime.InteropServices.Marshal::ReadIntPtr(native int, int32)
	IL_0053:  stloc.2 
	IL_0054:  ldloc.2 
	IL_0055:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_005a:  call bool native int::op_Inequality(native int, native int)
	IL_005f:  brtrue IL_0036

	IL_0064:  ldarg.1 
	IL_0065:  brfalse IL_0070

	IL_006a:  ldarg.0 
	IL_006b:  call void class GLib.Marshaller::g_strfreev(native int)
	IL_0070:  ldloc.1 
	IL_0071:  callvirt instance !0[] class [mscorlib]System.Collections.Generic.List`1<string>::ToArray()
	IL_0076:  ret 
    } // end of method Marshaller::NullTermPtrToStringArray

    // method line 487
    .method public static hidebysig 
           default string[] PtrToStringArrayGFree (native int string_array)  cil managed 
    {
        // Method begins at RVA 0x5d0c
	// Code size 118 (0x76)
	.maxstack 3
	.locals init (
		int32	V_0,
		string[]	V_1,
		int32	V_2,
		native int	V_3)
	IL_0000:  ldarg.0 
	IL_0001:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_0006:  call bool native int::op_Equality(native int, native int)
	IL_000b:  brfalse IL_0017

	IL_0010:  ldc.i4.0 
	IL_0011:  newarr string
	IL_0016:  ret 
	IL_0017:  ldc.i4.0 
	IL_0018:  stloc.0 
	IL_0019:  br IL_0022

	IL_001e:  ldloc.0 
	IL_001f:  ldc.i4.1 
	IL_0020:  add 
	IL_0021:  stloc.0 
	IL_0022:  ldarg.0 
	IL_0023:  ldloc.0 
	IL_0024:  call int32 native int::get_Size()
	IL_0029:  mul 
	IL_002a:  call native int class [mscorlib]System.Runtime.InteropServices.Marshal::ReadIntPtr(native int, int32)
	IL_002f:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_0034:  call bool native int::op_Inequality(native int, native int)
	IL_0039:  brtrue IL_001e

	IL_003e:  ldloc.0 
	IL_003f:  newarr string
	IL_0044:  stloc.1 
	IL_0045:  ldc.i4.0 
	IL_0046:  stloc.2 
	IL_0047:  br IL_0067

	IL_004c:  ldarg.0 
	IL_004d:  ldloc.2 
	IL_004e:  call int32 native int::get_Size()
	IL_0053:  mul 
	IL_0054:  call native int class [mscorlib]System.Runtime.InteropServices.Marshal::ReadIntPtr(native int, int32)
	IL_0059:  stloc.3 
	IL_005a:  ldloc.1 
	IL_005b:  ldloc.2 
	IL_005c:  ldloc.3 
	IL_005d:  call string class GLib.Marshaller::PtrToStringGFree(native int)
	IL_0062:  stelem.ref 
	IL_0063:  ldloc.2 
	IL_0064:  ldc.i4.1 
	IL_0065:  add 
	IL_0066:  stloc.2 
	IL_0067:  ldloc.2 
	IL_0068:  ldloc.0 
	IL_0069:  blt IL_004c

	IL_006e:  ldarg.0 
	IL_006f:  call void class GLib.Marshaller::Free(native int)
	IL_0074:  ldloc.1 
	IL_0075:  ret 
    } // end of method Marshaller::PtrToStringArrayGFree

    // method line 488
    .method private static hidebysig pinvokeimpl ("libglib-2.0-0.dll" as "g_malloc" cdecl )
           default native int g_malloc (native unsigned int size)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method Marshaller::g_malloc

    // method line 489
    .method public static hidebysig 
           default native int Malloc (unsigned int64 size)  cil managed 
    {
        // Method begins at RVA 0x5d90
	// Code size 12 (0xc)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  newobj instance void native unsigned int::'.ctor'(unsigned int64)
	IL_0006:  call native int class GLib.Marshaller::g_malloc(native unsigned int)
	IL_000b:  ret 
    } // end of method Marshaller::Malloc

    // method line 490
    .method private static hidebysig 
           default bool check_sixtyfour ()  cil managed 
    {
        // Method begins at RVA 0x5da0
	// Code size 39 (0x27)
	.maxstack 2
	.locals init (
		int32	V_0,
		int32	V_1,
		int32	V_2)
	IL_0000:  ldc.i4.4 
	IL_0001:  stloc.0 
	IL_0002:  ldc.i4.8 
	IL_0003:  stloc.1 
	IL_0004:  call int32 native int::get_Size()
	IL_0009:  stloc.2 
	IL_000a:  ldloc.2 
	IL_000b:  ldloc.0 
	IL_000c:  bne.un IL_0013

	IL_0011:  ldc.i4.0 
	IL_0012:  ret 
	IL_0013:  ldloc.2 
	IL_0014:  ldloc.1 
	IL_0015:  bne.un IL_001c

	IL_001a:  ldc.i4.1 
	IL_001b:  ret 
	IL_001c:  ldstr "Pointers are neither int- nor long-sized???"
	IL_0021:  newobj instance void class [mscorlib]System.Exception::'.ctor'(string)
	IL_0026:  throw 
    } // end of method Marshaller::check_sixtyfour

    // method line 491
    .method private static hidebysig 
           default native int make_buf_32 (string[] args)  cil managed 
    {
        // Method begins at RVA 0x5dd4
	// Code size 76 (0x4c)
	.maxstack 4
	.locals init (
		int32[]	V_0,
		int32	V_1,
		native int	V_2)
	IL_0000:  ldarg.0 
	IL_0001:  ldlen 
	IL_0002:  conv.i4 
	IL_0003:  newarr int32
	IL_0008:  stloc.0 
	IL_0009:  ldc.i4.0 
	IL_000a:  stloc.1 
	IL_000b:  br IL_0024

	IL_0010:  ldloc.0 
	IL_0011:  ldloc.1 
	IL_0012:  ldarg.0 
	IL_0013:  ldloc.1 
	IL_0014:  ldelem.ref 
	IL_0015:  call native int class [mscorlib]System.Runtime.InteropServices.Marshal::StringToHGlobalAuto(string)
	IL_001a:  call int32 native int::op_Explicit(native int)
	IL_001f:  stelem.i4 
	IL_0020:  ldloc.1 
	IL_0021:  ldc.i4.1 
	IL_0022:  add 
	IL_0023:  stloc.1 
	IL_0024:  ldloc.1 
	IL_0025:  ldarg.0 
	IL_0026:  ldlen 
	IL_0027:  conv.i4 
	IL_0028:  blt IL_0010

	IL_002d:  ldc.i4.4 
	IL_002e:  conv.i8 
	IL_002f:  ldarg.0 
	IL_0030:  ldlen 
	IL_0031:  conv.i4 
	IL_0032:  conv.i8 
	IL_0033:  mul 
	IL_0034:  newobj instance void native unsigned int::'.ctor'(unsigned int64)
	IL_0039:  call native int class GLib.Marshaller::g_malloc(native unsigned int)
	IL_003e:  stloc.2 
	IL_003f:  ldloc.0 
	IL_0040:  ldc.i4.0 
	IL_0041:  ldloc.2 
	IL_0042:  ldloc.0 
	IL_0043:  ldlen 
	IL_0044:  conv.i4 
	IL_0045:  call void class [mscorlib]System.Runtime.InteropServices.Marshal::Copy(int32[], int32, native int, int32)
	IL_004a:  ldloc.2 
	IL_004b:  ret 
    } // end of method Marshaller::make_buf_32

    // method line 492
    .method private static hidebysig 
           default native int make_buf_64 (string[] args)  cil managed 
    {
        // Method begins at RVA 0x5e2c
	// Code size 76 (0x4c)
	.maxstack 4
	.locals init (
		int64[]	V_0,
		int32	V_1,
		native int	V_2)
	IL_0000:  ldarg.0 
	IL_0001:  ldlen 
	IL_0002:  conv.i4 
	IL_0003:  newarr int64
	IL_0008:  stloc.0 
	IL_0009:  ldc.i4.0 
	IL_000a:  stloc.1 
	IL_000b:  br IL_0024

	IL_0010:  ldloc.0 
	IL_0011:  ldloc.1 
	IL_0012:  ldarg.0 
	IL_0013:  ldloc.1 
	IL_0014:  ldelem.ref 
	IL_0015:  call native int class [mscorlib]System.Runtime.InteropServices.Marshal::StringToHGlobalAuto(string)
	IL_001a:  call int64 native int::op_Explicit(native int)
	IL_001f:  stelem.i8 
	IL_0020:  ldloc.1 
	IL_0021:  ldc.i4.1 
	IL_0022:  add 
	IL_0023:  stloc.1 
	IL_0024:  ldloc.1 
	IL_0025:  ldarg.0 
	IL_0026:  ldlen 
	IL_0027:  conv.i4 
	IL_0028:  blt IL_0010

	IL_002d:  ldc.i4.8 
	IL_002e:  conv.i8 
	IL_002f:  ldarg.0 
	IL_0030:  ldlen 
	IL_0031:  conv.i4 
	IL_0032:  conv.i8 
	IL_0033:  mul 
	IL_0034:  newobj instance void native unsigned int::'.ctor'(unsigned int64)
	IL_0039:  call native int class GLib.Marshaller::g_malloc(native unsigned int)
	IL_003e:  stloc.2 
	IL_003f:  ldloc.0 
	IL_0040:  ldc.i4.0 
	IL_0041:  ldloc.2 
	IL_0042:  ldloc.0 
	IL_0043:  ldlen 
	IL_0044:  conv.i4 
	IL_0045:  call void class [mscorlib]System.Runtime.InteropServices.Marshal::Copy(int64[], int32, native int, int32)
	IL_004a:  ldloc.2 
	IL_004b:  ret 
    } // end of method Marshaller::make_buf_64

    // method line 493
    .method public static hidebysig 
           default native int ArgvToArrayPtr (string[] args)  cil managed 
    {
        .custom instance void class [mscorlib]System.ObsoleteAttribute::'.ctor'(string) =  (
		01 00 25 55 73 65 20 47 4C 69 62 2E 41 72 67 76   // ..%Use GLib.Argv
		20 69 6E 73 74 65 61 64 20 74 6F 20 61 76 6F 69   //  instead to avoi
		64 20 6C 65 61 6B 73 2E 00 00                   ) // d leaks...

        // Method begins at RVA 0x5e84
	// Code size 38 (0x26)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldlen 
	IL_0002:  conv.i4 
	IL_0003:  brtrue IL_000e

	IL_0008:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_000d:  ret 
	IL_000e:  call bool class GLib.Marshaller::check_sixtyfour()
	IL_0013:  brfalse IL_001f

	IL_0018:  ldarg.0 
	IL_0019:  call native int class GLib.Marshaller::make_buf_64(string[])
	IL_001e:  ret 
	IL_001f:  ldarg.0 
	IL_0020:  call native int class GLib.Marshaller::make_buf_32(string[])
	IL_0025:  ret 
    } // end of method Marshaller::ArgvToArrayPtr

    // method line 494
    .method private static hidebysig 
           default string[] unmarshal_32 (native int buf, int32 argc)  cil managed 
    {
        // Method begins at RVA 0x5eac
	// Code size 61 (0x3d)
	.maxstack 4
	.locals init (
		int32[]	V_0,
		string[]	V_1,
		int32	V_2)
	IL_0000:  ldarg.1 
	IL_0001:  newarr int32
	IL_0006:  stloc.0 
	IL_0007:  ldarg.1 
	IL_0008:  newarr string
	IL_000d:  stloc.1 
	IL_000e:  ldarg.0 
	IL_000f:  ldloc.0 
	IL_0010:  ldc.i4.0 
	IL_0011:  ldarg.1 
	IL_0012:  call void class [mscorlib]System.Runtime.InteropServices.Marshal::Copy(native int, int32[], int32, int32)
	IL_0017:  ldc.i4.0 
	IL_0018:  stloc.2 
	IL_0019:  br IL_0032

	IL_001e:  ldloc.1 
	IL_001f:  ldloc.2 
	IL_0020:  ldloc.0 
	IL_0021:  ldloc.2 
	IL_0022:  ldelem.i4 
	IL_0023:  call native int native int::op_Explicit(int32)
	IL_0028:  call string class [mscorlib]System.Runtime.InteropServices.Marshal::PtrToStringAuto(native int)
	IL_002d:  stelem.ref 
	IL_002e:  ldloc.2 
	IL_002f:  ldc.i4.1 
	IL_0030:  add 
	IL_0031:  stloc.2 
	IL_0032:  ldloc.2 
	IL_0033:  ldloc.0 
	IL_0034:  ldlen 
	IL_0035:  conv.i4 
	IL_0036:  blt IL_001e

	IL_003b:  ldloc.1 
	IL_003c:  ret 
    } // end of method Marshaller::unmarshal_32

    // method line 495
    .method private static hidebysig 
           default string[] unmarshal_64 (native int buf, int32 argc)  cil managed 
    {
        // Method begins at RVA 0x5ef8
	// Code size 61 (0x3d)
	.maxstack 4
	.locals init (
		int64[]	V_0,
		string[]	V_1,
		int32	V_2)
	IL_0000:  ldarg.1 
	IL_0001:  newarr int64
	IL_0006:  stloc.0 
	IL_0007:  ldarg.1 
	IL_0008:  newarr string
	IL_000d:  stloc.1 
	IL_000e:  ldarg.0 
	IL_000f:  ldloc.0 
	IL_0010:  ldc.i4.0 
	IL_0011:  ldarg.1 
	IL_0012:  call void class [mscorlib]System.Runtime.InteropServices.Marshal::Copy(native int, int64[], int32, int32)
	IL_0017:  ldc.i4.0 
	IL_0018:  stloc.2 
	IL_0019:  br IL_0032

	IL_001e:  ldloc.1 
	IL_001f:  ldloc.2 
	IL_0020:  ldloc.0 
	IL_0021:  ldloc.2 
	IL_0022:  ldelem.i8 
	IL_0023:  call native int native int::op_Explicit(int64)
	IL_0028:  call string class [mscorlib]System.Runtime.InteropServices.Marshal::PtrToStringAuto(native int)
	IL_002d:  stelem.ref 
	IL_002e:  ldloc.2 
	IL_002f:  ldc.i4.1 
	IL_0030:  add 
	IL_0031:  stloc.2 
	IL_0032:  ldloc.2 
	IL_0033:  ldloc.0 
	IL_0034:  ldlen 
	IL_0035:  conv.i4 
	IL_0036:  blt IL_001e

	IL_003b:  ldloc.1 
	IL_003c:  ret 
    } // end of method Marshaller::unmarshal_64

    // method line 496
    .method public static hidebysig 
           default string[] ArrayPtrToArgv (native int 'array', int32 argc)  cil managed 
    {
        .custom instance void class [mscorlib]System.ObsoleteAttribute::'.ctor'(string) =  (
		01 00 25 55 73 65 20 47 4C 69 62 2E 41 72 67 76   // ..%Use GLib.Argv
		20 69 6E 73 74 65 61 64 20 74 6F 20 61 76 6F 69   //  instead to avoi
		64 20 6C 65 61 6B 73 2E 00 00                   ) // d leaks...

        // Method begins at RVA 0x5f44
	// Code size 39 (0x27)
	.maxstack 8
	IL_0000:  ldarg.1 
	IL_0001:  brtrue IL_000d

	IL_0006:  ldc.i4.0 
	IL_0007:  newarr string
	IL_000c:  ret 
	IL_000d:  call bool class GLib.Marshaller::check_sixtyfour()
	IL_0012:  brfalse IL_001f

	IL_0017:  ldarg.0 
	IL_0018:  ldarg.1 
	IL_0019:  call string[] class GLib.Marshaller::unmarshal_64(native int, int32)
	IL_001e:  ret 
	IL_001f:  ldarg.0 
	IL_0020:  ldarg.1 
	IL_0021:  call string[] class GLib.Marshaller::unmarshal_32(native int, int32)
	IL_0026:  ret 
    } // end of method Marshaller::ArrayPtrToArgv

    // method line 497
    .method public static hidebysig 
           default native int DateTimeTotime_t (valuetype [mscorlib]System.DateTime time)  cil managed 
    {
        // Method begins at RVA 0x5f6c
	// Code size 34 (0x22)
	.maxstack 2
	.locals init (
		valuetype [mscorlib]System.TimeSpan	V_0)
	IL_0000:  ldarga.s 0
	IL_0002:  ldsfld valuetype [mscorlib]System.DateTime GLib.Marshaller::local_epoch
	IL_0007:  call instance valuetype [mscorlib]System.TimeSpan valuetype [mscorlib]System.DateTime::Subtract(valuetype [mscorlib]System.DateTime)
	IL_000c:  stloc.0 
	IL_000d:  ldloca.s 0
	IL_000f:  call instance float64 valuetype [mscorlib]System.TimeSpan::get_TotalSeconds()
	IL_0014:  conv.i8 
	IL_0015:  ldsfld int32 GLib.Marshaller::utc_offset
	IL_001a:  conv.i8 
	IL_001b:  sub 
	IL_001c:  newobj instance void native int::'.ctor'(int64)
	IL_0021:  ret 
    } // end of method Marshaller::DateTimeTotime_t

    // method line 498
    .method public static hidebysig 
           default valuetype [mscorlib]System.DateTime time_tToDateTime (native int time_t)  cil managed 
    {
        // Method begins at RVA 0x5f9c
	// Code size 26 (0x1a)
	.maxstack 8
	IL_0000:  ldsflda valuetype [mscorlib]System.DateTime GLib.Marshaller::local_epoch
	IL_0005:  ldarga.s 0
	IL_0007:  call instance int64 native int::ToInt64()
	IL_000c:  ldsfld int32 GLib.Marshaller::utc_offset
	IL_0011:  conv.i8 
	IL_0012:  add 
	IL_0013:  conv.r8 
	IL_0014:  call instance valuetype [mscorlib]System.DateTime valuetype [mscorlib]System.DateTime::AddSeconds(float64)
	IL_0019:  ret 
    } // end of method Marshaller::time_tToDateTime

    // method line 499
    .method private static hidebysig pinvokeimpl ("glibsharpglue-2" as "gtksharp_unichar_to_utf8_string" cdecl )
           default native int gtksharp_unichar_to_utf8_string (unsigned int32 c)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method Marshaller::gtksharp_unichar_to_utf8_string

    // method line 500
    .method public static hidebysig 
           default char GUnicharToChar (unsigned int32 ucs4_char)  cil managed 
    {
        // Method begins at RVA 0x5fb8
	// Code size 53 (0x35)
	.maxstack 2
	.locals init (
		native int	V_0,
		string	V_1)
	IL_0000:  ldarg.0 
	IL_0001:  brtrue IL_0008

	IL_0006:  ldc.i4.0 
	IL_0007:  ret 
	IL_0008:  ldarg.0 
	IL_0009:  call native int class GLib.Marshaller::gtksharp_unichar_to_utf8_string(unsigned int32)
	IL_000e:  stloc.0 
	IL_000f:  ldloc.0 
	IL_0010:  call string class GLib.Marshaller::PtrToStringGFree(native int)
	IL_0015:  stloc.1 
	IL_0016:  ldloc.1 
	IL_0017:  callvirt instance int32 string::get_Length()
	IL_001c:  ldc.i4.1 
	IL_001d:  ble IL_002d

	IL_0022:  ldstr "ucs4char is not representable by a char."
	IL_0027:  newobj instance void class [mscorlib]System.ArgumentOutOfRangeException::'.ctor'(string)
	IL_002c:  throw 
	IL_002d:  ldloc.1 
	IL_002e:  ldc.i4.0 
	IL_002f:  callvirt instance char string::get_Chars(int32)
	IL_0034:  ret 
    } // end of method Marshaller::GUnicharToChar

    // method line 501
    .method private static hidebysig pinvokeimpl ("glibsharpglue-2" as "glibsharp_utf16_to_unichar" cdecl )
           default unsigned int32 glibsharp_utf16_to_unichar (unsigned int16 c)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method Marshaller::glibsharp_utf16_to_unichar

    // method line 502
    .method public static hidebysig 
           default unsigned int32 CharToGUnichar (char c)  cil managed 
    {
        // Method begins at RVA 0x5ffc
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call unsigned int32 class GLib.Marshaller::glibsharp_utf16_to_unichar(unsigned int16)
	IL_0006:  ret 
    } // end of method Marshaller::CharToGUnichar

    // method line 503
    .method public static hidebysig 
           default native int StructureToPtrAlloc<T> (!!T o)  cil managed 
    {
        // Method begins at RVA 0x6004
	// Code size 22 (0x16)
	.maxstack 3
	.locals init (
		native int	V_0)
	IL_0000:  ldarg.0 
	IL_0001:  call int32 class [mscorlib]System.Runtime.InteropServices.Marshal::SizeOf<!!0> (!!0)
	IL_0006:  call native int class [mscorlib]System.Runtime.InteropServices.Marshal::AllocHGlobal(int32)
	IL_000b:  stloc.0 
	IL_000c:  ldarg.0 
	IL_000d:  ldloc.0 
	IL_000e:  ldc.i4.0 
	IL_000f:  call void class [mscorlib]System.Runtime.InteropServices.Marshal::StructureToPtr<!!0> (!!0, native int, bool)
	IL_0014:  ldloc.0 
	IL_0015:  ret 
    } // end of method Marshaller::StructureToPtrAlloc

    // method line 504
    .method public static hidebysig 
           default class [mscorlib]System.Array ListPtrToArray (native int list_ptr, class [mscorlib]System.Type list_type, bool owned, bool elements_owned, class [mscorlib]System.Type elem_type)  cil managed 
    {
        .custom instance void class [mscorlib]System.ObsoleteAttribute::'.ctor'(string) =  (
		01 00 23 55 73 65 20 74 68 65 20 4C 69 73 74 50   // ..#Use the ListP
		74 72 54 6F 41 72 72 61 79 3C 54 3E 20 6F 76 65   // trToArray<T> ove
		72 6C 6F 61 64 2E 00 00                         ) // rload...

        // Method begins at RVA 0x6028
	// Code size 79 (0x4f)
	.maxstack 4
	.locals init (
		class GLib.ListBase	V_0,
		class GLib.ListBase	V_1,
		class [mscorlib]System.Array	V_2)
	IL_0000:  ldarg.1 
	IL_0001:  ldtoken GLib.List
	IL_0006:  call class [mscorlib]System.Type class [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
	IL_000b:  call bool class [mscorlib]System.Type::op_Equality(class [mscorlib]System.Type, class [mscorlib]System.Type)
	IL_0010:  brfalse IL_0025

	IL_0015:  ldarg.0 
	IL_0016:  ldarg.s 4
	IL_0018:  ldarg.2 
	IL_0019:  ldarg.3 
	IL_001a:  newobj instance void class GLib.List::'.ctor'(native int, class [mscorlib]System.Type, bool, bool)
	IL_001f:  stloc.0 
	IL_0020:  br IL_0030

	IL_0025:  ldarg.0 
	IL_0026:  ldarg.s 4
	IL_0028:  ldarg.2 
	IL_0029:  ldarg.3 
	IL_002a:  newobj instance void class GLib.SList::'.ctor'(native int, class [mscorlib]System.Type, bool, bool)
	IL_002f:  stloc.0 
	IL_0030:  ldloc.0 
	IL_0031:  stloc.1 
	.try { // 0
	  IL_0032:  ldloc.0 
	  IL_0033:  ldarg.s 4
	  IL_0035:  call class [mscorlib]System.Array class GLib.Marshaller::ListToArray(class GLib.ListBase, class [mscorlib]System.Type)
	  IL_003a:  stloc.2 
	  IL_003b:  leave IL_004d

	} // end .try 0
	finally  { // 0
	  IL_0040:  ldloc.1 
	  IL_0041:  brfalse IL_004c

	  IL_0046:  ldloc.1 
	  IL_0047:  callvirt instance void class [mscorlib]System.IDisposable::Dispose()
	  IL_004c:  endfinally 
	} // end handler 0
	IL_004d:  ldloc.2 
	IL_004e:  ret 
    } // end of method Marshaller::ListPtrToArray

    // method line 505
    .method public static hidebysig 
           default class [mscorlib]System.Array ListToArray (class GLib.ListBase list, class [mscorlib]System.Type 'type')  cil managed 
    {
        .custom instance void class [mscorlib]System.ObsoleteAttribute::'.ctor'(string) =  (
		01 00 1F 55 73 65 20 74 68 65 20 4C 69 73 74 54   // ...Use the ListT
		6F 41 72 72 61 79 3C 54 3E 20 6F 76 65 72 6C 6F   // oArray<T> overlo
		61 64 00 00                                     ) // ad..

        // Method begins at RVA 0x6094
	// Code size 61 (0x3d)
	.maxstack 4
	.locals init (
		int32	V_0,
		class [mscorlib]System.Array	V_1)
	IL_0000:  ldarg.0 
	IL_0001:  callvirt instance int32 class GLib.ListBase::get_Count()
	IL_0006:  stloc.0 
	IL_0007:  ldarg.1 
	IL_0008:  ldloc.0 
	IL_0009:  call class [mscorlib]System.Array class [mscorlib]System.Array::CreateInstance(class [mscorlib]System.Type, int32)
	IL_000e:  stloc.1 
	IL_000f:  ldloc.0 
	IL_0010:  ldc.i4.0 
	IL_0011:  ble IL_001f

	IL_0016:  ldarg.0 
	IL_0017:  ldloc.1 
	IL_0018:  ldc.i4.0 
	IL_0019:  ldloc.0 
	IL_001a:  callvirt instance void class GLib.ListBase::CopyTo(class [mscorlib]System.Array, int32, int32)
	IL_001f:  ldarg.1 
	IL_0020:  ldtoken GLib.Opaque
	IL_0025:  call class [mscorlib]System.Type class [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
	IL_002a:  callvirt instance bool class [mscorlib]System.Type::IsSubclassOf(class [mscorlib]System.Type)
	IL_002f:  brfalse IL_003b

	IL_0034:  ldarg.0 
	IL_0035:  ldc.i4.0 
	IL_0036:  stfld bool GLib.ListBase::elements_owned
	IL_003b:  ldloc.1 
	IL_003c:  ret 
    } // end of method Marshaller::ListToArray

    // method line 506
    .method public static hidebysig 
           default !!T[] ListPtrToArray<T> (native int list_ptr, class [mscorlib]System.Type list_type, bool owned, bool elements_owned, class GLib.ListElementFree free_func)  cil managed 
    {
        // Method begins at RVA 0x60e0
	// Code size 97 (0x61)
	.maxstack 5
	.locals init (
		class GLib.ListBase	V_0,
		class GLib.ListBase	V_1,
		!!T[]	V_2)
	IL_0000:  ldarg.1 
	IL_0001:  ldtoken GLib.List
	IL_0006:  call class [mscorlib]System.Type class [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
	IL_000b:  call bool class [mscorlib]System.Type::op_Equality(class [mscorlib]System.Type, class [mscorlib]System.Type)
	IL_0010:  brfalse IL_002f

	IL_0015:  ldarg.0 
	IL_0016:  ldtoken !!T
	IL_001b:  call class [mscorlib]System.Type class [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
	IL_0020:  ldarg.2 
	IL_0021:  ldarg.3 
	IL_0022:  ldarg.s 4
	IL_0024:  newobj instance void class GLib.List::'.ctor'(native int, class [mscorlib]System.Type, bool, bool, class GLib.ListElementFree)
	IL_0029:  stloc.0 
	IL_002a:  br IL_0044

	IL_002f:  ldarg.0 
	IL_0030:  ldtoken !!T
	IL_0035:  call class [mscorlib]System.Type class [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
	IL_003a:  ldarg.2 
	IL_003b:  ldarg.3 
	IL_003c:  ldarg.s 4
	IL_003e:  newobj instance void class GLib.SList::'.ctor'(native int, class [mscorlib]System.Type, bool, bool, class GLib.ListElementFree)
	IL_0043:  stloc.0 
	IL_0044:  ldloc.0 
	IL_0045:  stloc.1 
	.try { // 0
	  IL_0046:  ldloc.0 
	  IL_0047:  call !!0[] class GLib.Marshaller::ListToArray<!!0> (class GLib.ListBase)
	  IL_004c:  stloc.2 
	  IL_004d:  leave IL_005f

	} // end .try 0
	finally  { // 0
	  IL_0052:  ldloc.1 
	  IL_0053:  brfalse IL_005e

	  IL_0058:  ldloc.1 
	  IL_0059:  callvirt instance void class [mscorlib]System.IDisposable::Dispose()
	  IL_005e:  endfinally 
	} // end handler 0
	IL_005f:  ldloc.2 
	IL_0060:  ret 
    } // end of method Marshaller::ListPtrToArray

    // method line 507
    .method public static hidebysig 
           default !!T[] ListToArray<T> (class GLib.ListBase list)  cil managed 
    {
        // Method begins at RVA 0x6160
	// Code size 68 (0x44)
	.maxstack 3
	.locals init (
		int32	V_0,
		!!T[]	V_1)
	IL_0000:  ldarg.0 
	IL_0001:  callvirt instance int32 class GLib.ListBase::get_Count()
	IL_0006:  stloc.0 
	IL_0007:  ldloc.0 
	IL_0008:  newarr !!0
	IL_000d:  stloc.1 
	IL_000e:  ldloc.0 
	IL_000f:  ldc.i4.0 
	IL_0010:  ble IL_001d

	IL_0015:  ldarg.0 
	IL_0016:  ldloc.1 
	IL_0017:  ldc.i4.0 
	IL_0018:  callvirt instance void class GLib.ListBase::CopyTo<!!0> (!!0[], int32)
	IL_001d:  ldtoken !!T
	IL_0022:  call class [mscorlib]System.Type class [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
	IL_0027:  ldtoken GLib.Opaque
	IL_002c:  call class [mscorlib]System.Type class [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
	IL_0031:  callvirt instance bool class [mscorlib]System.Type::IsSubclassOf(class [mscorlib]System.Type)
	IL_0036:  brfalse IL_0042

	IL_003b:  ldarg.0 
	IL_003c:  ldc.i4.0 
	IL_003d:  stfld bool GLib.ListBase::elements_owned
	IL_0042:  ldloc.1 
	IL_0043:  ret 
    } // end of method Marshaller::ListToArray

    // method line 508
    .method public static hidebysig 
           default !!T[] ListPtrToArray<T> (native int list_ptr, class [mscorlib]System.Type list_type, bool owned, bool elements_owned)  cil managed 
    {
        // Method begins at RVA 0x61b0
	// Code size 93 (0x5d)
	.maxstack 4
	.locals init (
		class GLib.ListBase	V_0,
		class GLib.ListBase	V_1,
		!!T[]	V_2)
	IL_0000:  ldarg.1 
	IL_0001:  ldtoken GLib.List
	IL_0006:  call class [mscorlib]System.Type class [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
	IL_000b:  call bool class [mscorlib]System.Type::op_Equality(class [mscorlib]System.Type, class [mscorlib]System.Type)
	IL_0010:  brfalse IL_002d

	IL_0015:  ldarg.0 
	IL_0016:  ldtoken !!T
	IL_001b:  call class [mscorlib]System.Type class [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
	IL_0020:  ldarg.2 
	IL_0021:  ldarg.3 
	IL_0022:  newobj instance void class GLib.List::'.ctor'(native int, class [mscorlib]System.Type, bool, bool)
	IL_0027:  stloc.0 
	IL_0028:  br IL_0040

	IL_002d:  ldarg.0 
	IL_002e:  ldtoken !!T
	IL_0033:  call class [mscorlib]System.Type class [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
	IL_0038:  ldarg.2 
	IL_0039:  ldarg.3 
	IL_003a:  newobj instance void class GLib.SList::'.ctor'(native int, class [mscorlib]System.Type, bool, bool)
	IL_003f:  stloc.0 
	IL_0040:  ldloc.0 
	IL_0041:  stloc.1 
	.try { // 0
	  IL_0042:  ldloc.0 
	  IL_0043:  call !!0[] class GLib.Marshaller::ListToArray<!!0> (class GLib.ListBase)
	  IL_0048:  stloc.2 
	  IL_0049:  leave IL_005b

	} // end .try 0
	finally  { // 0
	  IL_004e:  ldloc.1 
	  IL_004f:  brfalse IL_005a

	  IL_0054:  ldloc.1 
	  IL_0055:  callvirt instance void class [mscorlib]System.IDisposable::Dispose()
	  IL_005a:  endfinally 
	} // end handler 0
	IL_005b:  ldloc.2 
	IL_005c:  ret 
    } // end of method Marshaller::ListPtrToArray

    // method line 509
    .method public static hidebysig 
           default class [mscorlib]System.Array PtrArrayToArray (native int list_ptr, bool owned, bool elements_owned, class [mscorlib]System.Type elem_type)  cil managed 
    {
        .custom instance void class [mscorlib]System.ObsoleteAttribute::'.ctor'(string) =  (
		01 00 24 55 73 65 20 74 68 65 20 50 74 72 41 72   // ..$Use the PtrAr
		72 61 79 54 6F 41 72 72 61 79 3C 54 3E 20 6F 76   // rayToArray<T> ov
		65 72 6C 6F 61 64 2E 00 00                      ) // erload...

        // Method begins at RVA 0x622c
	// Code size 39 (0x27)
	.maxstack 4
	.locals init (
		class GLib.PtrArray	V_0,
		class [mscorlib]System.Array	V_1)
	IL_0000:  ldarg.0 
	IL_0001:  ldarg.3 
	IL_0002:  ldarg.1 
	IL_0003:  ldarg.2 
	IL_0004:  newobj instance void class GLib.PtrArray::'.ctor'(native int, class [mscorlib]System.Type, bool, bool)
	IL_0009:  stloc.0 
	IL_000a:  ldarg.3 
	IL_000b:  ldloc.0 
	IL_000c:  callvirt instance int32 class GLib.PtrArray::get_Count()
	IL_0011:  call class [mscorlib]System.Array class [mscorlib]System.Array::CreateInstance(class [mscorlib]System.Type, int32)
	IL_0016:  stloc.1 
	IL_0017:  ldloc.0 
	IL_0018:  ldloc.1 
	IL_0019:  ldc.i4.0 
	IL_001a:  callvirt instance void class GLib.PtrArray::CopyTo(class [mscorlib]System.Array, int32)
	IL_001f:  ldloc.0 
	IL_0020:  callvirt instance void class GLib.PtrArray::Dispose()
	IL_0025:  ldloc.1 
	IL_0026:  ret 
    } // end of method Marshaller::PtrArrayToArray

    // method line 510
    .method public static hidebysig 
           default !!T[] PtrArrayToArray<T> (native int list_ptr, bool owned, bool elements_owned)  cil managed 
    {
        // Method begins at RVA 0x6260
	// Code size 49 (0x31)
	.maxstack 4
	.locals init (
		class [mscorlib]System.Type	V_0,
		class GLib.PtrArray	V_1,
		!!T[]	V_2)
	IL_0000:  ldtoken !!T
	IL_0005:  call class [mscorlib]System.Type class [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
	IL_000a:  stloc.0 
	IL_000b:  ldarg.0 
	IL_000c:  ldloc.0 
	IL_000d:  ldarg.1 
	IL_000e:  ldarg.2 
	IL_000f:  newobj instance void class GLib.PtrArray::'.ctor'(native int, class [mscorlib]System.Type, bool, bool)
	IL_0014:  stloc.1 
	IL_0015:  ldloc.1 
	IL_0016:  callvirt instance int32 class GLib.PtrArray::get_Count()
	IL_001b:  newarr !!0
	IL_0020:  stloc.2 
	IL_0021:  ldloc.1 
	IL_0022:  ldloc.2 
	IL_0023:  ldc.i4.0 
	IL_0024:  callvirt instance void class GLib.PtrArray::CopyTo(class [mscorlib]System.Array, int32)
	IL_0029:  ldloc.1 
	IL_002a:  callvirt instance void class GLib.PtrArray::Dispose()
	IL_002f:  ldloc.2 
	IL_0030:  ret 
    } // end of method Marshaller::PtrArrayToArray

    // method line 511
    .method public static hidebysig 
           default !!T[] PtrArrayToArray<T> (native int list_ptr, bool owned, bool elements_owned, class GLib.ListElementFree free_func)  cil managed 
    {
        // Method begins at RVA 0x62a0
	// Code size 50 (0x32)
	.maxstack 5
	.locals init (
		class [mscorlib]System.Type	V_0,
		class GLib.PtrArray	V_1,
		!!T[]	V_2)
	IL_0000:  ldtoken !!T
	IL_0005:  call class [mscorlib]System.Type class [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
	IL_000a:  stloc.0 
	IL_000b:  ldarg.0 
	IL_000c:  ldloc.0 
	IL_000d:  ldarg.1 
	IL_000e:  ldarg.2 
	IL_000f:  ldarg.3 
	IL_0010:  newobj instance void class GLib.PtrArray::'.ctor'(native int, class [mscorlib]System.Type, bool, bool, class GLib.ListElementFree)
	IL_0015:  stloc.1 
	IL_0016:  ldloc.1 
	IL_0017:  callvirt instance int32 class GLib.PtrArray::get_Count()
	IL_001c:  newarr !!0
	IL_0021:  stloc.2 
	IL_0022:  ldloc.1 
	IL_0023:  ldloc.2 
	IL_0024:  ldc.i4.0 
	IL_0025:  callvirt instance void class GLib.PtrArray::CopyTo(class [mscorlib]System.Array, int32)
	IL_002a:  ldloc.1 
	IL_002b:  callvirt instance void class GLib.PtrArray::Dispose()
	IL_0030:  ldloc.2 
	IL_0031:  ret 
    } // end of method Marshaller::PtrArrayToArray

    // method line 512
    .method private static hidebysig specialname rtspecialname 
           default void '.cctor' ()  cil managed 
    {
        // Method begins at RVA 0x62e0
	// Code size 50 (0x32)
	.maxstack 6
	.locals init (
		valuetype [mscorlib]System.TimeSpan	V_0)
	IL_0000:  ldc.i4 1970
	IL_0005:  ldc.i4.1 
	IL_0006:  ldc.i4.1 
	IL_0007:  ldc.i4.0 
	IL_0008:  ldc.i4.0 
	IL_0009:  ldc.i4.0 
	IL_000a:  newobj instance void valuetype [mscorlib]System.DateTime::'.ctor'(int32, int32, int32, int32, int32, int32)
	IL_000f:  stsfld valuetype [mscorlib]System.DateTime GLib.Marshaller::local_epoch
	IL_0014:  call class [mscorlib]System.TimeZone class [mscorlib]System.TimeZone::get_CurrentTimeZone()
	IL_0019:  call valuetype [mscorlib]System.DateTime valuetype [mscorlib]System.DateTime::get_Now()
	IL_001e:  callvirt instance valuetype [mscorlib]System.TimeSpan class [mscorlib]System.TimeZone::GetUtcOffset(valuetype [mscorlib]System.DateTime)
	IL_0023:  stloc.0 
	IL_0024:  ldloca.s 0
	IL_0026:  call instance float64 valuetype [mscorlib]System.TimeSpan::get_TotalSeconds()
	IL_002b:  conv.i4 
	IL_002c:  stsfld int32 GLib.Marshaller::utc_offset
	IL_0031:  ret 
    } // end of method Marshaller::.cctor

  } // end of class GLib.Marshaller
}

.namespace GLib
{
  .class public auto ansi beforefieldinit MissingIntPtrCtorException
  	extends [mscorlib]System.Exception
  {

    // method line 513
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (string msg)  cil managed 
    {
        // Method begins at RVA 0x6320
	// Code size 8 (0x8)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldarg.1 
	IL_0002:  call instance void class [mscorlib]System.Exception::'.ctor'(string)
	IL_0007:  ret 
    } // end of method MissingIntPtrCtorException::.ctor

  } // end of class GLib.MissingIntPtrCtorException
}

.namespace GLib
{
  .class public auto ansi sealed NotifyHandler
  	extends [mscorlib]System.MulticastDelegate
  {

    // method line 514
    .method public virtual hidebysig newslot 
           instance default void Invoke (object o, class GLib.NotifyArgs args)  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method NotifyHandler::Invoke

    // method line 515
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (object 'object', native int 'method')  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method NotifyHandler::.ctor

    // method line 516
    .method public virtual hidebysig newslot 
           instance default class [mscorlib]System.IAsyncResult BeginInvoke (object o, class GLib.NotifyArgs args, class [mscorlib]System.AsyncCallback callback, object 'object')  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method NotifyHandler::BeginInvoke

    // method line 517
    .method public virtual hidebysig newslot 
           instance default void EndInvoke (class [mscorlib]System.IAsyncResult result)  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method NotifyHandler::EndInvoke

  } // end of class GLib.NotifyHandler
}

.namespace GLib
{
  .class public auto ansi beforefieldinit SignalArgs
  	extends [mscorlib]System.EventArgs
  {
    .field  private  object _ret
    .field  private  object[] _args

    // method line 518
    .method public hidebysig specialname 
           instance default void set_Args (object[] 'value')  cil managed 
    {
        // Method begins at RVA 0x632c
	// Code size 8 (0x8)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldarg.1 
	IL_0002:  stfld object[] GLib.SignalArgs::_args
	IL_0007:  ret 
    } // end of method SignalArgs::set_Args

    // method line 519
    .method public hidebysig specialname 
           instance default object[] get_Args ()  cil managed 
    {
        // Method begins at RVA 0x6338
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldfld object[] GLib.SignalArgs::_args
	IL_0006:  ret 
    } // end of method SignalArgs::get_Args

    // method line 520
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' ()  cil managed 
    {
        // Method begins at RVA 0x6340
	// Code size 21 (0x15)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void class [mscorlib]System.EventArgs::'.ctor'()
	IL_0006:  ldarg.0 
	IL_0007:  ldnull 
	IL_0008:  stfld object GLib.SignalArgs::_ret
	IL_000d:  ldarg.0 
	IL_000e:  ldnull 
	IL_000f:  stfld object[] GLib.SignalArgs::_args
	IL_0014:  ret 
    } // end of method SignalArgs::.ctor

    // method line 521
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (object 'retval')  cil managed 
    {
        // Method begins at RVA 0x6358
	// Code size 21 (0x15)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void class [mscorlib]System.EventArgs::'.ctor'()
	IL_0006:  ldarg.0 
	IL_0007:  ldarg.1 
	IL_0008:  stfld object GLib.SignalArgs::_ret
	IL_000d:  ldarg.0 
	IL_000e:  ldnull 
	IL_000f:  stfld object[] GLib.SignalArgs::_args
	IL_0014:  ret 
    } // end of method SignalArgs::.ctor

    // method line 522
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (object 'retval', object[] args)  cil managed 
    {
        // Method begins at RVA 0x6370
	// Code size 21 (0x15)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void class [mscorlib]System.EventArgs::'.ctor'()
	IL_0006:  ldarg.0 
	IL_0007:  ldarg.1 
	IL_0008:  stfld object GLib.SignalArgs::_ret
	IL_000d:  ldarg.0 
	IL_000e:  ldarg.2 
	IL_000f:  stfld object[] GLib.SignalArgs::_args
	IL_0014:  ret 
    } // end of method SignalArgs::.ctor

    // method line 523
    .method public hidebysig specialname 
           instance default object get_RetVal ()  cil managed 
    {
        // Method begins at RVA 0x6388
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldfld object GLib.SignalArgs::_ret
	IL_0006:  ret 
    } // end of method SignalArgs::get_RetVal

    // method line 524
    .method public hidebysig specialname 
           instance default void set_RetVal (object 'value')  cil managed 
    {
        // Method begins at RVA 0x6390
	// Code size 8 (0x8)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldarg.1 
	IL_0002:  stfld object GLib.SignalArgs::_ret
	IL_0007:  ret 
    } // end of method SignalArgs::set_RetVal

	.property instance object[] Args ()
	{
		.get instance default object[] GLib.SignalArgs::get_Args () 
		.set instance default void GLib.SignalArgs::set_Args (object[] 'value') 
	}
	.property instance object RetVal ()
	{
		.get instance default object GLib.SignalArgs::get_RetVal () 
		.set instance default void GLib.SignalArgs::set_RetVal (object 'value') 
	}
  } // end of class GLib.SignalArgs
}

.namespace GLib
{
  .class public auto ansi beforefieldinit NotifyArgs
  	extends GLib.SignalArgs
  {

    // method line 525
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' ()  cil managed 
    {
        // Method begins at RVA 0x639c
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void class GLib.SignalArgs::'.ctor'()
	IL_0006:  ret 
    } // end of method NotifyArgs::.ctor

    // method line 526
    .method private static hidebysig pinvokeimpl ("libgobject-2.0-0.dll" as "g_param_spec_get_name" cdecl )
           default native int g_param_spec_get_name (native int pspec)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method NotifyArgs::g_param_spec_get_name

    // method line 527
    .method public hidebysig specialname 
           instance default string get_Property ()  cil managed 
    {
        // Method begins at RVA 0x63a4
	// Code size 26 (0x1a)
	.maxstack 2
	.locals init (
		native int	V_0)
	IL_0000:  ldarg.0 
	IL_0001:  call instance object[] class GLib.SignalArgs::get_Args()
	IL_0006:  ldc.i4.0 
	IL_0007:  ldelem.ref 
	IL_0008:  unbox.any native int
	IL_000d:  call native int class GLib.NotifyArgs::g_param_spec_get_name(native int)
	IL_0012:  stloc.0 
	IL_0013:  ldloc.0 
	IL_0014:  call string class GLib.Marshaller::Utf8PtrToString(native int)
	IL_0019:  ret 
    } // end of method NotifyArgs::get_Property

	.property instance string Property ()
	{
		.get instance default string GLib.NotifyArgs::get_Property () 
	}
  } // end of class GLib.NotifyArgs
}

.namespace GLib
{
  .class public auto ansi beforefieldinit ObjectManager
  	extends [mscorlib]System.Object
  {
    .field  private static  valuetype [mscorlib]System.Reflection.BindingFlags 'flags'

    // method line 540
    .method public static hidebysig 
           default class GLib.Object CreateObject (native int raw)  cil managed 
    {
        // Method begins at RVA 0x63cc
	// Code size 77 (0x4d)
	.maxstack 3
	.locals init (
		class [mscorlib]System.Type	V_0,
		class GLib.Object	V_1)
	IL_0000:  ldarg.0 
	IL_0001:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_0006:  call bool native int::op_Equality(native int, native int)
	IL_000b:  brfalse IL_0012

	IL_0010:  ldnull 
	IL_0011:  ret 
	IL_0012:  ldarg.0 
	IL_0013:  call class [mscorlib]System.Type class GLib.ObjectManager::GetTypeOrParent(native int)
	IL_0018:  stloc.0 
	IL_0019:  ldloc.0 
	IL_001a:  ldnull 
	IL_001b:  call bool class [mscorlib]System.Type::op_Equality(class [mscorlib]System.Type, class [mscorlib]System.Type)
	IL_0020:  brfalse IL_0027

	IL_0025:  ldnull 
	IL_0026:  ret 
	.try { // 0
	  IL_0027:  ldarg.0 
	  IL_0028:  ldloc.0 
	  IL_0029:  call class GLib.Object class GLib.FastActivator::CreateObject(native int, class [mscorlib]System.Type)
	  IL_002e:  stloc.1 
	  IL_002f:  leave IL_004b

	} // end .try 0
	catch class [mscorlib]System.MissingMethodException { // 0
	  IL_0034:  pop 
	  IL_0035:  ldstr "GLib.Object subclass "
	  IL_003a:  ldloc.0 
	  IL_003b:  ldstr " must provide a protected or public IntPtr ctor to support wrapping of native object handles."
	  IL_0040:  call string string::Concat(object, object, object)
	  IL_0045:  newobj instance void class GLib.MissingIntPtrCtorException::'.ctor'(string)
	  IL_004a:  throw 
	} // end handler 0
	IL_004b:  ldloc.1 
	IL_004c:  ret 
    } // end of method ObjectManager::CreateObject

    // method line 541
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' ()  cil managed 
    {
        // Method begins at RVA 0x6438
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::'.ctor'()
	IL_0006:  ret 
    } // end of method ObjectManager::.ctor

    // method line 542
    .method private static hidebysig 
           default class [mscorlib]System.Type GetTypeOrParent (native int obj)  cil managed 
    {
        // Method begins at RVA 0x6440
	// Code size 65 (0x41)
	.maxstack 2
	.locals init (
		native int	V_0,
		class [mscorlib]System.Type	V_1)
	IL_0000:  ldarg.0 
	IL_0001:  call native int class GLib.ObjectManager::gtksharp_get_type_id(native int)
	IL_0006:  stloc.0 
	IL_0007:  ldloc.0 
	IL_0008:  call class [mscorlib]System.Type valuetype GLib.GType::LookupType(native int)
	IL_000d:  stloc.1 
	IL_000e:  br IL_0033

	IL_0013:  ldloc.0 
	IL_0014:  call native int class GLib.ObjectManager::g_type_parent(native int)
	IL_0019:  stloc.0 
	IL_001a:  ldloc.0 
	IL_001b:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_0020:  call bool native int::op_Equality(native int, native int)
	IL_0025:  brfalse IL_002c

	IL_002a:  ldnull 
	IL_002b:  ret 
	IL_002c:  ldloc.0 
	IL_002d:  call class [mscorlib]System.Type valuetype GLib.GType::LookupType(native int)
	IL_0032:  stloc.1 
	IL_0033:  ldloc.1 
	IL_0034:  ldnull 
	IL_0035:  call bool class [mscorlib]System.Type::op_Equality(class [mscorlib]System.Type, class [mscorlib]System.Type)
	IL_003a:  brtrue IL_0013

	IL_003f:  ldloc.1 
	IL_0040:  ret 
    } // end of method ObjectManager::GetTypeOrParent

    // method line 543
    .method public static hidebysig 
           default void RegisterType (string native_name, string managed_name, string 'assembly')  cil managed 
    {
        .custom instance void class [mscorlib]System.ObsoleteAttribute::'.ctor'(string) =  (
		01 00 28 52 65 70 6C 61 63 65 64 20 62 79 20 47   // ..(Replaced by G
		54 79 70 65 2E 52 65 67 69 73 74 65 72 20 28 47   // Type.Register (G
		54 79 70 65 2C 20 54 79 70 65 29 00 00          ) // Type, Type)..

        // Method begins at RVA 0x6490
	// Code size 19 (0x13)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldarg.1 
	IL_0002:  ldstr ","
	IL_0007:  ldarg.2 
	IL_0008:  call string string::Concat(string, string, string)
	IL_000d:  call void class GLib.ObjectManager::RegisterType(string, string)
	IL_0012:  ret 
    } // end of method ObjectManager::RegisterType

    // method line 544
    .method public static hidebysig 
           default void RegisterType (string native_name, string mangled)  cil managed 
    {
        .custom instance void class [mscorlib]System.ObsoleteAttribute::'.ctor'(string) =  (
		01 00 28 52 65 70 6C 61 63 65 64 20 62 79 20 47   // ..(Replaced by G
		54 79 70 65 2E 52 65 67 69 73 74 65 72 20 28 47   // Type.Register (G
		54 79 70 65 2C 20 54 79 70 65 29 00 00          ) // Type, Type)..

        // Method begins at RVA 0x64a4
	// Code size 18 (0x12)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call valuetype GLib.GType valuetype GLib.GType::FromName(string)
	IL_0006:  ldarg.1 
	IL_0007:  call class [mscorlib]System.Type class [mscorlib]System.Type::GetType(string)
	IL_000c:  call void class GLib.ObjectManager::RegisterType(valuetype GLib.GType, class [mscorlib]System.Type)
	IL_0011:  ret 
    } // end of method ObjectManager::RegisterType

    // method line 545
    .method public static hidebysig 
           default void RegisterType (valuetype GLib.GType native_type, class [mscorlib]System.Type 'type')  cil managed 
    {
        .custom instance void class [mscorlib]System.ObsoleteAttribute::'.ctor'(string) =  (
		01 00 28 52 65 70 6C 61 63 65 64 20 62 79 20 47   // ..(Replaced by G
		54 79 70 65 2E 52 65 67 69 73 74 65 72 20 28 47   // Type.Register (G
		54 79 70 65 2C 20 54 79 70 65 29 00 00          ) // Type, Type)..

        // Method begins at RVA 0x64b8
	// Code size 8 (0x8)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldarg.1 
	IL_0002:  call void valuetype GLib.GType::Register(valuetype GLib.GType, class [mscorlib]System.Type)
	IL_0007:  ret 
    } // end of method ObjectManager::RegisterType

    // method line 546
    .method private static hidebysig pinvokeimpl ("glibsharpglue-2" as "gtksharp_get_type_id" cdecl )
           default native int gtksharp_get_type_id (native int raw)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method ObjectManager::gtksharp_get_type_id

    // method line 547
    .method private static hidebysig pinvokeimpl ("libgobject-2.0-0.dll" as "g_type_parent" cdecl )
           default native int g_type_parent (native int typ)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method ObjectManager::g_type_parent

    // method line 548
    .method private static hidebysig specialname rtspecialname 
           default void '.cctor' ()  cil managed 
    {
        // Method begins at RVA 0x64c4
	// Code size 11 (0xb)
	.maxstack 8
	IL_0000:  ldc.i4 564
	IL_0005:  stsfld valuetype [mscorlib]System.Reflection.BindingFlags GLib.ObjectManager::'flags'
	IL_000a:  ret 
    } // end of method ObjectManager::.cctor

  } // end of class GLib.ObjectManager
}

.namespace GLib
{
  .class public auto ansi beforefieldinit Opaque
  	extends [mscorlib]System.Object
  	implements GLib.IWrapper, [mscorlib]System.IDisposable  {
    .field  private  native int _obj
    .field  private  bool owned
    .field  private static  object lockObject
    .field  private static  class [mscorlib]System.Collections.Generic.List`1<class GLib.Opaque> PendingFrees
    .field  private static  bool idleQueued

    // method line 549
    .method public static hidebysig 
           default class GLib.Opaque GetOpaque (native int o, class [mscorlib]System.Type 'type', bool owned)  cil managed 
    {
        // Method begins at RVA 0x64d0
	// Code size 54 (0x36)
	.maxstack 2
	.locals init (
		class GLib.Opaque	V_0)
	IL_0000:  ldarg.0 
	IL_0001:  ldarg.1 
	IL_0002:  call class GLib.Opaque class GLib.FastActivator::CreateOpaque(native int, class [mscorlib]System.Type)
	IL_0007:  stloc.0 
	IL_0008:  ldarg.2 
	IL_0009:  brfalse IL_002c

	IL_000e:  ldloc.0 
	IL_000f:  callvirt instance bool class GLib.Opaque::get_Owned()
	IL_0014:  brfalse IL_0020

	IL_0019:  ldloc.0 
	IL_001a:  ldarg.0 
	IL_001b:  callvirt instance void class GLib.Opaque::Unref(native int)
	IL_0020:  ldloc.0 
	IL_0021:  ldc.i4.1 
	IL_0022:  callvirt instance void class GLib.Opaque::set_Owned(bool)
	IL_0027:  br IL_0034

	IL_002c:  ldloc.0 
	IL_002d:  ldarg.0 
	IL_002e:  callvirt instance class GLib.Opaque class GLib.Opaque::Copy(native int)
	IL_0033:  stloc.0 
	IL_0034:  ldloc.0 
	IL_0035:  ret 
    } // end of method Opaque::GetOpaque

    // method line 550
    .method public virtual hidebysig newslot 
           instance default void Dispose ()  cil managed 
    {
        // Method begins at RVA 0x6514
	// Code size 18 (0x12)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_0006:  call instance void class GLib.Opaque::set_Raw(native int)
	IL_000b:  ldarg.0 
	IL_000c:  call void class [mscorlib]System.GC::SuppressFinalize(object)
	IL_0011:  ret 
    } // end of method Opaque::Dispose

    // method line 551
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' ()  cil managed 
    {
        // Method begins at RVA 0x6528
	// Code size 14 (0xe)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::'.ctor'()
	IL_0006:  ldarg.0 
	IL_0007:  ldc.i4.1 
	IL_0008:  stfld bool GLib.Opaque::owned
	IL_000d:  ret 
    } // end of method Opaque::.ctor

    // method line 552
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (native int raw)  cil managed 
    {
        // Method begins at RVA 0x6538
	// Code size 27 (0x1b)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::'.ctor'()
	IL_0006:  ldarg.0 
	IL_0007:  call void class [mscorlib]System.GC::SuppressFinalize(object)
	IL_000c:  ldarg.0 
	IL_000d:  ldc.i4.0 
	IL_000e:  stfld bool GLib.Opaque::owned
	IL_0013:  ldarg.0 
	IL_0014:  ldarg.1 
	IL_0015:  call instance void class GLib.Opaque::set_Raw(native int)
	IL_001a:  ret 
    } // end of method Opaque::.ctor

    // method line 553
    .method family hidebysig specialname 
           instance default void set_Raw (native int 'value')  cil managed 
    {
        // Method begins at RVA 0x6554
	// Code size 97 (0x61)
	.maxstack 2
	IL_0000:  ldarg.0 
	IL_0001:  ldfld native int GLib.Opaque::_obj
	IL_0006:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_000b:  call bool native int::op_Inequality(native int, native int)
	IL_0010:  brfalse IL_0038

	IL_0015:  ldarg.0 
	IL_0016:  ldarg.0 
	IL_0017:  ldfld native int GLib.Opaque::_obj
	IL_001c:  callvirt instance void class GLib.Opaque::Unref(native int)
	IL_0021:  ldarg.0 
	IL_0022:  ldfld bool GLib.Opaque::owned
	IL_0027:  brfalse IL_0038

	IL_002c:  ldarg.0 
	IL_002d:  ldarg.0 
	IL_002e:  ldfld native int GLib.Opaque::_obj
	IL_0033:  callvirt instance void class GLib.Opaque::Free(native int)
	IL_0038:  ldarg.0 
	IL_0039:  ldarg.1 
	IL_003a:  stfld native int GLib.Opaque::_obj
	IL_003f:  ldarg.0 
	IL_0040:  ldfld native int GLib.Opaque::_obj
	IL_0045:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_004a:  call bool native int::op_Inequality(native int, native int)
	IL_004f:  brfalse IL_0060

	IL_0054:  ldarg.0 
	IL_0055:  ldarg.0 
	IL_0056:  ldfld native int GLib.Opaque::_obj
	IL_005b:  callvirt instance void class GLib.Opaque::Ref(native int)
	IL_0060:  ret 
    } // end of method Opaque::set_Raw

    // method line 554
    .method public static hidebysig 
           default class GLib.Opaque GetOpaque (native int o)  cil managed 
    {
        .custom instance void class [mscorlib]System.ObsoleteAttribute::'.ctor'(string) =  (
		01 00 3C 55 73 65 20 6D 6F 72 65 20 65 78 70 6C   // ..<Use more expl
		69 63 69 74 20 6F 76 65 72 6C 6F 61 64 2E 20 20   // icit overload.  
		54 68 69 73 20 6D 65 74 68 6F 64 20 61 6C 77 61   // This method alwa
		79 73 20 72 65 74 75 72 6E 73 20 6E 75 6C 6C 00   // ys returns null.
		00                                              ) // .

        // Method begins at RVA 0x65c4
	// Code size 2 (0x2)
	.maxstack 8
	IL_0000:  ldnull 
	IL_0001:  ret 
    } // end of method Opaque::GetOpaque

    // method line 555
    .method public hidebysig specialname 
           instance default bool get_Owned ()  cil managed 
    {
        // Method begins at RVA 0x65c8
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldfld bool GLib.Opaque::owned
	IL_0006:  ret 
    } // end of method Opaque::get_Owned

    // method line 556
    .method family virtual hidebysig newslot 
           instance default void Unref (native int raw)  cil managed 
    {
        // Method begins at RVA 0x65d0
	// Code size 1 (0x1)
	.maxstack 8
	IL_0000:  ret 
    } // end of method Opaque::Unref

    // method line 557
    .method public hidebysig specialname 
           instance default void set_Owned (bool 'value')  cil managed 
    {
        // Method begins at RVA 0x65d4
	// Code size 44 (0x2c)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldfld bool GLib.Opaque::owned
	IL_0006:  ldarg.1 
	IL_0007:  bne.un IL_000d

	IL_000c:  ret 
	IL_000d:  ldarg.1 
	IL_000e:  brfalse IL_001e

	IL_0013:  ldarg.0 
	IL_0014:  call void class [mscorlib]System.GC::ReRegisterForFinalize(object)
	IL_0019:  br IL_0024

	IL_001e:  ldarg.0 
	IL_001f:  call void class [mscorlib]System.GC::SuppressFinalize(object)
	IL_0024:  ldarg.0 
	IL_0025:  ldarg.1 
	IL_0026:  stfld bool GLib.Opaque::owned
	IL_002b:  ret 
    } // end of method Opaque::set_Owned

    // method line 558
    .method family virtual hidebysig newslot 
           instance default class GLib.Opaque Copy (native int raw)  cil managed 
    {
        // Method begins at RVA 0x6604
	// Code size 2 (0x2)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ret 
    } // end of method Opaque::Copy

    // method line 559
    .method family hidebysig specialname 
           instance default native int get_Raw ()  cil managed 
    {
        // Method begins at RVA 0x6608
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldfld native int GLib.Opaque::_obj
	IL_0006:  ret 
    } // end of method Opaque::get_Raw

    // method line 560
    .method family virtual hidebysig newslot 
           instance default void Free (native int raw)  cil managed 
    {
        // Method begins at RVA 0x6610
	// Code size 1 (0x1)
	.maxstack 8
	IL_0000:  ret 
    } // end of method Opaque::Free

    // method line 561
    .method family virtual hidebysig newslot 
           instance default void Ref (native int raw)  cil managed 
    {
        // Method begins at RVA 0x6614
	// Code size 1 (0x1)
	.maxstack 8
	IL_0000:  ret 
    } // end of method Opaque::Ref

    // method line 562
    .method private hidebysig 
           instance default bool PerformQueuedFrees ()  cil managed 
    {
        // Method begins at RVA 0x6618
	// Code size 118 (0x76)
	.maxstack 2
	.locals init (
		class [mscorlib]System.Collections.Generic.List`1<class GLib.Opaque>	V_0,
		object	V_1,
		bool	V_2,
		class GLib.Opaque	V_3,
		valuetype [mscorlib]System.Collections.Generic.List`1/Enumerator<class GLib.Opaque>	V_4)
	IL_0000:  ldsfld object GLib.Opaque::lockObject
	IL_0005:  stloc.1 
	IL_0006:  ldc.i4.0 
	IL_0007:  stloc.2 
	.try { // 0
	  IL_0008:  ldloc.1 
	  IL_0009:  ldloca.s 2
	  IL_000b:  call void class [mscorlib]System.Threading.Monitor::Enter(object, [out] bool&)
	  IL_0010:  ldsfld class [mscorlib]System.Collections.Generic.List`1<class GLib.Opaque> GLib.Opaque::PendingFrees
	  IL_0015:  stloc.0 
	  IL_0016:  newobj instance void class [mscorlib]System.Collections.Generic.List`1<class GLib.Opaque>::'.ctor'()
	  IL_001b:  stsfld class [mscorlib]System.Collections.Generic.List`1<class GLib.Opaque> GLib.Opaque::PendingFrees
	  IL_0020:  ldc.i4.0 
	  IL_0021:  stsfld bool GLib.Opaque::idleQueued
	  IL_0026:  leave IL_0035

	} // end .try 0
	finally  { // 0
	  IL_002b:  ldloc.2 
	  IL_002c:  brfalse.s IL_0034

	  IL_002e:  ldloc.1 
	  IL_002f:  call void class [mscorlib]System.Threading.Monitor::Exit(object)
	  IL_0034:  endfinally 
	} // end handler 0
	IL_0035:  ldloc.0 
	IL_0036:  callvirt instance valuetype [mscorlib]System.Collections.Generic.List`1/Enumerator<!0> class [mscorlib]System.Collections.Generic.List`1<class GLib.Opaque>::GetEnumerator()
	IL_003b:  stloc.s 4
	.try { // 1
	  IL_003d:  br IL_0055

	  IL_0042:  ldloca.s 4
	  IL_0044:  call instance !0 valuetype [mscorlib]System.Collections.Generic.List`1/Enumerator<class GLib.Opaque>::get_Current()
	  IL_0049:  stloc.3 
	  IL_004a:  ldloc.3 
	  IL_004b:  ldsfld native int [mscorlib]System.IntPtr::Zero
	  IL_0050:  callvirt instance void class GLib.Opaque::set_Raw(native int)
	  IL_0055:  ldloca.s 4
	  IL_0057:  call instance bool valuetype [mscorlib]System.Collections.Generic.List`1/Enumerator<class GLib.Opaque>::MoveNext()
	  IL_005c:  brtrue IL_0042

	  IL_0061:  leave IL_0074

	} // end .try 1
	finally  { // 1
	  IL_0066:  ldloca.s 4
	  IL_0068:  constrained. valuetype [mscorlib]System.Collections.Generic.List`1/Enumerator<class GLib.Opaque>
	  IL_006e:  callvirt instance void class [mscorlib]System.IDisposable::Dispose()
	  IL_0073:  endfinally 
	} // end handler 1
	IL_0074:  ldc.i4.0 
	IL_0075:  ret 
    } // end of method Opaque::PerformQueuedFrees

    // method line 563
    .method family virtual hidebysig 
           instance default void Finalize ()  cil managed 
    {
        // Method begins at RVA 0x66b8
	// Code size 91 (0x5b)
	.maxstack 3
	.locals init (
		object	V_0,
		bool	V_1)
	.try { // 1
	  IL_0000:  ldsfld object GLib.Opaque::lockObject
	  IL_0005:  stloc.0 
	  IL_0006:  ldc.i4.0 
	  IL_0007:  stloc.1 
	  .try { // 0
	    IL_0008:  ldloc.0 
	    IL_0009:  ldloca.s 1
	    IL_000b:  call void class [mscorlib]System.Threading.Monitor::Enter(object, [out] bool&)
	    IL_0010:  ldsfld class [mscorlib]System.Collections.Generic.List`1<class GLib.Opaque> GLib.Opaque::PendingFrees
	    IL_0015:  ldarg.0 
	    IL_0016:  callvirt instance void class [mscorlib]System.Collections.Generic.List`1<class GLib.Opaque>::Add(!0)
	    IL_001b:  ldsfld bool GLib.Opaque::idleQueued
	    IL_0020:  brtrue IL_003f

	    IL_0025:  ldc.i4.1 
	    IL_0026:  stsfld bool GLib.Opaque::idleQueued
	    IL_002b:  ldc.i4.s 0x32
	    IL_002d:  ldarg.0 
	    IL_002e:  ldftn instance bool class GLib.Opaque::PerformQueuedFrees()
	    IL_0034:  newobj instance void class GLib.TimeoutHandler::'.ctor'(object, native int)
	    IL_0039:  call unsigned int32 class GLib.Timeout::Add(unsigned int32, class GLib.TimeoutHandler)
	    IL_003e:  pop 
	    IL_003f:  leave IL_004e

	  } // end .try 0
	  finally  { // 0
	    IL_0044:  ldloc.1 
	    IL_0045:  brfalse.s IL_004d

	    IL_0047:  ldloc.0 
	    IL_0048:  call void class [mscorlib]System.Threading.Monitor::Exit(object)
	    IL_004d:  endfinally 
	  } // end handler 0
	  IL_004e:  leave IL_005a

	} // end .try 1
	finally  { // 1
	  IL_0053:  ldarg.0 
	  IL_0054:  call instance void object::Finalize()
	  IL_0059:  endfinally 
	} // end handler 1
	IL_005a:  ret 
    } // end of method Opaque::Finalize

    // method line 564
    .method public final virtual hidebysig newslot specialname 
           instance default native int get_Handle ()  cil managed 
    {
        // Method begins at RVA 0x673c
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldfld native int GLib.Opaque::_obj
	IL_0006:  ret 
    } // end of method Opaque::get_Handle

    // method line 565
    .method public hidebysig specialname 
           instance default native int get_OwnedCopy ()  cil managed 
    {
        // Method begins at RVA 0x6744
	// Code size 27 (0x1b)
	.maxstack 2
	.locals init (
		class GLib.Opaque	V_0)
	IL_0000:  ldarg.0 
	IL_0001:  ldarg.0 
	IL_0002:  call instance native int class GLib.Opaque::get_Handle()
	IL_0007:  callvirt instance class GLib.Opaque class GLib.Opaque::Copy(native int)
	IL_000c:  stloc.0 
	IL_000d:  ldloc.0 
	IL_000e:  ldc.i4.0 
	IL_000f:  callvirt instance void class GLib.Opaque::set_Owned(bool)
	IL_0014:  ldloc.0 
	IL_0015:  callvirt instance native int class GLib.Opaque::get_Handle()
	IL_001a:  ret 
    } // end of method Opaque::get_OwnedCopy

    // method line 566
    .method public virtual hidebysig 
           instance default bool Equals (object o)  cil managed 
    {
        // Method begins at RVA 0x676c
	// Code size 36 (0x24)
	.maxstack 8
	IL_0000:  ldarg.1 
	IL_0001:  isinst GLib.Opaque
	IL_0006:  brtrue IL_000d

	IL_000b:  ldc.i4.0 
	IL_000c:  ret 
	IL_000d:  ldarg.0 
	IL_000e:  call instance native int class GLib.Opaque::get_Handle()
	IL_0013:  ldarg.1 
	IL_0014:  castclass GLib.Opaque
	IL_0019:  callvirt instance native int class GLib.Opaque::get_Handle()
	IL_001e:  call bool native int::op_Equality(native int, native int)
	IL_0023:  ret 
    } // end of method Opaque::Equals

    // method line 567
    .method public virtual hidebysig 
           instance default int32 GetHashCode ()  cil managed 
    {
        // Method begins at RVA 0x6794
	// Code size 21 (0x15)
	.maxstack 1
	.locals init (
		native int	V_0)
	IL_0000:  ldarg.0 
	IL_0001:  call instance native int class GLib.Opaque::get_Handle()
	IL_0006:  stloc.0 
	IL_0007:  ldloca.s 0
	IL_0009:  constrained. native int
	IL_000f:  callvirt instance int32 object::GetHashCode()
	IL_0014:  ret 
    } // end of method Opaque::GetHashCode

    // method line 568
    .method private static hidebysig specialname rtspecialname 
           default void '.cctor' ()  cil managed 
    {
        // Method begins at RVA 0x67b8
	// Code size 21 (0x15)
	.maxstack 8
	IL_0000:  newobj instance void object::'.ctor'()
	IL_0005:  stsfld object GLib.Opaque::lockObject
	IL_000a:  newobj instance void class [mscorlib]System.Collections.Generic.List`1<class GLib.Opaque>::'.ctor'()
	IL_000f:  stsfld class [mscorlib]System.Collections.Generic.List`1<class GLib.Opaque> GLib.Opaque::PendingFrees
	IL_0014:  ret 
    } // end of method Opaque::.cctor

	.property instance native int Raw ()
	{
		.get instance default native int GLib.Opaque::get_Raw () 
		.set instance default void GLib.Opaque::set_Raw (native int 'value') 
	}
	.property instance native int Handle ()
	{
		.get instance default native int GLib.Opaque::get_Handle () 
	}
	.property instance native int OwnedCopy ()
	{
		.custom instance void class [mscorlib]System.Diagnostics.DebuggerBrowsableAttribute::'.ctor'(valuetype [mscorlib]System.Diagnostics.DebuggerBrowsableState) =  (01 00 00 00 00 00 00 00 ) // ........

		.get instance default native int GLib.Opaque::get_OwnedCopy () 
	}
	.property instance bool Owned ()
	{
		.get instance default bool GLib.Opaque::get_Owned () 
		.set instance default void GLib.Opaque::set_Owned (bool 'value') 
	}
  } // end of class GLib.Opaque
}

.namespace GLib
{
  .class public auto ansi sealed beforefieldinit PropertyAttribute
  	extends [mscorlib]System.Attribute
  {
    .field  private  string blurb
    .field  private  string nickname
    .field  private  string name

    // method line 569
    .method public hidebysig specialname 
           instance default string get_Name ()  cil managed 
    {
        // Method begins at RVA 0x67d0
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldfld string GLib.PropertyAttribute::name
	IL_0006:  ret 
    } // end of method PropertyAttribute::get_Name

    // method line 570
    .method public hidebysig specialname 
           instance default string get_Nickname ()  cil managed 
    {
        // Method begins at RVA 0x67d8
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldfld string GLib.PropertyAttribute::nickname
	IL_0006:  ret 
    } // end of method PropertyAttribute::get_Nickname

    // method line 571
    .method public hidebysig specialname 
           instance default string get_Blurb ()  cil managed 
    {
        // Method begins at RVA 0x67e0
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldfld string GLib.PropertyAttribute::blurb
	IL_0006:  ret 
    } // end of method PropertyAttribute::get_Blurb

    // method line 572
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (string name)  cil managed 
    {
        // Method begins at RVA 0x67e8
	// Code size 14 (0xe)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void class [mscorlib]System.Attribute::'.ctor'()
	IL_0006:  ldarg.0 
	IL_0007:  ldarg.1 
	IL_0008:  stfld string GLib.PropertyAttribute::name
	IL_000d:  ret 
    } // end of method PropertyAttribute::.ctor

    // method line 573
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (string name, string nickname, string blurb)  cil managed 
    {
        // Method begins at RVA 0x67f8
	// Code size 28 (0x1c)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void class [mscorlib]System.Attribute::'.ctor'()
	IL_0006:  ldarg.0 
	IL_0007:  ldarg.1 
	IL_0008:  stfld string GLib.PropertyAttribute::name
	IL_000d:  ldarg.0 
	IL_000e:  ldarg.2 
	IL_000f:  stfld string GLib.PropertyAttribute::nickname
	IL_0014:  ldarg.0 
	IL_0015:  ldarg.3 
	IL_0016:  stfld string GLib.PropertyAttribute::blurb
	IL_001b:  ret 
    } // end of method PropertyAttribute::.ctor

    // method line 574
    .method public hidebysig specialname 
           instance default void set_Blurb (string 'value')  cil managed 
    {
        // Method begins at RVA 0x6818
	// Code size 8 (0x8)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldarg.1 
	IL_0002:  stfld string GLib.PropertyAttribute::blurb
	IL_0007:  ret 
    } // end of method PropertyAttribute::set_Blurb

    // method line 575
    .method public hidebysig specialname 
           instance default void set_Name (string 'value')  cil managed 
    {
        // Method begins at RVA 0x6824
	// Code size 8 (0x8)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldarg.1 
	IL_0002:  stfld string GLib.PropertyAttribute::name
	IL_0007:  ret 
    } // end of method PropertyAttribute::set_Name

    // method line 576
    .method public hidebysig specialname 
           instance default void set_Nickname (string 'value')  cil managed 
    {
        // Method begins at RVA 0x6830
	// Code size 8 (0x8)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldarg.1 
	IL_0002:  stfld string GLib.PropertyAttribute::nickname
	IL_0007:  ret 
    } // end of method PropertyAttribute::set_Nickname

	.property instance string Blurb ()
	{
		.set instance default void GLib.PropertyAttribute::set_Blurb (string 'value') 
		.get instance default string GLib.PropertyAttribute::get_Blurb () 
	}
	.property instance string Name ()
	{
		.set instance default void GLib.PropertyAttribute::set_Name (string 'value') 
		.get instance default string GLib.PropertyAttribute::get_Name () 
	}
	.property instance string Nickname ()
	{
		.get instance default string GLib.PropertyAttribute::get_Nickname () 
		.set instance default void GLib.PropertyAttribute::set_Nickname (string 'value') 
	}
  } // end of class GLib.PropertyAttribute
}

.namespace GLib
{
  .class public auto ansi beforefieldinit PtrArray
  	extends [mscorlib]System.Object
  	implements [mscorlib]System.IDisposable, [mscorlib]System.Collections.ICollection, [mscorlib]System.ICloneable, GLib.IWrapper, [mscorlib]System.Collections.IEnumerable  {
    .custom instance void class [mscorlib]System.Reflection.DefaultMemberAttribute::'.ctor'(string) =  (01 00 04 49 74 65 6D 00 00 ) // ...Item..

    .field  private  native int handle
    .field  private  bool 'managed'
    .field  assembly  bool elements_owned
    .field  family  class [mscorlib]System.Type element_type
    .field  private  class GLib.ListElementFree free_func
    .field  private static  class GLib.ListElementFree '<>f__mg$cache0'
    .custom instance void class [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::'.ctor'() =  (01 00 00 00 ) // ....


    // method line 577
    .method assembly hidebysig specialname rtspecialname 
           instance default void '.ctor' (native int raw, class [mscorlib]System.Type element_type, bool owned, bool elements_owned)  cil managed 
    {
        // Method begins at RVA 0x683c
	// Code size 41 (0x29)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldarg.1 
	IL_0002:  ldarg.2 
	IL_0003:  ldarg.3 
	IL_0004:  ldarg.s 4
	IL_0006:  ldsfld class GLib.ListElementFree GLib.PtrArray::'<>f__mg$cache0'
	IL_000b:  brtrue.s IL_001e

	IL_000d:  ldnull 
	IL_000e:  ldftn void class GLib.PtrArray::g_free(native int)
	IL_0014:  newobj instance void class GLib.ListElementFree::'.ctor'(object, native int)
	IL_0019:  stsfld class GLib.ListElementFree GLib.PtrArray::'<>f__mg$cache0'
	IL_001e:  ldsfld class GLib.ListElementFree GLib.PtrArray::'<>f__mg$cache0'
	IL_0023:  call instance void class GLib.PtrArray::'.ctor'(native int, class [mscorlib]System.Type, bool, bool, class GLib.ListElementFree)
	IL_0028:  ret 
    } // end of method PtrArray::.ctor

    // method line 578
    .method public final virtual hidebysig newslot specialname 
           instance default int32 get_Count ()  cil managed 
    {
        // Method begins at RVA 0x6868
	// Code size 12 (0xc)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance native int class GLib.PtrArray::get_Handle()
	IL_0006:  call int32 class GLib.PtrArray::gtksharp_ptr_array_get_count(native int)
	IL_000b:  ret 
    } // end of method PtrArray::get_Count

    // method line 579
    .method public final virtual hidebysig newslot 
           instance default void CopyTo (class [mscorlib]System.Array 'array', int32 index)  cil managed 
    {
        // Method begins at RVA 0x6878
	// Code size 105 (0x69)
	.maxstack 4
	.locals init (
		int32	V_0)
	IL_0000:  ldarg.1 
	IL_0001:  brtrue IL_0011

	IL_0006:  ldstr "Array can't be null."
	IL_000b:  newobj instance void class [mscorlib]System.ArgumentNullException::'.ctor'(string)
	IL_0010:  throw 
	IL_0011:  ldarg.2 
	IL_0012:  ldc.i4.0 
	IL_0013:  bge IL_0023

	IL_0018:  ldstr "Index must be greater than 0."
	IL_001d:  newobj instance void class [mscorlib]System.ArgumentOutOfRangeException::'.ctor'(string)
	IL_0022:  throw 
	IL_0023:  ldarg.2 
	IL_0024:  ldarg.0 
	IL_0025:  call instance int32 class GLib.PtrArray::get_Count()
	IL_002a:  add 
	IL_002b:  ldarg.1 
	IL_002c:  callvirt instance int32 class [mscorlib]System.Array::get_Length()
	IL_0031:  bge IL_0041

	IL_0036:  ldstr "Array not large enough to copy into starting at index."
	IL_003b:  newobj instance void class [mscorlib]System.ArgumentException::'.ctor'(string)
	IL_0040:  throw 
	IL_0041:  ldc.i4.0 
	IL_0042:  stloc.0 
	IL_0043:  br IL_005c

	IL_0048:  ldarg.1 
	IL_0049:  ldarg.2 
	IL_004a:  ldloc.0 
	IL_004b:  add 
	IL_004c:  ldarg.0 
	IL_004d:  ldloc.0 
	IL_004e:  call instance object class GLib.PtrArray::get_Item(int32)
	IL_0053:  callvirt instance void class [mscorlib]System.Collections.IList::set_Item(int32, object)
	IL_0058:  ldloc.0 
	IL_0059:  ldc.i4.1 
	IL_005a:  add 
	IL_005b:  stloc.0 
	IL_005c:  ldloc.0 
	IL_005d:  ldarg.0 
	IL_005e:  call instance int32 class GLib.PtrArray::get_Count()
	IL_0063:  blt IL_0048

	IL_0068:  ret 
    } // end of method PtrArray::CopyTo

    // method line 580
    .method public final virtual hidebysig newslot 
           instance default void Dispose ()  cil managed 
    {
        // Method begins at RVA 0x68f0
	// Code size 14 (0xe)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldc.i4.1 
	IL_0002:  call instance void class GLib.PtrArray::Dispose(bool)
	IL_0007:  ldarg.0 
	IL_0008:  call void class [mscorlib]System.GC::SuppressFinalize(object)
	IL_000d:  ret 
    } // end of method PtrArray::Dispose

    // method line 581
    .method assembly hidebysig specialname rtspecialname 
           instance default void '.ctor' (native int raw, class [mscorlib]System.Type element_type, bool owned, bool elements_owned, class GLib.ListElementFree free_func)  cil managed 
    {
        // Method begins at RVA 0x6900
	// Code size 76 (0x4c)
	.maxstack 2
	IL_0000:  ldarg.0 
	IL_0001:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_0006:  stfld native int GLib.PtrArray::handle
	IL_000b:  ldarg.0 
	IL_000c:  ldc.i4.0 
	IL_000d:  stfld bool GLib.PtrArray::'managed'
	IL_0012:  ldarg.0 
	IL_0013:  ldc.i4.0 
	IL_0014:  stfld bool GLib.PtrArray::elements_owned
	IL_0019:  ldarg.0 
	IL_001a:  ldnull 
	IL_001b:  stfld class [mscorlib]System.Type GLib.PtrArray::element_type
	IL_0020:  ldarg.0 
	IL_0021:  call instance void object::'.ctor'()
	IL_0026:  ldarg.0 
	IL_0027:  ldarg.1 
	IL_0028:  stfld native int GLib.PtrArray::handle
	IL_002d:  ldarg.0 
	IL_002e:  ldarg.2 
	IL_002f:  stfld class [mscorlib]System.Type GLib.PtrArray::element_type
	IL_0034:  ldarg.0 
	IL_0035:  ldarg.3 
	IL_0036:  stfld bool GLib.PtrArray::'managed'
	IL_003b:  ldarg.0 
	IL_003c:  ldarg.s 4
	IL_003e:  stfld bool GLib.PtrArray::elements_owned
	IL_0043:  ldarg.0 
	IL_0044:  ldarg.s 5
	IL_0046:  stfld class GLib.ListElementFree GLib.PtrArray::free_func
	IL_004b:  ret 
    } // end of method PtrArray::.ctor

    // method line 582
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (unsigned int32 n_preallocs, class [mscorlib]System.Type element_type, bool owned, bool elements_owned)  cil managed 
    {
        // Method begins at RVA 0x6958
	// Code size 73 (0x49)
	.maxstack 2
	IL_0000:  ldarg.0 
	IL_0001:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_0006:  stfld native int GLib.PtrArray::handle
	IL_000b:  ldarg.0 
	IL_000c:  ldc.i4.0 
	IL_000d:  stfld bool GLib.PtrArray::'managed'
	IL_0012:  ldarg.0 
	IL_0013:  ldc.i4.0 
	IL_0014:  stfld bool GLib.PtrArray::elements_owned
	IL_0019:  ldarg.0 
	IL_001a:  ldnull 
	IL_001b:  stfld class [mscorlib]System.Type GLib.PtrArray::element_type
	IL_0020:  ldarg.0 
	IL_0021:  call instance void object::'.ctor'()
	IL_0026:  ldarg.0 
	IL_0027:  ldarg.1 
	IL_0028:  call native int class GLib.PtrArray::g_ptr_array_sized_new(unsigned int32)
	IL_002d:  stfld native int GLib.PtrArray::handle
	IL_0032:  ldarg.0 
	IL_0033:  ldarg.2 
	IL_0034:  stfld class [mscorlib]System.Type GLib.PtrArray::element_type
	IL_0039:  ldarg.0 
	IL_003a:  ldarg.3 
	IL_003b:  stfld bool GLib.PtrArray::'managed'
	IL_0040:  ldarg.0 
	IL_0041:  ldarg.s 4
	IL_0043:  stfld bool GLib.PtrArray::elements_owned
	IL_0048:  ret 
    } // end of method PtrArray::.ctor

    // method line 583
    .method private static hidebysig pinvokeimpl ("libgobject-2.0-0.dll" as "g_ptr_array_sized_new" cdecl )
           default native int g_ptr_array_sized_new (unsigned int32 n_preallocs)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method PtrArray::g_ptr_array_sized_new

    // method line 584
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (class [mscorlib]System.Type element_type, bool owned, bool elements_owned)  cil managed 
    {
        // Method begins at RVA 0x69b0
	// Code size 71 (0x47)
	.maxstack 2
	IL_0000:  ldarg.0 
	IL_0001:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_0006:  stfld native int GLib.PtrArray::handle
	IL_000b:  ldarg.0 
	IL_000c:  ldc.i4.0 
	IL_000d:  stfld bool GLib.PtrArray::'managed'
	IL_0012:  ldarg.0 
	IL_0013:  ldc.i4.0 
	IL_0014:  stfld bool GLib.PtrArray::elements_owned
	IL_0019:  ldarg.0 
	IL_001a:  ldnull 
	IL_001b:  stfld class [mscorlib]System.Type GLib.PtrArray::element_type
	IL_0020:  ldarg.0 
	IL_0021:  call instance void object::'.ctor'()
	IL_0026:  ldarg.0 
	IL_0027:  call native int class GLib.PtrArray::g_ptr_array_new()
	IL_002c:  stfld native int GLib.PtrArray::handle
	IL_0031:  ldarg.0 
	IL_0032:  ldarg.1 
	IL_0033:  stfld class [mscorlib]System.Type GLib.PtrArray::element_type
	IL_0038:  ldarg.0 
	IL_0039:  ldarg.2 
	IL_003a:  stfld bool GLib.PtrArray::'managed'
	IL_003f:  ldarg.0 
	IL_0040:  ldarg.3 
	IL_0041:  stfld bool GLib.PtrArray::elements_owned
	IL_0046:  ret 
    } // end of method PtrArray::.ctor

    // method line 585
    .method private static hidebysig pinvokeimpl ("libgobject-2.0-0.dll" as "g_ptr_array_new" cdecl )
           default native int g_ptr_array_new ()  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method PtrArray::g_ptr_array_new

    // method line 586
    .method private static hidebysig pinvokeimpl ("libglib-2.0-0.dll" as "g_free" cdecl )
           default void g_free (native int item)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method PtrArray::g_free

    // method line 587
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (native int raw, class [mscorlib]System.Type element_type)  cil managed 
    {
        // Method begins at RVA 0x6a04
	// Code size 11 (0xb)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldarg.1 
	IL_0002:  ldarg.2 
	IL_0003:  ldc.i4.0 
	IL_0004:  ldc.i4.0 
	IL_0005:  call instance void class GLib.PtrArray::'.ctor'(native int, class [mscorlib]System.Type, bool, bool)
	IL_000a:  ret 
    } // end of method PtrArray::.ctor

    // method line 588
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (native int raw)  cil managed 
    {
        // Method begins at RVA 0x6a10
	// Code size 9 (0x9)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldarg.1 
	IL_0002:  ldnull 
	IL_0003:  call instance void class GLib.PtrArray::'.ctor'(native int, class [mscorlib]System.Type)
	IL_0008:  ret 
    } // end of method PtrArray::.ctor

    // method line 589
    .method family virtual hidebysig 
           instance default void Finalize ()  cil managed 
    {
        // Method begins at RVA 0x6a1c
	// Code size 20 (0x14)
	.maxstack 2
	.try { // 0
	  IL_0000:  ldarg.0 
	  IL_0001:  ldc.i4.0 
	  IL_0002:  call instance void class GLib.PtrArray::Dispose(bool)
	  IL_0007:  leave IL_0013

	} // end .try 0
	finally  { // 0
	  IL_000c:  ldarg.0 
	  IL_000d:  call instance void object::Finalize()
	  IL_0012:  endfinally 
	} // end handler 0
	IL_0013:  ret 
    } // end of method PtrArray::Finalize

    // method line 590
    .method private hidebysig 
           instance default void Dispose (bool disposing)  cil managed 
    {
        // Method begins at RVA 0x6a4c
	// Code size 211 (0xd3)
	.maxstack 3
	.locals init (
		int32	V_0,
		unsigned int32	V_1)
	IL_0000:  ldarg.0 
	IL_0001:  call instance native int class GLib.PtrArray::get_Handle()
	IL_0006:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_000b:  call bool native int::op_Equality(native int, native int)
	IL_0010:  brfalse IL_0016

	IL_0015:  ret 
	IL_0016:  ldarg.0 
	IL_0017:  ldfld bool GLib.PtrArray::elements_owned
	IL_001c:  brfalse IL_00b0

	IL_0021:  ldarg.0 
	IL_0022:  call instance int32 class GLib.PtrArray::get_Count()
	IL_0027:  stloc.0 
	IL_0028:  ldc.i4.0 
	IL_0029:  stloc.1 
	IL_002a:  br IL_00a7

	IL_002f:  ldtoken GLib.Object
	IL_0034:  call class [mscorlib]System.Type class [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
	IL_0039:  ldarg.0 
	IL_003a:  ldfld class [mscorlib]System.Type GLib.PtrArray::element_type
	IL_003f:  callvirt instance bool class [mscorlib]System.Type::IsAssignableFrom(class [mscorlib]System.Type)
	IL_0044:  brfalse IL_005a

	IL_0049:  ldarg.0 
	IL_004a:  ldloc.1 
	IL_004b:  call instance native int class GLib.PtrArray::NthData(unsigned int32)
	IL_0050:  call void class GLib.PtrArray::g_object_unref(native int)
	IL_0055:  br IL_00a3

	IL_005a:  ldtoken GLib.Opaque
	IL_005f:  call class [mscorlib]System.Type class [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
	IL_0064:  ldarg.0 
	IL_0065:  ldfld class [mscorlib]System.Type GLib.PtrArray::element_type
	IL_006a:  callvirt instance bool class [mscorlib]System.Type::IsAssignableFrom(class [mscorlib]System.Type)
	IL_006f:  brfalse IL_0091

	IL_0074:  ldarg.0 
	IL_0075:  ldloc.1 
	IL_0076:  call instance native int class GLib.PtrArray::NthData(unsigned int32)
	IL_007b:  ldarg.0 
	IL_007c:  ldfld class [mscorlib]System.Type GLib.PtrArray::element_type
	IL_0081:  ldc.i4.1 
	IL_0082:  call class GLib.Opaque class GLib.Opaque::GetOpaque(native int, class [mscorlib]System.Type, bool)
	IL_0087:  callvirt instance void class GLib.Opaque::Dispose()
	IL_008c:  br IL_00a3

	IL_0091:  ldarg.0 
	IL_0092:  ldfld class GLib.ListElementFree GLib.PtrArray::free_func
	IL_0097:  ldarg.0 
	IL_0098:  ldloc.1 
	IL_0099:  call instance native int class GLib.PtrArray::NthData(unsigned int32)
	IL_009e:  callvirt instance void class GLib.ListElementFree::Invoke(native int)
	IL_00a3:  ldloc.1 
	IL_00a4:  ldc.i4.1 
	IL_00a5:  add 
	IL_00a6:  stloc.1 
	IL_00a7:  ldloc.1 
	IL_00a8:  conv.u8 
	IL_00a9:  ldloc.0 
	IL_00aa:  conv.i8 
	IL_00ab:  blt IL_002f

	IL_00b0:  ldarg.0 
	IL_00b1:  ldfld bool GLib.PtrArray::'managed'
	IL_00b6:  brfalse IL_00c7

	IL_00bb:  ldarg.0 
	IL_00bc:  call instance native int class GLib.PtrArray::get_Handle()
	IL_00c1:  ldc.i4.1 
	IL_00c2:  call void class GLib.PtrArray::g_ptr_array_free(native int, bool)
	IL_00c7:  ldarg.0 
	IL_00c8:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_00cd:  stfld native int GLib.PtrArray::handle
	IL_00d2:  ret 
    } // end of method PtrArray::Dispose

    // method line 591
    .method private static hidebysig pinvokeimpl ("libgobject-2.0-0.dll" as "g_ptr_array_free" cdecl )
           default void g_ptr_array_free (native int raw, bool free_seg)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method PtrArray::g_ptr_array_free

    // method line 592
    .method private static hidebysig pinvokeimpl ("libglib-2.0-0.dll" as "g_object_unref" cdecl )
           default void g_object_unref (native int item)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method PtrArray::g_object_unref

    // method line 593
    .method public final virtual hidebysig newslot specialname 
           instance default native int get_Handle ()  cil managed 
    {
        // Method begins at RVA 0x6b2c
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldfld native int GLib.PtrArray::handle
	IL_0006:  ret 
    } // end of method PtrArray::get_Handle

    // method line 594
    .method assembly hidebysig 
           instance default native int NthData (unsigned int32 index)  cil managed 
    {
        // Method begins at RVA 0x6b34
	// Code size 13 (0xd)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance native int class GLib.PtrArray::get_Handle()
	IL_0006:  ldarg.1 
	IL_0007:  call native int class GLib.PtrArray::gtksharp_ptr_array_get_nth(native int, unsigned int32)
	IL_000c:  ret 
    } // end of method PtrArray::NthData

    // method line 595
    .method private static hidebysig pinvokeimpl ("glibsharpglue-2" as "gtksharp_ptr_array_get_array" cdecl )
           default native int gtksharp_ptr_array_get_array (native int raw)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method PtrArray::gtksharp_ptr_array_get_array

    // method line 596
    .method public hidebysig specialname 
           instance default native int get_ArrayPtr ()  cil managed 
    {
        // Method begins at RVA 0x6b44
	// Code size 12 (0xc)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance native int class GLib.PtrArray::get_Handle()
	IL_0006:  call native int class GLib.PtrArray::gtksharp_ptr_array_get_array(native int)
	IL_000b:  ret 
    } // end of method PtrArray::get_ArrayPtr

    // method line 597
    .method private static hidebysig pinvokeimpl ("libgobject-2.0-0.dll" as "g_ptr_array_add" cdecl )
           default void g_ptr_array_add (native int raw, native int val)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method PtrArray::g_ptr_array_add

    // method line 598
    .method public hidebysig 
           instance default void Add (native int val)  cil managed 
    {
        // Method begins at RVA 0x6b54
	// Code size 13 (0xd)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance native int class GLib.PtrArray::get_Handle()
	IL_0006:  ldarg.1 
	IL_0007:  call void class GLib.PtrArray::g_ptr_array_add(native int, native int)
	IL_000c:  ret 
    } // end of method PtrArray::Add

    // method line 599
    .method private static hidebysig pinvokeimpl ("libgobject-2.0-0.dll" as "g_ptr_array_remove" cdecl )
           default void g_ptr_array_remove (native int raw, native int data)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method PtrArray::g_ptr_array_remove

    // method line 600
    .method public hidebysig 
           instance default void Remove (native int data)  cil managed 
    {
        // Method begins at RVA 0x6b64
	// Code size 13 (0xd)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance native int class GLib.PtrArray::get_Handle()
	IL_0006:  ldarg.1 
	IL_0007:  call void class GLib.PtrArray::g_ptr_array_remove(native int, native int)
	IL_000c:  ret 
    } // end of method PtrArray::Remove

    // method line 601
    .method private static hidebysig pinvokeimpl ("libgobject-2.0-0.dll" as "g_ptr_array_remove_range" cdecl )
           default void g_ptr_array_remove_range (native int raw, unsigned int32 index, unsigned int32 length)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method PtrArray::g_ptr_array_remove_range

    // method line 602
    .method public hidebysig 
           instance default void RemoveRange (native int data, unsigned int32 index, unsigned int32 length)  cil managed 
    {
        // Method begins at RVA 0x6b74
	// Code size 14 (0xe)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance native int class GLib.PtrArray::get_Handle()
	IL_0006:  ldarg.2 
	IL_0007:  ldarg.3 
	IL_0008:  call void class GLib.PtrArray::g_ptr_array_remove_range(native int, unsigned int32, unsigned int32)
	IL_000d:  ret 
    } // end of method PtrArray::RemoveRange

    // method line 603
    .method private static hidebysig pinvokeimpl ("glibsharpglue-2" as "gtksharp_ptr_array_get_count" cdecl )
           default int32 gtksharp_ptr_array_get_count (native int raw)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method PtrArray::gtksharp_ptr_array_get_count

    // method line 604
    .method private static hidebysig pinvokeimpl ("glibsharpglue-2" as "gtksharp_ptr_array_get_nth" cdecl )
           default native int gtksharp_ptr_array_get_nth (native int raw, unsigned int32 idx)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method PtrArray::gtksharp_ptr_array_get_nth

    // method line 605
    .method public hidebysig specialname 
           instance default object get_Item (int32 index)  cil managed 
    {
        // Method begins at RVA 0x6b84
	// Code size 20 (0x14)
	.maxstack 2
	.locals init (
		native int	V_0,
		object	V_1)
	IL_0000:  ldarg.0 
	IL_0001:  ldarg.1 
	IL_0002:  call instance native int class GLib.PtrArray::NthData(unsigned int32)
	IL_0007:  stloc.0 
	IL_0008:  ldnull 
	IL_0009:  stloc.1 
	IL_000a:  ldarg.0 
	IL_000b:  ldloc.0 
	IL_000c:  call instance object class GLib.PtrArray::DataMarshal(native int)
	IL_0011:  stloc.1 
	IL_0012:  ldloc.1 
	IL_0013:  ret 
    } // end of method PtrArray::get_Item

    // method line 606
    .method assembly hidebysig 
           instance default object DataMarshal (native int data)  cil managed 
    {
        // Method begins at RVA 0x6ba4
	// Code size 314 (0x13a)
	.maxstack 5
	.locals init (
		object	V_0)
	IL_0000:  ldnull 
	IL_0001:  stloc.0 
	IL_0002:  ldarg.0 
	IL_0003:  ldfld class [mscorlib]System.Type GLib.PtrArray::element_type
	IL_0008:  ldnull 
	IL_0009:  call bool class [mscorlib]System.Type::op_Inequality(class [mscorlib]System.Type, class [mscorlib]System.Type)
	IL_000e:  brfalse IL_0125

	IL_0013:  ldarg.0 
	IL_0014:  ldfld class [mscorlib]System.Type GLib.PtrArray::element_type
	IL_0019:  ldtoken string
	IL_001e:  call class [mscorlib]System.Type class [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
	IL_0023:  call bool class [mscorlib]System.Type::op_Equality(class [mscorlib]System.Type, class [mscorlib]System.Type)
	IL_0028:  brfalse IL_0039

	IL_002d:  ldarg.1 
	IL_002e:  call string class GLib.Marshaller::Utf8PtrToString(native int)
	IL_0033:  stloc.0 
	IL_0034:  br IL_0120

	IL_0039:  ldarg.0 
	IL_003a:  ldfld class [mscorlib]System.Type GLib.PtrArray::element_type
	IL_003f:  ldtoken native int
	IL_0044:  call class [mscorlib]System.Type class [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
	IL_0049:  call bool class [mscorlib]System.Type::op_Equality(class [mscorlib]System.Type, class [mscorlib]System.Type)
	IL_004e:  brfalse IL_005f

	IL_0053:  ldarg.1 
	IL_0054:  box native int
	IL_0059:  stloc.0 
	IL_005a:  br IL_0120

	IL_005f:  ldarg.0 
	IL_0060:  ldfld class [mscorlib]System.Type GLib.PtrArray::element_type
	IL_0065:  ldtoken GLib.Object
	IL_006a:  call class [mscorlib]System.Type class [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
	IL_006f:  callvirt instance bool class [mscorlib]System.Type::IsSubclassOf(class [mscorlib]System.Type)
	IL_0074:  brfalse IL_0086

	IL_0079:  ldarg.1 
	IL_007a:  ldc.i4.0 
	IL_007b:  call class GLib.Object class GLib.Object::GetObject(native int, bool)
	IL_0080:  stloc.0 
	IL_0081:  br IL_0120

	IL_0086:  ldarg.0 
	IL_0087:  ldfld class [mscorlib]System.Type GLib.PtrArray::element_type
	IL_008c:  ldtoken GLib.Opaque
	IL_0091:  call class [mscorlib]System.Type class [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
	IL_0096:  callvirt instance bool class [mscorlib]System.Type::IsSubclassOf(class [mscorlib]System.Type)
	IL_009b:  brfalse IL_00b8

	IL_00a0:  ldarg.1 
	IL_00a1:  ldarg.0 
	IL_00a2:  ldfld class [mscorlib]System.Type GLib.PtrArray::element_type
	IL_00a7:  ldarg.0 
	IL_00a8:  ldfld bool GLib.PtrArray::elements_owned
	IL_00ad:  call class GLib.Opaque class GLib.Opaque::GetOpaque(native int, class [mscorlib]System.Type, bool)
	IL_00b2:  stloc.0 
	IL_00b3:  br IL_0120

	IL_00b8:  ldarg.0 
	IL_00b9:  ldfld class [mscorlib]System.Type GLib.PtrArray::element_type
	IL_00be:  ldtoken int32
	IL_00c3:  call class [mscorlib]System.Type class [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
	IL_00c8:  call bool class [mscorlib]System.Type::op_Equality(class [mscorlib]System.Type, class [mscorlib]System.Type)
	IL_00cd:  brfalse IL_00e3

	IL_00d2:  ldarg.1 
	IL_00d3:  call int32 native int::op_Explicit(native int)
	IL_00d8:  box int32
	IL_00dd:  stloc.0 
	IL_00de:  br IL_0120

	IL_00e3:  ldarg.0 
	IL_00e4:  ldfld class [mscorlib]System.Type GLib.PtrArray::element_type
	IL_00e9:  callvirt instance bool class [mscorlib]System.Type::get_IsValueType()
	IL_00ee:  brfalse IL_0105

	IL_00f3:  ldarg.1 
	IL_00f4:  ldarg.0 
	IL_00f5:  ldfld class [mscorlib]System.Type GLib.PtrArray::element_type
	IL_00fa:  call object class [mscorlib]System.Runtime.InteropServices.Marshal::PtrToStructure(native int, class [mscorlib]System.Type)
	IL_00ff:  stloc.0 
	IL_0100:  br IL_0120

	IL_0105:  ldarg.0 
	IL_0106:  ldfld class [mscorlib]System.Type GLib.PtrArray::element_type
	IL_010b:  ldc.i4.1 
	IL_010c:  newarr object
	IL_0111:  dup 
	IL_0112:  ldc.i4.0 
	IL_0113:  ldarg.1 
	IL_0114:  box native int
	IL_0119:  stelem.ref 
	IL_011a:  call object class [mscorlib]System.Activator::CreateInstance(class [mscorlib]System.Type, object[])
	IL_011f:  stloc.0 
	IL_0120:  br IL_0138

	IL_0125:  ldarg.1 
	IL_0126:  call bool class GLib.Object::IsObject(native int)
	IL_012b:  brfalse IL_0138

	IL_0130:  ldarg.1 
	IL_0131:  ldc.i4.0 
	IL_0132:  call class GLib.Object class GLib.Object::GetObject(native int, bool)
	IL_0137:  stloc.0 
	IL_0138:  ldloc.0 
	IL_0139:  ret 
    } // end of method PtrArray::DataMarshal

    // method line 607
    .method public final virtual hidebysig newslot specialname 
           instance default bool get_IsSynchronized ()  cil managed 
    {
        // Method begins at RVA 0x6cec
	// Code size 2 (0x2)
	.maxstack 8
	IL_0000:  ldc.i4.0 
	IL_0001:  ret 
    } // end of method PtrArray::get_IsSynchronized

    // method line 608
    .method public final virtual hidebysig newslot specialname 
           instance default object get_SyncRoot ()  cil managed 
    {
        // Method begins at RVA 0x6cf0
	// Code size 2 (0x2)
	.maxstack 8
	IL_0000:  ldnull 
	IL_0001:  ret 
    } // end of method PtrArray::get_SyncRoot

    // method line 609
    .method public final virtual hidebysig newslot 
           instance default class [mscorlib]System.Collections.IEnumerator GetEnumerator ()  cil managed 
    {
        // Method begins at RVA 0x6cf4
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  newobj instance void class GLib.PtrArray/ListEnumerator::'.ctor'(class GLib.PtrArray)
	IL_0006:  ret 
    } // end of method PtrArray::GetEnumerator

    // method line 610
    .method private static hidebysig pinvokeimpl ("libgobject-2.0-0.dll" as "g_ptr_array_copy" cdecl )
           default native int g_ptr_array_copy (native int raw)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method PtrArray::g_ptr_array_copy

    // method line 611
    .method public final virtual hidebysig newslot 
           instance default object Clone ()  cil managed 
    {
        // Method begins at RVA 0x6cfc
	// Code size 25 (0x19)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance native int class GLib.PtrArray::get_Handle()
	IL_0006:  call native int class GLib.PtrArray::g_ptr_array_copy(native int)
	IL_000b:  ldarg.0 
	IL_000c:  ldfld class [mscorlib]System.Type GLib.PtrArray::element_type
	IL_0011:  ldc.i4.0 
	IL_0012:  ldc.i4.0 
	IL_0013:  newobj instance void class GLib.PtrArray::'.ctor'(native int, class [mscorlib]System.Type, bool, bool)
	IL_0018:  ret 
    } // end of method PtrArray::Clone

	.property instance native int Handle ()
	{
		.get instance default native int GLib.PtrArray::get_Handle () 
	}
	.property instance native int ArrayPtr ()
	{
		.get instance default native int GLib.PtrArray::get_ArrayPtr () 
	}
	.property instance int32 Count ()
	{
		.get instance default int32 GLib.PtrArray::get_Count () 
	}
	.property instance object Item (int32)
	{
		.get instance default object GLib.PtrArray::get_Item (int32 index) 
	}
	.property instance bool IsSynchronized ()
	{
		.get instance default bool GLib.PtrArray::get_IsSynchronized () 
	}
	.property instance object SyncRoot ()
	{
		.get instance default object GLib.PtrArray::get_SyncRoot () 
	}
  .class nested private auto ansi beforefieldinit ListEnumerator
  	extends [mscorlib]System.Object
  	implements [mscorlib]System.Collections.IEnumerator  {
    .field  private  int32 current
    .field  private  class GLib.PtrArray vals

    // method line 612
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (class GLib.PtrArray vals)  cil managed 
    {
        // Method begins at RVA 0x6d18
	// Code size 21 (0x15)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldc.i4.m1 
	IL_0002:  stfld int32 GLib.PtrArray/ListEnumerator::current
	IL_0007:  ldarg.0 
	IL_0008:  call instance void object::'.ctor'()
	IL_000d:  ldarg.0 
	IL_000e:  ldarg.1 
	IL_000f:  stfld class GLib.PtrArray GLib.PtrArray/ListEnumerator::vals
	IL_0014:  ret 
    } // end of method ListEnumerator::.ctor

    // method line 613
    .method public final virtual hidebysig newslot specialname 
           instance default object get_Current ()  cil managed 
    {
        // Method begins at RVA 0x6d30
	// Code size 32 (0x20)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldfld int32 GLib.PtrArray/ListEnumerator::current
	IL_0006:  ldc.i4.m1 
	IL_0007:  bne.un IL_000e

	IL_000c:  ldnull 
	IL_000d:  ret 
	IL_000e:  ldarg.0 
	IL_000f:  ldfld class GLib.PtrArray GLib.PtrArray/ListEnumerator::vals
	IL_0014:  ldarg.0 
	IL_0015:  ldfld int32 GLib.PtrArray/ListEnumerator::current
	IL_001a:  callvirt instance object class GLib.PtrArray::get_Item(int32)
	IL_001f:  ret 
    } // end of method ListEnumerator::get_Current

    // method line 614
    .method public final virtual hidebysig newslot 
           instance default bool MoveNext ()  cil managed 
    {
        // Method begins at RVA 0x6d54
	// Code size 44 (0x2c)
	.maxstack 3
	.locals init (
		int32	V_0)
	IL_0000:  ldarg.0 
	IL_0001:  dup 
	IL_0002:  ldfld int32 GLib.PtrArray/ListEnumerator::current
	IL_0007:  ldc.i4.1 
	IL_0008:  add 
	IL_0009:  dup 
	IL_000a:  stloc.0 
	IL_000b:  stfld int32 GLib.PtrArray/ListEnumerator::current
	IL_0010:  ldloc.0 
	IL_0011:  ldarg.0 
	IL_0012:  ldfld class GLib.PtrArray GLib.PtrArray/ListEnumerator::vals
	IL_0017:  callvirt instance int32 class GLib.PtrArray::get_Count()
	IL_001c:  blt IL_002a

	IL_0021:  ldarg.0 
	IL_0022:  ldc.i4.m1 
	IL_0023:  stfld int32 GLib.PtrArray/ListEnumerator::current
	IL_0028:  ldc.i4.0 
	IL_0029:  ret 
	IL_002a:  ldc.i4.1 
	IL_002b:  ret 
    } // end of method ListEnumerator::MoveNext

    // method line 615
    .method public final virtual hidebysig newslot 
           instance default void Reset ()  cil managed 
    {
        // Method begins at RVA 0x6d8c
	// Code size 8 (0x8)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldc.i4.m1 
	IL_0002:  stfld int32 GLib.PtrArray/ListEnumerator::current
	IL_0007:  ret 
    } // end of method ListEnumerator::Reset

	.property instance object Current ()
	{
		.get instance default object GLib.PtrArray/ListEnumerator::get_Current () 
	}
  } // end of class ListEnumerator

  } // end of class GLib.PtrArray
}

.namespace GLib
{
  .class public auto ansi sealed ConnectFlags
  	extends [mscorlib]System.Enum
  {
    .custom instance void class [mscorlib]System.FlagsAttribute::'.ctor'() =  (01 00 00 00 ) // ....

    .field  public specialname rtspecialname  int32 value__
    .field public static literal  valuetype GLib.ConnectFlags After = int32(0x00000001)
    .field public static literal  valuetype GLib.ConnectFlags Swapped = int32(0x00000002)

  } // end of class GLib.ConnectFlags
}

.namespace GLib
{
  .class public auto ansi beforefieldinit Signal
  	extends [mscorlib]System.Object
  {
    .field  private  class GLib.ToggleRef tref
    .field  private  string name
    .field  private  class [mscorlib]System.Type args_type
    .field  private  class GLib.Signal/SignalClosure before_closure
    .field  private  class GLib.Signal/SignalClosure after_closure
    .field  private  class [mscorlib]System.Delegate marshaler
    .field  private static  class [mscorlib]System.EventHandler closure_disposed_cb
    .field  private static  class GLib.ClosureInvokedHandler closure_invoked_cb

    // method line 616
    .method public hidebysig specialname 
           instance default class [mscorlib]System.Delegate get_Handler ()  cil managed 
    {
        // Method begins at RVA 0x6d98
	// Code size 100 (0x64)
	.maxstack 2
	.locals init (
		valuetype GLib.Signal/InvocationHint*	V_0)
	IL_0000:  ldarg.0 
	IL_0001:  ldfld class GLib.ToggleRef GLib.Signal::tref
	IL_0006:  callvirt instance native int class GLib.ToggleRef::get_Handle()
	IL_000b:  call native int class GLib.Signal::g_signal_get_invocation_hint(native int)
	IL_0010:  call void* native int::op_Explicit(native int)
	IL_0015:  stloc.0 
	IL_0016:  ldloc.0 
	IL_0017:  ldfld valuetype GLib.Signal/Flags GLib.Signal/InvocationHint::run_type
	IL_001c:  ldc.i4.1 
	IL_001d:  bne.un IL_0043

	IL_0022:  ldarg.0 
	IL_0023:  ldfld class GLib.ToggleRef GLib.Signal::tref
	IL_0028:  callvirt instance class GLib.Object class GLib.ToggleRef::get_Target()
	IL_002d:  callvirt instance class [mscorlib]System.Collections.Hashtable class GLib.Object::get_BeforeSignals()
	IL_0032:  ldarg.0 
	IL_0033:  ldfld string GLib.Signal::name
	IL_0038:  callvirt instance object class [mscorlib]System.Collections.Hashtable::get_Item(object)
	IL_003d:  isinst [mscorlib]System.Delegate
	IL_0042:  ret 
	IL_0043:  ldarg.0 
	IL_0044:  ldfld class GLib.ToggleRef GLib.Signal::tref
	IL_0049:  callvirt instance class GLib.Object class GLib.ToggleRef::get_Target()
	IL_004e:  callvirt instance class [mscorlib]System.Collections.Hashtable class GLib.Object::get_AfterSignals()
	IL_0053:  ldarg.0 
	IL_0054:  ldfld string GLib.Signal::name
	IL_0059:  callvirt instance object class [mscorlib]System.Collections.Hashtable::get_Item(object)
	IL_005e:  isinst [mscorlib]System.Delegate
	IL_0063:  ret 
    } // end of method Signal::get_Handler

    // method line 617
    .method public static hidebysig 
           default class GLib.Signal Lookup (class GLib.Object obj, string name, class [mscorlib]System.Delegate marshaler)  cil managed 
    {
        // Method begins at RVA 0x6e08
	// Code size 40 (0x28)
	.maxstack 3
	.locals init (
		class GLib.Signal	V_0)
	IL_0000:  ldarg.0 
	IL_0001:  callvirt instance class GLib.ToggleRef class GLib.Object::get_ToggleRef()
	IL_0006:  callvirt instance class [mscorlib]System.Collections.Hashtable class GLib.ToggleRef::get_Signals()
	IL_000b:  ldarg.1 
	IL_000c:  callvirt instance object class [mscorlib]System.Collections.Hashtable::get_Item(object)
	IL_0011:  isinst GLib.Signal
	IL_0016:  stloc.0 
	IL_0017:  ldloc.0 
	IL_0018:  brtrue IL_0026

	IL_001d:  ldarg.0 
	IL_001e:  ldarg.1 
	IL_001f:  ldarg.2 
	IL_0020:  newobj instance void class GLib.Signal::'.ctor'(class GLib.Object, string, class [mscorlib]System.Delegate)
	IL_0025:  stloc.0 
	IL_0026:  ldloc.0 
	IL_0027:  ret 
    } // end of method Signal::Lookup

    // method line 618
    .method public hidebysig 
           instance default void AddDelegate (class [mscorlib]System.Delegate d)  cil managed 
    {
        // Method begins at RVA 0x6e3c
	// Code size 418 (0x1a2)
	.maxstack 4
	IL_0000:  ldarg.0 
	IL_0001:  ldfld class [mscorlib]System.Type GLib.Signal::args_type
	IL_0006:  ldnull 
	IL_0007:  call bool class [mscorlib]System.Type::op_Equality(class [mscorlib]System.Type, class [mscorlib]System.Type)
	IL_000c:  brfalse IL_0029

	IL_0011:  ldarg.0 
	IL_0012:  ldarg.1 
	IL_0013:  callvirt instance class [mscorlib]System.Reflection.MethodInfo class [mscorlib]System.Delegate::get_Method()
	IL_0018:  callvirt instance class [mscorlib]System.Reflection.ParameterInfo[] class [mscorlib]System.Reflection.MethodBase::GetParameters()
	IL_001d:  ldc.i4.1 
	IL_001e:  ldelem.ref 
	IL_001f:  callvirt instance class [mscorlib]System.Type class [mscorlib]System.Reflection.ParameterInfo::get_ParameterType()
	IL_0024:  stfld class [mscorlib]System.Type GLib.Signal::args_type
	IL_0029:  ldarg.1 
	IL_002a:  callvirt instance class [mscorlib]System.Reflection.MethodInfo class [mscorlib]System.Delegate::get_Method()
	IL_002f:  ldtoken GLib.ConnectBeforeAttribute
	IL_0034:  call class [mscorlib]System.Type class [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
	IL_0039:  ldc.i4.0 
	IL_003a:  callvirt instance bool class [mscorlib]System.Reflection.MemberInfo::IsDefined(class [mscorlib]System.Type, bool)
	IL_003f:  brfalse IL_00f5

	IL_0044:  ldarg.0 
	IL_0045:  ldfld class GLib.ToggleRef GLib.Signal::tref
	IL_004a:  callvirt instance class GLib.Object class GLib.ToggleRef::get_Target()
	IL_004f:  callvirt instance class [mscorlib]System.Collections.Hashtable class GLib.Object::get_BeforeSignals()
	IL_0054:  ldarg.0 
	IL_0055:  ldfld string GLib.Signal::name
	IL_005a:  ldarg.0 
	IL_005b:  ldfld class GLib.ToggleRef GLib.Signal::tref
	IL_0060:  callvirt instance class GLib.Object class GLib.ToggleRef::get_Target()
	IL_0065:  callvirt instance class [mscorlib]System.Collections.Hashtable class GLib.Object::get_BeforeSignals()
	IL_006a:  ldarg.0 
	IL_006b:  ldfld string GLib.Signal::name
	IL_0070:  callvirt instance object class [mscorlib]System.Collections.Hashtable::get_Item(object)
	IL_0075:  isinst [mscorlib]System.Delegate
	IL_007a:  ldarg.1 
	IL_007b:  call class [mscorlib]System.Delegate class [mscorlib]System.Delegate::Combine(class [mscorlib]System.Delegate, class [mscorlib]System.Delegate)
	IL_0080:  callvirt instance void class [mscorlib]System.Collections.Hashtable::set_Item(object, object)
	IL_0085:  ldarg.0 
	IL_0086:  ldfld class GLib.Signal/SignalClosure GLib.Signal::before_closure
	IL_008b:  brtrue IL_00f0

	IL_0090:  ldarg.0 
	IL_0091:  ldfld class [mscorlib]System.Delegate GLib.Signal::marshaler
	IL_0096:  brtrue IL_00b2

	IL_009b:  ldarg.0 
	IL_009c:  ldarg.0 
	IL_009d:  ldarg.0 
	IL_009e:  ldfld class [mscorlib]System.Type GLib.Signal::args_type
	IL_00a3:  newobj instance void class GLib.Signal/SignalClosure::'.ctor'(class GLib.Signal, class [mscorlib]System.Type)
	IL_00a8:  stfld class GLib.Signal/SignalClosure GLib.Signal::before_closure
	IL_00ad:  br IL_00c4

	IL_00b2:  ldarg.0 
	IL_00b3:  ldarg.0 
	IL_00b4:  ldarg.0 
	IL_00b5:  ldfld class [mscorlib]System.Delegate GLib.Signal::marshaler
	IL_00ba:  newobj instance void class GLib.Signal/SignalClosure::'.ctor'(class GLib.Signal, class [mscorlib]System.Delegate)
	IL_00bf:  stfld class GLib.Signal/SignalClosure GLib.Signal::before_closure
	IL_00c4:  ldarg.0 
	IL_00c5:  ldfld class GLib.Signal/SignalClosure GLib.Signal::before_closure
	IL_00ca:  call class [mscorlib]System.EventHandler class GLib.Signal::get_ClosureDisposedHandler()
	IL_00cf:  callvirt instance void class GLib.Signal/SignalClosure::add_Disposed(class [mscorlib]System.EventHandler)
	IL_00d4:  ldarg.0 
	IL_00d5:  ldfld class GLib.Signal/SignalClosure GLib.Signal::before_closure
	IL_00da:  call class GLib.ClosureInvokedHandler class GLib.Signal::get_ClosureInvokedHandler()
	IL_00df:  callvirt instance void class GLib.Signal/SignalClosure::add_Invoked(class GLib.ClosureInvokedHandler)
	IL_00e4:  ldarg.0 
	IL_00e5:  ldfld class GLib.Signal/SignalClosure GLib.Signal::before_closure
	IL_00ea:  ldc.i4.0 
	IL_00eb:  callvirt instance void class GLib.Signal/SignalClosure::Connect(bool)
	IL_00f0:  br IL_01a1

	IL_00f5:  ldarg.0 
	IL_00f6:  ldfld class GLib.ToggleRef GLib.Signal::tref
	IL_00fb:  callvirt instance class GLib.Object class GLib.ToggleRef::get_Target()
	IL_0100:  callvirt instance class [mscorlib]System.Collections.Hashtable class GLib.Object::get_AfterSignals()
	IL_0105:  ldarg.0 
	IL_0106:  ldfld string GLib.Signal::name
	IL_010b:  ldarg.0 
	IL_010c:  ldfld class GLib.ToggleRef GLib.Signal::tref
	IL_0111:  callvirt instance class GLib.Object class GLib.ToggleRef::get_Target()
	IL_0116:  callvirt instance class [mscorlib]System.Collections.Hashtable class GLib.Object::get_AfterSignals()
	IL_011b:  ldarg.0 
	IL_011c:  ldfld string GLib.Signal::name
	IL_0121:  callvirt instance object class [mscorlib]System.Collections.Hashtable::get_Item(object)
	IL_0126:  isinst [mscorlib]System.Delegate
	IL_012b:  ldarg.1 
	IL_012c:  call class [mscorlib]System.Delegate class [mscorlib]System.Delegate::Combine(class [mscorlib]System.Delegate, class [mscorlib]System.Delegate)
	IL_0131:  callvirt instance void class [mscorlib]System.Collections.Hashtable::set_Item(object, object)
	IL_0136:  ldarg.0 
	IL_0137:  ldfld class GLib.Signal/SignalClosure GLib.Signal::after_closure
	IL_013c:  brtrue IL_01a1

	IL_0141:  ldarg.0 
	IL_0142:  ldfld class [mscorlib]System.Delegate GLib.Signal::marshaler
	IL_0147:  brtrue IL_0163

	IL_014c:  ldarg.0 
	IL_014d:  ldarg.0 
	IL_014e:  ldarg.0 
	IL_014f:  ldfld class [mscorlib]System.Type GLib.Signal::args_type
	IL_0154:  newobj instance void class GLib.Signal/SignalClosure::'.ctor'(class GLib.Signal, class [mscorlib]System.Type)
	IL_0159:  stfld class GLib.Signal/SignalClosure GLib.Signal::after_closure
	IL_015e:  br IL_0175

	IL_0163:  ldarg.0 
	IL_0164:  ldarg.0 
	IL_0165:  ldarg.0 
	IL_0166:  ldfld class [mscorlib]System.Delegate GLib.Signal::marshaler
	IL_016b:  newobj instance void class GLib.Signal/SignalClosure::'.ctor'(class GLib.Signal, class [mscorlib]System.Delegate)
	IL_0170:  stfld class GLib.Signal/SignalClosure GLib.Signal::after_closure
	IL_0175:  ldarg.0 
	IL_0176:  ldfld class GLib.Signal/SignalClosure GLib.Signal::after_closure
	IL_017b:  call class [mscorlib]System.EventHandler class GLib.Signal::get_ClosureDisposedHandler()
	IL_0180:  callvirt instance void class GLib.Signal/SignalClosure::add_Disposed(class [mscorlib]System.EventHandler)
	IL_0185:  ldarg.0 
	IL_0186:  ldfld class GLib.Signal/SignalClosure GLib.Signal::after_closure
	IL_018b:  call class GLib.ClosureInvokedHandler class GLib.Signal::get_ClosureInvokedHandler()
	IL_0190:  callvirt instance void class GLib.Signal/SignalClosure::add_Invoked(class GLib.ClosureInvokedHandler)
	IL_0195:  ldarg.0 
	IL_0196:  ldfld class GLib.Signal/SignalClosure GLib.Signal::after_closure
	IL_019b:  ldc.i4.1 
	IL_019c:  callvirt instance void class GLib.Signal/SignalClosure::Connect(bool)
	IL_01a1:  ret 
    } // end of method Signal::AddDelegate

    // method line 619
    .method public hidebysig 
           instance default void RemoveDelegate (class [mscorlib]System.Delegate d)  cil managed 
    {
        // Method begins at RVA 0x6fec
	// Code size 210 (0xd2)
	.maxstack 3
	.locals init (
		bool	V_0,
		class [mscorlib]System.Collections.Hashtable	V_1,
		class [mscorlib]System.Delegate	V_2)
	IL_0000:  ldarg.0 
	IL_0001:  ldfld class GLib.ToggleRef GLib.Signal::tref
	IL_0006:  callvirt instance class GLib.Object class GLib.ToggleRef::get_Target()
	IL_000b:  brtrue IL_0011

	IL_0010:  ret 
	IL_0011:  ldarg.1 
	IL_0012:  callvirt instance class [mscorlib]System.Reflection.MethodInfo class [mscorlib]System.Delegate::get_Method()
	IL_0017:  ldtoken GLib.ConnectBeforeAttribute
	IL_001c:  call class [mscorlib]System.Type class [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
	IL_0021:  ldc.i4.0 
	IL_0022:  callvirt instance bool class [mscorlib]System.Reflection.MemberInfo::IsDefined(class [mscorlib]System.Type, bool)
	IL_0027:  stloc.0 
	IL_0028:  ldloc.0 
	IL_0029:  brfalse IL_0043

	IL_002e:  ldarg.0 
	IL_002f:  ldfld class GLib.ToggleRef GLib.Signal::tref
	IL_0034:  callvirt instance class GLib.Object class GLib.ToggleRef::get_Target()
	IL_0039:  callvirt instance class [mscorlib]System.Collections.Hashtable class GLib.Object::get_BeforeSignals()
	IL_003e:  br IL_0053

	IL_0043:  ldarg.0 
	IL_0044:  ldfld class GLib.ToggleRef GLib.Signal::tref
	IL_0049:  callvirt instance class GLib.Object class GLib.ToggleRef::get_Target()
	IL_004e:  callvirt instance class [mscorlib]System.Collections.Hashtable class GLib.Object::get_AfterSignals()
	IL_0053:  stloc.1 
	IL_0054:  ldloc.1 
	IL_0055:  ldarg.0 
	IL_0056:  ldfld string GLib.Signal::name
	IL_005b:  callvirt instance object class [mscorlib]System.Collections.Hashtable::get_Item(object)
	IL_0060:  isinst [mscorlib]System.Delegate
	IL_0065:  ldarg.1 
	IL_0066:  call class [mscorlib]System.Delegate class [mscorlib]System.Delegate::Remove(class [mscorlib]System.Delegate, class [mscorlib]System.Delegate)
	IL_006b:  stloc.2 
	IL_006c:  ldloc.2 
	IL_006d:  brfalse IL_0080

	IL_0072:  ldloc.1 
	IL_0073:  ldarg.0 
	IL_0074:  ldfld string GLib.Signal::name
	IL_0079:  ldloc.2 
	IL_007a:  callvirt instance void class [mscorlib]System.Collections.Hashtable::set_Item(object, object)
	IL_007f:  ret 
	IL_0080:  ldloc.1 
	IL_0081:  ldarg.0 
	IL_0082:  ldfld string GLib.Signal::name
	IL_0087:  callvirt instance void class [mscorlib]System.Collections.Hashtable::Remove(object)
	IL_008c:  ldloc.0 
	IL_008d:  brfalse IL_00b4

	IL_0092:  ldarg.0 
	IL_0093:  ldfld class GLib.Signal/SignalClosure GLib.Signal::before_closure
	IL_0098:  brfalse IL_00af

	IL_009d:  ldarg.0 
	IL_009e:  ldfld class GLib.Signal/SignalClosure GLib.Signal::before_closure
	IL_00a3:  callvirt instance void class GLib.Signal/SignalClosure::Dispose()
	IL_00a8:  ldarg.0 
	IL_00a9:  ldnull 
	IL_00aa:  stfld class GLib.Signal/SignalClosure GLib.Signal::before_closure
	IL_00af:  br IL_00d1

	IL_00b4:  ldarg.0 
	IL_00b5:  ldfld class GLib.Signal/SignalClosure GLib.Signal::after_closure
	IL_00ba:  brfalse IL_00d1

	IL_00bf:  ldarg.0 
	IL_00c0:  ldfld class GLib.Signal/SignalClosure GLib.Signal::after_closure
	IL_00c5:  callvirt instance void class GLib.Signal/SignalClosure::Dispose()
	IL_00ca:  ldarg.0 
	IL_00cb:  ldnull 
	IL_00cc:  stfld class GLib.Signal/SignalClosure GLib.Signal::after_closure
	IL_00d1:  ret 
    } // end of method Signal::RemoveDelegate

    // method line 620
    .method private hidebysig specialname rtspecialname 
           instance default void '.ctor' (class GLib.Object obj, string signal_name, class [mscorlib]System.Delegate marshaler)  cil managed 
    {
        // Method begins at RVA 0x70cc
	// Code size 56 (0x38)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::'.ctor'()
	IL_0006:  ldarg.0 
	IL_0007:  ldarg.1 
	IL_0008:  callvirt instance class GLib.ToggleRef class GLib.Object::get_ToggleRef()
	IL_000d:  stfld class GLib.ToggleRef GLib.Signal::tref
	IL_0012:  ldarg.0 
	IL_0013:  ldarg.2 
	IL_0014:  stfld string GLib.Signal::name
	IL_0019:  ldarg.0 
	IL_001a:  ldfld class GLib.ToggleRef GLib.Signal::tref
	IL_001f:  callvirt instance class [mscorlib]System.Collections.Hashtable class GLib.ToggleRef::get_Signals()
	IL_0024:  ldarg.0 
	IL_0025:  ldfld string GLib.Signal::name
	IL_002a:  ldarg.0 
	IL_002b:  callvirt instance void class [mscorlib]System.Collections.Hashtable::set_Item(object, object)
	IL_0030:  ldarg.0 
	IL_0031:  ldarg.3 
	IL_0032:  stfld class [mscorlib]System.Delegate GLib.Signal::marshaler
	IL_0037:  ret 
    } // end of method Signal::.ctor

    // method line 621
    .method private hidebysig specialname rtspecialname 
           instance default void '.ctor' (class GLib.Object obj, string signal_name, class [mscorlib]System.Type args_type)  cil managed 
    {
        // Method begins at RVA 0x7108
	// Code size 56 (0x38)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::'.ctor'()
	IL_0006:  ldarg.0 
	IL_0007:  ldarg.1 
	IL_0008:  callvirt instance class GLib.ToggleRef class GLib.Object::get_ToggleRef()
	IL_000d:  stfld class GLib.ToggleRef GLib.Signal::tref
	IL_0012:  ldarg.0 
	IL_0013:  ldarg.2 
	IL_0014:  stfld string GLib.Signal::name
	IL_0019:  ldarg.0 
	IL_001a:  ldarg.3 
	IL_001b:  stfld class [mscorlib]System.Type GLib.Signal::args_type
	IL_0020:  ldarg.0 
	IL_0021:  ldfld class GLib.ToggleRef GLib.Signal::tref
	IL_0026:  callvirt instance class [mscorlib]System.Collections.Hashtable class GLib.ToggleRef::get_Signals()
	IL_002b:  ldarg.0 
	IL_002c:  ldfld string GLib.Signal::name
	IL_0031:  ldarg.0 
	IL_0032:  callvirt instance void class [mscorlib]System.Collections.Hashtable::set_Item(object, object)
	IL_0037:  ret 
    } // end of method Signal::.ctor

    // method line 622
    .method assembly hidebysig 
           instance default void Free ()  cil managed 
    {
        // Method begins at RVA 0x7144
	// Code size 65 (0x41)
	.maxstack 2
	IL_0000:  ldarg.0 
	IL_0001:  ldfld class GLib.Signal/SignalClosure GLib.Signal::before_closure
	IL_0006:  brfalse IL_001d

	IL_000b:  ldarg.0 
	IL_000c:  ldfld class GLib.Signal/SignalClosure GLib.Signal::before_closure
	IL_0011:  callvirt instance void class GLib.Signal/SignalClosure::Dispose()
	IL_0016:  ldarg.0 
	IL_0017:  ldnull 
	IL_0018:  stfld class GLib.Signal/SignalClosure GLib.Signal::before_closure
	IL_001d:  ldarg.0 
	IL_001e:  ldfld class GLib.Signal/SignalClosure GLib.Signal::after_closure
	IL_0023:  brfalse IL_003a

	IL_0028:  ldarg.0 
	IL_0029:  ldfld class GLib.Signal/SignalClosure GLib.Signal::after_closure
	IL_002e:  callvirt instance void class GLib.Signal/SignalClosure::Dispose()
	IL_0033:  ldarg.0 
	IL_0034:  ldnull 
	IL_0035:  stfld class GLib.Signal/SignalClosure GLib.Signal::after_closure
	IL_003a:  ldarg.0 
	IL_003b:  call void class [mscorlib]System.GC::SuppressFinalize(object)
	IL_0040:  ret 
    } // end of method Signal::Free

    // method line 623
    .method private static hidebysig 
           default void ClosureDisposedCB (object o, class [mscorlib]System.EventArgs args)  cil managed 
    {
        // Method begins at RVA 0x7194
	// Code size 167 (0xa7)
	.maxstack 2
	.locals init (
		class GLib.Signal/SignalClosure	V_0,
		class GLib.Signal	V_1,
		class GLib.ToggleRef	V_2,
		string	V_3)
	IL_0000:  ldarg.0 
	IL_0001:  castclass GLib.Signal/SignalClosure
	IL_0006:  stloc.0 
	IL_0007:  ldloc.0 
	IL_0008:  ldfld class GLib.Signal GLib.Signal/SignalClosure::signal
	IL_000d:  stloc.1 
	IL_000e:  ldloc.1 
	IL_000f:  ldfld class GLib.ToggleRef GLib.Signal::tref
	IL_0014:  stloc.2 
	IL_0015:  ldloc.1 
	IL_0016:  ldfld string GLib.Signal::name
	IL_001b:  stloc.3 
	IL_001c:  ldloc.0 
	IL_001d:  call class [mscorlib]System.EventHandler class GLib.Signal::get_ClosureDisposedHandler()
	IL_0022:  callvirt instance void class GLib.Signal/SignalClosure::remove_Disposed(class [mscorlib]System.EventHandler)
	IL_0027:  ldloc.0 
	IL_0028:  call class GLib.ClosureInvokedHandler class GLib.Signal::get_ClosureInvokedHandler()
	IL_002d:  callvirt instance void class GLib.Signal/SignalClosure::remove_Invoked(class GLib.ClosureInvokedHandler)
	IL_0032:  ldloc.0 
	IL_0033:  ldloc.1 
	IL_0034:  ldfld class GLib.Signal/SignalClosure GLib.Signal::before_closure
	IL_0039:  bne.un IL_0066

	IL_003e:  ldloc.2 
	IL_003f:  callvirt instance class GLib.Object class GLib.ToggleRef::get_Target()
	IL_0044:  brfalse IL_005a

	IL_0049:  ldloc.2 
	IL_004a:  callvirt instance class GLib.Object class GLib.ToggleRef::get_Target()
	IL_004f:  callvirt instance class [mscorlib]System.Collections.Hashtable class GLib.Object::get_BeforeSignals()
	IL_0054:  ldloc.3 
	IL_0055:  callvirt instance void class [mscorlib]System.Collections.Hashtable::Remove(object)
	IL_005a:  ldloc.1 
	IL_005b:  ldnull 
	IL_005c:  stfld class GLib.Signal/SignalClosure GLib.Signal::before_closure
	IL_0061:  br IL_0089

	IL_0066:  ldloc.2 
	IL_0067:  callvirt instance class GLib.Object class GLib.ToggleRef::get_Target()
	IL_006c:  brfalse IL_0082

	IL_0071:  ldloc.2 
	IL_0072:  callvirt instance class GLib.Object class GLib.ToggleRef::get_Target()
	IL_0077:  callvirt instance class [mscorlib]System.Collections.Hashtable class GLib.Object::get_AfterSignals()
	IL_007c:  ldloc.3 
	IL_007d:  callvirt instance void class [mscorlib]System.Collections.Hashtable::Remove(object)
	IL_0082:  ldloc.1 
	IL_0083:  ldnull 
	IL_0084:  stfld class GLib.Signal/SignalClosure GLib.Signal::after_closure
	IL_0089:  ldloc.1 
	IL_008a:  ldfld class GLib.Signal/SignalClosure GLib.Signal::before_closure
	IL_008f:  brtrue IL_00a6

	IL_0094:  ldloc.1 
	IL_0095:  ldfld class GLib.Signal/SignalClosure GLib.Signal::after_closure
	IL_009a:  brtrue IL_00a6

	IL_009f:  ldloc.2 
	IL_00a0:  ldloc.3 
	IL_00a1:  callvirt instance void class GLib.ToggleRef::RemoveSignal(string)
	IL_00a6:  ret 
    } // end of method Signal::ClosureDisposedCB

    // method line 624
    .method private static hidebysig specialname 
           default class [mscorlib]System.EventHandler get_ClosureDisposedHandler ()  cil managed 
    {
        // Method begins at RVA 0x7248
	// Code size 33 (0x21)
	.maxstack 8
	IL_0000:  ldsfld class [mscorlib]System.EventHandler GLib.Signal::closure_disposed_cb
	IL_0005:  brtrue IL_001b

	IL_000a:  ldnull 
	IL_000b:  ldftn void class GLib.Signal::ClosureDisposedCB(object, class [mscorlib]System.EventArgs)
	IL_0011:  newobj instance void class [mscorlib]System.EventHandler::'.ctor'(object, native int)
	IL_0016:  stsfld class [mscorlib]System.EventHandler GLib.Signal::closure_disposed_cb
	IL_001b:  ldsfld class [mscorlib]System.EventHandler GLib.Signal::closure_disposed_cb
	IL_0020:  ret 
    } // end of method Signal::get_ClosureDisposedHandler

    // method line 625
    .method private static hidebysig specialname 
           default class GLib.ClosureInvokedHandler get_ClosureInvokedHandler ()  cil managed 
    {
        // Method begins at RVA 0x726c
	// Code size 33 (0x21)
	.maxstack 8
	IL_0000:  ldsfld class GLib.ClosureInvokedHandler GLib.Signal::closure_invoked_cb
	IL_0005:  brtrue IL_001b

	IL_000a:  ldnull 
	IL_000b:  ldftn void class GLib.Signal::ClosureInvokedCB(object, class GLib.ClosureInvokedArgs)
	IL_0011:  newobj instance void class GLib.ClosureInvokedHandler::'.ctor'(object, native int)
	IL_0016:  stsfld class GLib.ClosureInvokedHandler GLib.Signal::closure_invoked_cb
	IL_001b:  ldsfld class GLib.ClosureInvokedHandler GLib.Signal::closure_invoked_cb
	IL_0020:  ret 
    } // end of method Signal::get_ClosureInvokedHandler

    // method line 626
    .method private static hidebysig 
           default void ClosureInvokedCB (object o, class GLib.ClosureInvokedArgs args)  cil managed 
    {
        // Method begins at RVA 0x7290
	// Code size 133 (0x85)
	.maxstack 5
	.locals init (
		class GLib.Signal/SignalClosure	V_0,
		class [mscorlib]System.Delegate	V_1)
	IL_0000:  ldarg.0 
	IL_0001:  castclass GLib.Signal/SignalClosure
	IL_0006:  stloc.0 
	IL_0007:  ldloc.0 
	IL_0008:  ldloc.0 
	IL_0009:  ldfld class GLib.Signal GLib.Signal/SignalClosure::signal
	IL_000e:  ldfld class GLib.Signal/SignalClosure GLib.Signal::before_closure
	IL_0013:  bne.un IL_003e

	IL_0018:  ldarg.1 
	IL_0019:  callvirt instance class GLib.Object class GLib.ClosureInvokedArgs::get_Target()
	IL_001e:  callvirt instance class [mscorlib]System.Collections.Hashtable class GLib.Object::get_BeforeSignals()
	IL_0023:  ldloc.0 
	IL_0024:  ldfld class GLib.Signal GLib.Signal/SignalClosure::signal
	IL_0029:  ldfld string GLib.Signal::name
	IL_002e:  callvirt instance object class [mscorlib]System.Collections.Hashtable::get_Item(object)
	IL_0033:  isinst [mscorlib]System.Delegate
	IL_0038:  stloc.1 
	IL_0039:  br IL_005f

	IL_003e:  ldarg.1 
	IL_003f:  callvirt instance class GLib.Object class GLib.ClosureInvokedArgs::get_Target()
	IL_0044:  callvirt instance class [mscorlib]System.Collections.Hashtable class GLib.Object::get_AfterSignals()
	IL_0049:  ldloc.0 
	IL_004a:  ldfld class GLib.Signal GLib.Signal/SignalClosure::signal
	IL_004f:  ldfld string GLib.Signal::name
	IL_0054:  callvirt instance object class [mscorlib]System.Collections.Hashtable::get_Item(object)
	IL_0059:  isinst [mscorlib]System.Delegate
	IL_005e:  stloc.1 
	IL_005f:  ldloc.1 
	IL_0060:  brfalse IL_0084

	IL_0065:  ldloc.1 
	IL_0066:  ldc.i4.2 
	IL_0067:  newarr object
	IL_006c:  dup 
	IL_006d:  ldc.i4.0 
	IL_006e:  ldarg.1 
	IL_006f:  callvirt instance class GLib.Object class GLib.ClosureInvokedArgs::get_Target()
	IL_0074:  stelem.ref 
	IL_0075:  dup 
	IL_0076:  ldc.i4.1 
	IL_0077:  ldarg.1 
	IL_0078:  callvirt instance class [mscorlib]System.EventArgs class GLib.ClosureInvokedArgs::get_Args()
	IL_007d:  stelem.ref 
	IL_007e:  callvirt instance object class [mscorlib]System.Delegate::DynamicInvoke(object[])
	IL_0083:  pop 
	IL_0084:  ret 
    } // end of method Signal::ClosureInvokedCB

    // method line 627
    .method public static hidebysig 
           default class GLib.Signal Lookup (class GLib.Object obj, string name)  cil managed 
    {
        // Method begins at RVA 0x7324
	// Code size 18 (0x12)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldarg.1 
	IL_0002:  ldtoken [mscorlib]System.EventArgs
	IL_0007:  call class [mscorlib]System.Type class [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
	IL_000c:  call class GLib.Signal class GLib.Signal::Lookup(class GLib.Object, string, class [mscorlib]System.Type)
	IL_0011:  ret 
    } // end of method Signal::Lookup

    // method line 628
    .method public static hidebysig 
           default class GLib.Signal Lookup (class GLib.Object obj, string name, class [mscorlib]System.Type args_type)  cil managed 
    {
        // Method begins at RVA 0x7338
	// Code size 40 (0x28)
	.maxstack 3
	.locals init (
		class GLib.Signal	V_0)
	IL_0000:  ldarg.0 
	IL_0001:  callvirt instance class GLib.ToggleRef class GLib.Object::get_ToggleRef()
	IL_0006:  callvirt instance class [mscorlib]System.Collections.Hashtable class GLib.ToggleRef::get_Signals()
	IL_000b:  ldarg.1 
	IL_000c:  callvirt instance object class [mscorlib]System.Collections.Hashtable::get_Item(object)
	IL_0011:  isinst GLib.Signal
	IL_0016:  stloc.0 
	IL_0017:  ldloc.0 
	IL_0018:  brtrue IL_0026

	IL_001d:  ldarg.0 
	IL_001e:  ldarg.1 
	IL_001f:  ldarg.2 
	IL_0020:  newobj instance void class GLib.Signal::'.ctor'(class GLib.Object, string, class [mscorlib]System.Type)
	IL_0025:  stloc.0 
	IL_0026:  ldloc.0 
	IL_0027:  ret 
    } // end of method Signal::Lookup

    // method line 629
    .method private static hidebysig pinvokeimpl ("libgobject-2.0-0.dll" as "g_signal_get_invocation_hint" cdecl )
           default native int g_signal_get_invocation_hint (native int 'instance')  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method Signal::g_signal_get_invocation_hint

    // method line 630
    .method private static hidebysig 
           default void ParseSignalDetail (string signal_detail, [out] string& signal_name, [out] unsigned int32& gquark)  cil managed 
    {
        // Method begins at RVA 0x736c
	// Code size 80 (0x50)
	.maxstack 4
	.locals init (
		int32	V_0)
	IL_0000:  ldarg.0 
	IL_0001:  ldstr "::"
	IL_0006:  callvirt instance int32 string::IndexOf(string)
	IL_000b:  stloc.0 
	IL_000c:  ldloc.0 
	IL_000d:  ldc.i4.0 
	IL_000e:  bge IL_001e

	IL_0013:  ldarg.2 
	IL_0014:  ldc.i4.0 
	IL_0015:  stind.i4 
	IL_0016:  ldarg.1 
	IL_0017:  ldarg.0 
	IL_0018:  stind.ref 
	IL_0019:  br IL_004f

	IL_001e:  ldloc.0 
	IL_001f:  brtrue IL_0035

	IL_0024:  ldstr "Invalid detailed signal: "
	IL_0029:  ldarg.0 
	IL_002a:  call string string::Concat(string, string)
	IL_002f:  newobj instance void class [mscorlib]System.FormatException::'.ctor'(string)
	IL_0034:  throw 
	IL_0035:  ldarg.1 
	IL_0036:  ldarg.0 
	IL_0037:  ldc.i4.0 
	IL_0038:  ldloc.0 
	IL_0039:  callvirt instance string string::Substring(int32, int32)
	IL_003e:  stind.ref 
	IL_003f:  ldarg.2 
	IL_0040:  ldarg.0 
	IL_0041:  ldloc.0 
	IL_0042:  ldc.i4.2 
	IL_0043:  add 
	IL_0044:  callvirt instance string string::Substring(int32)
	IL_0049:  call unsigned int32 class GLib.Signal::GetGQuarkFromString(string)
	IL_004e:  stind.i4 
	IL_004f:  ret 
    } // end of method Signal::ParseSignalDetail

    // method line 631
    .method private static hidebysig 
           default unsigned int32 GetGQuarkFromString (string str)  cil managed 
    {
        // Method begins at RVA 0x73c8
	// Code size 22 (0x16)
	.maxstack 1
	.locals init (
		native int	V_0,
		unsigned int32	V_1)
	IL_0000:  ldarg.0 
	IL_0001:  call native int class GLib.Marshaller::StringToPtrGStrdup(string)
	IL_0006:  stloc.0 
	IL_0007:  ldloc.0 
	IL_0008:  call unsigned int32 class GLib.Signal::g_quark_from_string(native int)
	IL_000d:  stloc.1 
	IL_000e:  ldloc.0 
	IL_000f:  call void class GLib.Marshaller::Free(native int)
	IL_0014:  ldloc.1 
	IL_0015:  ret 
    } // end of method Signal::GetGQuarkFromString

    // method line 632
    .method public static hidebysig 
           default object Emit (class GLib.Object 'instance', string detailed_signal, object[] args)  cil managed 
    {
	.param [3]
	.custom instance void class [mscorlib]System.ParamArrayAttribute::'.ctor'() =  (01 00 00 00 ) // ....

        // Method begins at RVA 0x73ec
	// Code size 320 (0x140)
	.maxstack 4
	.locals init (
		unsigned int32	V_0,
		unsigned int32	V_1,
		string	V_2,
		valuetype GLib.Value[]	V_3,
		class GLib.ValueArray	V_4,
		int32	V_5,
		object	V_6,
		valuetype GLib.GType	V_7,
		valuetype GLib.Value	V_8,
		valuetype GLib.Value	V_9,
		valuetype GLib.Value[]	V_10,
		int32	V_11)
	IL_0000:  ldarg.1 
	IL_0001:  ldloca.s 2
	IL_0003:  ldloca.s 0
	IL_0005:  call void class GLib.Signal::ParseSignalDetail(string, [out] string&, [out] unsigned int32&)
	IL_000a:  ldloc.2 
	IL_000b:  ldarg.0 
	IL_000c:  call unsigned int32 class GLib.Signal::GetSignalId(string, class GLib.Object)
	IL_0011:  stloc.1 
	IL_0012:  ldloc.1 
	IL_0013:  ldc.i4.0 
	IL_0014:  bgt.un IL_002a

	IL_0019:  ldstr "Invalid signal name: "
	IL_001e:  ldloc.2 
	IL_001f:  call string string::Concat(string, string)
	IL_0024:  newobj instance void class [mscorlib]System.ArgumentException::'.ctor'(string)
	IL_0029:  throw 
	IL_002a:  ldarg.2 
	IL_002b:  ldlen 
	IL_002c:  conv.i4 
	IL_002d:  ldc.i4.1 
	IL_002e:  add 
	IL_002f:  newarr GLib.Value
	IL_0034:  stloc.3 
	IL_0035:  ldarg.2 
	IL_0036:  ldlen 
	IL_0037:  conv.i4 
	IL_0038:  ldc.i4.1 
	IL_0039:  add 
	IL_003a:  newobj instance void class GLib.ValueArray::'.ctor'(unsigned int32)
	IL_003f:  stloc.s 4
	IL_0041:  ldloc.3 
	IL_0042:  ldc.i4.0 
	IL_0043:  ldelema GLib.Value
	IL_0048:  ldarg.0 
	IL_0049:  newobj instance void valuetype GLib.Value::'.ctor'(class GLib.Object)
	IL_004e:  stobj GLib.Value
	IL_0053:  ldloc.s 4
	IL_0055:  ldloc.3 
	IL_0056:  ldc.i4.0 
	IL_0057:  ldelema GLib.Value
	IL_005c:  ldobj GLib.Value
	IL_0061:  callvirt instance void class GLib.ValueArray::Append(valuetype GLib.Value)
	IL_0066:  ldc.i4.1 
	IL_0067:  stloc.s 5
	IL_0069:  br IL_00a0

	IL_006e:  ldloc.3 
	IL_006f:  ldloc.s 5
	IL_0071:  ldelema GLib.Value
	IL_0076:  ldarg.2 
	IL_0077:  ldloc.s 5
	IL_0079:  ldc.i4.1 
	IL_007a:  sub 
	IL_007b:  ldelem.ref 
	IL_007c:  newobj instance void valuetype GLib.Value::'.ctor'(object)
	IL_0081:  stobj GLib.Value
	IL_0086:  ldloc.s 4
	IL_0088:  ldloc.3 
	IL_0089:  ldloc.s 5
	IL_008b:  ldelema GLib.Value
	IL_0090:  ldobj GLib.Value
	IL_0095:  callvirt instance void class GLib.ValueArray::Append(valuetype GLib.Value)
	IL_009a:  ldloc.s 5
	IL_009c:  ldc.i4.1 
	IL_009d:  add 
	IL_009e:  stloc.s 5
	IL_00a0:  ldloc.s 5
	IL_00a2:  ldloc.3 
	IL_00a3:  ldlen 
	IL_00a4:  conv.i4 
	IL_00a5:  blt IL_006e

	IL_00aa:  ldnull 
	IL_00ab:  stloc.s 6
	IL_00ad:  ldloc.1 
	IL_00ae:  call native int class GLib.Signal::glibsharp_signal_get_return_type(unsigned int32)
	IL_00b3:  ldsfld valuetype GLib.GType GLib.GType::None
	IL_00b8:  stloc.s 7
	IL_00ba:  ldloca.s 7
	IL_00bc:  call instance native int valuetype GLib.GType::get_Val()
	IL_00c1:  call bool native int::op_Inequality(native int, native int)
	IL_00c6:  brfalse IL_00f7

	IL_00cb:  ldsfld valuetype GLib.Value GLib.Value::Empty
	IL_00d0:  stloc.s 8
	IL_00d2:  ldloc.s 4
	IL_00d4:  callvirt instance native int class GLib.ValueArray::get_ArrayPtr()
	IL_00d9:  ldloc.1 
	IL_00da:  ldloc.0 
	IL_00db:  ldloca.s 8
	IL_00dd:  call void class GLib.Signal::g_signal_emitv(native int, unsigned int32, unsigned int32, [out] valuetype GLib.Value&)
	IL_00e2:  ldloca.s 8
	IL_00e4:  call instance object valuetype GLib.Value::get_Val()
	IL_00e9:  stloc.s 6
	IL_00eb:  ldloca.s 8
	IL_00ed:  call instance void valuetype GLib.Value::Dispose()
	IL_00f2:  br IL_010a

	IL_00f7:  ldloc.s 4
	IL_00f9:  callvirt instance native int class GLib.ValueArray::get_ArrayPtr()
	IL_00fe:  ldloc.1 
	IL_00ff:  ldloc.0 
	IL_0100:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_0105:  call void class GLib.Signal::g_signal_emitv(native int, unsigned int32, unsigned int32, native int)
	IL_010a:  ldloc.3 
	IL_010b:  stloc.s 10
	IL_010d:  ldc.i4.0 
	IL_010e:  stloc.s 11
	IL_0110:  br IL_0132

	IL_0115:  ldloc.s 10
	IL_0117:  ldloc.s 11
	IL_0119:  ldelema GLib.Value
	IL_011e:  ldobj GLib.Value
	IL_0123:  stloc.s 9
	IL_0125:  ldloca.s 9
	IL_0127:  call instance void valuetype GLib.Value::Dispose()
	IL_012c:  ldloc.s 11
	IL_012e:  ldc.i4.1 
	IL_012f:  add 
	IL_0130:  stloc.s 11
	IL_0132:  ldloc.s 11
	IL_0134:  ldloc.s 10
	IL_0136:  ldlen 
	IL_0137:  conv.i4 
	IL_0138:  blt IL_0115

	IL_013d:  ldloc.s 6
	IL_013f:  ret 
    } // end of method Signal::Emit

    // method line 633
    .method private static hidebysig 
           default unsigned int32 GetSignalId (string signal_name, class GLib.Object obj)  cil managed 
    {
        // Method begins at RVA 0x7538
	// Code size 20 (0x14)
	.maxstack 2
	.locals init (
		native int	V_0)
	IL_0000:  ldarg.1 
	IL_0001:  callvirt instance native int class GLib.Object::get_Handle()
	IL_0006:  call native int class GLib.Signal::gtksharp_get_type_id(native int)
	IL_000b:  stloc.0 
	IL_000c:  ldarg.0 
	IL_000d:  ldloc.0 
	IL_000e:  call unsigned int32 class GLib.Signal::GetSignalId(string, native int)
	IL_0013:  ret 
    } // end of method Signal::GetSignalId

    // method line 634
    .method private static hidebysig pinvokeimpl ("glibsharpglue-2" as "glibsharp_signal_get_return_type" cdecl )
           default native int glibsharp_signal_get_return_type (unsigned int32 signal_id)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method Signal::glibsharp_signal_get_return_type

    // method line 635
    .method private static hidebysig pinvokeimpl ("libgobject-2.0-0.dll" as "g_signal_emitv" cdecl )
           default void g_signal_emitv (native int instance_and_params, unsigned int32 signal_id, unsigned int32 gquark_detail, valuetype GLib.Value& return_value)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method Signal::g_signal_emitv

    // method line 636
    .method private static hidebysig pinvokeimpl ("libgobject-2.0-0.dll" as "g_signal_emitv" cdecl )
           default void g_signal_emitv (native int instance_and_params, unsigned int32 signal_id, unsigned int32 gquark_detail, native int return_value)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method Signal::g_signal_emitv

    // method line 637
    .method private static hidebysig pinvokeimpl ("libglib-2.0-0.dll" as "g_quark_from_string" cdecl )
           default unsigned int32 g_quark_from_string (native int str)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method Signal::g_quark_from_string

    // method line 638
    .method private static hidebysig pinvokeimpl ("glibsharpglue-2" as "gtksharp_get_type_id" cdecl )
           default native int gtksharp_get_type_id (native int raw)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method Signal::gtksharp_get_type_id

    // method line 639
    .method private static hidebysig 
           default unsigned int32 GetSignalId (string signal_name, native int typeid)  cil managed 
    {
        // Method begins at RVA 0x7558
	// Code size 23 (0x17)
	.maxstack 2
	.locals init (
		native int	V_0,
		unsigned int32	V_1)
	IL_0000:  ldarg.0 
	IL_0001:  call native int class GLib.Marshaller::StringToPtrGStrdup(string)
	IL_0006:  stloc.0 
	IL_0007:  ldloc.0 
	IL_0008:  ldarg.1 
	IL_0009:  call unsigned int32 class GLib.Signal::g_signal_lookup(native int, native int)
	IL_000e:  stloc.1 
	IL_000f:  ldloc.0 
	IL_0010:  call void class GLib.Marshaller::Free(native int)
	IL_0015:  ldloc.1 
	IL_0016:  ret 
    } // end of method Signal::GetSignalId

    // method line 640
    .method private static hidebysig pinvokeimpl ("libgobject-2.0-0.dll" as "g_signal_lookup" cdecl )
           default unsigned int32 g_signal_lookup (native int name, native int itype)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method Signal::g_signal_lookup

    // method line 641
    .method public static hidebysig 
           default unsigned int64 AddEmissionHook (string detailed_signal, valuetype GLib.GType 'type', class GLib.Signal/EmissionHook handler_func)  cil managed 
    {
        // Method begins at RVA 0x757c
	// Code size 77 (0x4d)
	.maxstack 5
	.locals init (
		unsigned int32	V_0,
		string	V_1,
		unsigned int32	V_2)
	IL_0000:  ldarg.0 
	IL_0001:  ldloca.s 1
	IL_0003:  ldloca.s 0
	IL_0005:  call void class GLib.Signal::ParseSignalDetail(string, [out] string&, [out] unsigned int32&)
	IL_000a:  ldloc.1 
	IL_000b:  ldarga.s 1
	IL_000d:  call instance native int valuetype GLib.GType::get_Val()
	IL_0012:  call unsigned int32 class GLib.Signal::GetSignalId(string, native int)
	IL_0017:  stloc.2 
	IL_0018:  ldloc.2 
	IL_0019:  ldc.i4.0 
	IL_001a:  bgt.un IL_0030

	IL_001f:  ldstr "Invalid signal name: "
	IL_0024:  ldloc.1 
	IL_0025:  call string string::Concat(string, string)
	IL_002a:  newobj instance void class [mscorlib]System.Exception::'.ctor'(string)
	IL_002f:  throw 
	IL_0030:  ldloc.2 
	IL_0031:  ldloc.0 
	IL_0032:  ldarg.2 
	IL_0033:  newobj instance void class GLib.Signal/EmissionHookMarshaler::'.ctor'(class GLib.Signal/EmissionHook)
	IL_0038:  call instance class GLib.Signal/EmissionHookNative class GLib.Signal/EmissionHookMarshaler::get_Callback()
	IL_003d:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_0042:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_0047:  call unsigned int64 class GLib.Signal::g_signal_add_emission_hook(unsigned int32, unsigned int32, class GLib.Signal/EmissionHookNative, native int, native int)
	IL_004c:  ret 
    } // end of method Signal::AddEmissionHook

    // method line 642
    .method private static hidebysig pinvokeimpl ("libgobject-2.0-0.dll" as "g_signal_add_emission_hook" cdecl )
           default unsigned int64 g_signal_add_emission_hook (unsigned int32 signal_id, unsigned int32 gquark_detail, class GLib.Signal/EmissionHookNative hook_func, native int hook_data, native int data_destroy)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method Signal::g_signal_add_emission_hook

	.property class [mscorlib]System.EventHandler ClosureDisposedHandler ()
	{
		.get default class [mscorlib]System.EventHandler GLib.Signal::get_ClosureDisposedHandler () 
	}
	.property class GLib.ClosureInvokedHandler ClosureInvokedHandler ()
	{
		.get default class GLib.ClosureInvokedHandler GLib.Signal::get_ClosureInvokedHandler () 
	}
	.property instance class [mscorlib]System.Delegate Handler ()
	{
		.get instance default class [mscorlib]System.Delegate GLib.Signal::get_Handler () 
	}
  .class nested public auto ansi sealed Flags
  	extends [mscorlib]System.Enum
  {
    .custom instance void class [mscorlib]System.FlagsAttribute::'.ctor'() =  (01 00 00 00 ) // ....

    .field  public specialname rtspecialname  int32 value__
    .field public static literal  valuetype GLib.Signal/Flags RunFirst = int32(0x00000001)
    .field public static literal  valuetype GLib.Signal/Flags RunLast = int32(0x00000002)
    .field public static literal  valuetype GLib.Signal/Flags RunCleanup = int32(0x00000004)
    .field public static literal  valuetype GLib.Signal/Flags NoRecurse = int32(0x00000008)
    .field public static literal  valuetype GLib.Signal/Flags Detailed = int32(0x00000010)
    .field public static literal  valuetype GLib.Signal/Flags Action = int32(0x00000020)
    .field public static literal  valuetype GLib.Signal/Flags NoHooks = int32(0x00000040)

  } // end of class Flags

  .class nested public sequential ansi sealed beforefieldinit InvocationHint
  	extends [mscorlib]System.ValueType
  {
    .field  public  unsigned int32 signal_id
    .field  public  unsigned int32 detail
    .field  public  valuetype GLib.Signal/Flags run_type

  } // end of class InvocationHint

  .class nested public auto ansi sealed EmissionHookNative
  	extends [mscorlib]System.MulticastDelegate
  {
    .custom instance void class [mscorlib]System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute::'.ctor'(valuetype [mscorlib]System.Runtime.InteropServices.CallingConvention) =  (01 00 02 00 00 00 00 00 ) // ........


    // method line 643
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (object 'object', native int 'method')  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method EmissionHookNative::.ctor

    // method line 644
    .method public virtual hidebysig newslot 
           instance default bool Invoke (valuetype GLib.Signal/InvocationHint& hint, unsigned int32 n_pvals, native int pvals, native int data)  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method EmissionHookNative::Invoke

    // method line 645
    .method public virtual hidebysig newslot 
           instance default class [mscorlib]System.IAsyncResult BeginInvoke (valuetype GLib.Signal/InvocationHint& hint, unsigned int32 n_pvals, native int pvals, native int data, class [mscorlib]System.AsyncCallback callback, object 'object')  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method EmissionHookNative::BeginInvoke

    // method line 646
    .method public virtual hidebysig newslot 
           instance default bool EndInvoke (valuetype GLib.Signal/InvocationHint& hint, class [mscorlib]System.IAsyncResult result)  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method EmissionHookNative::EndInvoke

  } // end of class EmissionHookNative

  .class nested public auto ansi sealed EmissionHook
  	extends [mscorlib]System.MulticastDelegate
  {

    // method line 647
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (object 'object', native int 'method')  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method EmissionHook::.ctor

    // method line 648
    .method public virtual hidebysig newslot 
           instance default bool Invoke (valuetype GLib.Signal/InvocationHint ihint, object[] inst_and_param_values)  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method EmissionHook::Invoke

    // method line 649
    .method public virtual hidebysig newslot 
           instance default class [mscorlib]System.IAsyncResult BeginInvoke (valuetype GLib.Signal/InvocationHint ihint, object[] inst_and_param_values, class [mscorlib]System.AsyncCallback callback, object 'object')  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method EmissionHook::BeginInvoke

    // method line 650
    .method public virtual hidebysig newslot 
           instance default bool EndInvoke (class [mscorlib]System.IAsyncResult result)  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method EmissionHook::EndInvoke

  } // end of class EmissionHook

  .class nested public auto ansi beforefieldinit EmissionHookMarshaler
  	extends [mscorlib]System.Object
  {
    .field  private  class GLib.Signal/EmissionHook 'handler'
    .field  private  class GLib.Signal/EmissionHookNative cb
    .field  private  native int user_data
    .field  private  valuetype [mscorlib]System.Runtime.InteropServices.GCHandle gch

    // method line 651
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (class GLib.Signal/EmissionHook 'handler')  cil managed 
    {
        // Method begins at RVA 0x75d8
	// Code size 44 (0x2c)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::'.ctor'()
	IL_0006:  ldarg.0 
	IL_0007:  ldarg.1 
	IL_0008:  stfld class GLib.Signal/EmissionHook GLib.Signal/EmissionHookMarshaler::'handler'
	IL_000d:  ldarg.0 
	IL_000e:  ldarg.0 
	IL_000f:  ldftn instance bool class GLib.Signal/EmissionHookMarshaler::NativeCallback([out] valuetype GLib.Signal/InvocationHint&, unsigned int32, native int, native int)
	IL_0015:  newobj instance void class GLib.Signal/EmissionHookNative::'.ctor'(object, native int)
	IL_001a:  stfld class GLib.Signal/EmissionHookNative GLib.Signal/EmissionHookMarshaler::cb
	IL_001f:  ldarg.0 
	IL_0020:  ldarg.0 
	IL_0021:  call valuetype [mscorlib]System.Runtime.InteropServices.GCHandle valuetype [mscorlib]System.Runtime.InteropServices.GCHandle::Alloc(object)
	IL_0026:  stfld valuetype [mscorlib]System.Runtime.InteropServices.GCHandle GLib.Signal/EmissionHookMarshaler::gch
	IL_002b:  ret 
    } // end of method EmissionHookMarshaler::.ctor

    // method line 652
    .method public hidebysig specialname 
           instance default class GLib.Signal/EmissionHookNative get_Callback ()  cil managed 
    {
        // Method begins at RVA 0x7608
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldfld class GLib.Signal/EmissionHookNative GLib.Signal/EmissionHookMarshaler::cb
	IL_0006:  ret 
    } // end of method EmissionHookMarshaler::get_Callback

    // method line 653
    .method private hidebysig 
           instance default bool NativeCallback (valuetype GLib.Signal/InvocationHint& hint, unsigned int32 n_pvals, native int pvals_ptr, native int data)  cil managed 
    {
        // Method begins at RVA 0x7610
	// Code size 121 (0x79)
	.maxstack 4
	.locals init (
		object[]	V_0,
		int32	V_1,
		int32	V_2,
		native int	V_3,
		valuetype GLib.Value	V_4,
		bool	V_5)
	IL_0000:  ldarg.2 
	IL_0001:  conv.u 
	IL_0002:  newarr object
	IL_0007:  stloc.0 
	IL_0008:  ldtoken GLib.Value
	IL_000d:  call class [mscorlib]System.Type class [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
	IL_0012:  call int32 class [mscorlib]System.Runtime.InteropServices.Marshal::SizeOf(class [mscorlib]System.Type)
	IL_0017:  stloc.1 
	IL_0018:  ldc.i4.0 
	IL_0019:  stloc.2 
	IL_001a:  br IL_0047

	IL_001f:  ldloca.s 3
	IL_0021:  ldarg.3 
	IL_0022:  call int64 native int::op_Explicit(native int)
	IL_0027:  ldloc.2 
	IL_0028:  ldloc.1 
	IL_0029:  mul 
	IL_002a:  conv.i8 
	IL_002b:  add 
	IL_002c:  call instance void native int::'.ctor'(int64)
	IL_0031:  ldloc.3 
	IL_0032:  call !!0 class [mscorlib]System.Runtime.InteropServices.Marshal::PtrToStructure<valuetype GLib.Value> (native int)
	IL_0037:  stloc.s 4
	IL_0039:  ldloc.0 
	IL_003a:  ldloc.2 
	IL_003b:  ldloca.s 4
	IL_003d:  call instance object valuetype GLib.Value::get_Val()
	IL_0042:  stelem.ref 
	IL_0043:  ldloc.2 
	IL_0044:  ldc.i4.1 
	IL_0045:  add 
	IL_0046:  stloc.2 
	IL_0047:  ldloc.2 
	IL_0048:  conv.i8 
	IL_0049:  ldarg.2 
	IL_004a:  conv.u8 
	IL_004b:  blt IL_001f

	IL_0050:  ldarg.0 
	IL_0051:  ldfld class GLib.Signal/EmissionHook GLib.Signal/EmissionHookMarshaler::'handler'
	IL_0056:  ldarg.1 
	IL_0057:  ldobj GLib.Signal/InvocationHint
	IL_005c:  ldloc.0 
	IL_005d:  callvirt instance bool class GLib.Signal/EmissionHook::Invoke(valuetype GLib.Signal/InvocationHint, object[])
	IL_0062:  stloc.s 5
	IL_0064:  ldloc.s 5
	IL_0066:  brtrue IL_0076

	IL_006b:  ldarg.0 
	IL_006c:  ldflda valuetype [mscorlib]System.Runtime.InteropServices.GCHandle GLib.Signal/EmissionHookMarshaler::gch
	IL_0071:  call instance void valuetype [mscorlib]System.Runtime.InteropServices.GCHandle::Free()
	IL_0076:  ldloc.s 5
	IL_0078:  ret 
    } // end of method EmissionHookMarshaler::NativeCallback

    // method line 654
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (class GLib.Signal/EmissionHookNative callback, native int user_data)  cil managed 
    {
        // Method begins at RVA 0x7698
	// Code size 39 (0x27)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::'.ctor'()
	IL_0006:  ldarg.0 
	IL_0007:  ldarg.1 
	IL_0008:  stfld class GLib.Signal/EmissionHookNative GLib.Signal/EmissionHookMarshaler::cb
	IL_000d:  ldarg.0 
	IL_000e:  ldarg.2 
	IL_000f:  stfld native int GLib.Signal/EmissionHookMarshaler::user_data
	IL_0014:  ldarg.0 
	IL_0015:  ldarg.0 
	IL_0016:  ldftn instance bool class GLib.Signal/EmissionHookMarshaler::NativeInvoker(valuetype GLib.Signal/InvocationHint, object[])
	IL_001c:  newobj instance void class GLib.Signal/EmissionHook::'.ctor'(object, native int)
	IL_0021:  stfld class GLib.Signal/EmissionHook GLib.Signal/EmissionHookMarshaler::'handler'
	IL_0026:  ret 
    } // end of method EmissionHookMarshaler::.ctor

    // method line 655
    .method private hidebysig 
           instance default bool NativeInvoker (valuetype GLib.Signal/InvocationHint ihint, object[] pvals)  cil managed 
    {
        // Method begins at RVA 0x76c0
	// Code size 199 (0xc7)
	.maxstack 5
	.locals init (
		int32	V_0,
		native int	V_1,
		valuetype GLib.Value[]	V_2,
		int32	V_3,
		native int	V_4,
		bool	V_5,
		valuetype GLib.Value	V_6,
		valuetype GLib.Value[]	V_7,
		int32	V_8)
	IL_0000:  ldtoken GLib.Value
	IL_0005:  call class [mscorlib]System.Type class [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
	IL_000a:  call int32 class [mscorlib]System.Runtime.InteropServices.Marshal::SizeOf(class [mscorlib]System.Type)
	IL_000f:  stloc.0 
	IL_0010:  ldarg.2 
	IL_0011:  ldlen 
	IL_0012:  conv.i4 
	IL_0013:  ldloc.0 
	IL_0014:  mul 
	IL_0015:  call native int class [mscorlib]System.Runtime.InteropServices.Marshal::AllocHGlobal(int32)
	IL_001a:  stloc.1 
	IL_001b:  ldarg.2 
	IL_001c:  ldlen 
	IL_001d:  conv.i4 
	IL_001e:  newarr GLib.Value
	IL_0023:  stloc.2 
	IL_0024:  ldc.i4.0 
	IL_0025:  stloc.3 
	IL_0026:  br IL_0069

	IL_002b:  ldloc.2 
	IL_002c:  ldloc.3 
	IL_002d:  ldelema GLib.Value
	IL_0032:  ldarg.2 
	IL_0033:  ldloc.3 
	IL_0034:  ldelem.ref 
	IL_0035:  newobj instance void valuetype GLib.Value::'.ctor'(object)
	IL_003a:  stobj GLib.Value
	IL_003f:  ldloca.s 4
	IL_0041:  ldloc.1 
	IL_0042:  call int64 native int::op_Explicit(native int)
	IL_0047:  ldloc.3 
	IL_0048:  ldloc.0 
	IL_0049:  mul 
	IL_004a:  conv.i8 
	IL_004b:  add 
	IL_004c:  call instance void native int::'.ctor'(int64)
	IL_0051:  ldloc.2 
	IL_0052:  ldloc.3 
	IL_0053:  ldelema GLib.Value
	IL_0058:  ldobj GLib.Value
	IL_005d:  ldloc.s 4
	IL_005f:  ldc.i4.0 
	IL_0060:  call void class [mscorlib]System.Runtime.InteropServices.Marshal::StructureToPtr<valuetype GLib.Value> (!!0, native int, bool)
	IL_0065:  ldloc.3 
	IL_0066:  ldc.i4.1 
	IL_0067:  add 
	IL_0068:  stloc.3 
	IL_0069:  ldloc.3 
	IL_006a:  ldarg.2 
	IL_006b:  ldlen 
	IL_006c:  conv.i4 
	IL_006d:  blt IL_002b

	IL_0072:  ldarg.0 
	IL_0073:  ldfld class GLib.Signal/EmissionHookNative GLib.Signal/EmissionHookMarshaler::cb
	IL_0078:  ldarga.s 1
	IL_007a:  ldarg.2 
	IL_007b:  ldlen 
	IL_007c:  conv.i4 
	IL_007d:  ldloc.1 
	IL_007e:  ldarg.0 
	IL_007f:  ldfld native int GLib.Signal/EmissionHookMarshaler::user_data
	IL_0084:  callvirt instance bool class GLib.Signal/EmissionHookNative::Invoke([out] valuetype GLib.Signal/InvocationHint&, unsigned int32, native int, native int)
	IL_0089:  stloc.s 5
	IL_008b:  ldloc.2 
	IL_008c:  stloc.s 7
	IL_008e:  ldc.i4.0 
	IL_008f:  stloc.s 8
	IL_0091:  br IL_00b3

	IL_0096:  ldloc.s 7
	IL_0098:  ldloc.s 8
	IL_009a:  ldelema GLib.Value
	IL_009f:  ldobj GLib.Value
	IL_00a4:  stloc.s 6
	IL_00a6:  ldloca.s 6
	IL_00a8:  call instance void valuetype GLib.Value::Dispose()
	IL_00ad:  ldloc.s 8
	IL_00af:  ldc.i4.1 
	IL_00b0:  add 
	IL_00b1:  stloc.s 8
	IL_00b3:  ldloc.s 8
	IL_00b5:  ldloc.s 7
	IL_00b7:  ldlen 
	IL_00b8:  conv.i4 
	IL_00b9:  blt IL_0096

	IL_00be:  ldloc.1 
	IL_00bf:  call void class [mscorlib]System.Runtime.InteropServices.Marshal::FreeHGlobal(native int)
	IL_00c4:  ldloc.s 5
	IL_00c6:  ret 
    } // end of method EmissionHookMarshaler::NativeInvoker

    // method line 656
    .method public hidebysig specialname 
           instance default class GLib.Signal/EmissionHook get_Invoker ()  cil managed 
    {
        // Method begins at RVA 0x7794
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldfld class GLib.Signal/EmissionHook GLib.Signal/EmissionHookMarshaler::'handler'
	IL_0006:  ret 
    } // end of method EmissionHookMarshaler::get_Invoker

	.property instance class GLib.Signal/EmissionHookNative Callback ()
	{
		.get instance default class GLib.Signal/EmissionHookNative GLib.Signal/EmissionHookMarshaler::get_Callback () 
	}
	.property instance class GLib.Signal/EmissionHook Invoker ()
	{
		.get instance default class GLib.Signal/EmissionHook GLib.Signal/EmissionHookMarshaler::get_Invoker () 
	}
  } // end of class EmissionHookMarshaler

  .class nested private auto ansi beforefieldinit SignalClosure
  	extends [mscorlib]System.Object
  	implements [mscorlib]System.IDisposable  {
    .field  assembly  class GLib.Signal signal
    .field  private  native int raw_closure
    .field  private  unsigned int32 id
    .field  private  valuetype [mscorlib]System.Nullable`1<valuetype [mscorlib]System.Runtime.InteropServices.GCHandle> gch
    .field  private static  class [mscorlib]System.Collections.Generic.Dictionary`2<native int,class GLib.Signal/SignalClosure> closures
    .field  private  class [mscorlib]System.EventHandler Disposed
    .custom instance void class [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::'.ctor'() =  (01 00 00 00 ) // ....

    .custom instance void class [mscorlib]System.Diagnostics.DebuggerBrowsableAttribute::'.ctor'(valuetype [mscorlib]System.Diagnostics.DebuggerBrowsableState) =  (01 00 00 00 00 00 00 00 ) // ........

    .field  private  class GLib.ClosureInvokedHandler Invoked
    .custom instance void class [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::'.ctor'() =  (01 00 00 00 ) // ....

    .custom instance void class [mscorlib]System.Diagnostics.DebuggerBrowsableAttribute::'.ctor'(valuetype [mscorlib]System.Diagnostics.DebuggerBrowsableState) =  (01 00 00 00 00 00 00 00 ) // ........

    .field  private static  class GLib.Signal/SignalClosure/ClosureMarshal marshaler
    .field  private static  class GLib.Signal/SignalClosure/ClosureNotify notify_handler

    // method line 657
    .method public final virtual hidebysig newslot 
           instance default void Dispose ()  cil managed 
    {
        // Method begins at RVA 0x779c
	// Code size 111 (0x6f)
	.maxstack 3
	.locals init (
		valuetype [mscorlib]System.Nullable`1<valuetype [mscorlib]System.Runtime.InteropServices.GCHandle>	V_0,
		valuetype [mscorlib]System.Runtime.InteropServices.GCHandle	V_1,
		valuetype [mscorlib]System.Nullable`1<valuetype [mscorlib]System.Runtime.InteropServices.GCHandle>	V_2)
	IL_0000:  ldarg.0 
	IL_0001:  call instance void class GLib.Signal/SignalClosure::Disconnect()
	IL_0006:  ldsfld class [mscorlib]System.Collections.Generic.Dictionary`2<native int,class GLib.Signal/SignalClosure> GLib.Signal/SignalClosure::closures
	IL_000b:  ldarg.0 
	IL_000c:  ldfld native int GLib.Signal/SignalClosure::raw_closure
	IL_0011:  callvirt instance bool class [mscorlib]System.Collections.Generic.Dictionary`2<native int, class GLib.Signal/SignalClosure>::Remove(!0)
	IL_0016:  pop 
	IL_0017:  ldarg.0 
	IL_0018:  ldfld valuetype [mscorlib]System.Nullable`1<valuetype [mscorlib]System.Runtime.InteropServices.GCHandle> GLib.Signal/SignalClosure::gch
	IL_001d:  stloc.0 
	IL_001e:  ldloca.s 0
	IL_0020:  call instance bool valuetype [mscorlib]System.Nullable`1<valuetype [mscorlib]System.Runtime.InteropServices.GCHandle>::get_HasValue()
	IL_0025:  brfalse IL_004c

	IL_002a:  ldarg.0 
	IL_002b:  ldflda valuetype [mscorlib]System.Nullable`1<valuetype [mscorlib]System.Runtime.InteropServices.GCHandle> GLib.Signal/SignalClosure::gch
	IL_0030:  call instance !0 valuetype [mscorlib]System.Nullable`1<valuetype [mscorlib]System.Runtime.InteropServices.GCHandle>::get_Value()
	IL_0035:  stloc.1 
	IL_0036:  ldloca.s 1
	IL_0038:  call instance void valuetype [mscorlib]System.Runtime.InteropServices.GCHandle::Free()
	IL_003d:  ldarg.0 
	IL_003e:  ldloca.s 2
	IL_0040:  initobj valuetype [mscorlib]System.Nullable`1<valuetype [mscorlib]System.Runtime.InteropServices.GCHandle>
	IL_0046:  ldloc.2 
	IL_0047:  stfld valuetype [mscorlib]System.Nullable`1<valuetype [mscorlib]System.Runtime.InteropServices.GCHandle> GLib.Signal/SignalClosure::gch
	IL_004c:  ldarg.0 
	IL_004d:  ldfld class [mscorlib]System.EventHandler GLib.Signal/SignalClosure::Disposed
	IL_0052:  brfalse IL_0068

	IL_0057:  ldarg.0 
	IL_0058:  ldfld class [mscorlib]System.EventHandler GLib.Signal/SignalClosure::Disposed
	IL_005d:  ldarg.0 
	IL_005e:  ldsfld class [mscorlib]System.EventArgs [mscorlib]System.EventArgs::Empty
	IL_0063:  callvirt instance void class [mscorlib]System.EventHandler::Invoke(object, class [mscorlib]System.EventArgs)
	IL_0068:  ldarg.0 
	IL_0069:  call void class [mscorlib]System.GC::SuppressFinalize(object)
	IL_006e:  ret 
    } // end of method SignalClosure::Dispose

    // method line 658
    .method public hidebysig specialname 
           instance default void remove_Disposed (class [mscorlib]System.EventHandler 'value')  cil managed 
    {
        // Method begins at RVA 0x7818
	// Code size 42 (0x2a)
	.maxstack 3
	.locals init (
		class [mscorlib]System.EventHandler	V_0,
		class [mscorlib]System.EventHandler	V_1)
	IL_0000:  ldarg.0 
	IL_0001:  ldfld class [mscorlib]System.EventHandler GLib.Signal/SignalClosure::Disposed
	IL_0006:  stloc.0 
	IL_0007:  ldloc.0 
	IL_0008:  stloc.1 
	IL_0009:  ldarg.0 
	IL_000a:  ldflda class [mscorlib]System.EventHandler GLib.Signal/SignalClosure::Disposed
	IL_000f:  ldloc.1 
	IL_0010:  ldarg.1 
	IL_0011:  call class [mscorlib]System.Delegate class [mscorlib]System.Delegate::Remove(class [mscorlib]System.Delegate, class [mscorlib]System.Delegate)
	IL_0016:  castclass [mscorlib]System.EventHandler
	IL_001b:  ldloc.0 
	IL_001c:  call !!0 class [mscorlib]System.Threading.Interlocked::CompareExchange<class [mscorlib]System.EventHandler> ([out] !!0&, !!0, !!0)
	IL_0021:  stloc.0 
	IL_0022:  ldloc.0 
	IL_0023:  ldloc.1 
	IL_0024:  bne.un IL_0007

	IL_0029:  ret 
    } // end of method SignalClosure::remove_Disposed

    // method line 659
    .method public hidebysig specialname 
           instance default void remove_Invoked (class GLib.ClosureInvokedHandler 'value')  cil managed 
    {
        // Method begins at RVA 0x7850
	// Code size 42 (0x2a)
	.maxstack 3
	.locals init (
		class GLib.ClosureInvokedHandler	V_0,
		class GLib.ClosureInvokedHandler	V_1)
	IL_0000:  ldarg.0 
	IL_0001:  ldfld class GLib.ClosureInvokedHandler GLib.Signal/SignalClosure::Invoked
	IL_0006:  stloc.0 
	IL_0007:  ldloc.0 
	IL_0008:  stloc.1 
	IL_0009:  ldarg.0 
	IL_000a:  ldflda class GLib.ClosureInvokedHandler GLib.Signal/SignalClosure::Invoked
	IL_000f:  ldloc.1 
	IL_0010:  ldarg.1 
	IL_0011:  call class [mscorlib]System.Delegate class [mscorlib]System.Delegate::Remove(class [mscorlib]System.Delegate, class [mscorlib]System.Delegate)
	IL_0016:  castclass GLib.ClosureInvokedHandler
	IL_001b:  ldloc.0 
	IL_001c:  call !!0 class [mscorlib]System.Threading.Interlocked::CompareExchange<class GLib.ClosureInvokedHandler> ([out] !!0&, !!0, !!0)
	IL_0021:  stloc.0 
	IL_0022:  ldloc.0 
	IL_0023:  ldloc.1 
	IL_0024:  bne.un IL_0007

	IL_0029:  ret 
    } // end of method SignalClosure::remove_Invoked

    // method line 660
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (class GLib.Signal sig, class [mscorlib]System.Type args_type)  cil managed 
    {
        // Method begins at RVA 0x7888
	// Code size 64 (0x40)
	.maxstack 4
	IL_0000:  ldarg.0 
	IL_0001:  ldc.i4.m1 
	IL_0002:  stfld unsigned int32 GLib.Signal/SignalClosure::id
	IL_0007:  ldarg.0 
	IL_0008:  call instance void object::'.ctor'()
	IL_000d:  ldarg.0 
	IL_000e:  call class GLib.Signal/SignalClosure/ClosureMarshal class GLib.Signal/SignalClosure::get_Marshaler()
	IL_0013:  call class GLib.Signal/SignalClosure/ClosureNotify class GLib.Signal/SignalClosure::get_Notify()
	IL_0018:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_001d:  call native int class GLib.Signal/SignalClosure::glibsharp_closure_new(class GLib.Signal/SignalClosure/ClosureMarshal, class GLib.Signal/SignalClosure/ClosureNotify, native int)
	IL_0022:  stfld native int GLib.Signal/SignalClosure::raw_closure
	IL_0027:  ldsfld class [mscorlib]System.Collections.Generic.Dictionary`2<native int,class GLib.Signal/SignalClosure> GLib.Signal/SignalClosure::closures
	IL_002c:  ldarg.0 
	IL_002d:  ldfld native int GLib.Signal/SignalClosure::raw_closure
	IL_0032:  ldarg.0 
	IL_0033:  callvirt instance void class [mscorlib]System.Collections.Generic.Dictionary`2<native int, class GLib.Signal/SignalClosure>::set_Item(!0, !1)
	IL_0038:  ldarg.0 
	IL_0039:  ldarg.1 
	IL_003a:  stfld class GLib.Signal GLib.Signal/SignalClosure::signal
	IL_003f:  ret 
    } // end of method SignalClosure::.ctor

    // method line 661
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (class GLib.Signal sig, class [mscorlib]System.Delegate custom_marshaler)  cil managed 
    {
        // Method begins at RVA 0x78d4
	// Code size 117 (0x75)
	.maxstack 4
	.locals init (
		valuetype [mscorlib]System.Nullable`1<valuetype [mscorlib]System.Runtime.InteropServices.GCHandle>	V_0,
		native int	V_1)
	IL_0000:  ldarg.0 
	IL_0001:  ldc.i4.m1 
	IL_0002:  stfld unsigned int32 GLib.Signal/SignalClosure::id
	IL_0007:  ldarg.0 
	IL_0008:  call instance void object::'.ctor'()
	IL_000d:  ldarg.0 
	IL_000e:  ldarg.1 
	IL_000f:  call valuetype [mscorlib]System.Runtime.InteropServices.GCHandle valuetype [mscorlib]System.Runtime.InteropServices.GCHandle::Alloc(object)
	IL_0014:  newobj instance void valuetype [mscorlib]System.Nullable`1<valuetype [mscorlib]System.Runtime.InteropServices.GCHandle>::'.ctor'(!0)
	IL_0019:  stfld valuetype [mscorlib]System.Nullable`1<valuetype [mscorlib]System.Runtime.InteropServices.GCHandle> GLib.Signal/SignalClosure::gch
	IL_001e:  ldarg.0 
	IL_001f:  ldarg.2 
	IL_0020:  ldarg.0 
	IL_0021:  ldfld valuetype [mscorlib]System.Nullable`1<valuetype [mscorlib]System.Runtime.InteropServices.GCHandle> GLib.Signal/SignalClosure::gch
	IL_0026:  stloc.0 
	IL_0027:  ldloca.s 0
	IL_0029:  call instance bool valuetype [mscorlib]System.Nullable`1<valuetype [mscorlib]System.Runtime.InteropServices.GCHandle>::get_HasValue()
	IL_002e:  brfalse IL_0044

	IL_0033:  ldloca.s 0
	IL_0035:  call instance !0 valuetype [mscorlib]System.Nullable`1<valuetype [mscorlib]System.Runtime.InteropServices.GCHandle>::GetValueOrDefault()
	IL_003a:  call native int valuetype [mscorlib]System.Runtime.InteropServices.GCHandle::op_Explicit(valuetype [mscorlib]System.Runtime.InteropServices.GCHandle)
	IL_003f:  br IL_004d

	IL_0044:  ldloca.s 1
	IL_0046:  initobj native int
	IL_004c:  ldloc.1 
	IL_004d:  call class GLib.Signal/SignalClosure/ClosureNotify class GLib.Signal/SignalClosure::get_Notify()
	IL_0052:  call native int class GLib.Signal/SignalClosure::g_cclosure_new(class [mscorlib]System.Delegate, native int, class GLib.Signal/SignalClosure/ClosureNotify)
	IL_0057:  stfld native int GLib.Signal/SignalClosure::raw_closure
	IL_005c:  ldsfld class [mscorlib]System.Collections.Generic.Dictionary`2<native int,class GLib.Signal/SignalClosure> GLib.Signal/SignalClosure::closures
	IL_0061:  ldarg.0 
	IL_0062:  ldfld native int GLib.Signal/SignalClosure::raw_closure
	IL_0067:  ldarg.0 
	IL_0068:  callvirt instance void class [mscorlib]System.Collections.Generic.Dictionary`2<native int, class GLib.Signal/SignalClosure>::set_Item(!0, !1)
	IL_006d:  ldarg.0 
	IL_006e:  ldarg.1 
	IL_006f:  stfld class GLib.Signal GLib.Signal/SignalClosure::signal
	IL_0074:  ret 
    } // end of method SignalClosure::.ctor

    // method line 662
    .method public hidebysig specialname 
           instance default void add_Disposed (class [mscorlib]System.EventHandler 'value')  cil managed 
    {
        // Method begins at RVA 0x7958
	// Code size 42 (0x2a)
	.maxstack 3
	.locals init (
		class [mscorlib]System.EventHandler	V_0,
		class [mscorlib]System.EventHandler	V_1)
	IL_0000:  ldarg.0 
	IL_0001:  ldfld class [mscorlib]System.EventHandler GLib.Signal/SignalClosure::Disposed
	IL_0006:  stloc.0 
	IL_0007:  ldloc.0 
	IL_0008:  stloc.1 
	IL_0009:  ldarg.0 
	IL_000a:  ldflda class [mscorlib]System.EventHandler GLib.Signal/SignalClosure::Disposed
	IL_000f:  ldloc.1 
	IL_0010:  ldarg.1 
	IL_0011:  call class [mscorlib]System.Delegate class [mscorlib]System.Delegate::Combine(class [mscorlib]System.Delegate, class [mscorlib]System.Delegate)
	IL_0016:  castclass [mscorlib]System.EventHandler
	IL_001b:  ldloc.0 
	IL_001c:  call !!0 class [mscorlib]System.Threading.Interlocked::CompareExchange<class [mscorlib]System.EventHandler> ([out] !!0&, !!0, !!0)
	IL_0021:  stloc.0 
	IL_0022:  ldloc.0 
	IL_0023:  ldloc.1 
	IL_0024:  bne.un IL_0007

	IL_0029:  ret 
    } // end of method SignalClosure::add_Disposed

    // method line 663
    .method public hidebysig specialname 
           instance default void add_Invoked (class GLib.ClosureInvokedHandler 'value')  cil managed 
    {
        // Method begins at RVA 0x7990
	// Code size 42 (0x2a)
	.maxstack 3
	.locals init (
		class GLib.ClosureInvokedHandler	V_0,
		class GLib.ClosureInvokedHandler	V_1)
	IL_0000:  ldarg.0 
	IL_0001:  ldfld class GLib.ClosureInvokedHandler GLib.Signal/SignalClosure::Invoked
	IL_0006:  stloc.0 
	IL_0007:  ldloc.0 
	IL_0008:  stloc.1 
	IL_0009:  ldarg.0 
	IL_000a:  ldflda class GLib.ClosureInvokedHandler GLib.Signal/SignalClosure::Invoked
	IL_000f:  ldloc.1 
	IL_0010:  ldarg.1 
	IL_0011:  call class [mscorlib]System.Delegate class [mscorlib]System.Delegate::Combine(class [mscorlib]System.Delegate, class [mscorlib]System.Delegate)
	IL_0016:  castclass GLib.ClosureInvokedHandler
	IL_001b:  ldloc.0 
	IL_001c:  call !!0 class [mscorlib]System.Threading.Interlocked::CompareExchange<class GLib.ClosureInvokedHandler> ([out] !!0&, !!0, !!0)
	IL_0021:  stloc.0 
	IL_0022:  ldloc.0 
	IL_0023:  ldloc.1 
	IL_0024:  bne.un IL_0007

	IL_0029:  ret 
    } // end of method SignalClosure::add_Invoked

    // method line 664
    .method public hidebysig 
           instance default void Connect (bool is_after)  cil managed 
    {
        // Method begins at RVA 0x79c8
	// Code size 59 (0x3b)
	.maxstack 5
	.locals init (
		native int	V_0)
	IL_0000:  ldarg.0 
	IL_0001:  ldfld class GLib.Signal GLib.Signal/SignalClosure::signal
	IL_0006:  ldfld string GLib.Signal::name
	IL_000b:  call native int class GLib.Marshaller::StringToPtrGStrdup(string)
	IL_0010:  stloc.0 
	IL_0011:  ldarg.0 
	IL_0012:  ldarg.0 
	IL_0013:  ldfld class GLib.Signal GLib.Signal/SignalClosure::signal
	IL_0018:  ldfld class GLib.ToggleRef GLib.Signal::tref
	IL_001d:  callvirt instance native int class GLib.ToggleRef::get_Handle()
	IL_0022:  ldloc.0 
	IL_0023:  ldarg.0 
	IL_0024:  ldfld native int GLib.Signal/SignalClosure::raw_closure
	IL_0029:  ldarg.1 
	IL_002a:  call unsigned int32 class GLib.Signal/SignalClosure::g_signal_connect_closure(native int, native int, native int, bool)
	IL_002f:  stfld unsigned int32 GLib.Signal/SignalClosure::id
	IL_0034:  ldloc.0 
	IL_0035:  call void class GLib.Marshaller::Free(native int)
	IL_003a:  ret 
    } // end of method SignalClosure::Connect

    // method line 665
    .method private static hidebysig specialname 
           default class GLib.Signal/SignalClosure/ClosureMarshal get_Marshaler ()  cil managed 
    {
        // Method begins at RVA 0x7a10
	// Code size 33 (0x21)
	.maxstack 8
	IL_0000:  ldsfld class GLib.Signal/SignalClosure/ClosureMarshal GLib.Signal/SignalClosure::marshaler
	IL_0005:  brtrue IL_001b

	IL_000a:  ldnull 
	IL_000b:  ldftn void class GLib.Signal/SignalClosure::MarshalCallback(native int, valuetype GLib.Value*, unsigned int32, valuetype GLib.Value*, native int, native int)
	IL_0011:  newobj instance void class GLib.Signal/SignalClosure/ClosureMarshal::'.ctor'(object, native int)
	IL_0016:  stsfld class GLib.Signal/SignalClosure/ClosureMarshal GLib.Signal/SignalClosure::marshaler
	IL_001b:  ldsfld class GLib.Signal/SignalClosure/ClosureMarshal GLib.Signal/SignalClosure::marshaler
	IL_0020:  ret 
    } // end of method SignalClosure::get_Marshaler

    // method line 666
    .method private static hidebysig specialname 
           default class GLib.Signal/SignalClosure/ClosureNotify get_Notify ()  cil managed 
    {
        // Method begins at RVA 0x7a34
	// Code size 33 (0x21)
	.maxstack 8
	IL_0000:  ldsfld class GLib.Signal/SignalClosure/ClosureNotify GLib.Signal/SignalClosure::notify_handler
	IL_0005:  brtrue IL_001b

	IL_000a:  ldnull 
	IL_000b:  ldftn void class GLib.Signal/SignalClosure::NotifyCallback(native int, native int)
	IL_0011:  newobj instance void class GLib.Signal/SignalClosure/ClosureNotify::'.ctor'(object, native int)
	IL_0016:  stsfld class GLib.Signal/SignalClosure/ClosureNotify GLib.Signal/SignalClosure::notify_handler
	IL_001b:  ldsfld class GLib.Signal/SignalClosure/ClosureNotify GLib.Signal/SignalClosure::notify_handler
	IL_0020:  ret 
    } // end of method SignalClosure::get_Notify

    // method line 667
    .method private static hidebysig pinvokeimpl ("glibsharpglue-2" as "glibsharp_closure_new" cdecl )
           default native int glibsharp_closure_new (class GLib.Signal/SignalClosure/ClosureMarshal marshaler, class GLib.Signal/SignalClosure/ClosureNotify notify, native int gch)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method SignalClosure::glibsharp_closure_new

    // method line 668
    .method private static hidebysig pinvokeimpl ("libgobject-2.0-0.dll" as "g_cclosure_new" cdecl )
           default native int g_cclosure_new (class [mscorlib]System.Delegate cb, native int user_data, class GLib.Signal/SignalClosure/ClosureNotify notify)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method SignalClosure::g_cclosure_new

    // method line 669
    .method private static hidebysig pinvokeimpl ("libgobject-2.0-0.dll" as "g_signal_connect_closure" cdecl )
           default unsigned int32 g_signal_connect_closure (native int obj, native int name, native int closure, bool is_after)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method SignalClosure::g_signal_connect_closure

    // method line 670
    .method public hidebysig 
           instance default void Disconnect ()  cil managed 
    {
        // Method begins at RVA 0x7a58
	// Code size 72 (0x48)
	.maxstack 2
	IL_0000:  ldarg.0 
	IL_0001:  ldfld unsigned int32 GLib.Signal/SignalClosure::id
	IL_0006:  ldc.i4.m1 
	IL_0007:  beq IL_0047

	IL_000c:  ldarg.0 
	IL_000d:  ldfld class GLib.Signal GLib.Signal/SignalClosure::signal
	IL_0012:  ldfld class GLib.ToggleRef GLib.Signal::tref
	IL_0017:  callvirt instance native int class GLib.ToggleRef::get_Handle()
	IL_001c:  ldarg.0 
	IL_001d:  ldfld unsigned int32 GLib.Signal/SignalClosure::id
	IL_0022:  call bool class GLib.Signal/SignalClosure::g_signal_handler_is_connected(native int, unsigned int32)
	IL_0027:  brfalse IL_0047

	IL_002c:  ldarg.0 
	IL_002d:  ldfld class GLib.Signal GLib.Signal/SignalClosure::signal
	IL_0032:  ldfld class GLib.ToggleRef GLib.Signal::tref
	IL_0037:  callvirt instance native int class GLib.ToggleRef::get_Handle()
	IL_003c:  ldarg.0 
	IL_003d:  ldfld unsigned int32 GLib.Signal/SignalClosure::id
	IL_0042:  call void class GLib.Signal/SignalClosure::g_signal_handler_disconnect(native int, unsigned int32)
	IL_0047:  ret 
    } // end of method SignalClosure::Disconnect

    // method line 671
    .method private static hidebysig pinvokeimpl ("libgobject-2.0-0.dll" as "g_signal_handler_is_connected" cdecl )
           default bool g_signal_handler_is_connected (native int 'instance', unsigned int32 'handler')  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method SignalClosure::g_signal_handler_is_connected

    // method line 672
    .method private static hidebysig pinvokeimpl ("libgobject-2.0-0.dll" as "g_signal_handler_disconnect" cdecl )
           default void g_signal_handler_disconnect (native int 'instance', unsigned int32 'handler')  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method SignalClosure::g_signal_handler_disconnect

    // method line 673
    .method public hidebysig 
           instance default void Invoke (class GLib.ClosureInvokedArgs args)  cil managed 
    {
        // Method begins at RVA 0x7aac
	// Code size 26 (0x1a)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldfld class GLib.ClosureInvokedHandler GLib.Signal/SignalClosure::Invoked
	IL_0006:  brtrue IL_000c

	IL_000b:  ret 
	IL_000c:  ldarg.0 
	IL_000d:  ldfld class GLib.ClosureInvokedHandler GLib.Signal/SignalClosure::Invoked
	IL_0012:  ldarg.0 
	IL_0013:  ldarg.1 
	IL_0014:  callvirt instance void class GLib.ClosureInvokedHandler::Invoke(object, class GLib.ClosureInvokedArgs)
	IL_0019:  ret 
    } // end of method SignalClosure::Invoke

    // method line 674
    .method private static hidebysig 
           default void MarshalCallback (native int raw_closure, valuetype GLib.Value* return_val, unsigned int32 n_param_vals, valuetype GLib.Value* param_values, native int invocation_hint, native int marshal_data)  cil managed 
    {
        // Method begins at RVA 0x7ac8
	// Code size 319 (0x13f)
	.maxstack 5
	.locals init (
		class GLib.Signal/SignalClosure	V_0,
		class GLib.Object	V_1,
		class GLib.SignalArgs	V_2,
		int32	V_3,
		class GLib.ClosureInvokedArgs	V_4,
		int32	V_5,
		class [mscorlib]System.Exception	V_6)
	IL_0000:  ldnull 
	IL_0001:  stloc.0 
	.try { // 0
	  IL_0002:  ldsfld class [mscorlib]System.Collections.Generic.Dictionary`2<native int,class GLib.Signal/SignalClosure> GLib.Signal/SignalClosure::closures
	  IL_0007:  ldarg.0 
	  IL_0008:  callvirt instance !1 class [mscorlib]System.Collections.Generic.Dictionary`2<native int, class GLib.Signal/SignalClosure>::get_Item(!0)
	  IL_000d:  stloc.0 
	  IL_000e:  ldarg.3 
	  IL_000f:  call instance object valuetype GLib.Value::get_Val()
	  IL_0014:  isinst GLib.Object
	  IL_0019:  stloc.1 
	  IL_001a:  ldloc.1 
	  IL_001b:  brtrue IL_0025

	  IL_0020:  leave IL_013e

	  IL_0025:  ldloc.0 
	  IL_0026:  ldfld class GLib.Signal GLib.Signal/SignalClosure::signal
	  IL_002b:  ldfld class [mscorlib]System.Type GLib.Signal::args_type
	  IL_0030:  ldtoken [mscorlib]System.EventArgs
	  IL_0035:  call class [mscorlib]System.Type class [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
	  IL_003a:  call bool class [mscorlib]System.Type::op_Equality(class [mscorlib]System.Type, class [mscorlib]System.Type)
	  IL_003f:  brfalse IL_005a

	  IL_0044:  ldloc.0 
	  IL_0045:  ldloc.1 
	  IL_0046:  ldsfld class [mscorlib]System.EventArgs [mscorlib]System.EventArgs::Empty
	  IL_004b:  newobj instance void class GLib.ClosureInvokedArgs::'.ctor'(class GLib.Object, class [mscorlib]System.EventArgs)
	  IL_0050:  callvirt instance void class GLib.Signal/SignalClosure::Invoke(class GLib.ClosureInvokedArgs)
	  IL_0055:  leave IL_013e

	  IL_005a:  ldloc.0 
	  IL_005b:  ldfld class GLib.Signal GLib.Signal/SignalClosure::signal
	  IL_0060:  ldfld class [mscorlib]System.Type GLib.Signal::args_type
	  IL_0065:  call class GLib.SignalArgs class GLib.FastActivator::CreateSignalArgs(class [mscorlib]System.Type)
	  IL_006a:  stloc.2 
	  IL_006b:  ldloc.2 
	  IL_006c:  ldarg.2 
	  IL_006d:  ldc.i4.1 
	  IL_006e:  sub 
	  IL_006f:  conv.u 
	  IL_0070:  newarr object
	  IL_0075:  callvirt instance void class GLib.SignalArgs::set_Args(object[])
	  IL_007a:  ldc.i4.1 
	  IL_007b:  stloc.3 
	  IL_007c:  br IL_009f

	  IL_0081:  ldloc.2 
	  IL_0082:  callvirt instance object[] class GLib.SignalArgs::get_Args()
	  IL_0087:  ldloc.3 
	  IL_0088:  ldc.i4.1 
	  IL_0089:  sub 
	  IL_008a:  ldarg.3 
	  IL_008b:  ldloc.3 
	  IL_008c:  conv.i 
	  IL_008d:  sizeof GLib.Value
	  IL_0093:  mul 
	  IL_0094:  add 
	  IL_0095:  call instance object valuetype GLib.Value::get_Val()
	  IL_009a:  stelem.ref 
	  IL_009b:  ldloc.3 
	  IL_009c:  ldc.i4.1 
	  IL_009d:  add 
	  IL_009e:  stloc.3 
	  IL_009f:  ldloc.3 
	  IL_00a0:  conv.i8 
	  IL_00a1:  ldarg.2 
	  IL_00a2:  conv.u8 
	  IL_00a3:  blt IL_0081

	  IL_00a8:  ldloc.1 
	  IL_00a9:  ldloc.2 
	  IL_00aa:  newobj instance void class GLib.ClosureInvokedArgs::'.ctor'(class GLib.Object, class [mscorlib]System.EventArgs)
	  IL_00af:  stloc.s 4
	  IL_00b1:  ldloc.0 
	  IL_00b2:  ldloc.s 4
	  IL_00b4:  callvirt instance void class GLib.Signal/SignalClosure::Invoke(class GLib.ClosureInvokedArgs)
	  IL_00b9:  ldc.i4.1 
	  IL_00ba:  stloc.s 5
	  IL_00bc:  br IL_00e3

	  IL_00c1:  ldarg.3 
	  IL_00c2:  ldloc.s 5
	  IL_00c4:  conv.i 
	  IL_00c5:  sizeof GLib.Value
	  IL_00cb:  mul 
	  IL_00cc:  add 
	  IL_00cd:  ldloc.2 
	  IL_00ce:  callvirt instance object[] class GLib.SignalArgs::get_Args()
	  IL_00d3:  ldloc.s 5
	  IL_00d5:  ldc.i4.1 
	  IL_00d6:  sub 
	  IL_00d7:  ldelem.ref 
	  IL_00d8:  call instance void valuetype GLib.Value::Update(object)
	  IL_00dd:  ldloc.s 5
	  IL_00df:  ldc.i4.1 
	  IL_00e0:  add 
	  IL_00e1:  stloc.s 5
	  IL_00e3:  ldloc.s 5
	  IL_00e5:  conv.i8 
	  IL_00e6:  ldarg.2 
	  IL_00e7:  conv.u8 
	  IL_00e8:  blt IL_00c1

	  IL_00ed:  ldarg.1 
	  IL_00ee:  brfalse IL_00fe

	  IL_00f3:  ldloc.2 
	  IL_00f4:  callvirt instance object class GLib.SignalArgs::get_RetVal()
	  IL_00f9:  brtrue IL_0103

	  IL_00fe:  leave IL_013e

	  IL_0103:  ldarg.1 
	  IL_0104:  ldloc.2 
	  IL_0105:  callvirt instance object class GLib.SignalArgs::get_RetVal()
	  IL_010a:  call instance void valuetype GLib.Value::set_Val(object)
	  IL_010f:  leave IL_013e

	} // end .try 0
	catch class [mscorlib]System.Exception { // 0
	  IL_0114:  stloc.s 6
	  IL_0116:  ldloc.0 
	  IL_0117:  brfalse IL_0131

	  IL_011c:  ldstr "Marshaling {0} signal"
	  IL_0121:  ldloc.0 
	  IL_0122:  ldfld class GLib.Signal GLib.Signal/SignalClosure::signal
	  IL_0127:  ldfld string GLib.Signal::name
	  IL_012c:  call void class [mscorlib]System.Console::WriteLine(string, object)
	  IL_0131:  ldloc.s 6
	  IL_0133:  ldc.i4.0 
	  IL_0134:  call void class GLib.ExceptionManager::RaiseUnhandledException(class [mscorlib]System.Exception, bool)
	  IL_0139:  leave IL_013e

	} // end handler 0
	IL_013e:  ret 
    } // end of method SignalClosure::MarshalCallback

    // method line 675
    .method private static hidebysig 
           default void NotifyCallback (native int data, native int raw_closure)  cil managed 
    {
        // Method begins at RVA 0x7c30
	// Code size 25 (0x19)
	.maxstack 3
	.locals init (
		class GLib.Signal/SignalClosure	V_0)
	IL_0000:  ldsfld class [mscorlib]System.Collections.Generic.Dictionary`2<native int,class GLib.Signal/SignalClosure> GLib.Signal/SignalClosure::closures
	IL_0005:  ldarg.1 
	IL_0006:  ldloca.s 0
	IL_0008:  callvirt instance bool class [mscorlib]System.Collections.Generic.Dictionary`2<native int, class GLib.Signal/SignalClosure>::TryGetValue(!0, [out] !1&)
	IL_000d:  brfalse IL_0018

	IL_0012:  ldloc.0 
	IL_0013:  callvirt instance void class GLib.Signal/SignalClosure::Dispose()
	IL_0018:  ret 
    } // end of method SignalClosure::NotifyCallback

    // method line 676
    .method private static hidebysig specialname rtspecialname 
           default void '.cctor' ()  cil managed 
    {
        // Method begins at RVA 0x7c58
	// Code size 16 (0x10)
	.maxstack 8
	IL_0000:  ldsfld class [mscorlib]System.Collections.Generic.IEqualityComparer`1<native int> GLib.IntPtrEqualityComparer::Instance
	IL_0005:  newobj instance void class [mscorlib]System.Collections.Generic.Dictionary`2<native int, class GLib.Signal/SignalClosure>::'.ctor'(class [mscorlib]System.Collections.Generic.IEqualityComparer`1<!0>)
	IL_000a:  stsfld class [mscorlib]System.Collections.Generic.Dictionary`2<native int,class GLib.Signal/SignalClosure> GLib.Signal/SignalClosure::closures
	IL_000f:  ret 
    } // end of method SignalClosure::.cctor

	.property class GLib.Signal/SignalClosure/ClosureMarshal Marshaler ()
	{
		.get default class GLib.Signal/SignalClosure/ClosureMarshal GLib.Signal/SignalClosure::get_Marshaler () 
	}
	.property class GLib.Signal/SignalClosure/ClosureNotify Notify ()
	{
		.get default class GLib.Signal/SignalClosure/ClosureNotify GLib.Signal/SignalClosure::get_Notify () 
	}
	.event [mscorlib]System.EventHandler Disposed
	{
		.removeon instance default void GLib.Signal/SignalClosure::remove_Disposed (class [mscorlib]System.EventHandler 'value') 
		.addon instance default void GLib.Signal/SignalClosure::add_Disposed (class [mscorlib]System.EventHandler 'value') 
	}
	.event GLib.ClosureInvokedHandler Invoked
	{
		.addon instance default void GLib.Signal/SignalClosure::add_Invoked (class GLib.ClosureInvokedHandler 'value') 
		.removeon instance default void GLib.Signal/SignalClosure::remove_Invoked (class GLib.ClosureInvokedHandler 'value') 
	}
  .class nested private auto ansi sealed ClosureMarshal
  	extends [mscorlib]System.MulticastDelegate
  {
    .custom instance void class [mscorlib]System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute::'.ctor'(valuetype [mscorlib]System.Runtime.InteropServices.CallingConvention) =  (01 00 02 00 00 00 00 00 ) // ........


    // method line 677
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (object 'object', native int 'method')  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method ClosureMarshal::.ctor

    // method line 678
    .method public virtual hidebysig newslot 
           instance default void Invoke (native int closure, valuetype GLib.Value* return_val, unsigned int32 n_param_vals, valuetype GLib.Value* param_values, native int invocation_hint, native int marshal_data)  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method ClosureMarshal::Invoke

    // method line 679
    .method public virtual hidebysig newslot 
           instance default class [mscorlib]System.IAsyncResult BeginInvoke (native int closure, valuetype GLib.Value* return_val, unsigned int32 n_param_vals, valuetype GLib.Value* param_values, native int invocation_hint, native int marshal_data, class [mscorlib]System.AsyncCallback callback, object 'object')  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method ClosureMarshal::BeginInvoke

    // method line 680
    .method public virtual hidebysig newslot 
           instance default void EndInvoke (class [mscorlib]System.IAsyncResult result)  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method ClosureMarshal::EndInvoke

  } // end of class ClosureMarshal

  .class nested private auto ansi sealed ClosureNotify
  	extends [mscorlib]System.MulticastDelegate
  {
    .custom instance void class [mscorlib]System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute::'.ctor'(valuetype [mscorlib]System.Runtime.InteropServices.CallingConvention) =  (01 00 02 00 00 00 00 00 ) // ........


    // method line 681
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (object 'object', native int 'method')  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method ClosureNotify::.ctor

    // method line 682
    .method public virtual hidebysig newslot 
           instance default void Invoke (native int data, native int closure)  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method ClosureNotify::Invoke

    // method line 683
    .method public virtual hidebysig newslot 
           instance default class [mscorlib]System.IAsyncResult BeginInvoke (native int data, native int closure, class [mscorlib]System.AsyncCallback callback, object 'object')  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method ClosureNotify::BeginInvoke

    // method line 684
    .method public virtual hidebysig newslot 
           instance default void EndInvoke (class [mscorlib]System.IAsyncResult result)  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method ClosureNotify::EndInvoke

  } // end of class ClosureNotify

  } // end of class SignalClosure

  } // end of class GLib.Signal
}

.namespace GLib
{
  .class public auto ansi sealed serializable beforefieldinit SignalAttribute
  	extends [mscorlib]System.Attribute
  {
    .custom instance void class [mscorlib]System.AttributeUsageAttribute::'.ctor'(valuetype [mscorlib]System.AttributeTargets) =  (
		01 00 00 02 00 00 01 00 54 02 09 49 6E 68 65 72   // ........T..Inher
		69 74 65 64 00                                  ) // ited.

    .field  private  string cname

    // method line 685
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (string cname)  cil managed 
    {
        // Method begins at RVA 0x7c6c
	// Code size 14 (0xe)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void class [mscorlib]System.Attribute::'.ctor'()
	IL_0006:  ldarg.0 
	IL_0007:  ldarg.1 
	IL_0008:  stfld string GLib.SignalAttribute::cname
	IL_000d:  ret 
    } // end of method SignalAttribute::.ctor

    // method line 686
    .method private hidebysig specialname rtspecialname 
           instance default void '.ctor' ()  cil managed 
    {
        // Method begins at RVA 0x7c7c
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void class [mscorlib]System.Attribute::'.ctor'()
	IL_0006:  ret 
    } // end of method SignalAttribute::.ctor

    // method line 687
    .method public hidebysig specialname 
           instance default string get_CName ()  cil managed 
    {
        // Method begins at RVA 0x7c84
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldfld string GLib.SignalAttribute::cname
	IL_0006:  ret 
    } // end of method SignalAttribute::get_CName

	.property instance string CName ()
	{
		.get instance default string GLib.SignalAttribute::get_CName () 
	}
  } // end of class GLib.SignalAttribute
}

.namespace GLib
{
  .class public auto ansi abstract beforefieldinit SignalCallback
  	extends [mscorlib]System.Object
  	implements [mscorlib]System.IDisposable  {
    .custom instance void class [mscorlib]System.ObsoleteAttribute::'.ctor'(string) =  (
		01 00 18 52 65 70 6C 61 63 65 64 20 62 79 20 47   // ...Replaced by G
		4C 69 62 2E 53 69 67 6E 61 6C 2E 00 00          ) // Lib.Signal...

    .field  family static  int32 _NextKey
    .field  family static  class [mscorlib]System.Collections.Generic.Dictionary`2<int32,class GLib.SignalCallback> _Instances
    .field  family  class GLib.Object _obj
    .field  family  class [mscorlib]System.Delegate _handler
    .field  family  int32 _key
    .field  family  class [mscorlib]System.Type _argstype
    .field  family  unsigned int32 _HandlerID

    // method line 688
    .method family hidebysig specialname rtspecialname 
           instance default void '.ctor' (class GLib.Object obj, class [mscorlib]System.Delegate eh, class [mscorlib]System.Type argstype)  cil managed 
    {
        // Method begins at RVA 0x7c8c
	// Code size 64 (0x40)
	.maxstack 4
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::'.ctor'()
	IL_0006:  ldarg.0 
	IL_0007:  ldsfld int32 GLib.SignalCallback::_NextKey
	IL_000c:  dup 
	IL_000d:  ldc.i4.1 
	IL_000e:  add 
	IL_000f:  stsfld int32 GLib.SignalCallback::_NextKey
	IL_0014:  stfld int32 GLib.SignalCallback::_key
	IL_0019:  ldarg.0 
	IL_001a:  ldarg.1 
	IL_001b:  stfld class GLib.Object GLib.SignalCallback::_obj
	IL_0020:  ldarg.0 
	IL_0021:  ldarg.2 
	IL_0022:  stfld class [mscorlib]System.Delegate GLib.SignalCallback::_handler
	IL_0027:  ldarg.0 
	IL_0028:  ldarg.3 
	IL_0029:  stfld class [mscorlib]System.Type GLib.SignalCallback::_argstype
	IL_002e:  ldsfld class [mscorlib]System.Collections.Generic.Dictionary`2<int32,class GLib.SignalCallback> GLib.SignalCallback::_Instances
	IL_0033:  ldarg.0 
	IL_0034:  ldfld int32 GLib.SignalCallback::_key
	IL_0039:  ldarg.0 
	IL_003a:  callvirt instance void class [mscorlib]System.Collections.Generic.Dictionary`2<int32, class GLib.SignalCallback>::set_Item(!0, !1)
	IL_003f:  ret 
    } // end of method SignalCallback::.ctor

    // method line 689
    .method public hidebysig 
           instance default void AddDelegate (class [mscorlib]System.Delegate d)  cil managed 
    {
        // Method begins at RVA 0x7cd8
	// Code size 19 (0x13)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldarg.0 
	IL_0002:  ldfld class [mscorlib]System.Delegate GLib.SignalCallback::_handler
	IL_0007:  ldarg.1 
	IL_0008:  call class [mscorlib]System.Delegate class [mscorlib]System.Delegate::Combine(class [mscorlib]System.Delegate, class [mscorlib]System.Delegate)
	IL_000d:  stfld class [mscorlib]System.Delegate GLib.SignalCallback::_handler
	IL_0012:  ret 
    } // end of method SignalCallback::AddDelegate

    // method line 690
    .method public hidebysig 
           instance default void RemoveDelegate (class [mscorlib]System.Delegate d)  cil managed 
    {
        // Method begins at RVA 0x7cec
	// Code size 19 (0x13)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldarg.0 
	IL_0002:  ldfld class [mscorlib]System.Delegate GLib.SignalCallback::_handler
	IL_0007:  ldarg.1 
	IL_0008:  call class [mscorlib]System.Delegate class [mscorlib]System.Delegate::Remove(class [mscorlib]System.Delegate, class [mscorlib]System.Delegate)
	IL_000d:  stfld class [mscorlib]System.Delegate GLib.SignalCallback::_handler
	IL_0012:  ret 
    } // end of method SignalCallback::RemoveDelegate

    // method line 691
    .method private static hidebysig pinvokeimpl ("libgobject-2.0-0.dll" as "g_signal_connect_data" cdecl )
           default unsigned int32 g_signal_connect_data (native int obj, native int name, class [mscorlib]System.Delegate cb, int32 key, native int p, int32 'flags')  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method SignalCallback::g_signal_connect_data

    // method line 692
    .method family hidebysig 
           instance default void Connect (string name, class [mscorlib]System.Delegate cb, int32 'flags')  cil managed 
    {
        // Method begins at RVA 0x7d00
	// Code size 51 (0x33)
	.maxstack 7
	.locals init (
		native int	V_0)
	IL_0000:  ldarg.1 
	IL_0001:  call native int class GLib.Marshaller::StringToPtrGStrdup(string)
	IL_0006:  stloc.0 
	IL_0007:  ldarg.0 
	IL_0008:  ldarg.0 
	IL_0009:  ldfld class GLib.Object GLib.SignalCallback::_obj
	IL_000e:  callvirt instance native int class GLib.Object::get_Handle()
	IL_0013:  ldloc.0 
	IL_0014:  ldarg.2 
	IL_0015:  ldarg.0 
	IL_0016:  ldfld int32 GLib.SignalCallback::_key
	IL_001b:  ldc.i4.0 
	IL_001c:  newobj instance void native int::'.ctor'(int32)
	IL_0021:  ldarg.3 
	IL_0022:  call unsigned int32 class GLib.SignalCallback::g_signal_connect_data(native int, native int, class [mscorlib]System.Delegate, int32, native int, int32)
	IL_0027:  stfld unsigned int32 GLib.SignalCallback::_HandlerID
	IL_002c:  ldloc.0 
	IL_002d:  call void class GLib.Marshaller::Free(native int)
	IL_0032:  ret 
    } // end of method SignalCallback::Connect

    // method line 693
    .method private static hidebysig pinvokeimpl ("libgobject-2.0-0.dll" as "g_signal_handler_disconnect" cdecl )
           default void g_signal_handler_disconnect (native int 'instance', unsigned int32 'handler')  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method SignalCallback::g_signal_handler_disconnect

    // method line 694
    .method private static hidebysig pinvokeimpl ("libgobject-2.0-0.dll" as "g_signal_handler_is_connected" cdecl )
           default bool g_signal_handler_is_connected (native int 'instance', unsigned int32 'handler')  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method SignalCallback::g_signal_handler_is_connected

    // method line 695
    .method family hidebysig 
           instance default void Disconnect ()  cil managed 
    {
        // Method begins at RVA 0x7d40
	// Code size 50 (0x32)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldfld class GLib.Object GLib.SignalCallback::_obj
	IL_0006:  callvirt instance native int class GLib.Object::get_Handle()
	IL_000b:  ldarg.0 
	IL_000c:  ldfld unsigned int32 GLib.SignalCallback::_HandlerID
	IL_0011:  call bool class GLib.SignalCallback::g_signal_handler_is_connected(native int, unsigned int32)
	IL_0016:  brfalse IL_0031

	IL_001b:  ldarg.0 
	IL_001c:  ldfld class GLib.Object GLib.SignalCallback::_obj
	IL_0021:  callvirt instance native int class GLib.Object::get_Handle()
	IL_0026:  ldarg.0 
	IL_0027:  ldfld unsigned int32 GLib.SignalCallback::_HandlerID
	IL_002c:  call void class GLib.SignalCallback::g_signal_handler_disconnect(native int, unsigned int32)
	IL_0031:  ret 
    } // end of method SignalCallback::Disconnect

    // method line 696
    .method public final virtual hidebysig newslot 
           instance default void Dispose ()  cil managed 
    {
        // Method begins at RVA 0x7d74
	// Code size 14 (0xe)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldc.i4.1 
	IL_0002:  callvirt instance void class GLib.SignalCallback::Dispose(bool)
	IL_0007:  ldarg.0 
	IL_0008:  call void class [mscorlib]System.GC::SuppressFinalize(object)
	IL_000d:  ret 
    } // end of method SignalCallback::Dispose

    // method line 697
    .method family virtual hidebysig newslot 
           instance default void Dispose (bool disposing)  cil managed 
    {
        // Method begins at RVA 0x7d84
	// Code size 28 (0x1c)
	.maxstack 8
	IL_0000:  ldarg.1 
	IL_0001:  brfalse IL_001b

	IL_0006:  ldarg.0 
	IL_0007:  ldnull 
	IL_0008:  stfld class GLib.Object GLib.SignalCallback::_obj
	IL_000d:  ldarg.0 
	IL_000e:  ldnull 
	IL_000f:  stfld class [mscorlib]System.Delegate GLib.SignalCallback::_handler
	IL_0014:  ldarg.0 
	IL_0015:  ldnull 
	IL_0016:  stfld class [mscorlib]System.Type GLib.SignalCallback::_argstype
	IL_001b:  ret 
    } // end of method SignalCallback::Dispose

    // method line 698
    .method family virtual hidebysig 
           instance default void Finalize ()  cil managed 
    {
        // Method begins at RVA 0x7da4
	// Code size 20 (0x14)
	.maxstack 2
	.try { // 0
	  IL_0000:  ldarg.0 
	  IL_0001:  ldc.i4.0 
	  IL_0002:  callvirt instance void class GLib.SignalCallback::Dispose(bool)
	  IL_0007:  leave IL_0013

	} // end .try 0
	finally  { // 0
	  IL_000c:  ldarg.0 
	  IL_000d:  call instance void object::Finalize()
	  IL_0012:  endfinally 
	} // end handler 0
	IL_0013:  ret 
    } // end of method SignalCallback::Finalize

    // method line 699
    .method private static hidebysig specialname rtspecialname 
           default void '.cctor' ()  cil managed 
    {
        // Method begins at RVA 0x7dd4
	// Code size 17 (0x11)
	.maxstack 8
	IL_0000:  ldc.i4.0 
	IL_0001:  stsfld int32 GLib.SignalCallback::_NextKey
	IL_0006:  newobj instance void class [mscorlib]System.Collections.Generic.Dictionary`2<int32, class GLib.SignalCallback>::'.ctor'()
	IL_000b:  stsfld class [mscorlib]System.Collections.Generic.Dictionary`2<int32,class GLib.SignalCallback> GLib.SignalCallback::_Instances
	IL_0010:  ret 
    } // end of method SignalCallback::.cctor

  } // end of class GLib.SignalCallback
}

.namespace GLib
{
  .class private auto ansi beforefieldinit ClosureInvokedArgs
  	extends [mscorlib]System.EventArgs
  {
    .field  private  class [mscorlib]System.EventArgs args
    .field  private  class GLib.Object obj
    .field  private  object result

    // method line 700
    .method public hidebysig specialname 
           instance default class GLib.Object get_Target ()  cil managed 
    {
        // Method begins at RVA 0x7de8
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldfld class GLib.Object GLib.ClosureInvokedArgs::obj
	IL_0006:  ret 
    } // end of method ClosureInvokedArgs::get_Target

    // method line 701
    .method public hidebysig specialname 
           instance default class [mscorlib]System.EventArgs get_Args ()  cil managed 
    {
        // Method begins at RVA 0x7df0
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldfld class [mscorlib]System.EventArgs GLib.ClosureInvokedArgs::args
	IL_0006:  ret 
    } // end of method ClosureInvokedArgs::get_Args

    // method line 702
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (class GLib.Object obj, class [mscorlib]System.EventArgs args)  cil managed 
    {
        // Method begins at RVA 0x7df8
	// Code size 21 (0x15)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void class [mscorlib]System.EventArgs::'.ctor'()
	IL_0006:  ldarg.0 
	IL_0007:  ldarg.1 
	IL_0008:  stfld class GLib.Object GLib.ClosureInvokedArgs::obj
	IL_000d:  ldarg.0 
	IL_000e:  ldarg.2 
	IL_000f:  stfld class [mscorlib]System.EventArgs GLib.ClosureInvokedArgs::args
	IL_0014:  ret 
    } // end of method ClosureInvokedArgs::.ctor

	.property instance class [mscorlib]System.EventArgs Args ()
	{
		.get instance default class [mscorlib]System.EventArgs GLib.ClosureInvokedArgs::get_Args () 
	}
	.property instance class GLib.Object Target ()
	{
		.get instance default class GLib.Object GLib.ClosureInvokedArgs::get_Target () 
	}
  } // end of class GLib.ClosureInvokedArgs
}

.namespace GLib
{
  .class private auto ansi sealed ClosureInvokedHandler
  	extends [mscorlib]System.MulticastDelegate
  {

    // method line 703
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (object 'object', native int 'method')  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method ClosureInvokedHandler::.ctor

    // method line 704
    .method public virtual hidebysig newslot 
           instance default void Invoke (object o, class GLib.ClosureInvokedArgs args)  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method ClosureInvokedHandler::Invoke

    // method line 705
    .method public virtual hidebysig newslot 
           instance default class [mscorlib]System.IAsyncResult BeginInvoke (object o, class GLib.ClosureInvokedArgs args, class [mscorlib]System.AsyncCallback callback, object 'object')  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method ClosureInvokedHandler::BeginInvoke

    // method line 706
    .method public virtual hidebysig newslot 
           instance default void EndInvoke (class [mscorlib]System.IAsyncResult result)  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method ClosureInvokedHandler::EndInvoke

  } // end of class GLib.ClosureInvokedHandler
}

.namespace GLib
{
  .class public auto ansi beforefieldinit SList
  	extends GLib.ListBase
  {
    .field  private static  class GLib.ListElementFree '<>f__mg$cache0'
    .custom instance void class [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::'.ctor'() =  (01 00 00 00 ) // ....


    // method line 707
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (native int raw, class [mscorlib]System.Type element_type, bool owned, bool elements_owned)  cil managed 
    {
        // Method begins at RVA 0x7e10
	// Code size 41 (0x29)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldarg.1 
	IL_0002:  ldarg.2 
	IL_0003:  ldarg.3 
	IL_0004:  ldarg.s 4
	IL_0006:  ldsfld class GLib.ListElementFree GLib.SList::'<>f__mg$cache0'
	IL_000b:  brtrue.s IL_001e

	IL_000d:  ldnull 
	IL_000e:  ldftn void class GLib.SList::g_free(native int)
	IL_0014:  newobj instance void class GLib.ListElementFree::'.ctor'(object, native int)
	IL_0019:  stsfld class GLib.ListElementFree GLib.SList::'<>f__mg$cache0'
	IL_001e:  ldsfld class GLib.ListElementFree GLib.SList::'<>f__mg$cache0'
	IL_0023:  call instance void class GLib.ListBase::'.ctor'(native int, class [mscorlib]System.Type, bool, bool, class GLib.ListElementFree)
	IL_0028:  ret 
    } // end of method SList::.ctor

    // method line 708
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (native int raw, class [mscorlib]System.Type element_type, bool owned, bool elements_owned, class GLib.ListElementFree free_func)  cil managed 
    {
        // Method begins at RVA 0x7e3c
	// Code size 14 (0xe)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldarg.1 
	IL_0002:  ldarg.2 
	IL_0003:  ldarg.3 
	IL_0004:  ldarg.s 4
	IL_0006:  ldarg.s 5
	IL_0008:  call instance void class GLib.ListBase::'.ctor'(native int, class [mscorlib]System.Type, bool, bool, class GLib.ListElementFree)
	IL_000d:  ret 
    } // end of method SList::.ctor

    // method line 709
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (native int raw)  cil managed 
    {
        // Method begins at RVA 0x7e4c
	// Code size 9 (0x9)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldarg.1 
	IL_0002:  ldnull 
	IL_0003:  call instance void class GLib.SList::'.ctor'(native int, class [mscorlib]System.Type)
	IL_0008:  ret 
    } // end of method SList::.ctor

    // method line 710
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (native int raw, class [mscorlib]System.Type element_type)  cil managed 
    {
        // Method begins at RVA 0x7e58
	// Code size 11 (0xb)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldarg.1 
	IL_0002:  ldarg.2 
	IL_0003:  ldc.i4.0 
	IL_0004:  ldc.i4.0 
	IL_0005:  call instance void class GLib.SList::'.ctor'(native int, class [mscorlib]System.Type, bool, bool)
	IL_000a:  ret 
    } // end of method SList::.ctor

    // method line 711
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (class [mscorlib]System.Type element_type)  cil managed 
    {
        // Method begins at RVA 0x7e64
	// Code size 13 (0xd)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_0006:  ldarg.1 
	IL_0007:  call instance void class GLib.SList::'.ctor'(native int, class [mscorlib]System.Type)
	IL_000c:  ret 
    } // end of method SList::.ctor

    // method line 712
    .method private static hidebysig pinvokeimpl ("libglib-2.0-0.dll" as "g_free" cdecl )
           default void g_free (native int item)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method SList::g_free

    // method line 713
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (object[] members, class [mscorlib]System.Type element_type, bool owned, bool elements_owned)  cil managed 
    {
        // Method begins at RVA 0x7e74
	// Code size 49 (0x31)
	.maxstack 5
	.locals init (
		object	V_0,
		object[]	V_1,
		int32	V_2)
	IL_0000:  ldarg.0 
	IL_0001:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_0006:  ldarg.2 
	IL_0007:  ldarg.3 
	IL_0008:  ldarg.s 4
	IL_000a:  call instance void class GLib.SList::'.ctor'(native int, class [mscorlib]System.Type, bool, bool)
	IL_000f:  ldarg.1 
	IL_0010:  stloc.1 
	IL_0011:  ldc.i4.0 
	IL_0012:  stloc.2 
	IL_0013:  br IL_0027

	IL_0018:  ldloc.1 
	IL_0019:  ldloc.2 
	IL_001a:  ldelem.ref 
	IL_001b:  stloc.0 
	IL_001c:  ldarg.0 
	IL_001d:  ldloc.0 
	IL_001e:  call instance void class GLib.ListBase::Append(object)
	IL_0023:  ldloc.2 
	IL_0024:  ldc.i4.1 
	IL_0025:  add 
	IL_0026:  stloc.2 
	IL_0027:  ldloc.2 
	IL_0028:  ldloc.1 
	IL_0029:  ldlen 
	IL_002a:  conv.i4 
	IL_002b:  blt IL_0018

	IL_0030:  ret 
    } // end of method SList::.ctor

    // method line 714
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (class [mscorlib]System.Array members, class [mscorlib]System.Type element_type, bool owned, bool elements_owned)  cil managed 
    {
        // Method begins at RVA 0x7eb4
	// Code size 78 (0x4e)
	.maxstack 5
	.locals init (
		object	V_0,
		class [mscorlib]System.Collections.IEnumerator	V_1,
		class [mscorlib]System.IDisposable	V_2)
	IL_0000:  ldarg.0 
	IL_0001:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_0006:  ldarg.2 
	IL_0007:  ldarg.3 
	IL_0008:  ldarg.s 4
	IL_000a:  call instance void class GLib.SList::'.ctor'(native int, class [mscorlib]System.Type, bool, bool)
	IL_000f:  ldarg.1 
	IL_0010:  callvirt instance class [mscorlib]System.Collections.IEnumerator class [mscorlib]System.Array::GetEnumerator()
	IL_0015:  stloc.1 
	.try { // 0
	  IL_0016:  br IL_0029

	  IL_001b:  ldloc.1 
	  IL_001c:  callvirt instance object class [mscorlib]System.Collections.IEnumerator::get_Current()
	  IL_0021:  stloc.0 
	  IL_0022:  ldarg.0 
	  IL_0023:  ldloc.0 
	  IL_0024:  call instance void class GLib.ListBase::Append(object)
	  IL_0029:  ldloc.1 
	  IL_002a:  callvirt instance bool class [mscorlib]System.Collections.IEnumerator::MoveNext()
	  IL_002f:  brtrue IL_001b

	  IL_0034:  leave IL_004d

	} // end .try 0
	finally  { // 0
	  IL_0039:  ldloc.1 
	  IL_003a:  isinst [mscorlib]System.IDisposable
	  IL_003f:  dup 
	  IL_0040:  stloc.2 
	  IL_0041:  brfalse IL_004c

	  IL_0046:  ldloc.2 
	  IL_0047:  callvirt instance void class [mscorlib]System.IDisposable::Dispose()
	  IL_004c:  endfinally 
	} // end handler 0
	IL_004d:  ret 
    } // end of method SList::.ctor

    // method line 715
    .method private static hidebysig pinvokeimpl ("libglib-2.0-0.dll" as "g_slist_copy" cdecl )
           default native int g_slist_copy (native int l)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method SList::g_slist_copy

    // method line 716
    .method public virtual hidebysig 
           instance default object Clone ()  cil managed 
    {
        // Method begins at RVA 0x7f20
	// Code size 17 (0x11)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance native int class GLib.ListBase::get_Handle()
	IL_0006:  call native int class GLib.SList::g_slist_copy(native int)
	IL_000b:  newobj instance void class GLib.SList::'.ctor'(native int)
	IL_0010:  ret 
    } // end of method SList::Clone

    // method line 717
    .method private static hidebysig pinvokeimpl ("libglib-2.0-0.dll" as "g_slist_length" cdecl )
           default int32 g_slist_length (native int l)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method SList::g_slist_length

    // method line 718
    .method assembly virtual hidebysig 
           instance default int32 Length (native int list)  cil managed 
    {
        // Method begins at RVA 0x7f34
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.1 
	IL_0001:  call int32 class GLib.SList::g_slist_length(native int)
	IL_0006:  ret 
    } // end of method SList::Length

    // method line 719
    .method private static hidebysig pinvokeimpl ("libglib-2.0-0.dll" as "g_slist_free" cdecl )
           default void g_slist_free (native int l)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method SList::g_slist_free

    // method line 720
    .method assembly virtual hidebysig 
           instance default void Free (native int list)  cil managed 
    {
        // Method begins at RVA 0x7f3c
	// Code size 23 (0x17)
	.maxstack 8
	IL_0000:  ldarg.1 
	IL_0001:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_0006:  call bool native int::op_Inequality(native int, native int)
	IL_000b:  brfalse IL_0016

	IL_0010:  ldarg.1 
	IL_0011:  call void class GLib.SList::g_slist_free(native int)
	IL_0016:  ret 
    } // end of method SList::Free

    // method line 721
    .method private static hidebysig pinvokeimpl ("libglib-2.0-0.dll" as "g_slist_append" cdecl )
           default native int g_slist_append (native int l, native int raw)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method SList::g_slist_append

    // method line 722
    .method assembly virtual hidebysig 
           instance default native int Append (native int list, native int raw)  cil managed 
    {
        // Method begins at RVA 0x7f54
	// Code size 8 (0x8)
	.maxstack 8
	IL_0000:  ldarg.1 
	IL_0001:  ldarg.2 
	IL_0002:  call native int class GLib.SList::g_slist_append(native int, native int)
	IL_0007:  ret 
    } // end of method SList::Append

    // method line 723
    .method private static hidebysig pinvokeimpl ("libglib-2.0-0.dll" as "g_slist_prepend" cdecl )
           default native int g_slist_prepend (native int l, native int raw)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method SList::g_slist_prepend

    // method line 724
    .method assembly virtual hidebysig 
           instance default native int Prepend (native int list, native int raw)  cil managed 
    {
        // Method begins at RVA 0x7f60
	// Code size 8 (0x8)
	.maxstack 8
	IL_0000:  ldarg.1 
	IL_0001:  ldarg.2 
	IL_0002:  call native int class GLib.SList::g_slist_prepend(native int, native int)
	IL_0007:  ret 
    } // end of method SList::Prepend

    // method line 725
    .method private static hidebysig pinvokeimpl ("libglib-2.0-0.dll" as "g_slist_nth_data" cdecl )
           default native int g_slist_nth_data (native int l, unsigned int32 n)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method SList::g_slist_nth_data

    // method line 726
    .method assembly virtual hidebysig 
           instance default native int NthData (unsigned int32 n)  cil managed 
    {
        // Method begins at RVA 0x7f6c
	// Code size 13 (0xd)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance native int class GLib.ListBase::get_Handle()
	IL_0006:  ldarg.1 
	IL_0007:  call native int class GLib.SList::g_slist_nth_data(native int, unsigned int32)
	IL_000c:  ret 
    } // end of method SList::NthData

  } // end of class GLib.SList
}

.namespace GLib
{
  .class public auto ansi sealed GSourceFunc
  	extends [mscorlib]System.MulticastDelegate
  {

    // method line 727
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (object 'object', native int 'method')  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method GSourceFunc::.ctor

    // method line 728
    .method public virtual hidebysig newslot 
           instance default bool Invoke ()  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method GSourceFunc::Invoke

    // method line 729
    .method public virtual hidebysig newslot 
           instance default class [mscorlib]System.IAsyncResult BeginInvoke (class [mscorlib]System.AsyncCallback callback, object 'object')  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method GSourceFunc::BeginInvoke

    // method line 730
    .method public virtual hidebysig newslot 
           instance default bool EndInvoke (class [mscorlib]System.IAsyncResult result)  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method GSourceFunc::EndInvoke

  } // end of class GLib.GSourceFunc
}

.namespace GLib
{
  .class public auto ansi beforefieldinit Source
  	extends [mscorlib]System.Object
  {
    .field  assembly static  class [mscorlib]System.Collections.Generic.Dictionary`2<unsigned int32,class GLib.SourceProxy> source_handlers

    // method line 735
    .method assembly static hidebysig 
           default void Add (class GLib.SourceProxy proxy)  cil managed 
    {
        // Method begins at RVA 0x7f7c
	// Code size 67 (0x43)
	.maxstack 3
	.locals init (
		object	V_0,
		bool	V_1)
	IL_0000:  ldsfld class [mscorlib]System.Collections.Generic.Dictionary`2<unsigned int32,class GLib.SourceProxy> GLib.Source::source_handlers
	IL_0005:  stloc.0 
	IL_0006:  ldc.i4.0 
	IL_0007:  stloc.1 
	.try { // 0
	  IL_0008:  ldloc.0 
	  IL_0009:  ldloca.s 1
	  IL_000b:  call void class [mscorlib]System.Threading.Monitor::Enter(object, [out] bool&)
	  IL_0010:  ldarg.0 
	  IL_0011:  ldfld bool GLib.SourceProxy::needsAdd
	  IL_0016:  brfalse IL_0033

	  IL_001b:  ldarg.0 
	  IL_001c:  ldc.i4.0 
	  IL_001d:  stfld bool GLib.SourceProxy::needsAdd
	  IL_0022:  ldsfld class [mscorlib]System.Collections.Generic.Dictionary`2<unsigned int32,class GLib.SourceProxy> GLib.Source::source_handlers
	  IL_0027:  ldarg.0 
	  IL_0028:  ldfld unsigned int32 GLib.SourceProxy::ID
	  IL_002d:  ldarg.0 
	  IL_002e:  callvirt instance void class [mscorlib]System.Collections.Generic.Dictionary`2<unsigned int32, class GLib.SourceProxy>::set_Item(!0, !1)
	  IL_0033:  leave IL_0042

	} // end .try 0
	finally  { // 0
	  IL_0038:  ldloc.1 
	  IL_0039:  brfalse.s IL_0041

	  IL_003b:  ldloc.0 
	  IL_003c:  call void class [mscorlib]System.Threading.Monitor::Exit(object)
	  IL_0041:  endfinally 
	} // end handler 0
	IL_0042:  ret 
    } // end of method Source::Add

    // method line 736
    .method private hidebysig specialname rtspecialname 
           instance default void '.ctor' ()  cil managed 
    {
        // Method begins at RVA 0x7fdc
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::'.ctor'()
	IL_0006:  ret 
    } // end of method Source::.ctor

    // method line 737
    .method private static hidebysig pinvokeimpl ("libglib-2.0-0.dll" as "g_source_remove" cdecl )
           default bool g_source_remove (unsigned int32 tag)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method Source::g_source_remove

    // method line 738
    .method public static hidebysig 
           default bool Remove (unsigned int32 tag)  cil managed 
    {
        // Method begins at RVA 0x7fe4
	// Code size 66 (0x42)
	.maxstack 3
	.locals init (
		bool	V_0,
		object	V_1,
		bool	V_2,
		class GLib.SourceProxy	V_3)
	IL_0000:  ldc.i4.1 
	IL_0001:  stloc.0 
	IL_0002:  ldsfld class [mscorlib]System.Collections.Generic.Dictionary`2<unsigned int32,class GLib.SourceProxy> GLib.Source::source_handlers
	IL_0007:  stloc.1 
	IL_0008:  ldc.i4.0 
	IL_0009:  stloc.2 
	.try { // 0
	  IL_000a:  ldloc.1 
	  IL_000b:  ldloca.s 2
	  IL_000d:  call void class [mscorlib]System.Threading.Monitor::Enter(object, [out] bool&)
	  IL_0012:  ldsfld class [mscorlib]System.Collections.Generic.Dictionary`2<unsigned int32,class GLib.SourceProxy> GLib.Source::source_handlers
	  IL_0017:  ldarg.0 
	  IL_0018:  ldloca.s 3
	  IL_001a:  callvirt instance bool class [mscorlib]System.Collections.Generic.Dictionary`2<unsigned int32, class GLib.SourceProxy>::TryGetValue(!0, [out] !1&)
	  IL_001f:  brfalse IL_0031

	  IL_0024:  ldarg.0 
	  IL_0025:  call bool class GLib.Source::g_source_remove(unsigned int32)
	  IL_002a:  stloc.0 
	  IL_002b:  ldloc.3 
	  IL_002c:  callvirt instance void class GLib.SourceProxy::Remove()
	  IL_0031:  leave IL_0040

	} // end .try 0
	finally  { // 0
	  IL_0036:  ldloc.2 
	  IL_0037:  brfalse.s IL_003f

	  IL_0039:  ldloc.1 
	  IL_003a:  call void class [mscorlib]System.Threading.Monitor::Exit(object)
	  IL_003f:  endfinally 
	} // end handler 0
	IL_0040:  ldloc.0 
	IL_0041:  ret 
    } // end of method Source::Remove

    // method line 739
    .method private static hidebysig specialname rtspecialname 
           default void '.cctor' ()  cil managed 
    {
        // Method begins at RVA 0x8044
	// Code size 11 (0xb)
	.maxstack 8
	IL_0000:  newobj instance void class [mscorlib]System.Collections.Generic.Dictionary`2<unsigned int32, class GLib.SourceProxy>::'.ctor'()
	IL_0005:  stsfld class [mscorlib]System.Collections.Generic.Dictionary`2<unsigned int32,class GLib.SourceProxy> GLib.Source::source_handlers
	IL_000a:  ret 
    } // end of method Source::.cctor

  } // end of class GLib.Source
}

.namespace GLib
{
  .class public auto ansi sealed SpawnError
  	extends [mscorlib]System.Enum
  {
    .field  public specialname rtspecialname  int32 value__
    .field public static literal  valuetype GLib.SpawnError Fork = int32(0x00000000)
    .field public static literal  valuetype GLib.SpawnError Read = int32(0x00000001)
    .field public static literal  valuetype GLib.SpawnError Chdir = int32(0x00000002)
    .field public static literal  valuetype GLib.SpawnError Acces = int32(0x00000003)
    .field public static literal  valuetype GLib.SpawnError Perm = int32(0x00000004)
    .field public static literal  valuetype GLib.SpawnError TooBig = int32(0x00000005)
    .field public static literal  valuetype GLib.SpawnError NoExec = int32(0x00000006)
    .field public static literal  valuetype GLib.SpawnError NameTooLong = int32(0x00000007)
    .field public static literal  valuetype GLib.SpawnError NoEnt = int32(0x00000008)
    .field public static literal  valuetype GLib.SpawnError NoMem = int32(0x00000009)
    .field public static literal  valuetype GLib.SpawnError NotDir = int32(0x0000000a)
    .field public static literal  valuetype GLib.SpawnError Loop = int32(0x0000000b)
    .field public static literal  valuetype GLib.SpawnError TxtBusy = int32(0x0000000c)
    .field public static literal  valuetype GLib.SpawnError IO = int32(0x0000000d)
    .field public static literal  valuetype GLib.SpawnError NFile = int32(0x0000000e)
    .field public static literal  valuetype GLib.SpawnError MFile = int32(0x0000000f)
    .field public static literal  valuetype GLib.SpawnError Inval = int32(0x00000010)
    .field public static literal  valuetype GLib.SpawnError IsDir = int32(0x00000011)
    .field public static literal  valuetype GLib.SpawnError LibBad = int32(0x00000012)
    .field public static literal  valuetype GLib.SpawnError Failed = int32(0x00000013)

  } // end of class GLib.SpawnError
}

.namespace GLib
{
  .class public auto ansi sealed SpawnFlags
  	extends [mscorlib]System.Enum
  {
    .custom instance void class [mscorlib]System.FlagsAttribute::'.ctor'() =  (01 00 00 00 ) // ....

    .field  public specialname rtspecialname  int32 value__
    .field public static literal  valuetype GLib.SpawnFlags LeaveDescriptorsOpen = int32(0x00000001)
    .field public static literal  valuetype GLib.SpawnFlags DoNotReapChild = int32(0x00000002)
    .field public static literal  valuetype GLib.SpawnFlags SearchPath = int32(0x00000004)
    .field public static literal  valuetype GLib.SpawnFlags StdoutToDevNull = int32(0x00000008)
    .field public static literal  valuetype GLib.SpawnFlags StderrToDevNull = int32(0x00000010)
    .field public static literal  valuetype GLib.SpawnFlags ChildInheritsStdin = int32(0x00000020)
    .field public static literal  valuetype GLib.SpawnFlags FileAndArgvZero = int32(0x00000040)

  } // end of class GLib.SpawnFlags
}

.namespace GLib
{
  .class public auto ansi sealed SpawnChildSetupFunc
  	extends [mscorlib]System.MulticastDelegate
  {

    // method line 740
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (object 'object', native int 'method')  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method SpawnChildSetupFunc::.ctor

    // method line 741
    .method public virtual hidebysig newslot 
           instance default void Invoke ()  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method SpawnChildSetupFunc::Invoke

    // method line 742
    .method public virtual hidebysig newslot 
           instance default class [mscorlib]System.IAsyncResult BeginInvoke (class [mscorlib]System.AsyncCallback callback, object 'object')  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method SpawnChildSetupFunc::BeginInvoke

    // method line 743
    .method public virtual hidebysig newslot 
           instance default void EndInvoke (class [mscorlib]System.IAsyncResult result)  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method SpawnChildSetupFunc::EndInvoke

  } // end of class GLib.SpawnChildSetupFunc
}

.namespace GLib
{
  .class private auto ansi sealed SpawnChildSetupFuncNative
  	extends [mscorlib]System.MulticastDelegate
  {
    .custom instance void class [mscorlib]System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute::'.ctor'(valuetype [mscorlib]System.Runtime.InteropServices.CallingConvention) =  (01 00 02 00 00 00 00 00 ) // ........


    // method line 744
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (object 'object', native int 'method')  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method SpawnChildSetupFuncNative::.ctor

    // method line 745
    .method public virtual hidebysig newslot 
           instance default void Invoke (native int gch)  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method SpawnChildSetupFuncNative::Invoke

    // method line 746
    .method public virtual hidebysig newslot 
           instance default class [mscorlib]System.IAsyncResult BeginInvoke (native int gch, class [mscorlib]System.AsyncCallback callback, object 'object')  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method SpawnChildSetupFuncNative::BeginInvoke

    // method line 747
    .method public virtual hidebysig newslot 
           instance default void EndInvoke (class [mscorlib]System.IAsyncResult result)  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method SpawnChildSetupFuncNative::EndInvoke

  } // end of class GLib.SpawnChildSetupFuncNative
}

.namespace GLib
{
  .class private auto ansi beforefieldinit SpawnChildSetupWrapper
  	extends [mscorlib]System.Object
  {
    .field  private  class GLib.SpawnChildSetupFunc 'handler'
    .field  public  native int Data
    .field  public  class GLib.SpawnChildSetupFuncNative NativeCallback

    // method line 748
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (class GLib.SpawnChildSetupFunc 'handler')  cil managed 
    {
        // Method begins at RVA 0x8050
	// Code size 56 (0x38)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::'.ctor'()
	IL_0006:  ldarg.1 
	IL_0007:  brtrue IL_000d

	IL_000c:  ret 
	IL_000d:  ldarg.0 
	IL_000e:  ldarg.1 
	IL_000f:  stfld class GLib.SpawnChildSetupFunc GLib.SpawnChildSetupWrapper::'handler'
	IL_0014:  ldarg.0 
	IL_0015:  ldarg.0 
	IL_0016:  call valuetype [mscorlib]System.Runtime.InteropServices.GCHandle valuetype [mscorlib]System.Runtime.InteropServices.GCHandle::Alloc(object)
	IL_001b:  call native int valuetype [mscorlib]System.Runtime.InteropServices.GCHandle::op_Explicit(valuetype [mscorlib]System.Runtime.InteropServices.GCHandle)
	IL_0020:  stfld native int GLib.SpawnChildSetupWrapper::Data
	IL_0025:  ldarg.0 
	IL_0026:  ldnull 
	IL_0027:  ldftn void class GLib.SpawnChildSetupWrapper::InvokeHandler(native int)
	IL_002d:  newobj instance void class GLib.SpawnChildSetupFuncNative::'.ctor'(object, native int)
	IL_0032:  stfld class GLib.SpawnChildSetupFuncNative GLib.SpawnChildSetupWrapper::NativeCallback
	IL_0037:  ret 
    } // end of method SpawnChildSetupWrapper::.ctor

    // method line 749
    .method private static hidebysig 
           default void InvokeHandler (native int data)  cil managed 
    {
        // Method begins at RVA 0x808c
	// Code size 54 (0x36)
	.maxstack 2
	.locals init (
		valuetype [mscorlib]System.Runtime.InteropServices.GCHandle	V_0)
	IL_0000:  ldarg.0 
	IL_0001:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_0006:  call bool native int::op_Equality(native int, native int)
	IL_000b:  brfalse IL_0011

	IL_0010:  ret 
	IL_0011:  ldarg.0 
	IL_0012:  call valuetype [mscorlib]System.Runtime.InteropServices.GCHandle valuetype [mscorlib]System.Runtime.InteropServices.GCHandle::op_Explicit(native int)
	IL_0017:  stloc.0 
	IL_0018:  ldloca.s 0
	IL_001a:  call instance object valuetype [mscorlib]System.Runtime.InteropServices.GCHandle::get_Target()
	IL_001f:  isinst GLib.SpawnChildSetupWrapper
	IL_0024:  ldfld class GLib.SpawnChildSetupFunc GLib.SpawnChildSetupWrapper::'handler'
	IL_0029:  callvirt instance void class GLib.SpawnChildSetupFunc::Invoke()
	IL_002e:  ldloca.s 0
	IL_0030:  call instance void valuetype [mscorlib]System.Runtime.InteropServices.GCHandle::Free()
	IL_0035:  ret 
    } // end of method SpawnChildSetupWrapper::InvokeHandler

  } // end of class GLib.SpawnChildSetupWrapper
}

.namespace GLib
{
  .class public auto ansi beforefieldinit Process
  	extends [mscorlib]System.Object
  {
    .field public static literal  int32 IgnorePipe = int32(0x7fffffff)
    .field public static literal  int32 RequestPipe = int32(0x00000000)
    .field  private  int64 pid

    // method line 750
    .method private hidebysig specialname rtspecialname 
           instance default void '.ctor' (int32 pid)  cil managed 
    {
        // Method begins at RVA 0x80d0
	// Code size 15 (0xf)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::'.ctor'()
	IL_0006:  ldarg.0 
	IL_0007:  ldarg.1 
	IL_0008:  conv.i8 
	IL_0009:  stfld int64 GLib.Process::pid
	IL_000e:  ret 
    } // end of method Process::.ctor

    // method line 751
    .method private static hidebysig pinvokeimpl ("libglib-2.0-0.dll" as "g_spawn_close_pid" cdecl )
           default void g_spawn_close_pid (int32 pid)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method Process::g_spawn_close_pid

    // method line 752
    .method public hidebysig 
           instance default void Close ()  cil managed 
    {
        // Method begins at RVA 0x80e0
	// Code size 13 (0xd)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldfld int64 GLib.Process::pid
	IL_0006:  conv.i4 
	IL_0007:  call void class GLib.Process::g_spawn_close_pid(int32)
	IL_000c:  ret 
    } // end of method Process::Close

    // method line 753
    .method private static hidebysig pinvokeimpl ("libglib-2.0-0.dll" as "g_spawn_async" cdecl )
           default bool g_spawn_async (native int dir, native int[] argv, native int[] envp, int32 'flags', class GLib.SpawnChildSetupFuncNative func, native int data, [out] int32& pid, [out] native int& 'error')  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method Process::g_spawn_async

    // method line 754
    .method private static hidebysig pinvokeimpl ("libglib-2.0-0.dll" as "g_spawn_async_utf8" cdecl )
           default bool g_spawn_async_utf8 (native int dir, native int[] argv, native int[] envp, int32 'flags', class GLib.SpawnChildSetupFuncNative func, native int data, [out] int32& pid, [out] native int& 'error')  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method Process::g_spawn_async_utf8

    // method line 755
    .method public static hidebysig 
           default bool SpawnAsync (string working_directory, string[] argv, string[] envp, valuetype GLib.SpawnFlags 'flags', class GLib.SpawnChildSetupFunc child_setup, [out] class GLib.Process& child_process)  cil managed 
    {
        // Method begins at RVA 0x80f0
	// Code size 160 (0xa0)
	.maxstack 8
	.locals init (
		int32	V_0,
		native int	V_1,
		native int	V_2,
		native int[]	V_3,
		native int[]	V_4,
		class GLib.SpawnChildSetupWrapper	V_5,
		bool	V_6)
	IL_0000:  ldarg.0 
	IL_0001:  call native int class GLib.Marshaller::StringToPtrGStrdup(string)
	IL_0006:  stloc.2 
	IL_0007:  ldarg.1 
	IL_0008:  call native int[] class GLib.Marshaller::StringArrayToNullTermPointer(string[])
	IL_000d:  stloc.3 
	IL_000e:  ldarg.2 
	IL_000f:  call native int[] class GLib.Marshaller::StringArrayToNullTermPointer(string[])
	IL_0014:  stloc.s 4
	IL_0016:  ldarg.s 4
	IL_0018:  newobj instance void class GLib.SpawnChildSetupWrapper::'.ctor'(class GLib.SpawnChildSetupFunc)
	IL_001d:  stloc.s 5
	IL_001f:  call bool class GLib.Global::get_IsWindowsPlatform()
	IL_0024:  brfalse IL_004c

	IL_0029:  ldloc.2 
	IL_002a:  ldloc.3 
	IL_002b:  ldloc.s 4
	IL_002d:  ldarg.3 
	IL_002e:  ldloc.s 5
	IL_0030:  ldfld class GLib.SpawnChildSetupFuncNative GLib.SpawnChildSetupWrapper::NativeCallback
	IL_0035:  ldloc.s 5
	IL_0037:  ldfld native int GLib.SpawnChildSetupWrapper::Data
	IL_003c:  ldloca.s 0
	IL_003e:  ldloca.s 1
	IL_0040:  call bool class GLib.Process::g_spawn_async_utf8(native int, native int[], native int[], int32, class GLib.SpawnChildSetupFuncNative, native int, [out] int32&, [out] native int&)
	IL_0045:  stloc.s 6
	IL_0047:  br IL_006a

	IL_004c:  ldloc.2 
	IL_004d:  ldloc.3 
	IL_004e:  ldloc.s 4
	IL_0050:  ldarg.3 
	IL_0051:  ldloc.s 5
	IL_0053:  ldfld class GLib.SpawnChildSetupFuncNative GLib.SpawnChildSetupWrapper::NativeCallback
	IL_0058:  ldloc.s 5
	IL_005a:  ldfld native int GLib.SpawnChildSetupWrapper::Data
	IL_005f:  ldloca.s 0
	IL_0061:  ldloca.s 1
	IL_0063:  call bool class GLib.Process::g_spawn_async(native int, native int[], native int[], int32, class GLib.SpawnChildSetupFuncNative, native int, [out] int32&, [out] native int&)
	IL_0068:  stloc.s 6
	IL_006a:  ldarg.s 5
	IL_006c:  ldloc.0 
	IL_006d:  newobj instance void class GLib.Process::'.ctor'(int32)
	IL_0072:  stind.ref 
	IL_0073:  ldloc.2 
	IL_0074:  call void class GLib.Marshaller::Free(native int)
	IL_0079:  ldloc.3 
	IL_007a:  call void class GLib.Marshaller::Free(native int[])
	IL_007f:  ldloc.s 4
	IL_0081:  call void class GLib.Marshaller::Free(native int[])
	IL_0086:  ldloc.1 
	IL_0087:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_008c:  call bool native int::op_Inequality(native int, native int)
	IL_0091:  brfalse IL_009d

	IL_0096:  ldloc.1 
	IL_0097:  newobj instance void class GLib.GException::'.ctor'(native int)
	IL_009c:  throw 
	IL_009d:  ldloc.s 6
	IL_009f:  ret 
    } // end of method Process::SpawnAsync

    // method line 756
    .method private static hidebysig pinvokeimpl ("libglib-2.0-0.dll" as "g_spawn_async_with_pipes" cdecl )
           default bool g_spawn_async_with_pipes (native int dir, native int[] argv, native int[] envp, int32 'flags', class GLib.SpawnChildSetupFuncNative func, native int data, [out] int32& pid, native int stdin, native int stdout, native int stderr, [out] native int& 'error')  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method Process::g_spawn_async_with_pipes

    // method line 757
    .method private static hidebysig pinvokeimpl ("libglib-2.0-0.dll" as "g_spawn_async_with_pipes_utf8" cdecl )
           default bool g_spawn_async_with_pipes_utf8 (native int dir, native int[] argv, native int[] envp, int32 'flags', class GLib.SpawnChildSetupFuncNative func, native int data, [out] int32& pid, native int stdin, native int stdout, native int stderr, [out] native int& 'error')  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method Process::g_spawn_async_with_pipes_utf8

    // method line 758
    .method public static hidebysig 
           default bool SpawnAsyncWithPipes (string working_directory, string[] argv, string[] envp, valuetype GLib.SpawnFlags 'flags', class GLib.SpawnChildSetupFunc child_setup, [out] class GLib.Process& child_process, int32& stdin, int32& stdout, int32& stderr)  cil managed 
    {
        // Method begins at RVA 0x819c
	// Code size 367 (0x16f)
	.maxstack 11
	.locals init (
		int32	V_0,
		native int	V_1,
		native int	V_2,
		native int[]	V_3,
		native int[]	V_4,
		class GLib.SpawnChildSetupWrapper	V_5,
		native int	V_6,
		native int	V_7,
		native int	V_8,
		bool	V_9)
	IL_0000:  ldarg.0 
	IL_0001:  call native int class GLib.Marshaller::StringToPtrGStrdup(string)
	IL_0006:  stloc.2 
	IL_0007:  ldarg.1 
	IL_0008:  call native int[] class GLib.Marshaller::StringArrayToNullTermPointer(string[])
	IL_000d:  stloc.3 
	IL_000e:  ldarg.2 
	IL_000f:  call native int[] class GLib.Marshaller::StringArrayToNullTermPointer(string[])
	IL_0014:  stloc.s 4
	IL_0016:  ldarg.s 4
	IL_0018:  newobj instance void class GLib.SpawnChildSetupWrapper::'.ctor'(class GLib.SpawnChildSetupFunc)
	IL_001d:  stloc.s 5
	IL_001f:  ldarg.s 6
	IL_0021:  ldind.i4 
	IL_0022:  ldc.i4 2147483647
	IL_0027:  bne.un IL_0036

	IL_002c:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_0031:  br IL_003c

	IL_0036:  ldc.i4.4 
	IL_0037:  call native int class [mscorlib]System.Runtime.InteropServices.Marshal::AllocHGlobal(int32)
	IL_003c:  stloc.s 6
	IL_003e:  ldarg.s 7
	IL_0040:  ldind.i4 
	IL_0041:  ldc.i4 2147483647
	IL_0046:  bne.un IL_0055

	IL_004b:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_0050:  br IL_005b

	IL_0055:  ldc.i4.4 
	IL_0056:  call native int class [mscorlib]System.Runtime.InteropServices.Marshal::AllocHGlobal(int32)
	IL_005b:  stloc.s 7
	IL_005d:  ldarg.s 8
	IL_005f:  ldind.i4 
	IL_0060:  ldc.i4 2147483647
	IL_0065:  bne.un IL_0074

	IL_006a:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_006f:  br IL_007a

	IL_0074:  ldc.i4.4 
	IL_0075:  call native int class [mscorlib]System.Runtime.InteropServices.Marshal::AllocHGlobal(int32)
	IL_007a:  stloc.s 8
	IL_007c:  call bool class GLib.Global::get_IsWindowsPlatform()
	IL_0081:  brfalse IL_00af

	IL_0086:  ldloc.2 
	IL_0087:  ldloc.3 
	IL_0088:  ldloc.s 4
	IL_008a:  ldarg.3 
	IL_008b:  ldloc.s 5
	IL_008d:  ldfld class GLib.SpawnChildSetupFuncNative GLib.SpawnChildSetupWrapper::NativeCallback
	IL_0092:  ldloc.s 5
	IL_0094:  ldfld native int GLib.SpawnChildSetupWrapper::Data
	IL_0099:  ldloca.s 0
	IL_009b:  ldloc.s 6
	IL_009d:  ldloc.s 7
	IL_009f:  ldloc.s 8
	IL_00a1:  ldloca.s 1
	IL_00a3:  call bool class GLib.Process::g_spawn_async_with_pipes_utf8(native int, native int[], native int[], int32, class GLib.SpawnChildSetupFuncNative, native int, [out] int32&, native int, native int, native int, [out] native int&)
	IL_00a8:  stloc.s 9
	IL_00aa:  br IL_00d3

	IL_00af:  ldloc.2 
	IL_00b0:  ldloc.3 
	IL_00b1:  ldloc.s 4
	IL_00b3:  ldarg.3 
	IL_00b4:  ldloc.s 5
	IL_00b6:  ldfld class GLib.SpawnChildSetupFuncNative GLib.SpawnChildSetupWrapper::NativeCallback
	IL_00bb:  ldloc.s 5
	IL_00bd:  ldfld native int GLib.SpawnChildSetupWrapper::Data
	IL_00c2:  ldloca.s 0
	IL_00c4:  ldloc.s 6
	IL_00c6:  ldloc.s 7
	IL_00c8:  ldloc.s 8
	IL_00ca:  ldloca.s 1
	IL_00cc:  call bool class GLib.Process::g_spawn_async_with_pipes(native int, native int[], native int[], int32, class GLib.SpawnChildSetupFuncNative, native int, [out] int32&, native int, native int, native int, [out] native int&)
	IL_00d1:  stloc.s 9
	IL_00d3:  ldarg.s 5
	IL_00d5:  ldloc.0 
	IL_00d6:  newobj instance void class GLib.Process::'.ctor'(int32)
	IL_00db:  stind.ref 
	IL_00dc:  ldloc.s 6
	IL_00de:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_00e3:  call bool native int::op_Inequality(native int, native int)
	IL_00e8:  brfalse IL_00fe

	IL_00ed:  ldarg.s 6
	IL_00ef:  ldloc.s 6
	IL_00f1:  call int32 class [mscorlib]System.Runtime.InteropServices.Marshal::ReadInt32(native int)
	IL_00f6:  stind.i4 
	IL_00f7:  ldloc.s 6
	IL_00f9:  call void class [mscorlib]System.Runtime.InteropServices.Marshal::FreeHGlobal(native int)
	IL_00fe:  ldloc.s 7
	IL_0100:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_0105:  call bool native int::op_Inequality(native int, native int)
	IL_010a:  brfalse IL_0120

	IL_010f:  ldarg.s 7
	IL_0111:  ldloc.s 7
	IL_0113:  call int32 class [mscorlib]System.Runtime.InteropServices.Marshal::ReadInt32(native int)
	IL_0118:  stind.i4 
	IL_0119:  ldloc.s 7
	IL_011b:  call void class [mscorlib]System.Runtime.InteropServices.Marshal::FreeHGlobal(native int)
	IL_0120:  ldloc.s 8
	IL_0122:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_0127:  call bool native int::op_Inequality(native int, native int)
	IL_012c:  brfalse IL_0142

	IL_0131:  ldarg.s 8
	IL_0133:  ldloc.s 8
	IL_0135:  call int32 class [mscorlib]System.Runtime.InteropServices.Marshal::ReadInt32(native int)
	IL_013a:  stind.i4 
	IL_013b:  ldloc.s 8
	IL_013d:  call void class [mscorlib]System.Runtime.InteropServices.Marshal::FreeHGlobal(native int)
	IL_0142:  ldloc.2 
	IL_0143:  call void class GLib.Marshaller::Free(native int)
	IL_0148:  ldloc.3 
	IL_0149:  call void class GLib.Marshaller::Free(native int[])
	IL_014e:  ldloc.s 4
	IL_0150:  call void class GLib.Marshaller::Free(native int[])
	IL_0155:  ldloc.1 
	IL_0156:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_015b:  call bool native int::op_Inequality(native int, native int)
	IL_0160:  brfalse IL_016c

	IL_0165:  ldloc.1 
	IL_0166:  newobj instance void class GLib.GException::'.ctor'(native int)
	IL_016b:  throw 
	IL_016c:  ldloc.s 9
	IL_016e:  ret 
    } // end of method Process::SpawnAsyncWithPipes

    // method line 759
    .method private static hidebysig pinvokeimpl ("libglib-2.0-0.dll" as "g_spawn_sync" cdecl )
           default bool g_spawn_sync (native int dir, native int[] argv, native int[] envp, int32 'flags', class GLib.SpawnChildSetupFuncNative func, native int data, [out] native int& stdout, [out] native int& stderr, [out] int32& exit_status, [out] native int& 'error')  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method Process::g_spawn_sync

    // method line 760
    .method private static hidebysig pinvokeimpl ("libglib-2.0-0.dll" as "g_spawn_sync_utf8" cdecl )
           default bool g_spawn_sync_utf8 (native int dir, native int[] argv, native int[] envp, int32 'flags', class GLib.SpawnChildSetupFuncNative func, native int data, [out] native int& stdout, [out] native int& stderr, [out] int32& exit_status, [out] native int& 'error')  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method Process::g_spawn_sync_utf8

    // method line 761
    .method public static hidebysig 
           default bool SpawnSync (string working_directory, string[] argv, string[] envp, valuetype GLib.SpawnFlags 'flags', class GLib.SpawnChildSetupFunc child_setup, [out] string& stdout, [out] string& stderr, [out] int32& exit_status)  cil managed 
    {
        // Method begins at RVA 0x8318
	// Code size 181 (0xb5)
	.maxstack 10
	.locals init (
		native int	V_0,
		native int	V_1,
		native int	V_2,
		native int	V_3,
		native int[]	V_4,
		native int[]	V_5,
		class GLib.SpawnChildSetupWrapper	V_6,
		bool	V_7)
	IL_0000:  ldarg.0 
	IL_0001:  call native int class GLib.Marshaller::StringToPtrGStrdup(string)
	IL_0006:  stloc.3 
	IL_0007:  ldarg.1 
	IL_0008:  call native int[] class GLib.Marshaller::StringArrayToNullTermPointer(string[])
	IL_000d:  stloc.s 4
	IL_000f:  ldarg.2 
	IL_0010:  call native int[] class GLib.Marshaller::StringArrayToNullTermPointer(string[])
	IL_0015:  stloc.s 5
	IL_0017:  ldarg.s 4
	IL_0019:  newobj instance void class GLib.SpawnChildSetupWrapper::'.ctor'(class GLib.SpawnChildSetupFunc)
	IL_001e:  stloc.s 6
	IL_0020:  call bool class GLib.Global::get_IsWindowsPlatform()
	IL_0025:  brfalse IL_0052

	IL_002a:  ldloc.3 
	IL_002b:  ldloc.s 4
	IL_002d:  ldloc.s 5
	IL_002f:  ldarg.3 
	IL_0030:  ldloc.s 6
	IL_0032:  ldfld class GLib.SpawnChildSetupFuncNative GLib.SpawnChildSetupWrapper::NativeCallback
	IL_0037:  ldloc.s 6
	IL_0039:  ldfld native int GLib.SpawnChildSetupWrapper::Data
	IL_003e:  ldloca.s 0
	IL_0040:  ldloca.s 1
	IL_0042:  ldarg.s 7
	IL_0044:  ldloca.s 2
	IL_0046:  call bool class GLib.Process::g_spawn_sync(native int, native int[], native int[], int32, class GLib.SpawnChildSetupFuncNative, native int, [out] native int&, [out] native int&, [out] int32&, [out] native int&)
	IL_004b:  stloc.s 7
	IL_004d:  br IL_0075

	IL_0052:  ldloc.3 
	IL_0053:  ldloc.s 4
	IL_0055:  ldloc.s 5
	IL_0057:  ldarg.3 
	IL_0058:  ldloc.s 6
	IL_005a:  ldfld class GLib.SpawnChildSetupFuncNative GLib.SpawnChildSetupWrapper::NativeCallback
	IL_005f:  ldloc.s 6
	IL_0061:  ldfld native int GLib.SpawnChildSetupWrapper::Data
	IL_0066:  ldloca.s 0
	IL_0068:  ldloca.s 1
	IL_006a:  ldarg.s 7
	IL_006c:  ldloca.s 2
	IL_006e:  call bool class GLib.Process::g_spawn_sync(native int, native int[], native int[], int32, class GLib.SpawnChildSetupFuncNative, native int, [out] native int&, [out] native int&, [out] int32&, [out] native int&)
	IL_0073:  stloc.s 7
	IL_0075:  ldloc.3 
	IL_0076:  call void class GLib.Marshaller::Free(native int)
	IL_007b:  ldloc.s 4
	IL_007d:  call void class GLib.Marshaller::Free(native int[])
	IL_0082:  ldloc.s 5
	IL_0084:  call void class GLib.Marshaller::Free(native int[])
	IL_0089:  ldarg.s 5
	IL_008b:  ldloc.0 
	IL_008c:  call string class GLib.Marshaller::PtrToStringGFree(native int)
	IL_0091:  stind.ref 
	IL_0092:  ldarg.s 6
	IL_0094:  ldloc.1 
	IL_0095:  call string class GLib.Marshaller::PtrToStringGFree(native int)
	IL_009a:  stind.ref 
	IL_009b:  ldloc.2 
	IL_009c:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_00a1:  call bool native int::op_Inequality(native int, native int)
	IL_00a6:  brfalse IL_00b2

	IL_00ab:  ldloc.2 
	IL_00ac:  newobj instance void class GLib.GException::'.ctor'(native int)
	IL_00b1:  throw 
	IL_00b2:  ldloc.s 7
	IL_00b4:  ret 
    } // end of method Process::SpawnSync

    // method line 762
    .method private static hidebysig pinvokeimpl ("libglib-2.0-0.dll" as "g_spawn_command_line_async" cdecl )
           default bool g_spawn_command_line_async (native int cmdline, [out] native int& 'error')  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method Process::g_spawn_command_line_async

    // method line 763
    .method private static hidebysig pinvokeimpl ("libglib-2.0-0.dll" as "g_spawn_command_line_async_utf8" cdecl )
           default bool g_spawn_command_line_async_utf8 (native int cmdline, [out] native int& 'error')  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method Process::g_spawn_command_line_async_utf8

    // method line 764
    .method public static hidebysig 
           default bool SpawnCommandLineAsync (string command_line)  cil managed 
    {
        // Method begins at RVA 0x83dc
	// Code size 71 (0x47)
	.maxstack 2
	.locals init (
		native int	V_0,
		native int	V_1,
		bool	V_2)
	IL_0000:  ldarg.0 
	IL_0001:  call native int class GLib.Marshaller::StringToPtrGStrdup(string)
	IL_0006:  stloc.1 
	IL_0007:  call bool class GLib.Global::get_IsWindowsPlatform()
	IL_000c:  brfalse IL_001f

	IL_0011:  ldloc.1 
	IL_0012:  ldloca.s 0
	IL_0014:  call bool class GLib.Process::g_spawn_command_line_async_utf8(native int, [out] native int&)
	IL_0019:  stloc.2 
	IL_001a:  br IL_0028

	IL_001f:  ldloc.1 
	IL_0020:  ldloca.s 0
	IL_0022:  call bool class GLib.Process::g_spawn_command_line_async(native int, [out] native int&)
	IL_0027:  stloc.2 
	IL_0028:  ldloc.1 
	IL_0029:  call void class GLib.Marshaller::Free(native int)
	IL_002e:  ldloc.0 
	IL_002f:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_0034:  call bool native int::op_Inequality(native int, native int)
	IL_0039:  brfalse IL_0045

	IL_003e:  ldloc.0 
	IL_003f:  newobj instance void class GLib.GException::'.ctor'(native int)
	IL_0044:  throw 
	IL_0045:  ldloc.2 
	IL_0046:  ret 
    } // end of method Process::SpawnCommandLineAsync

    // method line 765
    .method private static hidebysig pinvokeimpl ("libglib-2.0-0.dll" as "g_spawn_command_line_sync" cdecl )
           default bool g_spawn_command_line_sync (native int cmdline, [out] native int& stdout, [out] native int& stderr, [out] int32& exit_status, [out] native int& 'error')  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method Process::g_spawn_command_line_sync

    // method line 766
    .method private static hidebysig pinvokeimpl ("libglib-2.0-0.dll" as "g_spawn_command_line_sync_utf8" cdecl )
           default bool g_spawn_command_line_sync_utf8 (native int cmdline, [out] native int& stdout, [out] native int& stderr, [out] int32& exit_status, [out] native int& 'error')  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method Process::g_spawn_command_line_sync_utf8

    // method line 767
    .method public static hidebysig 
           default bool SpawnCommandLineSync (string command_line, [out] string& stdout, [out] string& stderr, [out] int32& exit_status)  cil managed 
    {
        // Method begins at RVA 0x8430
	// Code size 100 (0x64)
	.maxstack 5
	.locals init (
		native int	V_0,
		native int	V_1,
		native int	V_2,
		native int	V_3,
		bool	V_4)
	IL_0000:  ldarg.0 
	IL_0001:  call native int class GLib.Marshaller::StringToPtrGStrdup(string)
	IL_0006:  stloc.3 
	IL_0007:  call bool class GLib.Global::get_IsWindowsPlatform()
	IL_000c:  brfalse IL_0025

	IL_0011:  ldloc.3 
	IL_0012:  ldloca.s 1
	IL_0014:  ldloca.s 2
	IL_0016:  ldarg.3 
	IL_0017:  ldloca.s 0
	IL_0019:  call bool class GLib.Process::g_spawn_command_line_sync_utf8(native int, [out] native int&, [out] native int&, [out] int32&, [out] native int&)
	IL_001e:  stloc.s 4
	IL_0020:  br IL_0034

	IL_0025:  ldloc.3 
	IL_0026:  ldloca.s 1
	IL_0028:  ldloca.s 2
	IL_002a:  ldarg.3 
	IL_002b:  ldloca.s 0
	IL_002d:  call bool class GLib.Process::g_spawn_command_line_sync(native int, [out] native int&, [out] native int&, [out] int32&, [out] native int&)
	IL_0032:  stloc.s 4
	IL_0034:  ldloc.3 
	IL_0035:  call void class GLib.Marshaller::Free(native int)
	IL_003a:  ldarg.1 
	IL_003b:  ldloc.1 
	IL_003c:  call string class GLib.Marshaller::PtrToStringGFree(native int)
	IL_0041:  stind.ref 
	IL_0042:  ldarg.2 
	IL_0043:  ldloc.2 
	IL_0044:  call string class GLib.Marshaller::PtrToStringGFree(native int)
	IL_0049:  stind.ref 
	IL_004a:  ldloc.0 
	IL_004b:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_0050:  call bool native int::op_Inequality(native int, native int)
	IL_0055:  brfalse IL_0061

	IL_005a:  ldloc.0 
	IL_005b:  newobj instance void class GLib.GException::'.ctor'(native int)
	IL_0060:  throw 
	IL_0061:  ldloc.s 4
	IL_0063:  ret 
    } // end of method Process::SpawnCommandLineSync

  } // end of class GLib.Process
}

.namespace GLib
{
  .class public auto ansi beforefieldinit Thread
  	extends [mscorlib]System.Object
  {

    // method line 768
    .method public static hidebysig specialname 
           default bool get_Supported ()  cil managed 
    {
        // Method begins at RVA 0x84a0
	// Code size 2 (0x2)
	.maxstack 8
	IL_0000:  ldc.i4.1 
	IL_0001:  ret 
    } // end of method Thread::get_Supported

    // method line 769
    .method public static hidebysig 
           default void Init ()  cil managed 
    {
        // Method begins at RVA 0x84a4
	// Code size 1 (0x1)
	.maxstack 8
	IL_0000:  ret 
    } // end of method Thread::Init

    // method line 770
    .method private hidebysig specialname rtspecialname 
           instance default void '.ctor' ()  cil managed 
    {
        // Method begins at RVA 0x84a8
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::'.ctor'()
	IL_0006:  ret 
    } // end of method Thread::.ctor

	.property bool Supported ()
	{
		.get default bool GLib.Thread::get_Supported () 
	}
  } // end of class GLib.Thread
}

.namespace GLib
{
  .class public auto ansi sealed TimeoutHandler
  	extends [mscorlib]System.MulticastDelegate
  {

    // method line 771
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (object 'object', native int 'method')  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method TimeoutHandler::.ctor

    // method line 772
    .method public virtual hidebysig newslot 
           instance default bool Invoke ()  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method TimeoutHandler::Invoke

    // method line 773
    .method public virtual hidebysig newslot 
           instance default class [mscorlib]System.IAsyncResult BeginInvoke (class [mscorlib]System.AsyncCallback callback, object 'object')  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method TimeoutHandler::BeginInvoke

    // method line 774
    .method public virtual hidebysig newslot 
           instance default bool EndInvoke (class [mscorlib]System.IAsyncResult result)  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method TimeoutHandler::EndInvoke

  } // end of class GLib.TimeoutHandler
}

.namespace GLib
{
  .class public auto ansi beforefieldinit Timeout
  	extends [mscorlib]System.Object
  {

    // method line 775
    .method public static hidebysig 
           default unsigned int32 Add (unsigned int32 interval, class GLib.TimeoutHandler hndlr)  cil managed 
    {
        // Method begins at RVA 0x84b0
	// Code size 43 (0x2b)
	.maxstack 4
	.locals init (
		class GLib.Timeout/TimeoutProxy	V_0)
	IL_0000:  ldarg.1 
	IL_0001:  newobj instance void class GLib.Timeout/TimeoutProxy::'.ctor'(class GLib.TimeoutHandler)
	IL_0006:  stloc.0 
	IL_0007:  ldloc.0 
	IL_0008:  ldarg.0 
	IL_0009:  ldloc.0 
	IL_000a:  ldfld class GLib.SourceProxy/GSourceFuncInternal GLib.SourceProxy::Handler
	IL_000f:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_0014:  call unsigned int32 class GLib.Timeout::g_timeout_add(unsigned int32, class GLib.SourceProxy/GSourceFuncInternal, native int)
	IL_0019:  stfld unsigned int32 GLib.SourceProxy::ID
	IL_001e:  ldloc.0 
	IL_001f:  call void class GLib.Source::Add(class GLib.SourceProxy)
	IL_0024:  ldloc.0 
	IL_0025:  ldfld unsigned int32 GLib.SourceProxy::ID
	IL_002a:  ret 
    } // end of method Timeout::Add

    // method line 776
    .method private hidebysig specialname rtspecialname 
           instance default void '.ctor' ()  cil managed 
    {
        // Method begins at RVA 0x84e8
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::'.ctor'()
	IL_0006:  ret 
    } // end of method Timeout::.ctor

    // method line 777
    .method private static hidebysig pinvokeimpl ("libglib-2.0-0.dll" as "g_timeout_add" cdecl )
           default unsigned int32 g_timeout_add (unsigned int32 interval, class GLib.SourceProxy/GSourceFuncInternal d, native int data)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method Timeout::g_timeout_add

  .class nested assembly auto ansi beforefieldinit TimeoutProxy
  	extends GLib.SourceProxy
  {
    .field  private initonly  class GLib.TimeoutHandler real_handler

    // method line 778
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (class GLib.TimeoutHandler real)  cil managed 
    {
        // Method begins at RVA 0x84f0
	// Code size 14 (0xe)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void class GLib.SourceProxy::'.ctor'()
	IL_0006:  ldarg.0 
	IL_0007:  ldarg.1 
	IL_0008:  stfld class GLib.TimeoutHandler GLib.Timeout/TimeoutProxy::real_handler
	IL_000d:  ret 
    } // end of method TimeoutProxy::.ctor

    // method line 779
    .method family virtual hidebysig 
           instance default bool Invoke (native int data)  cil managed 
    {
        // Method begins at RVA 0x8500
	// Code size 12 (0xc)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldfld class GLib.TimeoutHandler GLib.Timeout/TimeoutProxy::real_handler
	IL_0006:  callvirt instance bool class GLib.TimeoutHandler::Invoke()
	IL_000b:  ret 
    } // end of method TimeoutProxy::Invoke

  } // end of class TimeoutProxy

  } // end of class GLib.Timeout
}

.namespace GLib
{
  .class private auto ansi beforefieldinit ToggleRef
  	extends [mscorlib]System.Object
  {
    .field  private  bool hardened
    .field  private  native int handle
    .field  private  object reference
    .field  private  valuetype [mscorlib]System.Runtime.InteropServices.GCHandle gch
    .field  private  class [mscorlib]System.Collections.Hashtable signals
    .field  private static  class GLib.ToggleRef/ToggleNotifyHandler toggle_notify_callback

    // method line 780
    .method public hidebysig 
           instance default void Free ()  cil managed 
    {
        // Method begins at RVA 0x8510
	// Code size 169 (0xa9)
	.maxstack 3
	.locals init (
		class [mscorlib]System.Collections.ICollection	V_0,
		class GLib.Signal	V_1,
		class [mscorlib]System.Collections.IEnumerator	V_2,
		class [mscorlib]System.IDisposable	V_3)
	IL_0000:  ldarg.0 
	IL_0001:  ldfld class [mscorlib]System.Collections.Hashtable GLib.ToggleRef::signals
	IL_0006:  brfalse IL_0060

	IL_000b:  ldarg.0 
	IL_000c:  ldfld class [mscorlib]System.Collections.Hashtable GLib.ToggleRef::signals
	IL_0011:  callvirt instance class [mscorlib]System.Collections.ICollection class [mscorlib]System.Collections.Hashtable::get_Values()
	IL_0016:  stloc.0 
	IL_0017:  ldarg.0 
	IL_0018:  ldnull 
	IL_0019:  stfld class [mscorlib]System.Collections.Hashtable GLib.ToggleRef::signals
	IL_001e:  ldloc.0 
	IL_001f:  callvirt instance class [mscorlib]System.Collections.IEnumerator class [mscorlib]System.Collections.IEnumerable::GetEnumerator()
	IL_0024:  stloc.2 
	.try { // 0
	  IL_0025:  br IL_003c

	  IL_002a:  ldloc.2 
	  IL_002b:  callvirt instance object class [mscorlib]System.Collections.IEnumerator::get_Current()
	  IL_0030:  castclass GLib.Signal
	  IL_0035:  stloc.1 
	  IL_0036:  ldloc.1 
	  IL_0037:  callvirt instance void class GLib.Signal::Free()
	  IL_003c:  ldloc.2 
	  IL_003d:  callvirt instance bool class [mscorlib]System.Collections.IEnumerator::MoveNext()
	  IL_0042:  brtrue IL_002a

	  IL_0047:  leave IL_0060

	} // end .try 0
	finally  { // 0
	  IL_004c:  ldloc.2 
	  IL_004d:  isinst [mscorlib]System.IDisposable
	  IL_0052:  dup 
	  IL_0053:  stloc.3 
	  IL_0054:  brfalse IL_005f

	  IL_0059:  ldloc.3 
	  IL_005a:  callvirt instance void class [mscorlib]System.IDisposable::Dispose()
	  IL_005f:  endfinally 
	} // end handler 0
	IL_0060:  ldarg.0 
	IL_0061:  ldfld bool GLib.ToggleRef::hardened
	IL_0066:  brfalse IL_007b

	IL_006b:  ldarg.0 
	IL_006c:  ldfld native int GLib.ToggleRef::handle
	IL_0071:  call void class GLib.ToggleRef::g_object_unref(native int)
	IL_0076:  br IL_0096

	IL_007b:  ldarg.0 
	IL_007c:  ldfld native int GLib.ToggleRef::handle
	IL_0081:  call class GLib.ToggleRef/ToggleNotifyHandler class GLib.ToggleRef::get_ToggleNotifyCallback()
	IL_0086:  ldarg.0 
	IL_0087:  ldfld valuetype [mscorlib]System.Runtime.InteropServices.GCHandle GLib.ToggleRef::gch
	IL_008c:  call native int valuetype [mscorlib]System.Runtime.InteropServices.GCHandle::op_Explicit(valuetype [mscorlib]System.Runtime.InteropServices.GCHandle)
	IL_0091:  call void class GLib.ToggleRef::g_object_remove_toggle_ref(native int, class GLib.ToggleRef/ToggleNotifyHandler, native int)
	IL_0096:  ldarg.0 
	IL_0097:  ldnull 
	IL_0098:  stfld object GLib.ToggleRef::reference
	IL_009d:  ldarg.0 
	IL_009e:  ldflda valuetype [mscorlib]System.Runtime.InteropServices.GCHandle GLib.ToggleRef::gch
	IL_00a3:  call instance void valuetype [mscorlib]System.Runtime.InteropServices.GCHandle::Free()
	IL_00a8:  ret 
    } // end of method ToggleRef::Free

    // method line 781
    .method public hidebysig specialname 
           instance default class GLib.Object get_Target ()  cil managed 
    {
        // Method begins at RVA 0x85d8
	// Code size 65 (0x41)
	.maxstack 1
	.locals init (
		class [mscorlib]System.WeakReference	V_0)
	IL_0000:  ldarg.0 
	IL_0001:  ldfld object GLib.ToggleRef::reference
	IL_0006:  brtrue IL_000d

	IL_000b:  ldnull 
	IL_000c:  ret 
	IL_000d:  ldarg.0 
	IL_000e:  ldfld object GLib.ToggleRef::reference
	IL_0013:  isinst GLib.Object
	IL_0018:  brfalse IL_0029

	IL_001d:  ldarg.0 
	IL_001e:  ldfld object GLib.ToggleRef::reference
	IL_0023:  isinst GLib.Object
	IL_0028:  ret 
	IL_0029:  ldarg.0 
	IL_002a:  ldfld object GLib.ToggleRef::reference
	IL_002f:  isinst [mscorlib]System.WeakReference
	IL_0034:  stloc.0 
	IL_0035:  ldloc.0 
	IL_0036:  callvirt instance object class [mscorlib]System.WeakReference::get_Target()
	IL_003b:  isinst GLib.Object
	IL_0040:  ret 
    } // end of method ToggleRef::get_Target

    // method line 782
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (class GLib.Object target)  cil managed 
    {
        // Method begins at RVA 0x8628
	// Code size 98 (0x62)
	.maxstack 3
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::'.ctor'()
	IL_0006:  ldarg.0 
	IL_0007:  ldarg.1 
	IL_0008:  callvirt instance native int class GLib.Object::get_Handle()
	IL_000d:  stfld native int GLib.ToggleRef::handle
	IL_0012:  ldarg.0 
	IL_0013:  ldarg.0 
	IL_0014:  call valuetype [mscorlib]System.Runtime.InteropServices.GCHandle valuetype [mscorlib]System.Runtime.InteropServices.GCHandle::Alloc(object)
	IL_0019:  stfld valuetype [mscorlib]System.Runtime.InteropServices.GCHandle GLib.ToggleRef::gch
	IL_001e:  ldarg.0 
	IL_001f:  ldarg.1 
	IL_0020:  stfld object GLib.ToggleRef::reference
	IL_0025:  ldarg.1 
	IL_0026:  callvirt instance native int class GLib.Object::get_Handle()
	IL_002b:  call class GLib.ToggleRef/ToggleNotifyHandler class GLib.ToggleRef::get_ToggleNotifyCallback()
	IL_0030:  ldarg.0 
	IL_0031:  ldfld valuetype [mscorlib]System.Runtime.InteropServices.GCHandle GLib.ToggleRef::gch
	IL_0036:  call native int valuetype [mscorlib]System.Runtime.InteropServices.GCHandle::op_Explicit(valuetype [mscorlib]System.Runtime.InteropServices.GCHandle)
	IL_003b:  call void class GLib.ToggleRef::g_object_add_toggle_ref(native int, class GLib.ToggleRef/ToggleNotifyHandler, native int)
	IL_0040:  ldarg.1 
	IL_0041:  ldfld bool GLib.Object::owned
	IL_0046:  brfalse IL_0061

	IL_004b:  ldarg.1 
	IL_004c:  isinst GLib.InitiallyUnowned
	IL_0051:  brtrue IL_0061

	IL_0056:  ldarg.1 
	IL_0057:  callvirt instance native int class GLib.Object::get_Handle()
	IL_005c:  call void class GLib.ToggleRef::g_object_unref(native int)
	IL_0061:  ret 
    } // end of method ToggleRef::.ctor

    // method line 783
    .method assembly hidebysig 
           instance default void Harden ()  cil managed 
    {
        // Method begins at RVA 0x8698
	// Code size 85 (0x55)
	.maxstack 3
	IL_0000:  ldarg.0 
	IL_0001:  ldfld native int GLib.ToggleRef::handle
	IL_0006:  call native int class GLib.ToggleRef::g_object_ref(native int)
	IL_000b:  pop 
	IL_000c:  ldarg.0 
	IL_000d:  ldfld native int GLib.ToggleRef::handle
	IL_0012:  call class GLib.ToggleRef/ToggleNotifyHandler class GLib.ToggleRef::get_ToggleNotifyCallback()
	IL_0017:  ldarg.0 
	IL_0018:  ldfld valuetype [mscorlib]System.Runtime.InteropServices.GCHandle GLib.ToggleRef::gch
	IL_001d:  call native int valuetype [mscorlib]System.Runtime.InteropServices.GCHandle::op_Explicit(valuetype [mscorlib]System.Runtime.InteropServices.GCHandle)
	IL_0022:  call void class GLib.ToggleRef::g_object_remove_toggle_ref(native int, class GLib.ToggleRef/ToggleNotifyHandler, native int)
	IL_0027:  ldarg.0 
	IL_0028:  ldfld object GLib.ToggleRef::reference
	IL_002d:  isinst [mscorlib]System.WeakReference
	IL_0032:  brfalse IL_004d

	IL_0037:  ldarg.0 
	IL_0038:  ldarg.0 
	IL_0039:  ldfld object GLib.ToggleRef::reference
	IL_003e:  isinst [mscorlib]System.WeakReference
	IL_0043:  callvirt instance object class [mscorlib]System.WeakReference::get_Target()
	IL_0048:  stfld object GLib.ToggleRef::reference
	IL_004d:  ldarg.0 
	IL_004e:  ldc.i4.1 
	IL_004f:  stfld bool GLib.ToggleRef::hardened
	IL_0054:  ret 
    } // end of method ToggleRef::Harden

    // method line 784
    .method public hidebysig specialname 
           instance default class [mscorlib]System.Collections.Hashtable get_Signals ()  cil managed 
    {
        // Method begins at RVA 0x86fc
	// Code size 29 (0x1d)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldfld class [mscorlib]System.Collections.Hashtable GLib.ToggleRef::signals
	IL_0006:  brtrue IL_0016

	IL_000b:  ldarg.0 
	IL_000c:  newobj instance void class [mscorlib]System.Collections.Hashtable::'.ctor'()
	IL_0011:  stfld class [mscorlib]System.Collections.Hashtable GLib.ToggleRef::signals
	IL_0016:  ldarg.0 
	IL_0017:  ldfld class [mscorlib]System.Collections.Hashtable GLib.ToggleRef::signals
	IL_001c:  ret 
    } // end of method ToggleRef::get_Signals

    // method line 785
    .method assembly hidebysig 
           instance default void RemoveSignal (string name)  cil managed 
    {
        // Method begins at RVA 0x871c
	// Code size 25 (0x19)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldfld class [mscorlib]System.Collections.Hashtable GLib.ToggleRef::signals
	IL_0006:  brtrue IL_000c

	IL_000b:  ret 
	IL_000c:  ldarg.0 
	IL_000d:  ldfld class [mscorlib]System.Collections.Hashtable GLib.ToggleRef::signals
	IL_0012:  ldarg.1 
	IL_0013:  callvirt instance void class [mscorlib]System.Collections.Hashtable::Remove(object)
	IL_0018:  ret 
    } // end of method ToggleRef::RemoveSignal

    // method line 786
    .method public hidebysig specialname 
           instance default native int get_Handle ()  cil managed 
    {
        // Method begins at RVA 0x8738
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldfld native int GLib.ToggleRef::handle
	IL_0006:  ret 
    } // end of method ToggleRef::get_Handle

    // method line 787
    .method private static hidebysig specialname 
           default class GLib.ToggleRef/ToggleNotifyHandler get_ToggleNotifyCallback ()  cil managed 
    {
        // Method begins at RVA 0x8740
	// Code size 33 (0x21)
	.maxstack 8
	IL_0000:  ldsfld class GLib.ToggleRef/ToggleNotifyHandler GLib.ToggleRef::toggle_notify_callback
	IL_0005:  brtrue IL_001b

	IL_000a:  ldnull 
	IL_000b:  ldftn void class GLib.ToggleRef::RefToggled(native int, native int, bool)
	IL_0011:  newobj instance void class GLib.ToggleRef/ToggleNotifyHandler::'.ctor'(object, native int)
	IL_0016:  stsfld class GLib.ToggleRef/ToggleNotifyHandler GLib.ToggleRef::toggle_notify_callback
	IL_001b:  ldsfld class GLib.ToggleRef/ToggleNotifyHandler GLib.ToggleRef::toggle_notify_callback
	IL_0020:  ret 
    } // end of method ToggleRef::get_ToggleNotifyCallback

    // method line 788
    .method private static hidebysig pinvokeimpl ("libgobject-2.0-0.dll" as "g_object_add_toggle_ref" cdecl )
           default void g_object_add_toggle_ref (native int raw, class GLib.ToggleRef/ToggleNotifyHandler notify_cb, native int data)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method ToggleRef::g_object_add_toggle_ref

    // method line 789
    .method private static hidebysig pinvokeimpl ("libgobject-2.0-0.dll" as "g_object_unref" cdecl )
           default void g_object_unref (native int raw)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method ToggleRef::g_object_unref

    // method line 790
    .method private static hidebysig pinvokeimpl ("libgobject-2.0-0.dll" as "g_object_remove_toggle_ref" cdecl )
           default void g_object_remove_toggle_ref (native int raw, class GLib.ToggleRef/ToggleNotifyHandler notify_cb, native int data)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method ToggleRef::g_object_remove_toggle_ref

    // method line 791
    .method private static hidebysig pinvokeimpl ("libgobject-2.0-0.dll" as "g_object_ref" cdecl )
           default native int g_object_ref (native int raw)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method ToggleRef::g_object_ref

    // method line 792
    .method private hidebysig 
           instance default void Toggle (bool is_last_ref)  cil managed 
    {
        // Method begins at RVA 0x8764
	// Code size 102 (0x66)
	.maxstack 2
	.locals init (
		class [mscorlib]System.WeakReference	V_0)
	IL_0000:  ldarg.1 
	IL_0001:  brfalse IL_002c

	IL_0006:  ldarg.0 
	IL_0007:  ldfld object GLib.ToggleRef::reference
	IL_000c:  isinst GLib.Object
	IL_0011:  brfalse IL_002c

	IL_0016:  ldarg.0 
	IL_0017:  ldarg.0 
	IL_0018:  ldfld object GLib.ToggleRef::reference
	IL_001d:  newobj instance void class [mscorlib]System.WeakReference::'.ctor'(object)
	IL_0022:  stfld object GLib.ToggleRef::reference
	IL_0027:  br IL_0065

	IL_002c:  ldarg.1 
	IL_002d:  brtrue IL_0065

	IL_0032:  ldarg.0 
	IL_0033:  ldfld object GLib.ToggleRef::reference
	IL_0038:  isinst [mscorlib]System.WeakReference
	IL_003d:  brfalse IL_0065

	IL_0042:  ldarg.0 
	IL_0043:  ldfld object GLib.ToggleRef::reference
	IL_0048:  isinst [mscorlib]System.WeakReference
	IL_004d:  stloc.0 
	IL_004e:  ldloc.0 
	IL_004f:  callvirt instance bool class [mscorlib]System.WeakReference::get_IsAlive()
	IL_0054:  brfalse IL_0065

	IL_0059:  ldarg.0 
	IL_005a:  ldloc.0 
	IL_005b:  callvirt instance object class [mscorlib]System.WeakReference::get_Target()
	IL_0060:  stfld object GLib.ToggleRef::reference
	IL_0065:  ret 
    } // end of method ToggleRef::Toggle

    // method line 793
    .method private static hidebysig 
           default void RefToggled (native int data, native int handle, bool is_last_ref)  cil managed 
    {
        // Method begins at RVA 0x87d8
	// Code size 46 (0x2e)
	.maxstack 2
	.locals init (
		valuetype [mscorlib]System.Runtime.InteropServices.GCHandle	V_0,
		class GLib.ToggleRef	V_1,
		class [mscorlib]System.Exception	V_2)
	.try { // 0
	  IL_0000:  ldarg.0 
	  IL_0001:  call valuetype [mscorlib]System.Runtime.InteropServices.GCHandle valuetype [mscorlib]System.Runtime.InteropServices.GCHandle::op_Explicit(native int)
	  IL_0006:  stloc.0 
	  IL_0007:  ldloca.s 0
	  IL_0009:  call instance object valuetype [mscorlib]System.Runtime.InteropServices.GCHandle::get_Target()
	  IL_000e:  isinst GLib.ToggleRef
	  IL_0013:  stloc.1 
	  IL_0014:  ldloc.1 
	  IL_0015:  ldarg.2 
	  IL_0016:  callvirt instance void class GLib.ToggleRef::Toggle(bool)
	  IL_001b:  leave IL_002d

	} // end .try 0
	catch class [mscorlib]System.Exception { // 0
	  IL_0020:  stloc.2 
	  IL_0021:  ldloc.2 
	  IL_0022:  ldc.i4.0 
	  IL_0023:  call void class GLib.ExceptionManager::RaiseUnhandledException(class [mscorlib]System.Exception, bool)
	  IL_0028:  leave IL_002d

	} // end handler 0
	IL_002d:  ret 
    } // end of method ToggleRef::RefToggled

	.property instance native int Handle ()
	{
		.get instance default native int GLib.ToggleRef::get_Handle () 
	}
	.property instance class [mscorlib]System.Collections.Hashtable Signals ()
	{
		.get instance default class [mscorlib]System.Collections.Hashtable GLib.ToggleRef::get_Signals () 
	}
	.property instance class GLib.Object Target ()
	{
		.get instance default class GLib.Object GLib.ToggleRef::get_Target () 
	}
	.property class GLib.ToggleRef/ToggleNotifyHandler ToggleNotifyCallback ()
	{
		.get default class GLib.ToggleRef/ToggleNotifyHandler GLib.ToggleRef::get_ToggleNotifyCallback () 
	}
  .class nested private auto ansi sealed ToggleNotifyHandler
  	extends [mscorlib]System.MulticastDelegate
  {
    .custom instance void class [mscorlib]System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute::'.ctor'(valuetype [mscorlib]System.Runtime.InteropServices.CallingConvention) =  (01 00 02 00 00 00 00 00 ) // ........


    // method line 794
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (object 'object', native int 'method')  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method ToggleNotifyHandler::.ctor

    // method line 795
    .method public virtual hidebysig newslot 
           instance default void Invoke (native int data, native int handle, bool is_last_ref)  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method ToggleNotifyHandler::Invoke

    // method line 796
    .method public virtual hidebysig newslot 
           instance default class [mscorlib]System.IAsyncResult BeginInvoke (native int data, native int handle, bool is_last_ref, class [mscorlib]System.AsyncCallback callback, object 'object')  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method ToggleNotifyHandler::BeginInvoke

    // method line 797
    .method public virtual hidebysig newslot 
           instance default void EndInvoke (class [mscorlib]System.IAsyncResult result)  runtime managed 
    {
        // Method begins at RVA 0x0
          // Disassembly of native methods is not supported
    } // end of method ToggleNotifyHandler::EndInvoke

  } // end of class ToggleNotifyHandler

  } // end of class GLib.ToggleRef
}

.namespace GLib
{
  .class public auto ansi beforefieldinit TypeConverter
  	extends [mscorlib]System.Object
  {

    // method line 798
    .method private hidebysig specialname rtspecialname 
           instance default void '.ctor' ()  cil managed 
    {
        // Method begins at RVA 0x8824
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::'.ctor'()
	IL_0006:  ret 
    } // end of method TypeConverter::.ctor

    // method line 799
    .method public static hidebysig 
           default valuetype GLib.GType LookupType (class [mscorlib]System.Type 'type')  cil managed 
    {
        .custom instance void class [mscorlib]System.ObsoleteAttribute::'.ctor'(string) =  (
		01 00 21 52 65 70 6C 61 63 65 64 20 62 79 20 65   // ..!Replaced by e
		78 70 6C 69 63 69 74 20 28 47 54 79 70 65 29 20   // xplicit (GType) 
		63 61 73 74 00 00                               ) // cast..

        // Method begins at RVA 0x882c
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call valuetype GLib.GType valuetype GLib.GType::op_Explicit(class [mscorlib]System.Type)
	IL_0006:  ret 
    } // end of method TypeConverter::LookupType

  } // end of class GLib.TypeConverter
}

.namespace GLib
{
  .class public auto ansi sealed TypeFundamentals
  	extends [mscorlib]System.Enum
  {
    .field  public specialname rtspecialname  int32 value__
    .field public static literal  valuetype GLib.TypeFundamentals TypeInvalid = int32(0x00000000)
    .field public static literal  valuetype GLib.TypeFundamentals TypeNone = int32(0x00000004)
    .field public static literal  valuetype GLib.TypeFundamentals TypeInterface = int32(0x00000008)
    .field public static literal  valuetype GLib.TypeFundamentals TypeChar = int32(0x0000000c)
    .field public static literal  valuetype GLib.TypeFundamentals TypeUChar = int32(0x00000010)
    .field public static literal  valuetype GLib.TypeFundamentals TypeBoolean = int32(0x00000014)
    .field public static literal  valuetype GLib.TypeFundamentals TypeInt = int32(0x00000018)
    .field public static literal  valuetype GLib.TypeFundamentals TypeUInt = int32(0x0000001c)
    .field public static literal  valuetype GLib.TypeFundamentals TypeLong = int32(0x00000020)
    .field public static literal  valuetype GLib.TypeFundamentals TypeULong = int32(0x00000024)
    .field public static literal  valuetype GLib.TypeFundamentals TypeInt64 = int32(0x00000028)
    .field public static literal  valuetype GLib.TypeFundamentals TypeUInt64 = int32(0x0000002c)
    .field public static literal  valuetype GLib.TypeFundamentals TypeEnum = int32(0x00000030)
    .field public static literal  valuetype GLib.TypeFundamentals TypeFlags = int32(0x00000034)
    .field public static literal  valuetype GLib.TypeFundamentals TypeFloat = int32(0x00000038)
    .field public static literal  valuetype GLib.TypeFundamentals TypeDouble = int32(0x0000003c)
    .field public static literal  valuetype GLib.TypeFundamentals TypeString = int32(0x00000040)
    .field public static literal  valuetype GLib.TypeFundamentals TypePointer = int32(0x00000044)
    .field public static literal  valuetype GLib.TypeFundamentals TypeBoxed = int32(0x00000048)
    .field public static literal  valuetype GLib.TypeFundamentals TypeParam = int32(0x0000004c)
    .field public static literal  valuetype GLib.TypeFundamentals TypeObject = int32(0x00000050)

  } // end of class GLib.TypeFundamentals
}

.namespace GLib
{
  .class public auto ansi sealed beforefieldinit TypeInitializerAttribute
  	extends [mscorlib]System.Attribute
  {
    .custom instance void class [mscorlib]System.AttributeUsageAttribute::'.ctor'(valuetype [mscorlib]System.AttributeTargets) =  (01 00 04 00 00 00 00 00 ) // ........

    .field  private  string method_name
    .field  private  class [mscorlib]System.Type 'type'

    // method line 800
    .method public hidebysig specialname 
           instance default class [mscorlib]System.Type get_Type ()  cil managed 
    {
        // Method begins at RVA 0x8834
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldfld class [mscorlib]System.Type GLib.TypeInitializerAttribute::'type'
	IL_0006:  ret 
    } // end of method TypeInitializerAttribute::get_Type

    // method line 801
    .method public hidebysig specialname 
           instance default string get_MethodName ()  cil managed 
    {
        // Method begins at RVA 0x883c
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldfld string GLib.TypeInitializerAttribute::method_name
	IL_0006:  ret 
    } // end of method TypeInitializerAttribute::get_MethodName

    // method line 802
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (class [mscorlib]System.Type 'type', string method_name)  cil managed 
    {
        // Method begins at RVA 0x8844
	// Code size 21 (0x15)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void class [mscorlib]System.Attribute::'.ctor'()
	IL_0006:  ldarg.0 
	IL_0007:  ldarg.1 
	IL_0008:  stfld class [mscorlib]System.Type GLib.TypeInitializerAttribute::'type'
	IL_000d:  ldarg.0 
	IL_000e:  ldarg.2 
	IL_000f:  stfld string GLib.TypeInitializerAttribute::method_name
	IL_0014:  ret 
    } // end of method TypeInitializerAttribute::.ctor

    // method line 803
    .method public hidebysig specialname 
           instance default void set_MethodName (string 'value')  cil managed 
    {
        // Method begins at RVA 0x885c
	// Code size 8 (0x8)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldarg.1 
	IL_0002:  stfld string GLib.TypeInitializerAttribute::method_name
	IL_0007:  ret 
    } // end of method TypeInitializerAttribute::set_MethodName

    // method line 804
    .method public hidebysig specialname 
           instance default void set_Type (class [mscorlib]System.Type 'value')  cil managed 
    {
        // Method begins at RVA 0x8868
	// Code size 8 (0x8)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldarg.1 
	IL_0002:  stfld class [mscorlib]System.Type GLib.TypeInitializerAttribute::'type'
	IL_0007:  ret 
    } // end of method TypeInitializerAttribute::set_Type

	.property instance string MethodName ()
	{
		.get instance default string GLib.TypeInitializerAttribute::get_MethodName () 
		.set instance default void GLib.TypeInitializerAttribute::set_MethodName (string 'value') 
	}
	.property instance class [mscorlib]System.Type Type ()
	{
		.get instance default class [mscorlib]System.Type GLib.TypeInitializerAttribute::get_Type () 
		.set instance default void GLib.TypeInitializerAttribute::set_Type (class [mscorlib]System.Type 'value') 
	}
  } // end of class GLib.TypeInitializerAttribute
}

.namespace GLib
{
  .class public auto ansi beforefieldinit UnwrappedObject
  	extends [mscorlib]System.Object
  {
    .custom instance void class [mscorlib]System.ObsoleteAttribute::'.ctor'(string) =  (
		01 00 37 52 65 70 6C 61 63 65 64 20 62 79 20 64   // ..7Replaced by d
		69 72 65 63 74 20 6F 62 6A 65 63 74 2D 74 79 70   // irect object-typ
		65 20 63 61 73 74 73 20 74 6F 2F 66 72 6F 6D 20   // e casts to/from 
		47 4C 69 62 2E 56 61 6C 75 65 00 00             ) // GLib.Value..

    .field  private  native int obj

    // method line 805
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (native int obj)  cil managed 
    {
        // Method begins at RVA 0x8874
	// Code size 14 (0xe)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::'.ctor'()
	IL_0006:  ldarg.0 
	IL_0007:  ldarg.1 
	IL_0008:  stfld native int GLib.UnwrappedObject::obj
	IL_000d:  ret 
    } // end of method UnwrappedObject::.ctor

    // method line 806
    .method public static hidebysig specialname 
           default native int op_Explicit (class GLib.UnwrappedObject obj)  cil managed 
    {
        // Method begins at RVA 0x8884
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldfld native int GLib.UnwrappedObject::obj
	IL_0006:  ret 
    } // end of method UnwrappedObject::op_Explicit

  } // end of class GLib.UnwrappedObject
}

.namespace GLib
{
  .class public auto ansi beforefieldinit ValueArray
  	extends [mscorlib]System.Object
  	implements [mscorlib]System.IDisposable, [mscorlib]System.Collections.ICollection, [mscorlib]System.ICloneable, GLib.IWrapper, [mscorlib]System.Collections.IEnumerable  {
    .custom instance void class [mscorlib]System.Reflection.DefaultMemberAttribute::'.ctor'(string) =  (01 00 04 49 74 65 6D 00 00 ) // ...Item..

    .field  private  native int handle
    .field  private static  object lockObject
    .field  private static  class [mscorlib]System.Collections.Generic.List`1<native int> PendingFrees
    .field  private static  bool idle_queued

    // method line 807
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (unsigned int32 n_preallocs)  cil managed 
    {
        // Method begins at RVA 0x888c
	// Code size 30 (0x1e)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_0006:  stfld native int GLib.ValueArray::handle
	IL_000b:  ldarg.0 
	IL_000c:  call instance void object::'.ctor'()
	IL_0011:  ldarg.0 
	IL_0012:  ldarg.1 
	IL_0013:  call native int class GLib.ValueArray::g_value_array_new(unsigned int32)
	IL_0018:  stfld native int GLib.ValueArray::handle
	IL_001d:  ret 
    } // end of method ValueArray::.ctor

    // method line 808
    .method public hidebysig 
           instance default void Append (valuetype GLib.Value val)  cil managed 
    {
        // Method begins at RVA 0x88ac
	// Code size 14 (0xe)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance native int class GLib.ValueArray::get_Handle()
	IL_0006:  ldarga.s 1
	IL_0008:  call void class GLib.ValueArray::g_value_array_append(native int, [out] valuetype GLib.Value&)
	IL_000d:  ret 
    } // end of method ValueArray::Append

    // method line 809
    .method public hidebysig specialname 
           instance default native int get_ArrayPtr ()  cil managed 
    {
        // Method begins at RVA 0x88bc
	// Code size 12 (0xc)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance native int class GLib.ValueArray::get_Handle()
	IL_0006:  call native int class GLib.ValueArray::gtksharp_value_array_get_array(native int)
	IL_000b:  ret 
    } // end of method ValueArray::get_ArrayPtr

    // method line 810
    .method private static hidebysig pinvokeimpl ("libgobject-2.0-0.dll" as "g_value_array_new" cdecl )
           default native int g_value_array_new (unsigned int32 n_preallocs)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method ValueArray::g_value_array_new

    // method line 811
    .method assembly hidebysig specialname rtspecialname 
           instance default void '.ctor' (native int raw)  cil managed 
    {
        // Method begins at RVA 0x88cc
	// Code size 25 (0x19)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_0006:  stfld native int GLib.ValueArray::handle
	IL_000b:  ldarg.0 
	IL_000c:  call instance void object::'.ctor'()
	IL_0011:  ldarg.0 
	IL_0012:  ldarg.1 
	IL_0013:  stfld native int GLib.ValueArray::handle
	IL_0018:  ret 
    } // end of method ValueArray::.ctor

    // method line 812
    .method family virtual hidebysig 
           instance default void Finalize ()  cil managed 
    {
        // Method begins at RVA 0x88e8
	// Code size 20 (0x14)
	.maxstack 2
	.try { // 0
	  IL_0000:  ldarg.0 
	  IL_0001:  ldc.i4.0 
	  IL_0002:  call instance void class GLib.ValueArray::Dispose(bool)
	  IL_0007:  leave IL_0013

	} // end .try 0
	finally  { // 0
	  IL_000c:  ldarg.0 
	  IL_000d:  call instance void object::Finalize()
	  IL_0012:  endfinally 
	} // end handler 0
	IL_0013:  ret 
    } // end of method ValueArray::Finalize

    // method line 813
    .method private hidebysig 
           instance default void Dispose (bool disposing)  cil managed 
    {
        // Method begins at RVA 0x8918
	// Code size 117 (0x75)
	.maxstack 3
	.locals init (
		object	V_0,
		bool	V_1)
	IL_0000:  ldarg.0 
	IL_0001:  call instance native int class GLib.ValueArray::get_Handle()
	IL_0006:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_000b:  call bool native int::op_Equality(native int, native int)
	IL_0010:  brfalse IL_0016

	IL_0015:  ret 
	IL_0016:  ldsfld object GLib.ValueArray::lockObject
	IL_001b:  stloc.0 
	IL_001c:  ldc.i4.0 
	IL_001d:  stloc.1 
	.try { // 0
	  IL_001e:  ldloc.0 
	  IL_001f:  ldloca.s 1
	  IL_0021:  call void class [mscorlib]System.Threading.Monitor::Enter(object, [out] bool&)
	  IL_0026:  ldsfld class [mscorlib]System.Collections.Generic.List`1<native int> GLib.ValueArray::PendingFrees
	  IL_002b:  ldarg.0 
	  IL_002c:  ldfld native int GLib.ValueArray::handle
	  IL_0031:  callvirt instance void class [mscorlib]System.Collections.Generic.List`1<native int>::Add(!0)
	  IL_0036:  ldsfld bool GLib.ValueArray::idle_queued
	  IL_003b:  brtrue IL_005a

	  IL_0040:  ldc.i4.s 0x32
	  IL_0042:  ldnull 
	  IL_0043:  ldftn bool class GLib.ValueArray::PerformFrees()
	  IL_0049:  newobj instance void class GLib.TimeoutHandler::'.ctor'(object, native int)
	  IL_004e:  call unsigned int32 class GLib.Timeout::Add(unsigned int32, class GLib.TimeoutHandler)
	  IL_0053:  pop 
	  IL_0054:  ldc.i4.1 
	  IL_0055:  stsfld bool GLib.ValueArray::idle_queued
	  IL_005a:  leave IL_0069

	} // end .try 0
	finally  { // 0
	  IL_005f:  ldloc.1 
	  IL_0060:  brfalse.s IL_0068

	  IL_0062:  ldloc.0 
	  IL_0063:  call void class [mscorlib]System.Threading.Monitor::Exit(object)
	  IL_0068:  endfinally 
	} // end handler 0
	IL_0069:  ldarg.0 
	IL_006a:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_006f:  stfld native int GLib.ValueArray::handle
	IL_0074:  ret 
    } // end of method ValueArray::Dispose

    // method line 814
    .method public final virtual hidebysig newslot 
           instance default void Dispose ()  cil managed 
    {
        // Method begins at RVA 0x89ac
	// Code size 14 (0xe)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldc.i4.1 
	IL_0002:  call instance void class GLib.ValueArray::Dispose(bool)
	IL_0007:  ldarg.0 
	IL_0008:  call void class [mscorlib]System.GC::SuppressFinalize(object)
	IL_000d:  ret 
    } // end of method ValueArray::Dispose

    // method line 815
    .method private static hidebysig pinvokeimpl ("libgobject-2.0-0.dll" as "g_value_array_free" cdecl )
           default void g_value_array_free (native int raw)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method ValueArray::g_value_array_free

    // method line 816
    .method public final virtual hidebysig newslot specialname 
           instance default native int get_Handle ()  cil managed 
    {
        // Method begins at RVA 0x89bc
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldfld native int GLib.ValueArray::handle
	IL_0006:  ret 
    } // end of method ValueArray::get_Handle

    // method line 817
    .method private static hidebysig 
           default bool PerformFrees ()  cil managed 
    {
        // Method begins at RVA 0x89c4
	// Code size 91 (0x5b)
	.maxstack 2
	.locals init (
		class [mscorlib]System.Collections.Generic.List`1<native int>	V_0,
		object	V_1,
		bool	V_2,
		int32	V_3)
	IL_0000:  ldsfld object GLib.ValueArray::lockObject
	IL_0005:  stloc.1 
	IL_0006:  ldc.i4.0 
	IL_0007:  stloc.2 
	.try { // 0
	  IL_0008:  ldloc.1 
	  IL_0009:  ldloca.s 2
	  IL_000b:  call void class [mscorlib]System.Threading.Monitor::Enter(object, [out] bool&)
	  IL_0010:  ldc.i4.0 
	  IL_0011:  stsfld bool GLib.ValueArray::idle_queued
	  IL_0016:  ldsfld class [mscorlib]System.Collections.Generic.List`1<native int> GLib.ValueArray::PendingFrees
	  IL_001b:  stloc.0 
	  IL_001c:  ldc.i4.8 
	  IL_001d:  newobj instance void class [mscorlib]System.Collections.Generic.List`1<native int>::'.ctor'(int32)
	  IL_0022:  stsfld class [mscorlib]System.Collections.Generic.List`1<native int> GLib.ValueArray::PendingFrees
	  IL_0027:  leave IL_0036

	} // end .try 0
	finally  { // 0
	  IL_002c:  ldloc.2 
	  IL_002d:  brfalse.s IL_0035

	  IL_002f:  ldloc.1 
	  IL_0030:  call void class [mscorlib]System.Threading.Monitor::Exit(object)
	  IL_0035:  endfinally 
	} // end handler 0
	IL_0036:  ldc.i4.0 
	IL_0037:  stloc.3 
	IL_0038:  br IL_004d

	IL_003d:  ldloc.0 
	IL_003e:  ldloc.3 
	IL_003f:  callvirt instance !0 class [mscorlib]System.Collections.Generic.List`1<native int>::get_Item(int32)
	IL_0044:  call void class GLib.ValueArray::g_value_array_free(native int)
	IL_0049:  ldloc.3 
	IL_004a:  ldc.i4.1 
	IL_004b:  add 
	IL_004c:  stloc.3 
	IL_004d:  ldloc.3 
	IL_004e:  ldloc.0 
	IL_004f:  callvirt instance int32 class [mscorlib]System.Collections.Generic.List`1<native int>::get_Count()
	IL_0054:  blt IL_003d

	IL_0059:  ldc.i4.0 
	IL_005a:  ret 
    } // end of method ValueArray::PerformFrees

    // method line 818
    .method private static hidebysig pinvokeimpl ("glibsharpglue-2" as "gtksharp_value_array_get_array" cdecl )
           default native int gtksharp_value_array_get_array (native int raw)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method ValueArray::gtksharp_value_array_get_array

    // method line 819
    .method private static hidebysig pinvokeimpl ("libgobject-2.0-0.dll" as "g_value_array_append" cdecl )
           default void g_value_array_append (native int raw, valuetype GLib.Value& val)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method ValueArray::g_value_array_append

    // method line 820
    .method private static hidebysig pinvokeimpl ("libgobject-2.0-0.dll" as "g_value_array_insert" cdecl )
           default void g_value_array_insert (native int raw, unsigned int32 idx, valuetype GLib.Value& val)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method ValueArray::g_value_array_insert

    // method line 821
    .method public hidebysig 
           instance default void Insert (unsigned int32 idx, valuetype GLib.Value val)  cil managed 
    {
        // Method begins at RVA 0x8a3c
	// Code size 15 (0xf)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance native int class GLib.ValueArray::get_Handle()
	IL_0006:  ldarg.1 
	IL_0007:  ldarga.s 2
	IL_0009:  call void class GLib.ValueArray::g_value_array_insert(native int, unsigned int32, [out] valuetype GLib.Value&)
	IL_000e:  ret 
    } // end of method ValueArray::Insert

    // method line 822
    .method private static hidebysig pinvokeimpl ("libgobject-2.0-0.dll" as "g_value_array_prepend" cdecl )
           default void g_value_array_prepend (native int raw, valuetype GLib.Value& val)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method ValueArray::g_value_array_prepend

    // method line 823
    .method public hidebysig 
           instance default void Prepend (valuetype GLib.Value val)  cil managed 
    {
        // Method begins at RVA 0x8a4c
	// Code size 14 (0xe)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance native int class GLib.ValueArray::get_Handle()
	IL_0006:  ldarga.s 1
	IL_0008:  call void class GLib.ValueArray::g_value_array_prepend(native int, [out] valuetype GLib.Value&)
	IL_000d:  ret 
    } // end of method ValueArray::Prepend

    // method line 824
    .method private static hidebysig pinvokeimpl ("libgobject-2.0-0.dll" as "g_value_array_remove" cdecl )
           default void g_value_array_remove (native int raw, unsigned int32 idx)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method ValueArray::g_value_array_remove

    // method line 825
    .method public hidebysig 
           instance default void Remove (unsigned int32 idx)  cil managed 
    {
        // Method begins at RVA 0x8a5c
	// Code size 13 (0xd)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance native int class GLib.ValueArray::get_Handle()
	IL_0006:  ldarg.1 
	IL_0007:  call void class GLib.ValueArray::g_value_array_remove(native int, unsigned int32)
	IL_000c:  ret 
    } // end of method ValueArray::Remove

    // method line 826
    .method private static hidebysig pinvokeimpl ("glibsharpglue-2" as "gtksharp_value_array_get_count" cdecl )
           default int32 gtksharp_value_array_get_count (native int raw)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method ValueArray::gtksharp_value_array_get_count

    // method line 827
    .method public final virtual hidebysig newslot specialname 
           instance default int32 get_Count ()  cil managed 
    {
        // Method begins at RVA 0x8a6c
	// Code size 12 (0xc)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance native int class GLib.ValueArray::get_Handle()
	IL_0006:  call int32 class GLib.ValueArray::gtksharp_value_array_get_count(native int)
	IL_000b:  ret 
    } // end of method ValueArray::get_Count

    // method line 828
    .method private static hidebysig pinvokeimpl ("libgobject-2.0-0.dll" as "g_value_array_get_nth" cdecl )
           default native int g_value_array_get_nth (native int raw, unsigned int32 idx)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method ValueArray::g_value_array_get_nth

    // method line 829
    .method public hidebysig specialname 
           instance default object get_Item (int32 index)  cil managed 
    {
        // Method begins at RVA 0x8a7c
	// Code size 31 (0x1f)
	.maxstack 2
	.locals init (
		valuetype GLib.Value	V_0)
	IL_0000:  ldsfld valuetype GLib.Value GLib.Value::Empty
	IL_0005:  stloc.0 
	IL_0006:  ldarg.0 
	IL_0007:  call instance native int class GLib.ValueArray::get_Handle()
	IL_000c:  ldarg.1 
	IL_000d:  call native int class GLib.ValueArray::g_value_array_get_nth(native int, unsigned int32)
	IL_0012:  ldloc.0 
	IL_0013:  call void class [mscorlib]System.Runtime.InteropServices.Marshal::PtrToStructure<valuetype GLib.Value> (native int, !!0)
	IL_0018:  ldloc.0 
	IL_0019:  box GLib.Value
	IL_001e:  ret 
    } // end of method ValueArray::get_Item

    // method line 830
    .method public final virtual hidebysig newslot specialname 
           instance default bool get_IsSynchronized ()  cil managed 
    {
        // Method begins at RVA 0x8aa8
	// Code size 2 (0x2)
	.maxstack 8
	IL_0000:  ldc.i4.0 
	IL_0001:  ret 
    } // end of method ValueArray::get_IsSynchronized

    // method line 831
    .method public final virtual hidebysig newslot specialname 
           instance default object get_SyncRoot ()  cil managed 
    {
        // Method begins at RVA 0x8aac
	// Code size 2 (0x2)
	.maxstack 8
	IL_0000:  ldnull 
	IL_0001:  ret 
    } // end of method ValueArray::get_SyncRoot

    // method line 832
    .method public final virtual hidebysig newslot 
           instance default void CopyTo (class [mscorlib]System.Array 'array', int32 index)  cil managed 
    {
        // Method begins at RVA 0x8ab0
	// Code size 105 (0x69)
	.maxstack 4
	.locals init (
		int32	V_0)
	IL_0000:  ldarg.1 
	IL_0001:  brtrue IL_0011

	IL_0006:  ldstr "Array can't be null."
	IL_000b:  newobj instance void class [mscorlib]System.ArgumentNullException::'.ctor'(string)
	IL_0010:  throw 
	IL_0011:  ldarg.2 
	IL_0012:  ldc.i4.0 
	IL_0013:  bge IL_0023

	IL_0018:  ldstr "Index must be greater than 0."
	IL_001d:  newobj instance void class [mscorlib]System.ArgumentOutOfRangeException::'.ctor'(string)
	IL_0022:  throw 
	IL_0023:  ldarg.2 
	IL_0024:  ldarg.0 
	IL_0025:  call instance int32 class GLib.ValueArray::get_Count()
	IL_002a:  add 
	IL_002b:  ldarg.1 
	IL_002c:  callvirt instance int32 class [mscorlib]System.Array::get_Length()
	IL_0031:  bge IL_0041

	IL_0036:  ldstr "Array not large enough to copy into starting at index."
	IL_003b:  newobj instance void class [mscorlib]System.ArgumentException::'.ctor'(string)
	IL_0040:  throw 
	IL_0041:  ldc.i4.0 
	IL_0042:  stloc.0 
	IL_0043:  br IL_005c

	IL_0048:  ldarg.1 
	IL_0049:  ldarg.2 
	IL_004a:  ldloc.0 
	IL_004b:  add 
	IL_004c:  ldarg.0 
	IL_004d:  ldloc.0 
	IL_004e:  call instance object class GLib.ValueArray::get_Item(int32)
	IL_0053:  callvirt instance void class [mscorlib]System.Collections.IList::set_Item(int32, object)
	IL_0058:  ldloc.0 
	IL_0059:  ldc.i4.1 
	IL_005a:  add 
	IL_005b:  stloc.0 
	IL_005c:  ldloc.0 
	IL_005d:  ldarg.0 
	IL_005e:  call instance int32 class GLib.ValueArray::get_Count()
	IL_0063:  blt IL_0048

	IL_0068:  ret 
    } // end of method ValueArray::CopyTo

    // method line 833
    .method public final virtual hidebysig newslot 
           instance default class [mscorlib]System.Collections.IEnumerator GetEnumerator ()  cil managed 
    {
        // Method begins at RVA 0x8b28
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  newobj instance void class GLib.ValueArray/ListEnumerator::'.ctor'(class GLib.ValueArray)
	IL_0006:  ret 
    } // end of method ValueArray::GetEnumerator

    // method line 834
    .method private static hidebysig pinvokeimpl ("libgobject-2.0-0.dll" as "g_value_array_copy" cdecl )
           default native int g_value_array_copy (native int raw)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method ValueArray::g_value_array_copy

    // method line 835
    .method public final virtual hidebysig newslot 
           instance default object Clone ()  cil managed 
    {
        // Method begins at RVA 0x8b30
	// Code size 17 (0x11)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance native int class GLib.ValueArray::get_Handle()
	IL_0006:  call native int class GLib.ValueArray::g_value_array_copy(native int)
	IL_000b:  newobj instance void class GLib.ValueArray::'.ctor'(native int)
	IL_0010:  ret 
    } // end of method ValueArray::Clone

    // method line 836
    .method private static hidebysig specialname rtspecialname 
           default void '.cctor' ()  cil managed 
    {
        // Method begins at RVA 0x8b44
	// Code size 28 (0x1c)
	.maxstack 8
	IL_0000:  newobj instance void object::'.ctor'()
	IL_0005:  stsfld object GLib.ValueArray::lockObject
	IL_000a:  ldc.i4.8 
	IL_000b:  newobj instance void class [mscorlib]System.Collections.Generic.List`1<native int>::'.ctor'(int32)
	IL_0010:  stsfld class [mscorlib]System.Collections.Generic.List`1<native int> GLib.ValueArray::PendingFrees
	IL_0015:  ldc.i4.0 
	IL_0016:  stsfld bool GLib.ValueArray::idle_queued
	IL_001b:  ret 
    } // end of method ValueArray::.cctor

	.property instance native int Handle ()
	{
		.get instance default native int GLib.ValueArray::get_Handle () 
	}
	.property instance native int ArrayPtr ()
	{
		.get instance default native int GLib.ValueArray::get_ArrayPtr () 
	}
	.property instance int32 Count ()
	{
		.get instance default int32 GLib.ValueArray::get_Count () 
	}
	.property instance object Item (int32)
	{
		.get instance default object GLib.ValueArray::get_Item (int32 index) 
	}
	.property instance bool IsSynchronized ()
	{
		.get instance default bool GLib.ValueArray::get_IsSynchronized () 
	}
	.property instance object SyncRoot ()
	{
		.get instance default object GLib.ValueArray::get_SyncRoot () 
	}
  .class nested private auto ansi beforefieldinit ListEnumerator
  	extends [mscorlib]System.Object
  	implements [mscorlib]System.Collections.IEnumerator  {
    .field  private  int32 current
    .field  private  class GLib.ValueArray vals

    // method line 837
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (class GLib.ValueArray vals)  cil managed 
    {
        // Method begins at RVA 0x8b64
	// Code size 21 (0x15)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldc.i4.m1 
	IL_0002:  stfld int32 GLib.ValueArray/ListEnumerator::current
	IL_0007:  ldarg.0 
	IL_0008:  call instance void object::'.ctor'()
	IL_000d:  ldarg.0 
	IL_000e:  ldarg.1 
	IL_000f:  stfld class GLib.ValueArray GLib.ValueArray/ListEnumerator::vals
	IL_0014:  ret 
    } // end of method ListEnumerator::.ctor

    // method line 838
    .method public final virtual hidebysig newslot specialname 
           instance default object get_Current ()  cil managed 
    {
        // Method begins at RVA 0x8b7c
	// Code size 32 (0x20)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldfld int32 GLib.ValueArray/ListEnumerator::current
	IL_0006:  ldc.i4.m1 
	IL_0007:  bne.un IL_000e

	IL_000c:  ldnull 
	IL_000d:  ret 
	IL_000e:  ldarg.0 
	IL_000f:  ldfld class GLib.ValueArray GLib.ValueArray/ListEnumerator::vals
	IL_0014:  ldarg.0 
	IL_0015:  ldfld int32 GLib.ValueArray/ListEnumerator::current
	IL_001a:  callvirt instance object class GLib.ValueArray::get_Item(int32)
	IL_001f:  ret 
    } // end of method ListEnumerator::get_Current

    // method line 839
    .method public final virtual hidebysig newslot 
           instance default bool MoveNext ()  cil managed 
    {
        // Method begins at RVA 0x8ba0
	// Code size 44 (0x2c)
	.maxstack 3
	.locals init (
		int32	V_0)
	IL_0000:  ldarg.0 
	IL_0001:  dup 
	IL_0002:  ldfld int32 GLib.ValueArray/ListEnumerator::current
	IL_0007:  ldc.i4.1 
	IL_0008:  add 
	IL_0009:  dup 
	IL_000a:  stloc.0 
	IL_000b:  stfld int32 GLib.ValueArray/ListEnumerator::current
	IL_0010:  ldloc.0 
	IL_0011:  ldarg.0 
	IL_0012:  ldfld class GLib.ValueArray GLib.ValueArray/ListEnumerator::vals
	IL_0017:  callvirt instance int32 class GLib.ValueArray::get_Count()
	IL_001c:  blt IL_002a

	IL_0021:  ldarg.0 
	IL_0022:  ldc.i4.m1 
	IL_0023:  stfld int32 GLib.ValueArray/ListEnumerator::current
	IL_0028:  ldc.i4.0 
	IL_0029:  ret 
	IL_002a:  ldc.i4.1 
	IL_002b:  ret 
    } // end of method ListEnumerator::MoveNext

    // method line 840
    .method public final virtual hidebysig newslot 
           instance default void Reset ()  cil managed 
    {
        // Method begins at RVA 0x8bd8
	// Code size 8 (0x8)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldc.i4.m1 
	IL_0002:  stfld int32 GLib.ValueArray/ListEnumerator::current
	IL_0007:  ret 
    } // end of method ListEnumerator::Reset

	.property instance object Current ()
	{
		.get instance default object GLib.ValueArray/ListEnumerator::get_Current () 
	}
  } // end of class ListEnumerator

  } // end of class GLib.ValueArray
}

.namespace GLib
{
  .class public sequential ansi sealed beforefieldinit Value
  	extends [mscorlib]System.ValueType
  	implements [mscorlib]System.IDisposable  {
    .field  private  native int 'type'
    .field  private  int64 pad_1
    .field  private  int64 pad_2
    .field  public static  valuetype GLib.Value Empty

    // method line 841
    .method public hidebysig specialname 
           instance default void set_Val (object 'value')  cil managed 
    {
        // Method begins at RVA 0x8be4
	// Code size 1054 (0x41e)
	.maxstack 3
	.locals init (
		valuetype GLib.GType	V_0,
		valuetype GLib.GType	V_1,
		valuetype GLib.GType	V_2,
		valuetype GLib.GType	V_3,
		valuetype GLib.GType	V_4,
		valuetype GLib.GType	V_5,
		valuetype GLib.GType	V_6,
		valuetype GLib.GType	V_7,
		valuetype GLib.GType	V_8,
		valuetype GLib.GType	V_9,
		valuetype GLib.GType	V_10,
		valuetype GLib.GType	V_11,
		native int	V_12,
		valuetype GLib.GType	V_13,
		native int	V_14,
		valuetype GLib.GType	V_15,
		valuetype GLib.GType	V_16,
		valuetype GLib.GType	V_17,
		native int	V_18,
		valuetype GLib.GType	V_19,
		valuetype GLib.GType	V_20,
		native int	V_21,
		valuetype GLib.GType	V_22)
	IL_0000:  ldarg.0 
	IL_0001:  ldfld native int GLib.Value::'type'
	IL_0006:  ldsfld valuetype GLib.GType GLib.GType::Boolean
	IL_000b:  stloc.0 
	IL_000c:  ldloca.s 0
	IL_000e:  call instance native int valuetype GLib.GType::get_Val()
	IL_0013:  call bool native int::op_Equality(native int, native int)
	IL_0018:  brfalse IL_002e

	IL_001d:  ldarg.0 
	IL_001e:  ldarg.1 
	IL_001f:  unbox.any bool
	IL_0024:  call void valuetype GLib.Value::g_value_set_boolean([out] valuetype GLib.Value&, bool)
	IL_0029:  br IL_041d

	IL_002e:  ldarg.0 
	IL_002f:  ldfld native int GLib.Value::'type'
	IL_0034:  ldsfld valuetype GLib.GType GLib.GType::UChar
	IL_0039:  stloc.1 
	IL_003a:  ldloca.s 1
	IL_003c:  call instance native int valuetype GLib.GType::get_Val()
	IL_0041:  call bool native int::op_Equality(native int, native int)
	IL_0046:  brfalse IL_005c

	IL_004b:  ldarg.0 
	IL_004c:  ldarg.1 
	IL_004d:  unbox.any unsigned int8
	IL_0052:  call void valuetype GLib.Value::g_value_set_uchar([out] valuetype GLib.Value&, unsigned int8)
	IL_0057:  br IL_041d

	IL_005c:  ldarg.0 
	IL_005d:  ldfld native int GLib.Value::'type'
	IL_0062:  ldsfld valuetype GLib.GType GLib.GType::Char
	IL_0067:  stloc.2 
	IL_0068:  ldloca.s 2
	IL_006a:  call instance native int valuetype GLib.GType::get_Val()
	IL_006f:  call bool native int::op_Equality(native int, native int)
	IL_0074:  brfalse IL_008a

	IL_0079:  ldarg.0 
	IL_007a:  ldarg.1 
	IL_007b:  unbox.any int8
	IL_0080:  call void valuetype GLib.Value::g_value_set_char([out] valuetype GLib.Value&, int8)
	IL_0085:  br IL_041d

	IL_008a:  ldarg.0 
	IL_008b:  ldfld native int GLib.Value::'type'
	IL_0090:  ldsfld valuetype GLib.GType GLib.GType::Int
	IL_0095:  stloc.3 
	IL_0096:  ldloca.s 3
	IL_0098:  call instance native int valuetype GLib.GType::get_Val()
	IL_009d:  call bool native int::op_Equality(native int, native int)
	IL_00a2:  brfalse IL_00b8

	IL_00a7:  ldarg.0 
	IL_00a8:  ldarg.1 
	IL_00a9:  unbox.any int32
	IL_00ae:  call void valuetype GLib.Value::g_value_set_int([out] valuetype GLib.Value&, int32)
	IL_00b3:  br IL_041d

	IL_00b8:  ldarg.0 
	IL_00b9:  ldfld native int GLib.Value::'type'
	IL_00be:  ldsfld valuetype GLib.GType GLib.GType::UInt
	IL_00c3:  stloc.s 4
	IL_00c5:  ldloca.s 4
	IL_00c7:  call instance native int valuetype GLib.GType::get_Val()
	IL_00cc:  call bool native int::op_Equality(native int, native int)
	IL_00d1:  brfalse IL_00e7

	IL_00d6:  ldarg.0 
	IL_00d7:  ldarg.1 
	IL_00d8:  unbox.any unsigned int32
	IL_00dd:  call void valuetype GLib.Value::g_value_set_uint([out] valuetype GLib.Value&, unsigned int32)
	IL_00e2:  br IL_041d

	IL_00e7:  ldarg.0 
	IL_00e8:  ldfld native int GLib.Value::'type'
	IL_00ed:  ldsfld valuetype GLib.GType GLib.GType::Int64
	IL_00f2:  stloc.s 5
	IL_00f4:  ldloca.s 5
	IL_00f6:  call instance native int valuetype GLib.GType::get_Val()
	IL_00fb:  call bool native int::op_Equality(native int, native int)
	IL_0100:  brfalse IL_0116

	IL_0105:  ldarg.0 
	IL_0106:  ldarg.1 
	IL_0107:  unbox.any int64
	IL_010c:  call void valuetype GLib.Value::g_value_set_int64([out] valuetype GLib.Value&, int64)
	IL_0111:  br IL_041d

	IL_0116:  ldarg.0 
	IL_0117:  ldfld native int GLib.Value::'type'
	IL_011c:  ldsfld valuetype GLib.GType GLib.GType::UInt64
	IL_0121:  stloc.s 6
	IL_0123:  ldloca.s 6
	IL_0125:  call instance native int valuetype GLib.GType::get_Val()
	IL_012a:  call bool native int::op_Equality(native int, native int)
	IL_012f:  brfalse IL_0145

	IL_0134:  ldarg.0 
	IL_0135:  ldarg.1 
	IL_0136:  unbox.any unsigned int64
	IL_013b:  call void valuetype GLib.Value::g_value_set_uint64([out] valuetype GLib.Value&, unsigned int64)
	IL_0140:  br IL_041d

	IL_0145:  ldarg.0 
	IL_0146:  ldfld native int GLib.Value::'type'
	IL_014b:  ldsfld valuetype GLib.GType GLib.GType::Enum
	IL_0150:  stloc.s 7
	IL_0152:  ldloca.s 7
	IL_0154:  call instance native int valuetype GLib.GType::get_Val()
	IL_0159:  call bool valuetype GLib.Value::g_type_is_a(native int, native int)
	IL_015e:  brfalse IL_0174

	IL_0163:  ldarg.0 
	IL_0164:  ldarg.1 
	IL_0165:  unbox.any int32
	IL_016a:  call void valuetype GLib.Value::g_value_set_enum([out] valuetype GLib.Value&, int32)
	IL_016f:  br IL_041d

	IL_0174:  ldarg.0 
	IL_0175:  ldfld native int GLib.Value::'type'
	IL_017a:  ldsfld valuetype GLib.GType GLib.GType::Flags
	IL_017f:  stloc.s 8
	IL_0181:  ldloca.s 8
	IL_0183:  call instance native int valuetype GLib.GType::get_Val()
	IL_0188:  call bool valuetype GLib.Value::g_type_is_a(native int, native int)
	IL_018d:  brfalse IL_01a3

	IL_0192:  ldarg.0 
	IL_0193:  ldarg.1 
	IL_0194:  unbox.any int32
	IL_0199:  call void valuetype GLib.Value::g_value_set_flags([out] valuetype GLib.Value&, unsigned int32)
	IL_019e:  br IL_041d

	IL_01a3:  ldarg.0 
	IL_01a4:  ldfld native int GLib.Value::'type'
	IL_01a9:  ldsfld valuetype GLib.GType GLib.GType::Float
	IL_01ae:  stloc.s 9
	IL_01b0:  ldloca.s 9
	IL_01b2:  call instance native int valuetype GLib.GType::get_Val()
	IL_01b7:  call bool native int::op_Equality(native int, native int)
	IL_01bc:  brfalse IL_01d2

	IL_01c1:  ldarg.0 
	IL_01c2:  ldarg.1 
	IL_01c3:  unbox.any float32
	IL_01c8:  call void valuetype GLib.Value::g_value_set_float([out] valuetype GLib.Value&, float32)
	IL_01cd:  br IL_041d

	IL_01d2:  ldarg.0 
	IL_01d3:  ldfld native int GLib.Value::'type'
	IL_01d8:  ldsfld valuetype GLib.GType GLib.GType::Double
	IL_01dd:  stloc.s 10
	IL_01df:  ldloca.s 10
	IL_01e1:  call instance native int valuetype GLib.GType::get_Val()
	IL_01e6:  call bool native int::op_Equality(native int, native int)
	IL_01eb:  brfalse IL_0201

	IL_01f0:  ldarg.0 
	IL_01f1:  ldarg.1 
	IL_01f2:  unbox.any float64
	IL_01f7:  call void valuetype GLib.Value::g_value_set_double([out] valuetype GLib.Value&, float64)
	IL_01fc:  br IL_041d

	IL_0201:  ldarg.0 
	IL_0202:  ldfld native int GLib.Value::'type'
	IL_0207:  ldsfld valuetype GLib.GType GLib.GType::String
	IL_020c:  stloc.s 11
	IL_020e:  ldloca.s 11
	IL_0210:  call instance native int valuetype GLib.GType::get_Val()
	IL_0215:  call bool native int::op_Equality(native int, native int)
	IL_021a:  brfalse IL_0240

	IL_021f:  ldarg.1 
	IL_0220:  castclass string
	IL_0225:  call native int class GLib.Marshaller::StringToPtrGStrdup(string)
	IL_022a:  stloc.s 12
	IL_022c:  ldarg.0 
	IL_022d:  ldloc.s 12
	IL_022f:  call void valuetype GLib.Value::g_value_set_string([out] valuetype GLib.Value&, native int)
	IL_0234:  ldloc.s 12
	IL_0236:  call void class GLib.Marshaller::Free(native int)
	IL_023b:  br IL_041d

	IL_0240:  ldarg.0 
	IL_0241:  ldfld native int GLib.Value::'type'
	IL_0246:  ldsfld valuetype GLib.GType GLib.GType::Pointer
	IL_024b:  stloc.s 13
	IL_024d:  ldloca.s 13
	IL_024f:  call instance native int valuetype GLib.GType::get_Val()
	IL_0254:  call bool native int::op_Equality(native int, native int)
	IL_0259:  brfalse IL_02de

	IL_025e:  ldarg.1 
	IL_025f:  isinst native int
	IL_0264:  brfalse IL_0276

	IL_0269:  ldarg.0 
	IL_026a:  ldarg.1 
	IL_026b:  unbox.any native int
	IL_0270:  call void valuetype GLib.Value::g_value_set_pointer([out] valuetype GLib.Value&, native int)
	IL_0275:  ret 
	IL_0276:  ldarg.1 
	IL_0277:  isinst GLib.IWrapper
	IL_027c:  brfalse IL_0293

	IL_0281:  ldarg.0 
	IL_0282:  ldarg.1 
	IL_0283:  castclass GLib.IWrapper
	IL_0288:  callvirt instance native int class GLib.IWrapper::get_Handle()
	IL_028d:  call void valuetype GLib.Value::g_value_set_pointer([out] valuetype GLib.Value&, native int)
	IL_0292:  ret 
	IL_0293:  ldarg.1 
	IL_0294:  call native int class GLib.ManagedValue::WrapObject(object)
	IL_0299:  stloc.s 14
	IL_029b:  ldarg.0 
	IL_029c:  ldfld native int GLib.Value::'type'
	IL_02a1:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_02a6:  call bool native int::op_Inequality(native int, native int)
	IL_02ab:  brfalse IL_02b6

	IL_02b0:  ldarg.0 
	IL_02b1:  call void valuetype GLib.Value::g_value_unset([out] valuetype GLib.Value&)
	IL_02b6:  ldarg.0 
	IL_02b7:  call valuetype GLib.GType class GLib.ManagedValue::get_GType()
	IL_02bc:  stloc.s 15
	IL_02be:  ldloca.s 15
	IL_02c0:  call instance native int valuetype GLib.GType::get_Val()
	IL_02c5:  call void valuetype GLib.Value::g_value_init([out] valuetype GLib.Value&, native int)
	IL_02ca:  ldarg.0 
	IL_02cb:  ldloc.s 14
	IL_02cd:  call void valuetype GLib.Value::g_value_set_boxed([out] valuetype GLib.Value&, native int)
	IL_02d2:  ldloc.s 14
	IL_02d4:  call void class GLib.ManagedValue::ReleaseWrapper(native int)
	IL_02d9:  br IL_041d

	IL_02de:  ldarg.0 
	IL_02df:  ldfld native int GLib.Value::'type'
	IL_02e4:  ldsfld valuetype GLib.GType GLib.GType::Param
	IL_02e9:  stloc.s 16
	IL_02eb:  ldloca.s 16
	IL_02ed:  call instance native int valuetype GLib.GType::get_Val()
	IL_02f2:  call bool native int::op_Equality(native int, native int)
	IL_02f7:  brfalse IL_030d

	IL_02fc:  ldarg.0 
	IL_02fd:  ldarg.1 
	IL_02fe:  unbox.any native int
	IL_0303:  call void valuetype GLib.Value::g_value_set_param([out] valuetype GLib.Value&, native int)
	IL_0308:  br IL_041d

	IL_030d:  ldarg.0 
	IL_030e:  ldfld native int GLib.Value::'type'
	IL_0313:  call valuetype GLib.GType class GLib.ManagedValue::get_GType()
	IL_0318:  stloc.s 17
	IL_031a:  ldloca.s 17
	IL_031c:  call instance native int valuetype GLib.GType::get_Val()
	IL_0321:  call bool native int::op_Equality(native int, native int)
	IL_0326:  brfalse IL_0347

	IL_032b:  ldarg.1 
	IL_032c:  call native int class GLib.ManagedValue::WrapObject(object)
	IL_0331:  stloc.s 18
	IL_0333:  ldarg.0 
	IL_0334:  ldloc.s 18
	IL_0336:  call void valuetype GLib.Value::g_value_set_boxed([out] valuetype GLib.Value&, native int)
	IL_033b:  ldloc.s 18
	IL_033d:  call void class GLib.ManagedValue::ReleaseWrapper(native int)
	IL_0342:  br IL_041d

	IL_0347:  ldarg.0 
	IL_0348:  ldfld native int GLib.Value::'type'
	IL_034d:  ldsfld valuetype GLib.GType GLib.GType::Object
	IL_0352:  stloc.s 19
	IL_0354:  ldloca.s 19
	IL_0356:  call instance native int valuetype GLib.GType::get_Val()
	IL_035b:  call bool valuetype GLib.Value::g_type_is_a(native int, native int)
	IL_0360:  brfalse IL_039c

	IL_0365:  ldarg.1 
	IL_0366:  isinst GLib.Object
	IL_036b:  brfalse IL_0386

	IL_0370:  ldarg.0 
	IL_0371:  ldarg.1 
	IL_0372:  isinst GLib.Object
	IL_0377:  callvirt instance native int class GLib.Object::get_Handle()
	IL_037c:  call void valuetype GLib.Value::g_value_set_object([out] valuetype GLib.Value&, native int)
	IL_0381:  br IL_0397

	IL_0386:  ldarg.0 
	IL_0387:  ldarg.1 
	IL_0388:  isinst GLib.GInterfaceAdapter
	IL_038d:  callvirt instance native int class GLib.GInterfaceAdapter::get_Handle()
	IL_0392:  call void valuetype GLib.Value::g_value_set_object([out] valuetype GLib.Value&, native int)
	IL_0397:  br IL_041d

	IL_039c:  ldarg.0 
	IL_039d:  ldfld native int GLib.Value::'type'
	IL_03a2:  ldsfld valuetype GLib.GType GLib.GType::Boxed
	IL_03a7:  stloc.s 20
	IL_03a9:  ldloca.s 20
	IL_03ab:  call instance native int valuetype GLib.GType::get_Val()
	IL_03b0:  call bool valuetype GLib.Value::g_type_is_a(native int, native int)
	IL_03b5:  brfalse IL_03f3

	IL_03ba:  ldarg.1 
	IL_03bb:  isinst GLib.IWrapper
	IL_03c0:  brfalse IL_03d7

	IL_03c5:  ldarg.0 
	IL_03c6:  ldarg.1 
	IL_03c7:  castclass GLib.IWrapper
	IL_03cc:  callvirt instance native int class GLib.IWrapper::get_Handle()
	IL_03d1:  call void valuetype GLib.Value::g_value_set_boxed([out] valuetype GLib.Value&, native int)
	IL_03d6:  ret 
	IL_03d7:  ldarg.1 
	IL_03d8:  call native int class GLib.Marshaller::StructureToPtrAlloc(object)
	IL_03dd:  stloc.s 21
	IL_03df:  ldarg.0 
	IL_03e0:  ldloc.s 21
	IL_03e2:  call void valuetype GLib.Value::g_value_set_boxed([out] valuetype GLib.Value&, native int)
	IL_03e7:  ldloc.s 21
	IL_03e9:  call void class [mscorlib]System.Runtime.InteropServices.Marshal::FreeHGlobal(native int)
	IL_03ee:  br IL_041d

	IL_03f3:  ldstr "Unknown type "
	IL_03f8:  ldloca.s 22
	IL_03fa:  ldarg.0 
	IL_03fb:  ldfld native int GLib.Value::'type'
	IL_0400:  call instance void valuetype GLib.GType::'.ctor'(native int)
	IL_0405:  ldloca.s 22
	IL_0407:  constrained. GLib.GType
	IL_040d:  callvirt instance string object::ToString()
	IL_0412:  call string string::Concat(string, string)
	IL_0417:  newobj instance void class [mscorlib]System.Exception::'.ctor'(string)
	IL_041c:  throw 
	IL_041d:  ret 
    } // end of method Value::set_Val

    // method line 842
    .method public hidebysig specialname 
           instance default object get_Val ()  cil managed 
    {
        // Method begins at RVA 0x9010
	// Code size 808 (0x328)
	.maxstack 3
	.locals init (
		valuetype GLib.GType	V_0,
		valuetype GLib.GType	V_1,
		valuetype GLib.GType	V_2,
		valuetype GLib.GType	V_3,
		valuetype GLib.GType	V_4,
		valuetype GLib.GType	V_5,
		valuetype GLib.GType	V_6,
		valuetype GLib.GType	V_7,
		valuetype GLib.GType	V_8,
		valuetype GLib.GType	V_9,
		valuetype GLib.GType	V_10,
		valuetype GLib.GType	V_11,
		valuetype GLib.GType	V_12,
		valuetype GLib.GType	V_13,
		valuetype GLib.GType	V_14,
		valuetype GLib.GType	V_15,
		valuetype GLib.GType	V_16,
		valuetype GLib.GType	V_17)
	IL_0000:  ldarg.0 
	IL_0001:  ldfld native int GLib.Value::'type'
	IL_0006:  ldsfld valuetype GLib.GType GLib.GType::Boolean
	IL_000b:  stloc.0 
	IL_000c:  ldloca.s 0
	IL_000e:  call instance native int valuetype GLib.GType::get_Val()
	IL_0013:  call bool native int::op_Equality(native int, native int)
	IL_0018:  brfalse IL_002e

	IL_001d:  ldarg.0 
	IL_001e:  ldobj GLib.Value
	IL_0023:  call bool valuetype GLib.Value::op_Explicit(valuetype GLib.Value)
	IL_0028:  box bool
	IL_002d:  ret 
	IL_002e:  ldarg.0 
	IL_002f:  ldfld native int GLib.Value::'type'
	IL_0034:  ldsfld valuetype GLib.GType GLib.GType::UChar
	IL_0039:  stloc.1 
	IL_003a:  ldloca.s 1
	IL_003c:  call instance native int valuetype GLib.GType::get_Val()
	IL_0041:  call bool native int::op_Equality(native int, native int)
	IL_0046:  brfalse IL_005c

	IL_004b:  ldarg.0 
	IL_004c:  ldobj GLib.Value
	IL_0051:  call unsigned int8 valuetype GLib.Value::op_Explicit(valuetype GLib.Value)
	IL_0056:  box unsigned int8
	IL_005b:  ret 
	IL_005c:  ldarg.0 
	IL_005d:  ldfld native int GLib.Value::'type'
	IL_0062:  ldsfld valuetype GLib.GType GLib.GType::Char
	IL_0067:  stloc.2 
	IL_0068:  ldloca.s 2
	IL_006a:  call instance native int valuetype GLib.GType::get_Val()
	IL_006f:  call bool native int::op_Equality(native int, native int)
	IL_0074:  brfalse IL_008a

	IL_0079:  ldarg.0 
	IL_007a:  ldobj GLib.Value
	IL_007f:  call int8 valuetype GLib.Value::op_Explicit(valuetype GLib.Value)
	IL_0084:  box int8
	IL_0089:  ret 
	IL_008a:  ldarg.0 
	IL_008b:  ldfld native int GLib.Value::'type'
	IL_0090:  ldsfld valuetype GLib.GType GLib.GType::Int
	IL_0095:  stloc.3 
	IL_0096:  ldloca.s 3
	IL_0098:  call instance native int valuetype GLib.GType::get_Val()
	IL_009d:  call bool native int::op_Equality(native int, native int)
	IL_00a2:  brfalse IL_00b8

	IL_00a7:  ldarg.0 
	IL_00a8:  ldobj GLib.Value
	IL_00ad:  call int32 valuetype GLib.Value::op_Explicit(valuetype GLib.Value)
	IL_00b2:  box int32
	IL_00b7:  ret 
	IL_00b8:  ldarg.0 
	IL_00b9:  ldfld native int GLib.Value::'type'
	IL_00be:  ldsfld valuetype GLib.GType GLib.GType::UInt
	IL_00c3:  stloc.s 4
	IL_00c5:  ldloca.s 4
	IL_00c7:  call instance native int valuetype GLib.GType::get_Val()
	IL_00cc:  call bool native int::op_Equality(native int, native int)
	IL_00d1:  brfalse IL_00e7

	IL_00d6:  ldarg.0 
	IL_00d7:  ldobj GLib.Value
	IL_00dc:  call unsigned int32 valuetype GLib.Value::op_Explicit(valuetype GLib.Value)
	IL_00e1:  box unsigned int32
	IL_00e6:  ret 
	IL_00e7:  ldarg.0 
	IL_00e8:  ldfld native int GLib.Value::'type'
	IL_00ed:  ldsfld valuetype GLib.GType GLib.GType::Int64
	IL_00f2:  stloc.s 5
	IL_00f4:  ldloca.s 5
	IL_00f6:  call instance native int valuetype GLib.GType::get_Val()
	IL_00fb:  call bool native int::op_Equality(native int, native int)
	IL_0100:  brfalse IL_0116

	IL_0105:  ldarg.0 
	IL_0106:  ldobj GLib.Value
	IL_010b:  call int64 valuetype GLib.Value::op_Explicit(valuetype GLib.Value)
	IL_0110:  box int64
	IL_0115:  ret 
	IL_0116:  ldarg.0 
	IL_0117:  ldfld native int GLib.Value::'type'
	IL_011c:  ldsfld valuetype GLib.GType GLib.GType::UInt64
	IL_0121:  stloc.s 6
	IL_0123:  ldloca.s 6
	IL_0125:  call instance native int valuetype GLib.GType::get_Val()
	IL_012a:  call bool native int::op_Equality(native int, native int)
	IL_012f:  brfalse IL_0145

	IL_0134:  ldarg.0 
	IL_0135:  ldobj GLib.Value
	IL_013a:  call unsigned int64 valuetype GLib.Value::op_Explicit(valuetype GLib.Value)
	IL_013f:  box unsigned int64
	IL_0144:  ret 
	IL_0145:  ldarg.0 
	IL_0146:  ldfld native int GLib.Value::'type'
	IL_014b:  ldsfld valuetype GLib.GType GLib.GType::Enum
	IL_0150:  stloc.s 7
	IL_0152:  ldloca.s 7
	IL_0154:  call instance native int valuetype GLib.GType::get_Val()
	IL_0159:  call bool valuetype GLib.Value::g_type_is_a(native int, native int)
	IL_015e:  brtrue IL_0181

	IL_0163:  ldarg.0 
	IL_0164:  ldfld native int GLib.Value::'type'
	IL_0169:  ldsfld valuetype GLib.GType GLib.GType::Flags
	IL_016e:  stloc.s 8
	IL_0170:  ldloca.s 8
	IL_0172:  call instance native int valuetype GLib.GType::get_Val()
	IL_0177:  call bool valuetype GLib.Value::g_type_is_a(native int, native int)
	IL_017c:  brfalse IL_018d

	IL_0181:  ldarg.0 
	IL_0182:  ldobj GLib.Value
	IL_0187:  call class [mscorlib]System.Enum valuetype GLib.Value::op_Explicit(valuetype GLib.Value)
	IL_018c:  ret 
	IL_018d:  ldarg.0 
	IL_018e:  ldfld native int GLib.Value::'type'
	IL_0193:  ldsfld valuetype GLib.GType GLib.GType::Float
	IL_0198:  stloc.s 9
	IL_019a:  ldloca.s 9
	IL_019c:  call instance native int valuetype GLib.GType::get_Val()
	IL_01a1:  call bool native int::op_Equality(native int, native int)
	IL_01a6:  brfalse IL_01bc

	IL_01ab:  ldarg.0 
	IL_01ac:  ldobj GLib.Value
	IL_01b1:  call float32 valuetype GLib.Value::op_Explicit(valuetype GLib.Value)
	IL_01b6:  box float32
	IL_01bb:  ret 
	IL_01bc:  ldarg.0 
	IL_01bd:  ldfld native int GLib.Value::'type'
	IL_01c2:  ldsfld valuetype GLib.GType GLib.GType::Double
	IL_01c7:  stloc.s 10
	IL_01c9:  ldloca.s 10
	IL_01cb:  call instance native int valuetype GLib.GType::get_Val()
	IL_01d0:  call bool native int::op_Equality(native int, native int)
	IL_01d5:  brfalse IL_01eb

	IL_01da:  ldarg.0 
	IL_01db:  ldobj GLib.Value
	IL_01e0:  call float64 valuetype GLib.Value::op_Explicit(valuetype GLib.Value)
	IL_01e5:  box float64
	IL_01ea:  ret 
	IL_01eb:  ldarg.0 
	IL_01ec:  ldfld native int GLib.Value::'type'
	IL_01f1:  ldsfld valuetype GLib.GType GLib.GType::String
	IL_01f6:  stloc.s 11
	IL_01f8:  ldloca.s 11
	IL_01fa:  call instance native int valuetype GLib.GType::get_Val()
	IL_01ff:  call bool native int::op_Equality(native int, native int)
	IL_0204:  brfalse IL_0215

	IL_0209:  ldarg.0 
	IL_020a:  ldobj GLib.Value
	IL_020f:  call string valuetype GLib.Value::op_Explicit(valuetype GLib.Value)
	IL_0214:  ret 
	IL_0215:  ldarg.0 
	IL_0216:  ldfld native int GLib.Value::'type'
	IL_021b:  ldsfld valuetype GLib.GType GLib.GType::Pointer
	IL_0220:  stloc.s 12
	IL_0222:  ldloca.s 12
	IL_0224:  call instance native int valuetype GLib.GType::get_Val()
	IL_0229:  call bool native int::op_Equality(native int, native int)
	IL_022e:  brfalse IL_0244

	IL_0233:  ldarg.0 
	IL_0234:  ldobj GLib.Value
	IL_0239:  call native int valuetype GLib.Value::op_Explicit(valuetype GLib.Value)
	IL_023e:  box native int
	IL_0243:  ret 
	IL_0244:  ldarg.0 
	IL_0245:  ldfld native int GLib.Value::'type'
	IL_024a:  ldsfld valuetype GLib.GType GLib.GType::Param
	IL_024f:  stloc.s 13
	IL_0251:  ldloca.s 13
	IL_0253:  call instance native int valuetype GLib.GType::get_Val()
	IL_0258:  call bool native int::op_Equality(native int, native int)
	IL_025d:  brfalse IL_026e

	IL_0262:  ldarg.0 
	IL_0263:  call native int valuetype GLib.Value::g_value_get_param([out] valuetype GLib.Value&)
	IL_0268:  box native int
	IL_026d:  ret 
	IL_026e:  ldarg.0 
	IL_026f:  ldfld native int GLib.Value::'type'
	IL_0274:  call valuetype GLib.GType class GLib.ManagedValue::get_GType()
	IL_0279:  stloc.s 14
	IL_027b:  ldloca.s 14
	IL_027d:  call instance native int valuetype GLib.GType::get_Val()
	IL_0282:  call bool native int::op_Equality(native int, native int)
	IL_0287:  brfalse IL_0298

	IL_028c:  ldarg.0 
	IL_028d:  call native int valuetype GLib.Value::g_value_get_boxed([out] valuetype GLib.Value&)
	IL_0292:  call object class GLib.ManagedValue::ObjectForWrapper(native int)
	IL_0297:  ret 
	IL_0298:  ldarg.0 
	IL_0299:  ldfld native int GLib.Value::'type'
	IL_029e:  ldsfld valuetype GLib.GType GLib.GType::Object
	IL_02a3:  stloc.s 15
	IL_02a5:  ldloca.s 15
	IL_02a7:  call instance native int valuetype GLib.GType::get_Val()
	IL_02ac:  call bool valuetype GLib.Value::g_type_is_a(native int, native int)
	IL_02b1:  brfalse IL_02c2

	IL_02b6:  ldarg.0 
	IL_02b7:  ldobj GLib.Value
	IL_02bc:  call class GLib.Object valuetype GLib.Value::op_Explicit(valuetype GLib.Value)
	IL_02c1:  ret 
	IL_02c2:  ldarg.0 
	IL_02c3:  ldfld native int GLib.Value::'type'
	IL_02c8:  ldsfld valuetype GLib.GType GLib.GType::Boxed
	IL_02cd:  stloc.s 16
	IL_02cf:  ldloca.s 16
	IL_02d1:  call instance native int valuetype GLib.GType::get_Val()
	IL_02d6:  call bool valuetype GLib.Value::g_type_is_a(native int, native int)
	IL_02db:  brfalse IL_02e7

	IL_02e0:  ldarg.0 
	IL_02e1:  call instance object valuetype GLib.Value::ToBoxed()
	IL_02e6:  ret 
	IL_02e7:  ldarg.0 
	IL_02e8:  ldfld native int GLib.Value::'type'
	IL_02ed:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_02f2:  call bool native int::op_Equality(native int, native int)
	IL_02f7:  brfalse IL_02fe

	IL_02fc:  ldnull 
	IL_02fd:  ret 
	IL_02fe:  ldstr "Unknown type "
	IL_0303:  ldloca.s 17
	IL_0305:  ldarg.0 
	IL_0306:  ldfld native int GLib.Value::'type'
	IL_030b:  call instance void valuetype GLib.GType::'.ctor'(native int)
	IL_0310:  ldloca.s 17
	IL_0312:  constrained. GLib.GType
	IL_0318:  callvirt instance string object::ToString()
	IL_031d:  call string string::Concat(string, string)
	IL_0322:  newobj instance void class [mscorlib]System.Exception::'.ctor'(string)
	IL_0327:  throw 
    } // end of method Value::get_Val

    // method line 843
    .method public final virtual hidebysig newslot 
           instance default void Dispose ()  cil managed 
    {
        // Method begins at RVA 0x9344
	// Code size 28 (0x1c)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldfld native int GLib.Value::'type'
	IL_0006:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_000b:  call bool native int::op_Inequality(native int, native int)
	IL_0010:  brfalse IL_001b

	IL_0015:  ldarg.0 
	IL_0016:  call void valuetype GLib.Value::g_value_unset([out] valuetype GLib.Value&)
	IL_001b:  ret 
    } // end of method Value::Dispose

    // method line 844
    .method assembly hidebysig specialname rtspecialname 
           instance default void '.ctor' (class GLib.Object obj, native int prop)  cil managed 
    {
        // Method begins at RVA 0x9364
	// Code size 43 (0x2b)
	.maxstack 4
	.locals init (
		int64	V_0)
	IL_0000:  ldarg.0 
	IL_0001:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_0006:  stfld native int GLib.Value::'type'
	IL_000b:  ldarg.0 
	IL_000c:  ldarg.0 
	IL_000d:  ldc.i4.0 
	IL_000e:  conv.i8 
	IL_000f:  dup 
	IL_0010:  stloc.0 
	IL_0011:  stfld int64 GLib.Value::pad_2
	IL_0016:  ldloc.0 
	IL_0017:  stfld int64 GLib.Value::pad_1
	IL_001c:  ldarg.0 
	IL_001d:  ldarg.1 
	IL_001e:  callvirt instance native int class GLib.Object::get_Handle()
	IL_0023:  ldarg.2 
	IL_0024:  call native int valuetype GLib.Value::gtksharp_value_create_from_property([out] valuetype GLib.Value&, native int, native int)
	IL_0029:  pop 
	IL_002a:  ret 
    } // end of method Value::.ctor

    // method line 845
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (class GLib.Object val)  cil managed 
    {
        // Method begins at RVA 0x939c
	// Code size 57 (0x39)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldarg.1 
	IL_0002:  brtrue IL_0011

	IL_0007:  ldsfld valuetype GLib.GType GLib.GType::Object
	IL_000c:  br IL_0017

	IL_0011:  ldarg.1 
	IL_0012:  callvirt instance valuetype GLib.GType class GLib.Object::get_NativeType()
	IL_0017:  call instance void valuetype GLib.Value::'.ctor'(valuetype GLib.GType)
	IL_001c:  ldarg.0 
	IL_001d:  ldarg.1 
	IL_001e:  brtrue IL_002d

	IL_0023:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_0028:  br IL_0033

	IL_002d:  ldarg.1 
	IL_002e:  callvirt instance native int class GLib.Object::get_Handle()
	IL_0033:  call void valuetype GLib.Value::g_value_set_object([out] valuetype GLib.Value&, native int)
	IL_0038:  ret 
    } // end of method Value::.ctor

    // method line 846
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (object obj)  cil managed 
    {
        // Method begins at RVA 0x93d8
	// Code size 61 (0x3d)
	.maxstack 4
	.locals init (
		int64	V_0,
		valuetype GLib.GType	V_1)
	IL_0000:  ldarg.0 
	IL_0001:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_0006:  stfld native int GLib.Value::'type'
	IL_000b:  ldarg.0 
	IL_000c:  ldarg.0 
	IL_000d:  ldc.i4.0 
	IL_000e:  conv.i8 
	IL_000f:  dup 
	IL_0010:  stloc.0 
	IL_0011:  stfld int64 GLib.Value::pad_2
	IL_0016:  ldloc.0 
	IL_0017:  stfld int64 GLib.Value::pad_1
	IL_001c:  ldarg.1 
	IL_001d:  callvirt instance class [mscorlib]System.Type object::GetType()
	IL_0022:  call valuetype GLib.GType valuetype GLib.GType::op_Explicit(class [mscorlib]System.Type)
	IL_0027:  stloc.1 
	IL_0028:  ldarg.0 
	IL_0029:  ldloca.s 1
	IL_002b:  call instance native int valuetype GLib.GType::get_Val()
	IL_0030:  call void valuetype GLib.Value::g_value_init([out] valuetype GLib.Value&, native int)
	IL_0035:  ldarg.0 
	IL_0036:  ldarg.1 
	IL_0037:  call instance void valuetype GLib.Value::set_Val(object)
	IL_003c:  ret 
    } // end of method Value::.ctor

    // method line 847
    .method assembly hidebysig 
           instance default void Update (object val)  cil managed 
    {
        // Method begins at RVA 0x9424
	// Code size 54 (0x36)
	.maxstack 3
	.locals init (
		valuetype GLib.GType	V_0)
	IL_0000:  ldarg.0 
	IL_0001:  ldfld native int GLib.Value::'type'
	IL_0006:  ldsfld valuetype GLib.GType GLib.GType::Boxed
	IL_000b:  stloc.0 
	IL_000c:  ldloca.s 0
	IL_000e:  call instance native int valuetype GLib.GType::get_Val()
	IL_0013:  call bool valuetype GLib.Value::g_type_is_a(native int, native int)
	IL_0018:  brfalse IL_0035

	IL_001d:  ldarg.1 
	IL_001e:  isinst GLib.IWrapper
	IL_0023:  brtrue IL_0035

	IL_0028:  ldarg.1 
	IL_0029:  ldarg.0 
	IL_002a:  call native int valuetype GLib.Value::g_value_get_boxed([out] valuetype GLib.Value&)
	IL_002f:  ldc.i4.0 
	IL_0030:  call void class [mscorlib]System.Runtime.InteropServices.Marshal::StructureToPtr(object, native int, bool)
	IL_0035:  ret 
    } // end of method Value::Update

    // method line 848
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (valuetype GLib.GType gtype)  cil managed 
    {
        // Method begins at RVA 0x9468
	// Code size 42 (0x2a)
	.maxstack 4
	.locals init (
		int64	V_0)
	IL_0000:  ldarg.0 
	IL_0001:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_0006:  stfld native int GLib.Value::'type'
	IL_000b:  ldarg.0 
	IL_000c:  ldarg.0 
	IL_000d:  ldc.i4.0 
	IL_000e:  conv.i8 
	IL_000f:  dup 
	IL_0010:  stloc.0 
	IL_0011:  stfld int64 GLib.Value::pad_2
	IL_0016:  ldloc.0 
	IL_0017:  stfld int64 GLib.Value::pad_1
	IL_001c:  ldarg.0 
	IL_001d:  ldarga.s 1
	IL_001f:  call instance native int valuetype GLib.GType::get_Val()
	IL_0024:  call void valuetype GLib.Value::g_value_init([out] valuetype GLib.Value&, native int)
	IL_0029:  ret 
    } // end of method Value::.ctor

    // method line 849
    .method private static hidebysig pinvokeimpl ("libgobject-2.0-0.dll" as "g_value_init" cdecl )
           default void g_value_init (valuetype GLib.Value& val, native int gtype)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method Value::g_value_init

    // method line 850
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (bool val)  cil managed 
    {
        // Method begins at RVA 0x94a0
	// Code size 19 (0x13)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldsfld valuetype GLib.GType GLib.GType::Boolean
	IL_0006:  call instance void valuetype GLib.Value::'.ctor'(valuetype GLib.GType)
	IL_000b:  ldarg.0 
	IL_000c:  ldarg.1 
	IL_000d:  call void valuetype GLib.Value::g_value_set_boolean([out] valuetype GLib.Value&, bool)
	IL_0012:  ret 
    } // end of method Value::.ctor

    // method line 851
    .method private static hidebysig pinvokeimpl ("libgobject-2.0-0.dll" as "g_value_set_boolean" cdecl )
           default void g_value_set_boolean (valuetype GLib.Value& val, bool data)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method Value::g_value_set_boolean

    // method line 852
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (unsigned int8 val)  cil managed 
    {
        // Method begins at RVA 0x94b4
	// Code size 19 (0x13)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldsfld valuetype GLib.GType GLib.GType::UChar
	IL_0006:  call instance void valuetype GLib.Value::'.ctor'(valuetype GLib.GType)
	IL_000b:  ldarg.0 
	IL_000c:  ldarg.1 
	IL_000d:  call void valuetype GLib.Value::g_value_set_uchar([out] valuetype GLib.Value&, unsigned int8)
	IL_0012:  ret 
    } // end of method Value::.ctor

    // method line 853
    .method private static hidebysig pinvokeimpl ("libgobject-2.0-0.dll" as "g_value_set_uchar" cdecl )
           default void g_value_set_uchar (valuetype GLib.Value& val, unsigned int8 data)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method Value::g_value_set_uchar

    // method line 854
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (int8 val)  cil managed 
    {
        // Method begins at RVA 0x94c8
	// Code size 19 (0x13)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldsfld valuetype GLib.GType GLib.GType::Char
	IL_0006:  call instance void valuetype GLib.Value::'.ctor'(valuetype GLib.GType)
	IL_000b:  ldarg.0 
	IL_000c:  ldarg.1 
	IL_000d:  call void valuetype GLib.Value::g_value_set_char([out] valuetype GLib.Value&, int8)
	IL_0012:  ret 
    } // end of method Value::.ctor

    // method line 855
    .method private static hidebysig pinvokeimpl ("libgobject-2.0-0.dll" as "g_value_set_char" cdecl )
           default void g_value_set_char (valuetype GLib.Value& val, int8 data)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method Value::g_value_set_char

    // method line 856
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (int32 val)  cil managed 
    {
        // Method begins at RVA 0x94dc
	// Code size 19 (0x13)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldsfld valuetype GLib.GType GLib.GType::Int
	IL_0006:  call instance void valuetype GLib.Value::'.ctor'(valuetype GLib.GType)
	IL_000b:  ldarg.0 
	IL_000c:  ldarg.1 
	IL_000d:  call void valuetype GLib.Value::g_value_set_int([out] valuetype GLib.Value&, int32)
	IL_0012:  ret 
    } // end of method Value::.ctor

    // method line 857
    .method private static hidebysig pinvokeimpl ("libgobject-2.0-0.dll" as "g_value_set_int" cdecl )
           default void g_value_set_int (valuetype GLib.Value& val, int32 data)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method Value::g_value_set_int

    // method line 858
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (unsigned int32 val)  cil managed 
    {
        // Method begins at RVA 0x94f0
	// Code size 19 (0x13)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldsfld valuetype GLib.GType GLib.GType::UInt
	IL_0006:  call instance void valuetype GLib.Value::'.ctor'(valuetype GLib.GType)
	IL_000b:  ldarg.0 
	IL_000c:  ldarg.1 
	IL_000d:  call void valuetype GLib.Value::g_value_set_uint([out] valuetype GLib.Value&, unsigned int32)
	IL_0012:  ret 
    } // end of method Value::.ctor

    // method line 859
    .method private static hidebysig pinvokeimpl ("libgobject-2.0-0.dll" as "g_value_set_uint" cdecl )
           default void g_value_set_uint (valuetype GLib.Value& val, unsigned int32 data)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method Value::g_value_set_uint

    // method line 860
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (unsigned int16 val)  cil managed 
    {
        // Method begins at RVA 0x9504
	// Code size 19 (0x13)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldsfld valuetype GLib.GType GLib.GType::UInt
	IL_0006:  call instance void valuetype GLib.Value::'.ctor'(valuetype GLib.GType)
	IL_000b:  ldarg.0 
	IL_000c:  ldarg.1 
	IL_000d:  call void valuetype GLib.Value::g_value_set_uint([out] valuetype GLib.Value&, unsigned int32)
	IL_0012:  ret 
    } // end of method Value::.ctor

    // method line 861
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (int64 val)  cil managed 
    {
        // Method begins at RVA 0x9518
	// Code size 19 (0x13)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldsfld valuetype GLib.GType GLib.GType::Int64
	IL_0006:  call instance void valuetype GLib.Value::'.ctor'(valuetype GLib.GType)
	IL_000b:  ldarg.0 
	IL_000c:  ldarg.1 
	IL_000d:  call void valuetype GLib.Value::g_value_set_int64([out] valuetype GLib.Value&, int64)
	IL_0012:  ret 
    } // end of method Value::.ctor

    // method line 862
    .method private static hidebysig pinvokeimpl ("libgobject-2.0-0.dll" as "g_value_set_int64" cdecl )
           default void g_value_set_int64 (valuetype GLib.Value& val, int64 data)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method Value::g_value_set_int64

    // method line 863
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (unsigned int64 val)  cil managed 
    {
        // Method begins at RVA 0x952c
	// Code size 19 (0x13)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldsfld valuetype GLib.GType GLib.GType::UInt64
	IL_0006:  call instance void valuetype GLib.Value::'.ctor'(valuetype GLib.GType)
	IL_000b:  ldarg.0 
	IL_000c:  ldarg.1 
	IL_000d:  call void valuetype GLib.Value::g_value_set_uint64([out] valuetype GLib.Value&, unsigned int64)
	IL_0012:  ret 
    } // end of method Value::.ctor

    // method line 864
    .method private static hidebysig pinvokeimpl ("libgobject-2.0-0.dll" as "g_value_set_uint64" cdecl )
           default void g_value_set_uint64 (valuetype GLib.Value& val, unsigned int64 data)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method Value::g_value_set_uint64

    // method line 865
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (class GLib.EnumWrapper wrap, string type_name)  cil managed 
    {
        .custom instance void class [mscorlib]System.ObsoleteAttribute::'.ctor'(string) =  (
		01 00 25 52 65 70 6C 61 63 65 64 20 62 79 20 56   // ..%Replaced by V
		61 6C 75 65 28 6F 62 6A 65 63 74 29 20 63 6F 6E   // alue(object) con
		73 74 72 75 63 74 6F 72 00 00                   ) // structor..

        // Method begins at RVA 0x9540
	// Code size 90 (0x5a)
	.maxstack 4
	.locals init (
		int64	V_0,
		native int	V_1)
	IL_0000:  ldarg.0 
	IL_0001:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_0006:  stfld native int GLib.Value::'type'
	IL_000b:  ldarg.0 
	IL_000c:  ldarg.0 
	IL_000d:  ldc.i4.0 
	IL_000e:  conv.i8 
	IL_000f:  dup 
	IL_0010:  stloc.0 
	IL_0011:  stfld int64 GLib.Value::pad_2
	IL_0016:  ldloc.0 
	IL_0017:  stfld int64 GLib.Value::pad_1
	IL_001c:  ldarg.2 
	IL_001d:  call native int class GLib.Marshaller::StringToPtrGStrdup(string)
	IL_0022:  stloc.1 
	IL_0023:  ldarg.0 
	IL_0024:  ldloc.1 
	IL_0025:  call native int valuetype GLib.Value::gtksharp_value_create_from_type_name([out] valuetype GLib.Value&, native int)
	IL_002a:  pop 
	IL_002b:  ldloc.1 
	IL_002c:  call void class GLib.Marshaller::Free(native int)
	IL_0031:  ldarg.1 
	IL_0032:  ldfld bool GLib.EnumWrapper::'flags'
	IL_0037:  brfalse IL_004d

	IL_003c:  ldarg.0 
	IL_003d:  ldarg.1 
	IL_003e:  call int32 class GLib.EnumWrapper::op_Explicit(class GLib.EnumWrapper)
	IL_0043:  call void valuetype GLib.Value::g_value_set_flags([out] valuetype GLib.Value&, unsigned int32)
	IL_0048:  br IL_0059

	IL_004d:  ldarg.0 
	IL_004e:  ldarg.1 
	IL_004f:  call int32 class GLib.EnumWrapper::op_Explicit(class GLib.EnumWrapper)
	IL_0054:  call void valuetype GLib.Value::g_value_set_enum([out] valuetype GLib.Value&, int32)
	IL_0059:  ret 
    } // end of method Value::.ctor

    // method line 866
    .method private static hidebysig pinvokeimpl ("glibsharpglue-2" as "gtksharp_value_create_from_type_name" cdecl )
           default native int gtksharp_value_create_from_type_name (valuetype GLib.Value& val, native int type_name)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method Value::gtksharp_value_create_from_type_name

    // method line 867
    .method private static hidebysig pinvokeimpl ("libgobject-2.0-0.dll" as "g_value_set_flags" cdecl )
           default void g_value_set_flags (valuetype GLib.Value& val, unsigned int32 data)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method Value::g_value_set_flags

    // method line 868
    .method private static hidebysig pinvokeimpl ("libgobject-2.0-0.dll" as "g_value_set_enum" cdecl )
           default void g_value_set_enum (valuetype GLib.Value& val, int32 data)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method Value::g_value_set_enum

    // method line 869
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (float32 val)  cil managed 
    {
        // Method begins at RVA 0x95a8
	// Code size 19 (0x13)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldsfld valuetype GLib.GType GLib.GType::Float
	IL_0006:  call instance void valuetype GLib.Value::'.ctor'(valuetype GLib.GType)
	IL_000b:  ldarg.0 
	IL_000c:  ldarg.1 
	IL_000d:  call void valuetype GLib.Value::g_value_set_float([out] valuetype GLib.Value&, float32)
	IL_0012:  ret 
    } // end of method Value::.ctor

    // method line 870
    .method private static hidebysig pinvokeimpl ("libgobject-2.0-0.dll" as "g_value_set_float" cdecl )
           default void g_value_set_float (valuetype GLib.Value& val, float32 data)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method Value::g_value_set_float

    // method line 871
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (float64 val)  cil managed 
    {
        // Method begins at RVA 0x95bc
	// Code size 19 (0x13)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldsfld valuetype GLib.GType GLib.GType::Double
	IL_0006:  call instance void valuetype GLib.Value::'.ctor'(valuetype GLib.GType)
	IL_000b:  ldarg.0 
	IL_000c:  ldarg.1 
	IL_000d:  call void valuetype GLib.Value::g_value_set_double([out] valuetype GLib.Value&, float64)
	IL_0012:  ret 
    } // end of method Value::.ctor

    // method line 872
    .method private static hidebysig pinvokeimpl ("libgobject-2.0-0.dll" as "g_value_set_double" cdecl )
           default void g_value_set_double (valuetype GLib.Value& val, float64 data)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method Value::g_value_set_double

    // method line 873
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (string val)  cil managed 
    {
        // Method begins at RVA 0x95d0
	// Code size 32 (0x20)
	.maxstack 2
	.locals init (
		native int	V_0)
	IL_0000:  ldarg.0 
	IL_0001:  ldsfld valuetype GLib.GType GLib.GType::String
	IL_0006:  call instance void valuetype GLib.Value::'.ctor'(valuetype GLib.GType)
	IL_000b:  ldarg.1 
	IL_000c:  call native int class GLib.Marshaller::StringToPtrGStrdup(string)
	IL_0011:  stloc.0 
	IL_0012:  ldarg.0 
	IL_0013:  ldloc.0 
	IL_0014:  call void valuetype GLib.Value::g_value_set_string([out] valuetype GLib.Value&, native int)
	IL_0019:  ldloc.0 
	IL_001a:  call void class GLib.Marshaller::Free(native int)
	IL_001f:  ret 
    } // end of method Value::.ctor

    // method line 874
    .method private static hidebysig pinvokeimpl ("libgobject-2.0-0.dll" as "g_value_set_string" cdecl )
           default void g_value_set_string (valuetype GLib.Value& val, native int data)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method Value::g_value_set_string

    // method line 875
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (native int val)  cil managed 
    {
        // Method begins at RVA 0x95fc
	// Code size 19 (0x13)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldsfld valuetype GLib.GType GLib.GType::Pointer
	IL_0006:  call instance void valuetype GLib.Value::'.ctor'(valuetype GLib.GType)
	IL_000b:  ldarg.0 
	IL_000c:  ldarg.1 
	IL_000d:  call void valuetype GLib.Value::g_value_set_pointer([out] valuetype GLib.Value&, native int)
	IL_0012:  ret 
    } // end of method Value::.ctor

    // method line 876
    .method private static hidebysig pinvokeimpl ("libgobject-2.0-0.dll" as "g_value_set_pointer" cdecl )
           default void g_value_set_pointer (valuetype GLib.Value& val, native int data)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method Value::g_value_set_pointer

    // method line 877
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (class GLib.Opaque val, string type_name)  cil managed 
    {
        // Method begins at RVA 0x9610
	// Code size 62 (0x3e)
	.maxstack 4
	.locals init (
		int64	V_0,
		native int	V_1)
	IL_0000:  ldarg.0 
	IL_0001:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_0006:  stfld native int GLib.Value::'type'
	IL_000b:  ldarg.0 
	IL_000c:  ldarg.0 
	IL_000d:  ldc.i4.0 
	IL_000e:  conv.i8 
	IL_000f:  dup 
	IL_0010:  stloc.0 
	IL_0011:  stfld int64 GLib.Value::pad_2
	IL_0016:  ldloc.0 
	IL_0017:  stfld int64 GLib.Value::pad_1
	IL_001c:  ldarg.2 
	IL_001d:  call native int class GLib.Marshaller::StringToPtrGStrdup(string)
	IL_0022:  stloc.1 
	IL_0023:  ldarg.0 
	IL_0024:  ldloc.1 
	IL_0025:  call native int valuetype GLib.Value::gtksharp_value_create_from_type_name([out] valuetype GLib.Value&, native int)
	IL_002a:  pop 
	IL_002b:  ldloc.1 
	IL_002c:  call void class GLib.Marshaller::Free(native int)
	IL_0031:  ldarg.0 
	IL_0032:  ldarg.1 
	IL_0033:  callvirt instance native int class GLib.Opaque::get_Handle()
	IL_0038:  call void valuetype GLib.Value::g_value_set_boxed([out] valuetype GLib.Value&, native int)
	IL_003d:  ret 
    } // end of method Value::.ctor

    // method line 878
    .method private static hidebysig pinvokeimpl ("libgobject-2.0-0.dll" as "g_value_set_boxed" cdecl )
           default void g_value_set_boxed (valuetype GLib.Value& val, native int data)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method Value::g_value_set_boxed

    // method line 879
    .method private static hidebysig pinvokeimpl ("libgobject-2.0-0.dll" as "g_value_set_object" cdecl )
           default void g_value_set_object (valuetype GLib.Value& val, native int data)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method Value::g_value_set_object

    // method line 880
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (class GLib.GInterfaceAdapter val)  cil managed 
    {
        // Method begins at RVA 0x965c
	// Code size 57 (0x39)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldarg.1 
	IL_0002:  brtrue IL_0011

	IL_0007:  ldsfld valuetype GLib.GType GLib.GType::Object
	IL_000c:  br IL_0017

	IL_0011:  ldarg.1 
	IL_0012:  callvirt instance valuetype GLib.GType class GLib.GInterfaceAdapter::get_GType()
	IL_0017:  call instance void valuetype GLib.Value::'.ctor'(valuetype GLib.GType)
	IL_001c:  ldarg.0 
	IL_001d:  ldarg.1 
	IL_001e:  brtrue IL_002d

	IL_0023:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_0028:  br IL_0033

	IL_002d:  ldarg.1 
	IL_002e:  callvirt instance native int class GLib.GInterfaceAdapter::get_Handle()
	IL_0033:  call void valuetype GLib.Value::g_value_set_object([out] valuetype GLib.Value&, native int)
	IL_0038:  ret 
    } // end of method Value::.ctor

    // method line 881
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (class GLib.Object obj, string prop_name)  cil managed 
    {
        // Method begins at RVA 0x9698
	// Code size 56 (0x38)
	.maxstack 4
	.locals init (
		int64	V_0,
		native int	V_1)
	IL_0000:  ldarg.0 
	IL_0001:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_0006:  stfld native int GLib.Value::'type'
	IL_000b:  ldarg.0 
	IL_000c:  ldarg.0 
	IL_000d:  ldc.i4.0 
	IL_000e:  conv.i8 
	IL_000f:  dup 
	IL_0010:  stloc.0 
	IL_0011:  stfld int64 GLib.Value::pad_2
	IL_0016:  ldloc.0 
	IL_0017:  stfld int64 GLib.Value::pad_1
	IL_001c:  ldarg.2 
	IL_001d:  call native int class GLib.Marshaller::StringToPtrGStrdup(string)
	IL_0022:  stloc.1 
	IL_0023:  ldarg.0 
	IL_0024:  ldarg.1 
	IL_0025:  callvirt instance native int class GLib.Object::get_Handle()
	IL_002a:  ldloc.1 
	IL_002b:  call native int valuetype GLib.Value::gtksharp_value_create_from_property([out] valuetype GLib.Value&, native int, native int)
	IL_0030:  pop 
	IL_0031:  ldloc.1 
	IL_0032:  call void class GLib.Marshaller::Free(native int)
	IL_0037:  ret 
    } // end of method Value::.ctor

    // method line 882
    .method private static hidebysig pinvokeimpl ("glibsharpglue-2" as "gtksharp_value_create_from_property" cdecl )
           default native int gtksharp_value_create_from_property (valuetype GLib.Value& val, native int obj, native int name)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method Value::gtksharp_value_create_from_property

    // method line 883
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (class GLib.Object obj, string prop_name, class GLib.EnumWrapper wrap)  cil managed 
    {
        .custom instance void class [mscorlib]System.ObsoleteAttribute::'.ctor'() =  (01 00 00 00 ) // ....

        // Method begins at RVA 0x96dc
	// Code size 104 (0x68)
	.maxstack 4
	.locals init (
		int64	V_0,
		native int	V_1,
		valuetype GLib.GType	V_2)
	IL_0000:  ldarg.0 
	IL_0001:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_0006:  stfld native int GLib.Value::'type'
	IL_000b:  ldarg.0 
	IL_000c:  ldarg.0 
	IL_000d:  ldc.i4.0 
	IL_000e:  conv.i8 
	IL_000f:  dup 
	IL_0010:  stloc.0 
	IL_0011:  stfld int64 GLib.Value::pad_2
	IL_0016:  ldloc.0 
	IL_0017:  stfld int64 GLib.Value::pad_1
	IL_001c:  ldarg.2 
	IL_001d:  call native int class GLib.Marshaller::StringToPtrGStrdup(string)
	IL_0022:  stloc.1 
	IL_0023:  ldarg.0 
	IL_0024:  ldarg.1 
	IL_0025:  callvirt instance valuetype GLib.GType class GLib.Object::get_NativeType()
	IL_002a:  stloc.2 
	IL_002b:  ldloca.s 2
	IL_002d:  call instance native int valuetype GLib.GType::get_Val()
	IL_0032:  ldloc.1 
	IL_0033:  call native int valuetype GLib.Value::gtksharp_value_create_from_type_and_property([out] valuetype GLib.Value&, native int, native int)
	IL_0038:  pop 
	IL_0039:  ldloc.1 
	IL_003a:  call void class GLib.Marshaller::Free(native int)
	IL_003f:  ldarg.3 
	IL_0040:  ldfld bool GLib.EnumWrapper::'flags'
	IL_0045:  brfalse IL_005b

	IL_004a:  ldarg.0 
	IL_004b:  ldarg.3 
	IL_004c:  call int32 class GLib.EnumWrapper::op_Explicit(class GLib.EnumWrapper)
	IL_0051:  call void valuetype GLib.Value::g_value_set_flags([out] valuetype GLib.Value&, unsigned int32)
	IL_0056:  br IL_0067

	IL_005b:  ldarg.0 
	IL_005c:  ldarg.3 
	IL_005d:  call int32 class GLib.EnumWrapper::op_Explicit(class GLib.EnumWrapper)
	IL_0062:  call void valuetype GLib.Value::g_value_set_enum([out] valuetype GLib.Value&, int32)
	IL_0067:  ret 
    } // end of method Value::.ctor

    // method line 884
    .method private static hidebysig pinvokeimpl ("glibsharpglue-2" as "gtksharp_value_create_from_type_and_property" cdecl )
           default native int gtksharp_value_create_from_type_and_property (valuetype GLib.Value& val, native int gtype, native int name)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method Value::gtksharp_value_create_from_type_and_property

    // method line 885
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (native int obj, string prop_name, class GLib.Opaque val)  cil managed 
    {
        .custom instance void class [mscorlib]System.ObsoleteAttribute::'.ctor'() =  (01 00 00 00 ) // ....

        // Method begins at RVA 0x9750
	// Code size 63 (0x3f)
	.maxstack 4
	.locals init (
		int64	V_0,
		native int	V_1)
	IL_0000:  ldarg.0 
	IL_0001:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_0006:  stfld native int GLib.Value::'type'
	IL_000b:  ldarg.0 
	IL_000c:  ldarg.0 
	IL_000d:  ldc.i4.0 
	IL_000e:  conv.i8 
	IL_000f:  dup 
	IL_0010:  stloc.0 
	IL_0011:  stfld int64 GLib.Value::pad_2
	IL_0016:  ldloc.0 
	IL_0017:  stfld int64 GLib.Value::pad_1
	IL_001c:  ldarg.2 
	IL_001d:  call native int class GLib.Marshaller::StringToPtrGStrdup(string)
	IL_0022:  stloc.1 
	IL_0023:  ldarg.0 
	IL_0024:  ldarg.1 
	IL_0025:  ldloc.1 
	IL_0026:  call native int valuetype GLib.Value::gtksharp_value_create_from_property([out] valuetype GLib.Value&, native int, native int)
	IL_002b:  pop 
	IL_002c:  ldloc.1 
	IL_002d:  call void class GLib.Marshaller::Free(native int)
	IL_0032:  ldarg.0 
	IL_0033:  ldarg.3 
	IL_0034:  callvirt instance native int class GLib.Opaque::get_Handle()
	IL_0039:  call void valuetype GLib.Value::g_value_set_boxed([out] valuetype GLib.Value&, native int)
	IL_003e:  ret 
    } // end of method Value::.ctor

    // method line 886
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (string[] val)  cil managed 
    {
        // Method begins at RVA 0x979c
	// Code size 164 (0xa4)
	.maxstack 4
	.locals init (
		native int	V_0,
		int32	V_1,
		int32	V_2)
	IL_0000:  ldarg.0 
	IL_0001:  call native int valuetype GLib.Value::g_strv_get_type()
	IL_0006:  newobj instance void valuetype GLib.GType::'.ctor'(native int)
	IL_000b:  call instance void valuetype GLib.Value::'.ctor'(valuetype GLib.GType)
	IL_0010:  ldarg.1 
	IL_0011:  brtrue IL_0022

	IL_0016:  ldarg.0 
	IL_0017:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_001c:  call void valuetype GLib.Value::g_value_set_boxed([out] valuetype GLib.Value&, native int)
	IL_0021:  ret 
	IL_0022:  ldarg.1 
	IL_0023:  ldlen 
	IL_0024:  conv.i4 
	IL_0025:  ldc.i4.1 
	IL_0026:  add 
	IL_0027:  call int32 native int::get_Size()
	IL_002c:  mul 
	IL_002d:  call native int class [mscorlib]System.Runtime.InteropServices.Marshal::AllocHGlobal(int32)
	IL_0032:  stloc.0 
	IL_0033:  ldc.i4.0 
	IL_0034:  stloc.1 
	IL_0035:  br IL_0053

	IL_003a:  ldloc.0 
	IL_003b:  ldloc.1 
	IL_003c:  call int32 native int::get_Size()
	IL_0041:  mul 
	IL_0042:  ldarg.1 
	IL_0043:  ldloc.1 
	IL_0044:  ldelem.ref 
	IL_0045:  call native int class GLib.Marshaller::StringToPtrGStrdup(string)
	IL_004a:  call void class [mscorlib]System.Runtime.InteropServices.Marshal::WriteIntPtr(native int, int32, native int)
	IL_004f:  ldloc.1 
	IL_0050:  ldc.i4.1 
	IL_0051:  add 
	IL_0052:  stloc.1 
	IL_0053:  ldloc.1 
	IL_0054:  ldarg.1 
	IL_0055:  ldlen 
	IL_0056:  conv.i4 
	IL_0057:  blt IL_003a

	IL_005c:  ldloc.0 
	IL_005d:  ldarg.1 
	IL_005e:  ldlen 
	IL_005f:  conv.i4 
	IL_0060:  call int32 native int::get_Size()
	IL_0065:  mul 
	IL_0066:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_006b:  call void class [mscorlib]System.Runtime.InteropServices.Marshal::WriteIntPtr(native int, int32, native int)
	IL_0070:  ldarg.0 
	IL_0071:  ldloc.0 
	IL_0072:  call void valuetype GLib.Value::g_value_set_boxed([out] valuetype GLib.Value&, native int)
	IL_0077:  ldc.i4.0 
	IL_0078:  stloc.2 
	IL_0079:  br IL_0094

	IL_007e:  ldloc.0 
	IL_007f:  ldloc.2 
	IL_0080:  call int32 native int::get_Size()
	IL_0085:  mul 
	IL_0086:  call native int class [mscorlib]System.Runtime.InteropServices.Marshal::ReadIntPtr(native int, int32)
	IL_008b:  call void class GLib.Marshaller::Free(native int)
	IL_0090:  ldloc.2 
	IL_0091:  ldc.i4.1 
	IL_0092:  add 
	IL_0093:  stloc.2 
	IL_0094:  ldloc.2 
	IL_0095:  ldarg.1 
	IL_0096:  ldlen 
	IL_0097:  conv.i4 
	IL_0098:  blt IL_007e

	IL_009d:  ldloc.0 
	IL_009e:  call void class [mscorlib]System.Runtime.InteropServices.Marshal::FreeHGlobal(native int)
	IL_00a3:  ret 
    } // end of method Value::.ctor

    // method line 887
    .method private static hidebysig pinvokeimpl ("libgobject-2.0-0.dll" as "g_strv_get_type" cdecl )
           default native int g_strv_get_type ()  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method Value::g_strv_get_type

    // method line 888
    .method private static hidebysig pinvokeimpl ("libgobject-2.0-0.dll" as "g_value_unset" cdecl )
           default void g_value_unset (valuetype GLib.Value& val)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method Value::g_value_unset

    // method line 889
    .method public hidebysig 
           instance default void Init (valuetype GLib.GType gtype)  cil managed 
    {
        // Method begins at RVA 0x984c
	// Code size 14 (0xe)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldarga.s 1
	IL_0003:  call instance native int valuetype GLib.GType::get_Val()
	IL_0008:  call void valuetype GLib.Value::g_value_init([out] valuetype GLib.Value&, native int)
	IL_000d:  ret 
    } // end of method Value::Init

    // method line 890
    .method public static hidebysig specialname 
           default bool op_Explicit (valuetype GLib.Value val)  cil managed 
    {
        // Method begins at RVA 0x985c
	// Code size 8 (0x8)
	.maxstack 8
	IL_0000:  ldarga.s 0
	IL_0002:  call bool valuetype GLib.Value::g_value_get_boolean([out] valuetype GLib.Value&)
	IL_0007:  ret 
    } // end of method Value::op_Explicit

    // method line 891
    .method private static hidebysig pinvokeimpl ("libgobject-2.0-0.dll" as "g_value_get_boolean" cdecl )
           default bool g_value_get_boolean (valuetype GLib.Value& val)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method Value::g_value_get_boolean

    // method line 892
    .method public static hidebysig specialname 
           default unsigned int8 op_Explicit (valuetype GLib.Value val)  cil managed 
    {
        // Method begins at RVA 0x9868
	// Code size 8 (0x8)
	.maxstack 8
	IL_0000:  ldarga.s 0
	IL_0002:  call unsigned int8 valuetype GLib.Value::g_value_get_uchar([out] valuetype GLib.Value&)
	IL_0007:  ret 
    } // end of method Value::op_Explicit

    // method line 893
    .method private static hidebysig pinvokeimpl ("libgobject-2.0-0.dll" as "g_value_get_uchar" cdecl )
           default unsigned int8 g_value_get_uchar (valuetype GLib.Value& val)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method Value::g_value_get_uchar

    // method line 894
    .method public static hidebysig specialname 
           default int8 op_Explicit (valuetype GLib.Value val)  cil managed 
    {
        // Method begins at RVA 0x9874
	// Code size 8 (0x8)
	.maxstack 8
	IL_0000:  ldarga.s 0
	IL_0002:  call int8 valuetype GLib.Value::g_value_get_char([out] valuetype GLib.Value&)
	IL_0007:  ret 
    } // end of method Value::op_Explicit

    // method line 895
    .method private static hidebysig pinvokeimpl ("libgobject-2.0-0.dll" as "g_value_get_char" cdecl )
           default int8 g_value_get_char (valuetype GLib.Value& val)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method Value::g_value_get_char

    // method line 896
    .method public static hidebysig specialname 
           default int32 op_Explicit (valuetype GLib.Value val)  cil managed 
    {
        // Method begins at RVA 0x9880
	// Code size 8 (0x8)
	.maxstack 8
	IL_0000:  ldarga.s 0
	IL_0002:  call int32 valuetype GLib.Value::g_value_get_int([out] valuetype GLib.Value&)
	IL_0007:  ret 
    } // end of method Value::op_Explicit

    // method line 897
    .method private static hidebysig pinvokeimpl ("libgobject-2.0-0.dll" as "g_value_get_int" cdecl )
           default int32 g_value_get_int (valuetype GLib.Value& val)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method Value::g_value_get_int

    // method line 898
    .method public static hidebysig specialname 
           default unsigned int32 op_Explicit (valuetype GLib.Value val)  cil managed 
    {
        // Method begins at RVA 0x988c
	// Code size 8 (0x8)
	.maxstack 8
	IL_0000:  ldarga.s 0
	IL_0002:  call unsigned int32 valuetype GLib.Value::g_value_get_uint([out] valuetype GLib.Value&)
	IL_0007:  ret 
    } // end of method Value::op_Explicit

    // method line 899
    .method private static hidebysig pinvokeimpl ("libgobject-2.0-0.dll" as "g_value_get_uint" cdecl )
           default unsigned int32 g_value_get_uint (valuetype GLib.Value& val)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method Value::g_value_get_uint

    // method line 900
    .method public static hidebysig specialname 
           default unsigned int16 op_Explicit (valuetype GLib.Value val)  cil managed 
    {
        // Method begins at RVA 0x9898
	// Code size 9 (0x9)
	.maxstack 8
	IL_0000:  ldarga.s 0
	IL_0002:  call unsigned int32 valuetype GLib.Value::g_value_get_uint([out] valuetype GLib.Value&)
	IL_0007:  conv.u2 
	IL_0008:  ret 
    } // end of method Value::op_Explicit

    // method line 901
    .method public static hidebysig specialname 
           default int64 op_Explicit (valuetype GLib.Value val)  cil managed 
    {
        // Method begins at RVA 0x98a4
	// Code size 8 (0x8)
	.maxstack 8
	IL_0000:  ldarga.s 0
	IL_0002:  call int64 valuetype GLib.Value::g_value_get_int64([out] valuetype GLib.Value&)
	IL_0007:  ret 
    } // end of method Value::op_Explicit

    // method line 902
    .method private static hidebysig pinvokeimpl ("libgobject-2.0-0.dll" as "g_value_get_int64" cdecl )
           default int64 g_value_get_int64 (valuetype GLib.Value& val)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method Value::g_value_get_int64

    // method line 903
    .method public static hidebysig specialname 
           default unsigned int64 op_Explicit (valuetype GLib.Value val)  cil managed 
    {
        // Method begins at RVA 0x98b0
	// Code size 8 (0x8)
	.maxstack 8
	IL_0000:  ldarga.s 0
	IL_0002:  call unsigned int64 valuetype GLib.Value::g_value_get_uint64([out] valuetype GLib.Value&)
	IL_0007:  ret 
    } // end of method Value::op_Explicit

    // method line 904
    .method private static hidebysig pinvokeimpl ("libgobject-2.0-0.dll" as "g_value_get_uint64" cdecl )
           default unsigned int64 g_value_get_uint64 (valuetype GLib.Value& val)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method Value::g_value_get_uint64

    // method line 905
    .method public static hidebysig specialname 
           default class GLib.EnumWrapper op_Explicit (valuetype GLib.Value val)  cil managed 
    {
        .custom instance void class [mscorlib]System.ObsoleteAttribute::'.ctor'(string) =  (
		01 00 15 52 65 70 6C 61 63 65 64 20 62 79 20 45   // ...Replaced by E
		6E 75 6D 20 63 61 73 74 00 00                   ) // num cast..

        // Method begins at RVA 0x98bc
	// Code size 40 (0x28)
	.maxstack 8
	IL_0000:  ldarga.s 0
	IL_0002:  call bool valuetype GLib.Value::glibsharp_value_holds_flags([out] valuetype GLib.Value&)
	IL_0007:  brfalse IL_001a

	IL_000c:  ldarga.s 0
	IL_000e:  call unsigned int32 valuetype GLib.Value::g_value_get_flags([out] valuetype GLib.Value&)
	IL_0013:  ldc.i4.1 
	IL_0014:  newobj instance void class GLib.EnumWrapper::'.ctor'(int32, bool)
	IL_0019:  ret 
	IL_001a:  ldarga.s 0
	IL_001c:  call int32 valuetype GLib.Value::g_value_get_enum([out] valuetype GLib.Value&)
	IL_0021:  ldc.i4.0 
	IL_0022:  newobj instance void class GLib.EnumWrapper::'.ctor'(int32, bool)
	IL_0027:  ret 
    } // end of method Value::op_Explicit

    // method line 906
    .method private static hidebysig pinvokeimpl ("glibsharpglue-2" as "glibsharp_value_holds_flags" cdecl )
           default bool glibsharp_value_holds_flags (valuetype GLib.Value& val)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method Value::glibsharp_value_holds_flags

    // method line 907
    .method private static hidebysig pinvokeimpl ("libgobject-2.0-0.dll" as "g_value_get_flags" cdecl )
           default unsigned int32 g_value_get_flags (valuetype GLib.Value& val)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method Value::g_value_get_flags

    // method line 908
    .method private static hidebysig pinvokeimpl ("libgobject-2.0-0.dll" as "g_value_get_enum" cdecl )
           default int32 g_value_get_enum (valuetype GLib.Value& val)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method Value::g_value_get_enum

    // method line 909
    .method public static hidebysig specialname 
           default class [mscorlib]System.Enum op_Explicit (valuetype GLib.Value val)  cil managed 
    {
        // Method begins at RVA 0x98e8
	// Code size 72 (0x48)
	.maxstack 2
	IL_0000:  ldarga.s 0
	IL_0002:  call bool valuetype GLib.Value::glibsharp_value_holds_flags([out] valuetype GLib.Value&)
	IL_0007:  brfalse IL_002a

	IL_000c:  ldarga.s 0
	IL_000e:  ldfld native int GLib.Value::'type'
	IL_0013:  call class [mscorlib]System.Type valuetype GLib.GType::LookupType(native int)
	IL_0018:  ldarga.s 0
	IL_001a:  call unsigned int32 valuetype GLib.Value::g_value_get_flags([out] valuetype GLib.Value&)
	IL_001f:  call object class [mscorlib]System.Enum::ToObject(class [mscorlib]System.Type, unsigned int32)
	IL_0024:  castclass [mscorlib]System.Enum
	IL_0029:  ret 
	IL_002a:  ldarga.s 0
	IL_002c:  ldfld native int GLib.Value::'type'
	IL_0031:  call class [mscorlib]System.Type valuetype GLib.GType::LookupType(native int)
	IL_0036:  ldarga.s 0
	IL_0038:  call int32 valuetype GLib.Value::g_value_get_enum([out] valuetype GLib.Value&)
	IL_003d:  call object class [mscorlib]System.Enum::ToObject(class [mscorlib]System.Type, int32)
	IL_0042:  castclass [mscorlib]System.Enum
	IL_0047:  ret 
    } // end of method Value::op_Explicit

    // method line 910
    .method public static hidebysig specialname 
           default float32 op_Explicit (valuetype GLib.Value val)  cil managed 
    {
        // Method begins at RVA 0x993c
	// Code size 8 (0x8)
	.maxstack 8
	IL_0000:  ldarga.s 0
	IL_0002:  call float32 valuetype GLib.Value::g_value_get_float([out] valuetype GLib.Value&)
	IL_0007:  ret 
    } // end of method Value::op_Explicit

    // method line 911
    .method private static hidebysig pinvokeimpl ("libgobject-2.0-0.dll" as "g_value_get_float" cdecl )
           default float32 g_value_get_float (valuetype GLib.Value& val)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method Value::g_value_get_float

    // method line 912
    .method public static hidebysig specialname 
           default float64 op_Explicit (valuetype GLib.Value val)  cil managed 
    {
        // Method begins at RVA 0x9948
	// Code size 8 (0x8)
	.maxstack 8
	IL_0000:  ldarga.s 0
	IL_0002:  call float64 valuetype GLib.Value::g_value_get_double([out] valuetype GLib.Value&)
	IL_0007:  ret 
    } // end of method Value::op_Explicit

    // method line 913
    .method private static hidebysig pinvokeimpl ("libgobject-2.0-0.dll" as "g_value_get_double" cdecl )
           default float64 g_value_get_double (valuetype GLib.Value& val)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method Value::g_value_get_double

    // method line 914
    .method public static hidebysig specialname 
           default string op_Explicit (valuetype GLib.Value val)  cil managed 
    {
        // Method begins at RVA 0x9954
	// Code size 37 (0x25)
	.maxstack 2
	.locals init (
		native int	V_0)
	IL_0000:  ldarga.s 0
	IL_0002:  call native int valuetype GLib.Value::g_value_get_string([out] valuetype GLib.Value&)
	IL_0007:  stloc.0 
	IL_0008:  ldloc.0 
	IL_0009:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_000e:  call bool native int::op_Equality(native int, native int)
	IL_0013:  brfalse IL_001e

	IL_0018:  ldnull 
	IL_0019:  br IL_0024

	IL_001e:  ldloc.0 
	IL_001f:  call string class GLib.Marshaller::Utf8PtrToString(native int)
	IL_0024:  ret 
    } // end of method Value::op_Explicit

    // method line 915
    .method private static hidebysig pinvokeimpl ("libgobject-2.0-0.dll" as "g_value_get_string" cdecl )
           default native int g_value_get_string (valuetype GLib.Value& val)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method Value::g_value_get_string

    // method line 916
    .method public static hidebysig specialname 
           default native int op_Explicit (valuetype GLib.Value val)  cil managed 
    {
        // Method begins at RVA 0x9988
	// Code size 8 (0x8)
	.maxstack 8
	IL_0000:  ldarga.s 0
	IL_0002:  call native int valuetype GLib.Value::g_value_get_pointer([out] valuetype GLib.Value&)
	IL_0007:  ret 
    } // end of method Value::op_Explicit

    // method line 917
    .method private static hidebysig pinvokeimpl ("libgobject-2.0-0.dll" as "g_value_get_pointer" cdecl )
           default native int g_value_get_pointer (valuetype GLib.Value& val)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method Value::g_value_get_pointer

    // method line 918
    .method public static hidebysig specialname 
           default class GLib.Opaque op_Explicit (valuetype GLib.Value val)  cil managed 
    {
        // Method begins at RVA 0x9994
	// Code size 31 (0x1f)
	.maxstack 8
	IL_0000:  ldarga.s 0
	IL_0002:  call native int valuetype GLib.Value::g_value_get_boxed([out] valuetype GLib.Value&)
	IL_0007:  ldarga.s 0
	IL_0009:  ldfld native int GLib.Value::'type'
	IL_000e:  newobj instance void valuetype GLib.GType::'.ctor'(native int)
	IL_0013:  call class [mscorlib]System.Type valuetype GLib.GType::op_Explicit(valuetype GLib.GType)
	IL_0018:  ldc.i4.0 
	IL_0019:  call class GLib.Opaque class GLib.Opaque::GetOpaque(native int, class [mscorlib]System.Type, bool)
	IL_001e:  ret 
    } // end of method Value::op_Explicit

    // method line 919
    .method private static hidebysig pinvokeimpl ("libgobject-2.0-0.dll" as "g_value_get_boxed" cdecl )
           default native int g_value_get_boxed (valuetype GLib.Value& val)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method Value::g_value_get_boxed

    // method line 920
    .method public static hidebysig specialname 
           default class GLib.Boxed op_Explicit (valuetype GLib.Value val)  cil managed 
    {
        // Method begins at RVA 0x99b4
	// Code size 13 (0xd)
	.maxstack 8
	IL_0000:  ldarga.s 0
	IL_0002:  call native int valuetype GLib.Value::g_value_get_boxed([out] valuetype GLib.Value&)
	IL_0007:  newobj instance void class GLib.Boxed::'.ctor'(native int)
	IL_000c:  ret 
    } // end of method Value::op_Explicit

    // method line 921
    .method public static hidebysig specialname 
           default class GLib.Object op_Explicit (valuetype GLib.Value val)  cil managed 
    {
        // Method begins at RVA 0x99c4
	// Code size 14 (0xe)
	.maxstack 8
	IL_0000:  ldarga.s 0
	IL_0002:  call native int valuetype GLib.Value::g_value_get_object([out] valuetype GLib.Value&)
	IL_0007:  ldc.i4.0 
	IL_0008:  call class GLib.Object class GLib.Object::GetObject(native int, bool)
	IL_000d:  ret 
    } // end of method Value::op_Explicit

    // method line 922
    .method private static hidebysig pinvokeimpl ("libgobject-2.0-0.dll" as "g_value_get_object" cdecl )
           default native int g_value_get_object (valuetype GLib.Value& val)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method Value::g_value_get_object

    // method line 923
    .method public static hidebysig specialname 
           default class GLib.UnwrappedObject op_Explicit (valuetype GLib.Value val)  cil managed 
    {
        .custom instance void class [mscorlib]System.ObsoleteAttribute::'.ctor'(string) =  (
		01 00 1C 52 65 70 6C 61 63 65 64 20 62 79 20 47   // ...Replaced by G
		4C 69 62 2E 4F 62 6A 65 63 74 20 63 61 73 74 00   // Lib.Object cast.
		00                                              ) // .

        // Method begins at RVA 0x99d4
	// Code size 13 (0xd)
	.maxstack 8
	IL_0000:  ldarga.s 0
	IL_0002:  call native int valuetype GLib.Value::g_value_get_object([out] valuetype GLib.Value&)
	IL_0007:  newobj instance void class GLib.UnwrappedObject::'.ctor'(native int)
	IL_000c:  ret 
    } // end of method Value::op_Explicit

    // method line 924
    .method public static hidebysig specialname 
           default string[] op_Explicit (valuetype GLib.Value val)  cil managed 
    {
        // Method begins at RVA 0x99e4
	// Code size 113 (0x71)
	.maxstack 5
	.locals init (
		native int	V_0,
		int32	V_1,
		string[]	V_2,
		int32	V_3)
	IL_0000:  ldarga.s 0
	IL_0002:  call native int valuetype GLib.Value::g_value_get_boxed([out] valuetype GLib.Value&)
	IL_0007:  stloc.0 
	IL_0008:  ldloc.0 
	IL_0009:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_000e:  call bool native int::op_Equality(native int, native int)
	IL_0013:  brfalse IL_001a

	IL_0018:  ldnull 
	IL_0019:  ret 
	IL_001a:  ldc.i4.0 
	IL_001b:  stloc.1 
	IL_001c:  br IL_0025

	IL_0021:  ldloc.1 
	IL_0022:  ldc.i4.1 
	IL_0023:  add 
	IL_0024:  stloc.1 
	IL_0025:  ldloc.0 
	IL_0026:  ldloc.1 
	IL_0027:  call int32 native int::get_Size()
	IL_002c:  mul 
	IL_002d:  call native int class [mscorlib]System.Runtime.InteropServices.Marshal::ReadIntPtr(native int, int32)
	IL_0032:  ldsfld native int [mscorlib]System.IntPtr::Zero
	IL_0037:  call bool native int::op_Inequality(native int, native int)
	IL_003c:  brtrue IL_0021

	IL_0041:  ldloc.1 
	IL_0042:  newarr string
	IL_0047:  stloc.2 
	IL_0048:  ldc.i4.0 
	IL_0049:  stloc.3 
	IL_004a:  br IL_0068

	IL_004f:  ldloc.2 
	IL_0050:  ldloc.3 
	IL_0051:  ldloc.0 
	IL_0052:  ldloc.3 
	IL_0053:  call int32 native int::get_Size()
	IL_0058:  mul 
	IL_0059:  call native int class [mscorlib]System.Runtime.InteropServices.Marshal::ReadIntPtr(native int, int32)
	IL_005e:  call string class GLib.Marshaller::Utf8PtrToString(native int)
	IL_0063:  stelem.ref 
	IL_0064:  ldloc.3 
	IL_0065:  ldc.i4.1 
	IL_0066:  add 
	IL_0067:  stloc.3 
	IL_0068:  ldloc.3 
	IL_0069:  ldloc.1 
	IL_006a:  blt IL_004f

	IL_006f:  ldloc.2 
	IL_0070:  ret 
    } // end of method Value::op_Explicit

    // method line 925
    .method private hidebysig 
           instance default object ToBoxed ()  cil managed 
    {
        // Method begins at RVA 0x9a64
	// Code size 114 (0x72)
	.maxstack 3
	.locals init (
		native int	V_0,
		class [mscorlib]System.Type	V_1,
		valuetype GLib.GType	V_2)
	IL_0000:  ldarg.0 
	IL_0001:  call native int valuetype GLib.Value::g_value_get_boxed([out] valuetype GLib.Value&)
	IL_0006:  stloc.0 
	IL_0007:  ldarg.0 
	IL_0008:  ldfld native int GLib.Value::'type'
	IL_000d:  call class [mscorlib]System.Type valuetype GLib.GType::LookupType(native int)
	IL_0012:  stloc.1 
	IL_0013:  ldloc.1 
	IL_0014:  ldnull 
	IL_0015:  call bool class [mscorlib]System.Type::op_Equality(class [mscorlib]System.Type, class [mscorlib]System.Type)
	IL_001a:  brfalse IL_0049

	IL_001f:  ldstr "Unknown type "
	IL_0024:  ldloca.s 2
	IL_0026:  ldarg.0 
	IL_0027:  ldfld native int GLib.Value::'type'
	IL_002c:  call instance void valuetype GLib.GType::'.ctor'(native int)
	IL_0031:  ldloca.s 2
	IL_0033:  constrained. GLib.GType
	IL_0039:  callvirt instance string object::ToString()
	IL_003e:  call string string::Concat(string, string)
	IL_0043:  newobj instance void class [mscorlib]System.Exception::'.ctor'(string)
	IL_0048:  throw 
	IL_0049:  ldloc.1 
	IL_004a:  ldtoken GLib.Opaque
	IL_004f:  call class [mscorlib]System.Type class [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
	IL_0054:  callvirt instance bool class [mscorlib]System.Type::IsSubclassOf(class [mscorlib]System.Type)
	IL_0059:  brfalse IL_006a

	IL_005e:  ldarg.0 
	IL_005f:  ldobj GLib.Value
	IL_0064:  call class GLib.Opaque valuetype GLib.Value::op_Explicit(valuetype GLib.Value)
	IL_0069:  ret 
	IL_006a:  ldloc.0 
	IL_006b:  ldloc.1 
	IL_006c:  call object class GLib.FastActivator::CreateBoxed(native int, class [mscorlib]System.Type)
	IL_0071:  ret 
    } // end of method Value::ToBoxed

    // method line 926
    .method private static hidebysig pinvokeimpl ("libgobject-2.0-0.dll" as "g_type_is_a" cdecl )
           default bool g_type_is_a (native int 'type', native int is_a_type)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method Value::g_type_is_a

    // method line 927
    .method private static hidebysig pinvokeimpl ("libgobject-2.0-0.dll" as "g_value_get_param" cdecl )
           default native int g_value_get_param (valuetype GLib.Value& val)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method Value::g_value_get_param

    // method line 928
    .method private static hidebysig pinvokeimpl ("libgobject-2.0-0.dll" as "g_value_set_param" cdecl )
           default void g_value_set_param (valuetype GLib.Value& val, native int data)  cil managed preservesig 
    {
        // Method begins at RVA 0x0
    } // end of method Value::g_value_set_param

	.property instance object Val ()
	{
		.get instance default object GLib.Value::get_Val () 
		.set instance default void GLib.Value::set_Val (object 'value') 
	}
  } // end of class GLib.Value
}

