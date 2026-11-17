using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Xml.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.Options;
using MonoDevelop.Core;
using MonoDevelop.Ide.Services;

namespace MonoDevelop.AvaloniaShell.Views;

// ---------- F3: Text Editor > Source Analysis > C# > Code Style ----------
// Port of the legacy CodeStylePage (MonoDevelop.CSharp.OptionProvider), a
// GridOptionPreviewControl over the 34 C# CodeStyle options. The GTK UI is a Xwt
// TreeView (group nodes + item rows) with a Property combo and a Severity combo per
// row, plus a read-only preview editor below. The Avalonia port rebuilds that tree
// in code and wires the SAME persistence the GTK writes through
// MonoDevelopGlobalOptionPersister's "Non property case" (the 34 CodeStyle options
// are not in RoslynPreferences.wrapMap, only the 7 formatting bools are):
//   key   = OptionsExtensions.GetPropertyName (== RoslynPropName, replicated in
//           PreferencesDialog.axaml.cs) -> "Roslyn.<Name>.C#" (per-language) or
//           "Roslyn.<Name>" (the 7 non-per-language enums).
//   value = CodeStyleOption<T>.ToXElement().ToString(); the key is deleted when the
//           stored value equals the option default (legacy "don't store default").
//   load  = CodeStyleOption<T>.FromXElement(XElement.Parse(stored)); absent key ->
//           the option default.
// The 28 CSharpCodeStyleOptions + PreferBracesPreference/ExpressionBodyPreference
// come from the MonoRoslynCompat shim (public); the 6 core CodeStyleOptions +
// CodeStyleOption<T> + NotificationOption come from the real Roslyn 5.9.0. Both UIs
// bind the same types and write the same keys, so settings are shared.
//
// The live preview reuses the F2 RoslynPreviewService (core-only AdhocWorkspace +
// Formatter) to normalize the selected snippet and strip the //[ ... //] markers,
// exactly like the legacy AbstractOptionPreviewViewModel.UpdatePreview. The
// code-style content of the preview is carried by which snippet is selected (the
// legacy truePreview/falsePreview pairs and the 3 enum previews), so the core-only
// formatter is faithful for the preview.
partial class PreferencesDialog
{
	sealed class CsStyleRow
	{
		public Microsoft.CodeAnalysis.Options.IOption Option;
		public string Description;
		public int GroupIndex;
		public string[] PrefLabels;
		public string[] Previews;
		public object[] EnumValues; // null for bool rows
		public ComboBox PrefCombo;
		public ComboBox SevCombo;
		public TreeViewItem RowItem;
	}

	readonly List<CsStyleRow> csStyleRows = new ();
	readonly List<string> csStyleGroups = new ();
	RoslynPreviewService? csStylePreviewSvc;
	bool csStyleLoading;

	// The 5 severities, in the legacy GetDefaultNotifications() order:
	// None, Suggestion, Warning, Error, Silent.
	static readonly NotificationOption[] CsStyleSeverities =
	{
		NotificationOption.None, NotificationOption.Suggestion,
		NotificationOption.Warning, NotificationOption.Error, NotificationOption.Silent
	};

	static readonly string s_fieldDeclarationPreviewTrue = @"
class C{
    int capacity;
    void Method()
    {
//[
        this.capacity = 0;
//]
    }
}";
	static readonly string s_fieldDeclarationPreviewFalse = @"
class C{
    int capacity;
    void Method()
    {
//[
        capacity = 0;
//]
    }
}";
	static readonly string s_propertyDeclarationPreviewTrue = @"
class C{
    public int Id { get; set; }
    void Method()
    {
//[
        this.Id = 0;
//]
    }
}";
	static readonly string s_propertyDeclarationPreviewFalse = @"
class C{
    public int Id { get; set; }
    void Method()
    {
//[
        Id = 0;
//]
    }
}";
	static readonly string s_eventDeclarationPreviewTrue = @"
using System;
class C{
    event EventHandler Elapsed;
    void Handler(object sender, EventArgs args)
    {
//[
        this.Elapsed += Handler;
//]
    }
}";
	static readonly string s_eventDeclarationPreviewFalse = @"
using System;
class C{
    event EventHandler Elapsed;
    void Handler(object sender, EventArgs args)
    {
//[
        Elapsed += Handler;
//]
    }
}";
	static readonly string s_methodDeclarationPreviewTrue = @"
using System;
class C{
    void Display()
    {
//[
        this.Display();
//]
    }
}";
	static readonly string s_methodDeclarationPreviewFalse = @"
using System;
class C{
    void Display()
    {
//[
        Display();
//]
    }
}";
	static readonly string s_intrinsicPreviewDeclarationTrue = @"
class Program
{
//[
    private int _member;
    static void M(int argument)
    {
        int local;
    }
//]
}";
	static readonly string s_intrinsicPreviewDeclarationFalse = @"
using System;
class Program
{
//[
    private Int32 _member;
    static void M(Int32 argument)
    {
        Int32 local;
    }
//]
}";
	static readonly string s_intrinsicPreviewMemberAccessTrue = @"
class Program
{
//[
    static void M()
    {
        var local = int.MaxValue;
    }
//]
}";
	static readonly string s_intrinsicPreviewMemberAccessFalse = @"
using System;
class Program
{
//[
    static void M()
    {
        var local = Int32.MaxValue;
    }
//]
}";
	static readonly string s_varForIntrinsicsPreviewTrue = $@"
using System;
class C{{
    void Method()
    {{
//[
        var x = 5; // {GettextCatalog.GetString ("built-in types")}
//]
    }}
}}";
	static readonly string s_varForIntrinsicsPreviewFalse = $@"
using System;
class C{{
    void Method()
    {{
//[
        int x = 5; // {GettextCatalog.GetString ("built-in types")}
//]
    }}
}}";
	static readonly string s_varWhereApparentPreviewTrue = $@"
using System;
class C{{
    void Method()
    {{
//[
        var cobj = new C(); // {GettextCatalog.GetString ("type is apparent from assignment expression")}
//]
    }}
}}";
	static readonly string s_varWhereApparentPreviewFalse = $@"
using System;
class C{{
    void Method()
    {{
//[
        C cobj = new C(); // {GettextCatalog.GetString ("type is apparent from assignment expression")}
//]
    }}
}}";
	static readonly string s_varWherePossiblePreviewTrue = $@"
using System;
class C{{
    void Init()
    {{
//[
        var f = this.Init(); // {GettextCatalog.GetString ("everywhere else")}
//]
    }}
}}";
	static readonly string s_varWherePossiblePreviewFalse = $@"
using System;
class C{{
    void Init()
    {{
//[
        Action f = this.Init(); // {GettextCatalog.GetString ("everywhere else")}
//]
    }}
}}";
	static readonly string s_preferThrowExpression = $@"
using System;

class C
{{
    private string s;

    void M1(string s)
    {{
//[
        // {GettextCatalog.GetString ("Prefer:")}
        this.s = s ?? throw new ArgumentNullException(nameof(s));
//]
    }}
    void M2(string s)
    {{
//[
        // {GettextCatalog.GetString ("Over:")}
        if (s == null)
        {{
            throw new ArgumentNullException(nameof(s));
        }}

        this.s = s;
//]
    }}
}}
";
	static readonly string s_preferCoalesceExpression = $@"
using System;

class C
{{
    private string s;

    void M1(string s)
    {{
//[
        // {GettextCatalog.GetString ("Prefer:")}
        var v = x ?? y;
//]
    }}
    void M2(string s)
    {{
//[
        // {GettextCatalog.GetString ("Over:")}
		var v = x != null ? x : y; // {GettextCatalog.GetString ("or")}
        var v = x == null ? y : x;
//]
    }}
}}
";
	static readonly string s_preferConditionalDelegateCall = $@"
using System;

class C
{{
    private string s;

    void M1(string s)
    {{
//[
        // {GettextCatalog.GetString ("Prefer:")}
        func?.Invoke(args);
//]
    }}
    void M2(string s)
    {{
//[
        // {GettextCatalog.GetString ("Over:")}
        if (func != null)
        {{
            func(args);
        }}
//]
    }}
}}
";
	static readonly string s_preferNullPropagation = $@"
using System;

class C
{{
    void M1(object o)
    {{
//[
        // {GettextCatalog.GetString ("Prefer:")}
        var v = o?.ToString();
//]
    }}
    void M2(object o)
    {{
//[
        // {GettextCatalog.GetString ("Over:")}
		var v = o == null ? null : o.ToString(); // {GettextCatalog.GetString ("or")}
        var v = o != null ? o.ToString() : null;
//]
    }}
}}
";
	static readonly string s_preferPatternMatchingOverAsWithNullCheck = $@"
class C
{{
    void M1()
    {{
//[
        // {GettextCatalog.GetString ("Prefer:")}
        if (o is string s)
        {{
        }}
//]
    }}
    void M2()
    {{
//[
        // {GettextCatalog.GetString ("Over:")}
        var s = o as string;
        if (s != null)
        {{
        }}
//]
    }}
}}
";
	static readonly string s_preferPatternMatchingOverIsWithCastCheck = $@"
class C
{{
    void M1()
    {{
//[
        // {GettextCatalog.GetString ("Prefer:")}
        if (o is int i)
        {{
        }}
//]
    }}
    void M2()
    {{
//[
        // {GettextCatalog.GetString ("Over:")}
        if (o is int)
        {{
            var i = (int)o;
        }}
//]
    }}
}}
";
	static readonly string s_preferObjectInitializer = $@"
using System;

class Customer
{{
    private int Age;

    void M1()
    {{
//[
        // {GettextCatalog.GetString ("Prefer:")}
        var c = new Customer()
        {{
            Age = 21
        }};
//]
    }}
    void M2()
    {{
//[
        // {GettextCatalog.GetString ("Over:")}
        var c = new Customer();
        c.Age = 21;
//]
    }}
}}
";
	static readonly string s_preferCollectionInitializer = $@"
using System.Collections.Generic;

class Customer
{{
    private int Age;

    void M1()
    {{
//[
        // {GettextCatalog.GetString ("Prefer:")}
        var list = new List<int>
        {{
            1,
            2,
            3
        }};
//]
    }}
    void M2()
    {{
//[
        // {GettextCatalog.GetString ("Over:")}
        var list = new List<int>();
        list.Add(1);
        list.Add(2);
        list.Add(3);
//]
    }}
}}
";
	static readonly string s_preferExplicitTupleName = $@"
class Customer
{{
    void M1()
    {{
//[
        // {GettextCatalog.GetString ("Prefer:")}
        (string name, int age) customer = GetCustomer();
        var name = customer.name;
        var age = customer.age;
//]
    }}
    void M2()
    {{
//[
        // {GettextCatalog.GetString ("Over:")}
        (string name, int age) customer = GetCustomer();
        var name = customer.Item1;
        var age = customer.Item2;
//]
    }}
}}
";
	static readonly string s_preferSimpleDefaultExpression = $@"
using System.Threading;

class Customer1
{{
//[
    // {GettextCatalog.GetString ("Prefer:")}
    void DoWork(CancellationToken cancellationToken = default) {{ }}
//]
}}
class Customer2
{{
//[
    // {GettextCatalog.GetString ("Over:")}
    void DoWork(CancellationToken cancellationToken = default(CancellationToken)) {{ }}
//]
}}
";
	static readonly string s_preferInferredTupleName = $@"
using System.Threading;

class Customer
{{
    void M1(int age, string name)
    {{
//[
        // {GettextCatalog.GetString ("Prefer:")}
        var tuple = (age, name);
//]
    }}
    void M2(int age, string name)
    {{
//[
        // {GettextCatalog.GetString ("Over:")}
        var tuple = (age: age, name: name);
//]
    }}
}}
";
	static readonly string s_preferInferredAnonymousTypeMemberName = $@"
using System.Threading;

class Customer
{{
    void M1(int age, string name)
    {{
//[
        // {GettextCatalog.GetString ("Prefer:")}
        var anon = new {{ age, name }};
//]
    }}
    void M2(int age, string name)
    {{
//[
        // {GettextCatalog.GetString ("Over:")}
        var anon = new {{ age = age, name = name }};
//]
    }}
}}
";
	static readonly string s_preferInlinedVariableDeclaration = $@"
using System;

class Customer
{{
    void M1(string value)
    {{
//[
        // {GettextCatalog.GetString ("Prefer:")}
        if (int.TryParse(value, out int i))
        {{
        }}
//]
    }}
    void M2(string value)
    {{
//[
        // {GettextCatalog.GetString ("Over:")}
        int i;
        if (int.TryParse(value, out i))
        {{
        }}
//]
    }}
}}
";
	static readonly string s_preferDeconstructedVariableDeclaration = $@"
using System;

class Customer
{{
    void M1(string value)
    {{
//[
        // {GettextCatalog.GetString ("Prefer:")}
        var (name, age) = GetPersonTuple();
        Console.WriteLine($""{{name}} {{age}}"");

        (int x, int y) = GetPointTuple();
        Console.WriteLine($""{{x}} {{y}}"");
//]
    }}
    void M2(string value)
    {{
//[
        // {GettextCatalog.GetString ("Over:")}
        var person = GetPersonTuple();
        Console.WriteLine($""{{person.name}} {{person.age}}"");

        (int x, int y) point = GetPointTuple();
        Console.WriteLine($""{{point.x}} {{point.y}}"");
//]
    }}
}}
";
	static readonly string s_preferBraces = $@"
using System;

class Customer
{{
    private int Age;

    void M1()
    {{
//[
        // {GettextCatalog.GetString ("Prefer:")}
        if (test)
        {{
            this.Display();
        }}
//]
    }}
    void M2()
    {{
//[
        // {GettextCatalog.GetString ("Over:")}
        if (test)
            this.Display();
//]
    }}
}}
";
	static readonly string s_doNotPreferBraces = $@"
using System;

class Customer
{{
    private int Age;

    void M1()
    {{
//[
        // {GettextCatalog.GetString ("Allow:")}
        if (test) Console.WriteLine(""Text"");

        // {GettextCatalog.GetString ("Allow:")}
        if (test)
            Console.WriteLine(""Text"");

        // {GettextCatalog.GetString ("Allow:")}
        if (test)
            Console.WriteLine(
                ""Text"");

        // {GettextCatalog.GetString ("Allow:")}
        if (test)
        {{
            Console.WriteLine(
                ""Text"");
        }}
//]
    }}
}}
";
	static readonly string s_preferBracesWhenMultiline = $@"
using System;

class Customer
{{
    private int Age;

    void M1()
    {{
//[
        // {GettextCatalog.GetString ("Allow:")}
        if (test) Console.WriteLine(""Text"");

        // {GettextCatalog.GetString ("Allow:")}
        if (test)
            Console.WriteLine(""Text"");

        // {GettextCatalog.GetString ("Allow:")}
        if (test)
        {{
            Console.WriteLine(
                ""Text"");
        }}
//]
    }}
    void M2()
    {{
//[
        // {GettextCatalog.GetString ("Over:")}
        if (test)
            Console.WriteLine(
                ""Text"");
//]
    }}
}}
";
	static readonly string s_preferAutoProperties = $@"
using System;

class Customer1
{{
//[
    // {GettextCatalog.GetString ("Prefer:")}
    public int Age {{ get; }}
//]
}}
class Customer2
{{
//[
    // {GettextCatalog.GetString ("Over:")}
    private int age;

    public int Age
    {{
        get
        {{
            return age;
        }}
    }}
//]
}}
";
	static readonly string s_preferLocalFunctionOverAnonymousFunction = $@"
using System;

class Customer
{{
    void M1(string value)
    {{
//[
        // {GettextCatalog.GetString ("Prefer:")}
        int fibonacci(int n)
        {{
            return n <= 1 ? n : fibonacci(n - 1) + fibonacci(n - 2);
        }}
//]
    }}
    void M2(string value)
    {{
//[
        // {GettextCatalog.GetString ("Over:")}
        Func<int, int> fibonacci = null;
        fibonacci = (int n) =>
        {{
            return n <= 1 ? n : fibonacci(n - 1) + fibonacci(n - 2);
        }};
//]
    }}
}}
";
	static readonly string s_preferIsNullOverReferenceEquals = $@"
using System;

class Customer
{{
    void M1(string value1, string value2)
    {{
//[
        // {GettextCatalog.GetString ("Prefer:")}
        if (value1 is null)
            return;

        if (value2 is null)
            return;
//]
    }}
    void M2(string value1, string value2)
    {{
//[
        // {GettextCatalog.GetString ("Over:")}
        if (object.ReferenceEquals(value1, null))
            return;

        if ((object)value2 == null)
            return;
//]
    }}
}}
";
	static readonly string s_preferExpressionBodyForMethods = @"
using System;

//[
class Customer
{
    private int Age;

    public int GetAge() => this.Age;
}
//]
";
	static readonly string s_preferBlockBodyForMethods = @"
using System;

//[
class Customer
{
    private int Age;

    public int GetAge()
    {
        return this.Age;
    }
}
//]
";
	static readonly string s_preferExpressionBodyForConstructors = @"
using System;

//[
class Customer
{
    private int Age;

    public Customer(int age) => Age = age;
}
//]
";
	static readonly string s_preferBlockBodyForConstructors = @"
using System;

//[
class Customer
{
    private int Age;

    public Customer(int age)
    {
        Age = age;
    }
}
//]
";
	static readonly string s_preferExpressionBodyForOperators = @"
using System;

struct ComplexNumber
{
//[
    public static ComplexNumber operator +(ComplexNumber c1, ComplexNumber c2)
        => new ComplexNumber(c1.Real + c2.Real, c1.Imaginary + c2.Imaginary);
//]
}
";
	static readonly string s_preferBlockBodyForOperators = @"
using System;

struct ComplexNumber
{
//[
    public static ComplexNumber operator +(ComplexNumber c1, ComplexNumber c2)
    {
        return new ComplexNumber(c1.Real + c2.Real, c1.Imaginary + c2.Imaginary);
    }
//]
}
";
	static readonly string s_preferExpressionBodyForProperties = @"
using System;

//[
class Customer
{
    private int _age;
    public int Age => _age;
}
//]
";
	static readonly string s_preferBlockBodyForProperties = @"
using System;

//[
class Customer
{
    private int _age;
    public int Age { get { return _age; } }
}
//]
";
	static readonly string s_preferExpressionBodyForAccessors = @"
using System;

//[
class Customer
{
    private int _age;
    public int Age
    {
        get => _age;
        set => _age = value;
    }
}
//]
";
	static readonly string s_preferBlockBodyForAccessors = @"
using System;

//[
class Customer
{
    private int _age;
    public int Age
    {
        get { return _age; }
        set { _age = value; }
    }
}
//]
";
	static readonly string s_preferExpressionBodyForIndexers = @"
using System;

//[
class List<T>
{
    private T[] _values;
    public T this[int i] => _values[i];
}
//]
";
	static readonly string s_preferBlockBodyForIndexers = @"
using System;

//[
class List<T>
{
    private T[] _values;
    public T this[int i] { get { return _values[i]; } }
}
//]
";
	static readonly string s_preferReadonly = $@"
class Customer1
{{
//[
        // {GettextCatalog.GetString ("Prefer:")}
        // '_value' can only be assigned in constructor
        private readonly int _value = 0;
//]
}}
class Customer2
{{
//[
        // {GettextCatalog.GetString ("Over:")}
        // '_value' can be assigned anywhere
        private int _value = 0;
//]
}}
";


	void BuildCsStyleModel ()
	{
		csStyleRows.Clear ();
		csStyleGroups.Clear ();
		csStyleGroups.AddRange (new[] {
			"'this.' preferences:",
		"predefined type preferences:",
		"'var' preferences:",
		"Code block preferences:",
		"Expression preferences:",
		"Variable preferences:",
		"'null' checking:",
		"Field preferences:"
		});
		csStyleRows.Add (new CsStyleRow { Option = CodeStyleOptions.QualifyFieldAccess,
			Description = GettextCatalog.GetString("Qualify field access with 'this'"), GroupIndex = 0,
			PrefLabels = new string[] { GettextCatalog.GetString("Prefer 'this.'"), GettextCatalog.GetString("Do not prefer 'this.'") }, Previews = new string[] { s_fieldDeclarationPreviewTrue, s_fieldDeclarationPreviewFalse } });
		csStyleRows.Add (new CsStyleRow { Option = CodeStyleOptions.QualifyPropertyAccess,
			Description = GettextCatalog.GetString("Qualify property access with 'this'"), GroupIndex = 0,
			PrefLabels = new string[] { GettextCatalog.GetString("Prefer 'this.'"), GettextCatalog.GetString("Do not prefer 'this.'") }, Previews = new string[] { s_propertyDeclarationPreviewTrue, s_propertyDeclarationPreviewFalse } });
		csStyleRows.Add (new CsStyleRow { Option = CodeStyleOptions.QualifyMethodAccess,
			Description = GettextCatalog.GetString("Qualify method access with 'this'"), GroupIndex = 0,
			PrefLabels = new string[] { GettextCatalog.GetString("Prefer 'this.'"), GettextCatalog.GetString("Do not prefer 'this.'") }, Previews = new string[] { s_methodDeclarationPreviewTrue, s_methodDeclarationPreviewFalse } });
		csStyleRows.Add (new CsStyleRow { Option = CodeStyleOptions.QualifyEventAccess,
			Description = GettextCatalog.GetString("Qualify event access with 'this'"), GroupIndex = 0,
			PrefLabels = new string[] { GettextCatalog.GetString("Prefer 'this.'"), GettextCatalog.GetString("Do not prefer 'this.'") }, Previews = new string[] { s_eventDeclarationPreviewTrue, s_eventDeclarationPreviewFalse } });
		csStyleRows.Add (new CsStyleRow { Option = CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInDeclaration,
			Description = GettextCatalog.GetString("For locals, parameters and members"), GroupIndex = 1,
			PrefLabels = new string[] { GettextCatalog.GetString("Prefer predefined type"), GettextCatalog.GetString("Prefer framework type") }, Previews = new string[] { s_intrinsicPreviewDeclarationTrue, s_intrinsicPreviewDeclarationFalse } });
		csStyleRows.Add (new CsStyleRow { Option = CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInMemberAccess,
			Description = GettextCatalog.GetString("For member access expressions"), GroupIndex = 1,
			PrefLabels = new string[] { GettextCatalog.GetString("Prefer predefined type"), GettextCatalog.GetString("Prefer framework type") }, Previews = new string[] { s_intrinsicPreviewMemberAccessTrue, s_intrinsicPreviewMemberAccessFalse } });
		csStyleRows.Add (new CsStyleRow { Option = CSharpCodeStyleOptions.VarForBuiltInTypes,
			Description = GettextCatalog.GetString("For built-in types"), GroupIndex = 2,
			PrefLabels = new string[] { GettextCatalog.GetString("Prefer 'var'"), GettextCatalog.GetString("Prefer explicit type") }, Previews = new string[] { s_varForIntrinsicsPreviewTrue, s_varForIntrinsicsPreviewFalse } });
		csStyleRows.Add (new CsStyleRow { Option = CSharpCodeStyleOptions.VarWhenTypeIsApparent,
			Description = GettextCatalog.GetString("When variable type is apparent"), GroupIndex = 2,
			PrefLabels = new string[] { GettextCatalog.GetString("Prefer 'var'"), GettextCatalog.GetString("Prefer explicit type") }, Previews = new string[] { s_varWhereApparentPreviewTrue, s_varWhereApparentPreviewFalse } });
		csStyleRows.Add (new CsStyleRow { Option = CSharpCodeStyleOptions.VarElsewhere,
			Description = GettextCatalog.GetString("Elsewhere"), GroupIndex = 2,
			PrefLabels = new string[] { GettextCatalog.GetString("Prefer 'var'"), GettextCatalog.GetString("Prefer explicit type") }, Previews = new string[] { s_varWherePossiblePreviewTrue, s_varWherePossiblePreviewFalse } });
		csStyleRows.Add (new CsStyleRow { Option = CSharpCodeStyleOptions.PreferBraces,
			Description = GettextCatalog.GetString("Prefer Braces"), GroupIndex = 3,
			PrefLabels = new string[] { GettextCatalog.GetString("Yes"), GettextCatalog.GetString("No"), GettextCatalog.GetString("When on multiple lines") }, Previews = new string[] { s_preferBraces, s_doNotPreferBraces, s_preferBracesWhenMultiline },
			EnumValues = new object[] { PreferBracesPreference.Always, PreferBracesPreference.None, PreferBracesPreference.WhenMultiline }, });
		csStyleRows.Add (new CsStyleRow { Option = CSharpCodeStyleOptions.PreferAutoProperties,
			Description = GettextCatalog.GetString("Prefer auto properties"), GroupIndex = 3,
			PrefLabels = new string[] { GettextCatalog.GetString("Yes"), GettextCatalog.GetString("No") }, Previews = new string[] { s_preferAutoProperties, s_preferAutoProperties } });
		csStyleRows.Add (new CsStyleRow { Option = CSharpCodeStyleOptions.PreferObjectInitializer,
			Description = GettextCatalog.GetString("Prefer object initializer"), GroupIndex = 4,
			PrefLabels = new string[] { GettextCatalog.GetString("Yes"), GettextCatalog.GetString("No") }, Previews = new string[] { s_preferObjectInitializer, s_preferObjectInitializer } });
		csStyleRows.Add (new CsStyleRow { Option = CSharpCodeStyleOptions.PreferCollectionInitializer,
			Description = GettextCatalog.GetString("Prefer collection initializer"), GroupIndex = 4,
			PrefLabels = new string[] { GettextCatalog.GetString("Yes"), GettextCatalog.GetString("No") }, Previews = new string[] { s_preferCollectionInitializer, s_preferCollectionInitializer } });
		csStyleRows.Add (new CsStyleRow { Option = CSharpCodeStyleOptions.PreferPatternMatchingOverIsWithCastCheck,
			Description = GettextCatalog.GetString("Prefer pattern matching over 'is' with 'cast' check"), GroupIndex = 4,
			PrefLabels = new string[] { GettextCatalog.GetString("Yes"), GettextCatalog.GetString("No") }, Previews = new string[] { s_preferPatternMatchingOverIsWithCastCheck, s_preferPatternMatchingOverIsWithCastCheck } });
		csStyleRows.Add (new CsStyleRow { Option = CSharpCodeStyleOptions.PreferPatternMatchingOverAsWithNullCheck,
			Description = GettextCatalog.GetString("Prefer pattern matching over 'as' with 'null' check"), GroupIndex = 4,
			PrefLabels = new string[] { GettextCatalog.GetString("Yes"), GettextCatalog.GetString("No") }, Previews = new string[] { s_preferPatternMatchingOverAsWithNullCheck, s_preferPatternMatchingOverAsWithNullCheck } });
		csStyleRows.Add (new CsStyleRow { Option = CSharpCodeStyleOptions.PreferExplicitTupleNames,
			Description = GettextCatalog.GetString("Prefer explicit tuple name"), GroupIndex = 4,
			PrefLabels = new string[] { GettextCatalog.GetString("Yes"), GettextCatalog.GetString("No") }, Previews = new string[] { s_preferExplicitTupleName, s_preferExplicitTupleName } });
		csStyleRows.Add (new CsStyleRow { Option = CSharpCodeStyleOptions.PreferSimpleDefaultExpression,
			Description = GettextCatalog.GetString("Prefer simple 'default' expression"), GroupIndex = 4,
			PrefLabels = new string[] { GettextCatalog.GetString("Yes"), GettextCatalog.GetString("No") }, Previews = new string[] { s_preferSimpleDefaultExpression, s_preferSimpleDefaultExpression } });
		csStyleRows.Add (new CsStyleRow { Option = CSharpCodeStyleOptions.PreferInferredTupleNames,
			Description = GettextCatalog.GetString("Prefer inferred tuple element names"), GroupIndex = 4,
			PrefLabels = new string[] { GettextCatalog.GetString("Yes"), GettextCatalog.GetString("No") }, Previews = new string[] { s_preferInferredTupleName, s_preferInferredTupleName } });
		csStyleRows.Add (new CsStyleRow { Option = CSharpCodeStyleOptions.PreferInferredAnonymousTypeMemberNames,
			Description = GettextCatalog.GetString("Prefer inferred anonymous type member names"), GroupIndex = 4,
			PrefLabels = new string[] { GettextCatalog.GetString("Yes"), GettextCatalog.GetString("No") }, Previews = new string[] { s_preferInferredAnonymousTypeMemberName, s_preferInferredAnonymousTypeMemberName } });
		csStyleRows.Add (new CsStyleRow { Option = CSharpCodeStyleOptions.PreferLocalOverAnonymousFunction,
			Description = GettextCatalog.GetString("Prefer local function over anonymous function"), GroupIndex = 4,
			PrefLabels = new string[] { GettextCatalog.GetString("Yes"), GettextCatalog.GetString("No") }, Previews = new string[] { s_preferLocalFunctionOverAnonymousFunction, s_preferLocalFunctionOverAnonymousFunction } });
		csStyleRows.Add (new CsStyleRow { Option = CSharpCodeStyleOptions.PreferExpressionBodiedMethods,
			Description = GettextCatalog.GetString("Use expression body for methods"), GroupIndex = 4,
			PrefLabels = new string[] { GettextCatalog.GetString("Never"), GettextCatalog.GetString("When possible"), GettextCatalog.GetString("When on single line") }, Previews = new string[] { s_preferBlockBodyForMethods, s_preferExpressionBodyForMethods, s_preferExpressionBodyForMethods },
			EnumValues = new object[] { ExpressionBodyPreference.Never, ExpressionBodyPreference.WhenPossible, ExpressionBodyPreference.WhenOnSingleLine }, });
		csStyleRows.Add (new CsStyleRow { Option = CSharpCodeStyleOptions.PreferExpressionBodiedConstructors,
			Description = GettextCatalog.GetString("Use expression body for constructors"), GroupIndex = 4,
			PrefLabels = new string[] { GettextCatalog.GetString("Never"), GettextCatalog.GetString("When possible"), GettextCatalog.GetString("When on single line") }, Previews = new string[] { s_preferBlockBodyForConstructors, s_preferExpressionBodyForConstructors, s_preferExpressionBodyForConstructors },
			EnumValues = new object[] { ExpressionBodyPreference.Never, ExpressionBodyPreference.WhenPossible, ExpressionBodyPreference.WhenOnSingleLine }, });
		csStyleRows.Add (new CsStyleRow { Option = CSharpCodeStyleOptions.PreferExpressionBodiedOperators,
			Description = GettextCatalog.GetString("Use expression body for operators"), GroupIndex = 4,
			PrefLabels = new string[] { GettextCatalog.GetString("Never"), GettextCatalog.GetString("When possible"), GettextCatalog.GetString("When on single line") }, Previews = new string[] { s_preferBlockBodyForOperators, s_preferExpressionBodyForOperators, s_preferExpressionBodyForOperators },
			EnumValues = new object[] { ExpressionBodyPreference.Never, ExpressionBodyPreference.WhenPossible, ExpressionBodyPreference.WhenOnSingleLine }, });
		csStyleRows.Add (new CsStyleRow { Option = CSharpCodeStyleOptions.PreferExpressionBodiedProperties,
			Description = GettextCatalog.GetString("Use expression body for properties"), GroupIndex = 4,
			PrefLabels = new string[] { GettextCatalog.GetString("Never"), GettextCatalog.GetString("When possible"), GettextCatalog.GetString("When on single line") }, Previews = new string[] { s_preferBlockBodyForProperties, s_preferExpressionBodyForProperties, s_preferExpressionBodyForProperties },
			EnumValues = new object[] { ExpressionBodyPreference.Never, ExpressionBodyPreference.WhenPossible, ExpressionBodyPreference.WhenOnSingleLine }, });
		csStyleRows.Add (new CsStyleRow { Option = CSharpCodeStyleOptions.PreferExpressionBodiedIndexers,
			Description = GettextCatalog.GetString("Use expression body for indexers"), GroupIndex = 4,
			PrefLabels = new string[] { GettextCatalog.GetString("Never"), GettextCatalog.GetString("When possible"), GettextCatalog.GetString("When on single line") }, Previews = new string[] { s_preferBlockBodyForIndexers, s_preferExpressionBodyForIndexers, s_preferExpressionBodyForIndexers },
			EnumValues = new object[] { ExpressionBodyPreference.Never, ExpressionBodyPreference.WhenPossible, ExpressionBodyPreference.WhenOnSingleLine }, });
		csStyleRows.Add (new CsStyleRow { Option = CSharpCodeStyleOptions.PreferExpressionBodiedAccessors,
			Description = GettextCatalog.GetString("Use expression body for accessors"), GroupIndex = 4,
			PrefLabels = new string[] { GettextCatalog.GetString("Never"), GettextCatalog.GetString("When possible"), GettextCatalog.GetString("When on single line") }, Previews = new string[] { s_preferBlockBodyForAccessors, s_preferExpressionBodyForAccessors, s_preferExpressionBodyForAccessors },
			EnumValues = new object[] { ExpressionBodyPreference.Never, ExpressionBodyPreference.WhenPossible, ExpressionBodyPreference.WhenOnSingleLine }, });
		csStyleRows.Add (new CsStyleRow { Option = CSharpCodeStyleOptions.PreferInlinedVariableDeclaration,
			Description = GettextCatalog.GetString("Prefer inlined variable declaration"), GroupIndex = 5,
			PrefLabels = new string[] { GettextCatalog.GetString("Yes"), GettextCatalog.GetString("No") }, Previews = new string[] { s_preferInlinedVariableDeclaration, s_preferInlinedVariableDeclaration } });
		csStyleRows.Add (new CsStyleRow { Option = CSharpCodeStyleOptions.PreferDeconstructedVariableDeclaration,
			Description = GettextCatalog.GetString("Prefer deconstructed variable declaration"), GroupIndex = 5,
			PrefLabels = new string[] { GettextCatalog.GetString("Yes"), GettextCatalog.GetString("No") }, Previews = new string[] { s_preferDeconstructedVariableDeclaration, s_preferDeconstructedVariableDeclaration } });
		csStyleRows.Add (new CsStyleRow { Option = CSharpCodeStyleOptions.PreferThrowExpression,
			Description = GettextCatalog.GetString("Prefer throw-expression"), GroupIndex = 6,
			PrefLabels = new string[] { GettextCatalog.GetString("Yes"), GettextCatalog.GetString("No") }, Previews = new string[] { s_preferThrowExpression, s_preferThrowExpression } });
		csStyleRows.Add (new CsStyleRow { Option = CSharpCodeStyleOptions.PreferConditionalDelegateCall,
			Description = GettextCatalog.GetString("Prefer conditional delegate call"), GroupIndex = 6,
			PrefLabels = new string[] { GettextCatalog.GetString("Yes"), GettextCatalog.GetString("No") }, Previews = new string[] { s_preferConditionalDelegateCall, s_preferConditionalDelegateCall } });
		csStyleRows.Add (new CsStyleRow { Option = CSharpCodeStyleOptions.PreferCoalesceExpression,
			Description = GettextCatalog.GetString("Prefer coalesce expression"), GroupIndex = 6,
			PrefLabels = new string[] { GettextCatalog.GetString("Yes"), GettextCatalog.GetString("No") }, Previews = new string[] { s_preferCoalesceExpression, s_preferCoalesceExpression } });
		csStyleRows.Add (new CsStyleRow { Option = CSharpCodeStyleOptions.PreferNullPropagation,
			Description = GettextCatalog.GetString("Prefer null propagation"), GroupIndex = 6,
			PrefLabels = new string[] { GettextCatalog.GetString("Yes"), GettextCatalog.GetString("No") }, Previews = new string[] { s_preferNullPropagation, s_preferNullPropagation } });
		csStyleRows.Add (new CsStyleRow { Option = CSharpCodeStyleOptions.PreferIsNullCheckOverReferenceEqualityMethod,
			Description = GettextCatalog.GetString("Prefer 'is null' for reference equality checks"), GroupIndex = 6,
			PrefLabels = new string[] { GettextCatalog.GetString("Yes"), GettextCatalog.GetString("No") }, Previews = new string[] { s_preferIsNullOverReferenceEquals, s_preferIsNullOverReferenceEquals } });
		csStyleRows.Add (new CsStyleRow { Option = CSharpCodeStyleOptions.PreferReadonly,
			Description = GettextCatalog.GetString("Prefer readonly"), GroupIndex = 7,
			PrefLabels = new string[] { GettextCatalog.GetString("Yes"), GettextCatalog.GetString("No") }, Previews = new string[] { s_preferReadonly, s_preferReadonly } });
	}

	void LoadCodeStylePanel ()
	{
		csStyleLoading = true;
		try {
			BuildCsStyleModel ();
			CsStyleTree.Items.Clear ();
			var groupItems = new Dictionary<int, TreeViewItem> ();
			foreach (var row in csStyleRows) {
				if (!groupItems.TryGetValue (row.GroupIndex, out var groupItem)) {
					groupItem = new TreeViewItem { Header = csStyleGroups[row.GroupIndex], IsExpanded = true };
					groupItems[row.GroupIndex] = groupItem;
					CsStyleTree.Items.Add (groupItem);
				}
				// Three proportional columns, like the legacy Xwt TreeView where all
				// three columns set Expands = true (Description/Property/Severity share
				// the width); fixed 150px columns truncated the longest labels.
				var grid = new Grid { ColumnDefinitions = new ColumnDefinitions ("*,*,*") };
				var desc = new TextBlock { Text = row.Description, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness (0, 0, 8, 0) };
				Grid.SetColumn (desc, 0);
				row.PrefCombo = new ComboBox { VerticalAlignment = VerticalAlignment.Center };
				foreach (var p in row.PrefLabels)
					row.PrefCombo.Items.Add (new ComboBoxItem { Content = p });
				Grid.SetColumn (row.PrefCombo, 1);
				row.SevCombo = new ComboBox { VerticalAlignment = VerticalAlignment.Center };
				foreach (var s in CsStyleSeverities)
					row.SevCombo.Items.Add (new ComboBoxItem { Content = s.Name });
				Grid.SetColumn (row.SevCombo, 2);
				grid.Children.Add (desc);
				grid.Children.Add (row.PrefCombo);
				grid.Children.Add (row.SevCombo);

				var rowItem = new TreeViewItem { Header = grid };
				row.RowItem = rowItem;

				// Load the stored value into the combos (before wiring the preview).
				var (prefIdx, sevIdx) = CsCodeStyleGet (row);
				row.PrefCombo.SelectedIndex = prefIdx;
				row.SevCombo.SelectedIndex = sevIdx;

				groupItem.Items.Add (rowItem);
			}
			// Wire preview updates after the selections are set (avoids 34 workspace ops).
			CsStyleTree.SelectionChanged += OnCsStyleSelectionChanged;
			foreach (var row in csStyleRows)
				row.PrefCombo.SelectionChanged += OnCsStylePrefChanged;
			MainWindow.Instance?.Output ("[prefs-codestyle] loaded: rows=" + csStyleRows.Count + " groups=" + csStyleGroups.Count);
			// The legacy control selects the first item and previews it. Set the
			// selection through the TreeView's public property (not the item's
			// IsSelected) so the selection model registers it even before the
			// dialog is shown (headless QA runs).
			var first = csStyleRows[0];
			CsStyleTree.SelectedItem = first.RowItem;
			UpdateCsStylePreview (first);
			// QA: report the live preview state (formatted, markers stripped).
			var pv = CsStylePreview.Text ?? "";
			var firstLine = pv.Split ('\n').FirstOrDefault (s => s.Trim ().Length > 0) ?? "";
			MainWindow.Instance?.Output ("[prefs-codestyle] preview: chars=" + pv.Length
				+ " markers-stripped=" + !pv.Contains ("//[")
				+ " firstline='" + firstLine.Trim () + "'");
		} finally {
			csStyleLoading = false;
		}
	}

	void OnCsStyleSelectionChanged (object? sender, SelectionChangedEventArgs e)
	{
		if (csStyleLoading)
			return;
		if (CsStyleTree.SelectedItem is not TreeViewItem rowItem)
			return;
		var row = csStyleRows.FirstOrDefault (r => ReferenceEquals (r.RowItem, rowItem));
		if (row is not null)
			UpdateCsStylePreview (row);
	}

	void OnCsStylePrefChanged (object? sender, SelectionChangedEventArgs e)
	{
		if (csStyleLoading)
			return;
		if (sender is not ComboBox combo)
			return;
		var row = csStyleRows.FirstOrDefault (r => ReferenceEquals (r.PrefCombo, combo));
		if (row is null)
			return;
		if (ReferenceEquals (CsStyleTree.SelectedItem, row.RowItem))
			UpdateCsStylePreview (row);
	}

	void UpdateCsStylePreview (CsStyleRow row)
	{
		var idx = Math.Max (0, row.PrefCombo.SelectedIndex);
		var snippet = row.Previews[idx];
		csStylePreviewSvc ??= new RoslynPreviewService ();
		try {
			CsStylePreview.Text = csStylePreviewSvc.Format (snippet, null, CancellationToken.None);
		} catch (Exception ex) {
			CsStylePreview.Text = snippet;
			MainWindow.Instance?.Output ("[prefs-codestyle] preview EXC " + ex.Message);
		}
	}

	/// <summary>Loads a CodeStyle option into (prefIndex, severityIndex), mirroring the
	/// legacy BooleanCodeStyleOptionViewModel/EnumCodeStyleOptionViewModel ctors: the
	/// preference index comes from the stored Value, the severity index is matched by
	/// ReportDiagnostic.Severity (None/Silent both map to the stored "Hidden" severity,
	/// which FromXElement resolves back to Silent). The <paramref name="row"/> supplies
	/// the enum values (for the 7 enum options); the row lookup is by the shared static
	/// option instance.</summary>
	(int, int) CsCodeStyleGet (CsStyleRow row)
	{
		var option = row.Option;
		var key = RoslynPropName (option);
		var stored = SettingsStore.GetString (key);
		object loaded;
		if (stored is null)
			loaded = option.DefaultValue;
		else {
			try {
				var fromX = option.Type.GetMethod ("FromXElement", BindingFlags.Public | BindingFlags.Static);
				loaded = fromX.Invoke (null, new object[] { XElement.Parse (stored) });
			} catch {
				loaded = option.DefaultValue;
			}
		}
		var value = option.Type.GetProperty ("Value")!.GetValue (loaded);
		var notif = (NotificationOption)option.Type.GetProperty ("Notification")!.GetValue (loaded);

		// severity index by ReportDiagnostic severity (legacy first-match).
		int sevIdx = Array.FindIndex (CsStyleSeverities, s => s.Severity == notif.Severity);
		if (sevIdx < 0)
			sevIdx = Array.IndexOf (CsStyleSeverities, notif);
		if (sevIdx < 0)
			sevIdx = 0;

		// preference index: enums by EnumValues.IndexOf; bools map false->1, true->0
		// (the legacy Yes/No preference list has Yes=true first).
		int prefIdx;
		if (row.EnumValues is not null) {
			prefIdx = Array.IndexOf (row.EnumValues, value);
			if (prefIdx < 0)
				prefIdx = 0;
		} else {
			var b = value is bool v ? v : value is System.IConvertible c && c.ToBoolean (null);
			prefIdx = b ? 0 : 1;
		}
		return (prefIdx, sevIdx);
	}

	/// <summary>Stores a CodeStyle option (the "Non property case" of
	/// MonoDevelopGlobalOptionPersister.TryPersist): serializes the selected
	/// CodeStyleOption via ToXElement, or deletes the key when it equals the option
	/// default. Uses reflection (not the ICodeStyleOption interface) because both the
	/// real Roslyn and the MonoRoslynCompat shim define a
	/// Microsoft.CodeAnalysis.CodeStyle.ICodeStyleOption, so the interface name is
	/// ambiguous in this shell; the constructed object is the same real
	/// CodeStyleOption&lt;T&gt; instance either UI would build, so ToXElement output is
	/// identical.</summary>
	void CsCodeStyleSet (CsStyleRow row)
	{
		var option = row.Option;
		var key = RoslynPropName (option);
		var notif = CsStyleSeverities[Math.Max (0, row.SevCombo.SelectedIndex)];
		var idx = Math.Max (0, row.PrefCombo.SelectedIndex);
		object value = row.EnumValues is not null ? row.EnumValues[idx] : idx == 0;
		var constructed = BuildCsCodeStyleOption (option, value, notif);
		var def = option.DefaultValue;
		if (def is not null && def.Equals (constructed))
			SettingsStore.SetString (key, null); // legacy: don't store the default value
		else {
			// ToXElement returns an XElement; the legacy persister stores
			// element.ToString() (the same serialized form FromXElement parses back).
			var toX = option.Type.GetMethod ("ToXElement", BindingFlags.Public | BindingFlags.Instance);
			var xml = toX!.Invoke (constructed, null)!.ToString ();
			SettingsStore.SetString (key, xml);
		}
	}

	/// <summary>Constructs the option's CodeStyleOption&lt;T&gt; value (reflection, so
	/// it works for both bool and enum options without naming the ICodeStyleOption
	/// interface, which is ambiguous between the real Roslyn and the shim).</summary>
	static object BuildCsCodeStyleOption (Microsoft.CodeAnalysis.Options.IOption option, object value, NotificationOption notif)
	{
		var t = option.Type.GenericTypeArguments[0];
		var ctor = option.Type.GetConstructor (new[] { t, typeof (NotificationOption) });
		return ctor!.Invoke (new object[] { value, notif })!;
	}

	void StoreCodeStylePanel ()
	{
		foreach (var row in csStyleRows)
			CsCodeStyleSet (row);
		MainWindow.Instance?.Output ("[prefs-codestyle] stored: rows=" + csStyleRows.Count);
	}

	/// <summary>The preference index of a row's option default (bool: default Value ->
	/// true=0/false=1; enum: the default value's position in EnumValues).</summary>
	int CsStyleDefaultPrefIndex (CsStyleRow row)
	{
		var def = row.Option.DefaultValue;
		var value = row.Option.Type.GetProperty ("Value")!.GetValue (def);
		if (row.EnumValues is not null) {
			var idx = Array.IndexOf (row.EnumValues, value);
			return idx < 0 ? 0 : idx;
		}
		var b = value is bool v ? v : value is System.IConvertible c && c.ToBoolean (null);
		return b ? 0 : 1;
	}

	/// <summary>The severity index of a row's option default (all defaults are Silent
	/// -> index 4, matched by ReportDiagnostic severity like the load path).</summary>
	int CsStyleDefaultSevIndex (CsStyleRow row)
	{
		var def = row.Option.DefaultValue;
		var notif = (NotificationOption)row.Option.Type.GetProperty ("Notification")!.GetValue (def);
		var idx = Array.FindIndex (CsStyleSeverities, s => s.Severity == notif.Severity);
		return idx < 0 ? 0 : idx;
	}

	/// <summary>QA oracle for the Code Style persistence: snapshots the current values
	/// of all 34 keys, drives the panel store with a non-default preference + severity
	/// on every row, re-reads each key from the store and asserts the round-trip
	/// (value + severity), confirms the legacy default-delete behavior, then restores
	/// the previous values (no residue).</summary>
	public void QaCodeStyleOracle ()
	{
		// Snapshot the previous state of every key ("" == absent) so we can restore.
		var keys = csStyleRows.Select (r => RoslynPropName (r.Option)).ToArray ();
		var prev = keys.Select (k => SettingsStore.GetString (k) ?? "").ToArray ();
		try {
			// Drive the panel like a user: every row to a NON-default preference (the
			// index just past its default) + a non-default severity (Warning), then store.
			for (int i = 0; i < csStyleRows.Count; i++) {
				var row = csStyleRows[i];
				var count = row.PrefCombo.Items.Count;
				var defIdx = CsStyleDefaultPrefIndex (row);
				var target = (defIdx + 1) % count;
				if (target == defIdx)
					target = (defIdx + 2) % count;
				row.PrefCombo.SelectedIndex = target;
				row.SevCombo.SelectedIndex = 2; // Warning (non-default; the default is Silent)
			}
			StoreCodeStylePanel ();

			// Re-read from the store: every key must be present and round-trip to the
			// chosen preference + Warning severity.
			bool ok = true;
			for (int i = 0; i < csStyleRows.Count; i++) {
				var row = csStyleRows[i];
				if ((SettingsStore.GetString (keys[i]) ?? "") == "") { ok = false; break; }
				var (prefIdx, sevIdx) = CsCodeStyleGet (row);
				if (sevIdx != 2 || prefIdx != row.PrefCombo.SelectedIndex) { ok = false; break; }
			}
			MainWindow.Instance?.Output ("[prefs-codestyle] roundtrip: non-default stored+loaded ok=" + ok + " keys=" + keys.Length);

			// Live preview follows the selected preference (legacy UpdatePreview
			// path): drive the first row (Qualify field access) explicitly through
			// both variants and require the previewed snippet to swap.
			var first = csStyleRows[0];
			first.PrefCombo.SelectedIndex = 1; // Do not prefer 'this.'
			var pvTxt = CsStylePreview.Text ?? "";
			var pvNoThis = pvTxt.Contains ("capacity = 0;") && !pvTxt.Contains ("this.capacity");
			first.PrefCombo.SelectedIndex = 0; // Prefer 'this.'
			pvTxt = CsStylePreview.Text ?? "";
			var pvThis = pvTxt.Contains ("this.capacity = 0;");
			MainWindow.Instance?.Output ("[prefs-codestyle] preview-follows-pref: nothis-shown=" + pvNoThis + " this-shown=" + pvThis);

			// Legacy default-delete: restore the first row to its default value +
			// default severity and store; the key must be removed (absent).
			first.PrefCombo.SelectedIndex = CsStyleDefaultPrefIndex (first);
			first.SevCombo.SelectedIndex = CsStyleDefaultSevIndex (first);
			CsCodeStyleSet (first);
			var afterDefault = SettingsStore.GetString (RoslynPropName (first.Option));
			bool delOk = afterDefault is null;
			MainWindow.Instance?.Output ("[prefs-codestyle] default-delete: key absent ok=" + delOk);
		} finally {
			for (int i = 0; i < keys.Length; i++)
				SettingsStore.SetString (keys[i], prev[i] == "" ? null : prev[i]);
			MainWindow.Instance?.Output ("[prefs-codestyle] restored previous keys");
		}
	}
}
