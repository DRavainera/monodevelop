import sys

src, dst = sys.argv[1], sys.argv[2]
t = open(src, 'rb').read().decode('latin-1')

fixes = []

fieldset_old = (
    '\t  IL_00a0:  ldloc.0 \n'
    '\t  IL_00a1:  ldnull \n'
    '\t  IL_00a2:  ldnull \n'
    '\t  IL_00a3:  callvirt instance void class [mscorlib]System.Reflection.FieldInfo::SetValue(object, object, valuetype [mscorlib]System.Reflection.BindingFlags, class [mscorlib]System.Reflection.Binder, class [mscorlib]System.Globalization.CultureInfo)')
fieldset_new = (
    '\t  IL_00a3:  callvirt instance void class [mscorlib]System.Reflection.FieldInfo::SetValue(object, object)')
fixes.append(('FieldInfo.SetValue 5-arg', fieldset_old, fieldset_new))

activator_old = (
    '\tIL_002e:  ldloc.1 \n'
    '\tIL_002f:  ldnull \n'
    '\tIL_0030:  ldc.i4.1 \n'
    '\tIL_0031:  newarr [mscorlib]System.Object\n'
    '\tIL_0036:  dup \n'
    '\tIL_0037:  ldc.i4.0 \n'
    '\tIL_0038:  ldloc.0 \n'
    '\tIL_0039:  box [mscorlib]System.IntPtr\n'
    '\tIL_003e:  stelem.ref \n'
    '\tIL_003f:  ldnull \n'
    '\tIL_0040:  call object class [mscorlib]System.Activator::CreateInstance(class [mscorlib]System.Type, valuetype [mscorlib]System.Reflection.BindingFlags, class [mscorlib]System.Reflection.Binder, object[], class [mscorlib]System.Globalization.CultureInfo)')
activator_new = (
    '\tIL_0030:  ldc.i4.1 \n'
    '\tIL_0031:  newarr [mscorlib]System.Object\n'
    '\tIL_0036:  dup \n'
    '\tIL_0037:  ldc.i4.0 \n'
    '\tIL_0038:  ldloc.0 \n'
    '\tIL_0039:  box [mscorlib]System.IntPtr\n'
    '\tIL_003e:  stelem.ref \n'
    '\tIL_0040:  call object class [mscorlib]System.Activator::CreateInstance(class [mscorlib]System.Type, object[])')
fixes.append(('Activator.CreateInstance 5-arg', activator_old, activator_new))

applied = 0
for name, old, new in fixes:
    if old in t:
        assert t.count(old) == 1, name
        t = t.replace(old, new)
        applied += 1

if applied == 0:
    sys.stderr.write('no recognized patterns in %s\n' % src)
    sys.exit(1)

open(dst, 'wb').write(t.encode('latin-1'))
print('applied %d fix(es) -> %s' % (applied, dst))