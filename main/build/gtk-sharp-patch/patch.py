import sys

src, dst = sys.argv[1], sys.argv[2]
t = open(src, 'rb').read().decode('latin-1')

fixes = 0

old = 'g_markup_escape_text (native int text, int32 len)'
new = 'g_markup_escape_text (native int text, int64 len)'
assert t.count(old) == 1
t = t.replace(old, new); fixes += 1

old = ('\tIL_0014:  ldc.i4.m1 \n'
       '\tIL_0015:  call native int class GLib.Markup::g_markup_escape_text(native int, int32)')
new = ('\tIL_0014:  ldc.i4.m1 \n'
       '\tIL_0015:  conv.i8 \n'
       '\tIL_0016:  call native int class GLib.Markup::g_markup_escape_text(native int, int64)')
assert t.count(old) == 1
t = t.replace(old, new); fixes += 1

for line in ('\tIL_0024:  ldnull \n',
             '\tIL_0038:  ldc.i4.0 \n',
             '\tIL_0039:  newarr [mscorlib]System.Reflection.ParameterModifier\n'):
    assert t.count(line) == 1, line
    t = t.replace(line, ''); fixes += 1

old = ('GetConstructor(valuetype [mscorlib]System.Reflection.BindingFlags, '
       'class [mscorlib]System.Reflection.Binder, '
       'class [mscorlib]System.Type[], '
       'valuetype [mscorlib]System.Reflection.ParameterModifier[])')
new = ('GetConstructor(valuetype [mscorlib]System.Reflection.BindingFlags, '
       'class [mscorlib]System.Type[])')
assert t.count(old) == 1
t = t.replace(old, new); fixes += 1

old = '.method private static hidebysig pinvokeimpl ("libglib-2.0-0.dll" as "g_object_unref" cdecl )'
new = '.method private static hidebysig pinvokeimpl ("libgobject-2.0-0.dll" as "g_object_unref" cdecl )'
assert t.count(old) == 1
t = t.replace(old, new); fixes += 1

open(dst, 'wb').write(t.encode('latin-1'))
print('applied %d fixes -> %s' % (fixes, dst))