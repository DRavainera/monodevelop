// Auto-generated from the AspNet/AspNetCore/VB addins xft catalogs
// (~/opencode/aspnet_vbnet_port/newfile_templates.json) by gen_templates.py —
// do not edit by hand; re-run the generator when the addins templates change.
// Partial de NewFileDialog: catálogo de los 3 addins + árboles de categorías.
using System.Collections.Generic;
namespace MonoDevelop.AvaloniaShell.Views;
partial class NewFileDialog
{

	// ===== AspNet clásico (34 templates, gate ProjectType=AspNetApp) =====
	static readonly NewFileTemplate AspWebForm_Empty = new () {
		Id = "WebForm-Empty",
		Name = @"Web Form",
		Description = @"Creates an ASP.NET Web Form.",
		Language = @"C#",
		IconId = @"md-html-file-icon",
		DefaultFilename = @"",
		Root = "C#",
		ProjectType = "AspNetApp",
		Files = {
			new () {
				Name = @"${Name}.aspx",
				Body = @"
<%@ Page Language=""C#"" %>
<!DOCTYPE html>
<html>
<head runat=""server"">
	<title>${Name}</title>
	<script runat=""server"">
	
	</script>
</head>
<body>
	<form id=""form1"" runat=""server"">
	
	</form>
</body>
</html>

		",
			},
		},
	};
	static readonly NewFileTemplate AspWebForm_CodeBehind = new () {
		Id = "WebForm-CodeBehind",
		Name = @"Web Form with CodeBehind",
		Description = @"Creates an ASP.NET Web Form with a CodeBehind class.",
		Language = @"C#",
		IconId = @"md-html-file-icon",
		DefaultFilename = @"",
		Root = "C#",
		ProjectType = "AspNetApp",
		Files = {
			new () {
				Name = @"${Name}.aspx",
				DefaultExtension = @".aspx",
				Body = @"
<%@ Page Language=""C#"" Inherits=""${Namespace}.${EscapedIdentifier}"" %>
<!DOCTYPE html>
<html>
<head runat=""server"">
	<title>${Name}</title>
</head>
<body>
	<form id=""form1"" runat=""server"">
	</form>
</body>
</html>

		",
			},
			new () {
				Name = @"${Name}.aspx.cs",
				Body = @"
using System;
using System.Web;
using System.Web.UI;

namespace ${Namespace} {
	
	public partial class ${EscapedIdentifier} : System.Web.UI.Page
	{
		
	}
}

		",
			},
			new () {
				Name = @"${Name}.aspx.designer.cs",
				AddStandardHeader = false,
				Body = @"
using System;
using System.Web;
using System.Web.UI;

namespace ${Namespace} {
	
	public partial class ${EscapedIdentifier}
	{
		
	}
}

		",
			},
		},
	};
	static readonly NewFileTemplate AspWebHandler_Empty = new () {
		Id = "WebHandler-Empty",
		Name = @"Web Handler",
		Description = @"Creates an ASP.NET Web Handler.",
		Language = @"C#",
		IconId = @"md-html-file-icon",
		DefaultFilename = @"",
		Root = "C#",
		ProjectType = "AspNetApp",
		Files = {
			new () {
				Name = @"${Name}.ashx",
				Body = @"
<%@ WebHandler Language=""C#"" Class=""${Namespace}.${EscapedIdentifier}"" %>
using System;
using System.Web;

namespace ${Namespace}
{

	public class ${EscapedIdentifier} : System.Web.IHttpHandler
	{
	
		public void ProcessRequest (HttpContext context)
		{
		}
	
		public bool IsReusable {
			get {
				return false;
			}
		}
	}
}
		",
			},
		},
	};
	static readonly NewFileTemplate AspWebHandler_CodeBehind = new () {
		Id = "WebHandler-CodeBehind",
		Name = @"Web Handler with CodeBehind",
		Description = @"Creates an ASP.NET Web Handler with a CodeBehind class.",
		Language = @"C#",
		IconId = @"md-html-file-icon",
		DefaultFilename = @"",
		Root = "C#",
		ProjectType = "AspNetApp",
		Files = {
			new () {
				Name = @"${Name}.ashx",
				Body = @"
<%@ WebHandler Language=""C#"" Class=""${Namespace}.${EscapedIdentifier}"" %>

		",
			},
			new () {
				Name = @"${Name}.ashx.cs",
				Body = @"
using System;
using System.Web;
using System.Web.UI;

namespace ${Namespace}
{	
	
	public class ${EscapedIdentifier} : System.Web.IHttpHandler
	{
	
		public void ProcessRequest (HttpContext context)
		{
			
		}
	
		public bool IsReusable {
			get {
				return false;
			}
		}
	}
}
	
		",
			},
		},
	};
	static readonly NewFileTemplate AspWebControl_Empty = new () {
		Id = "WebControl-Empty",
		Name = @"User Control",
		Description = @"Creates an empty ASP.NET user control.",
		Language = @"C#",
		IconId = @"md-html-file-icon",
		DefaultFilename = @"",
		Root = "C#",
		ProjectType = "AspNetApp",
		Files = {
			new () {
				Name = @"${Name}.ascx",
				Body = @"
<%@ Control Language=""C#"" ClassName=""${Namespace}.${Name}"" %>

<script runat=""server"">

</script>

		",
			},
		},
	};
	static readonly NewFileTemplate AspWebControl_CodeBehind = new () {
		Id = "WebControl-CodeBehind",
		Name = @"User Control with CodeBehind",
		Description = @"Creates an ASP.NET user control with a CodeBehind class.",
		Language = @"C#",
		IconId = @"md-html-file-icon",
		DefaultFilename = @"",
		Root = "C#",
		ProjectType = "AspNetApp",
		Files = {
			new () {
				Name = @"${Name}.ascx",
				DefaultExtension = @".ascx",
				Body = @"
<%@ Control Language=""C#"" Inherits=""${Namespace}.${EscapedIdentifier}"" %>

		",
			},
			new () {
				Name = @"${Name}.ascx.cs",
				Body = @"
using System;
using System.Web;
using System.Web.UI;

namespace ${Namespace} {

	public partial class ${EscapedIdentifier} : System.Web.UI.UserControl
	{
	}
}

		",
			},
			new () {
				Name = @"${Name}.ascx.designer.cs",
				AddStandardHeader = false,
				Body = @"
using System;
using System.Web;
using System.Web.UI;

namespace ${Namespace} {

	public partial class ${EscapedIdentifier}
	{
	}
}

		",
			},
		},
	};
	static readonly NewFileTemplate AspWebService_Empty = new () {
		Id = "WebService-Empty",
		Name = @"Web Service",
		Description = @"Creates an ASP.NET web service.",
		Language = @"C#",
		IconId = @"md-html-file-icon",
		DefaultFilename = @"",
		Root = "C#",
		ProjectType = "AspNetApp",
		Files = {
			new () {
				Name = @"${Name}.asmx",
				Body = @"
<%@ WebService Language=""C#"" Class=""${Namespace}.${EscapedIdentifier}"" %>
using System;
using System.Web.Services;

namespace ${Namespace}
{
	class ${EscapedIdentifier}
	{
	}
}

		",
			},
		},
	};
	static readonly NewFileTemplate AspWebService_CodeBehind = new () {
		Id = "WebService-CodeBehind",
		Name = @"Web Service with CodeBehind",
		Description = @"Creates an ASP.NET web service with a CodeBehind class.",
		Language = @"C#",
		IconId = @"md-html-file-icon",
		DefaultFilename = @"",
		Root = "C#",
		ProjectType = "AspNetApp",
		Files = {
			new () {
				Name = @"${Name}.asmx",
				Body = @"
<%@ WebService Language=""C#"" Class=""${Namespace}.${EscapedIdentifier}"" %>

		",
			},
			new () {
				Name = @"${Name}.asmx.cs",
				Body = @"
using System;
using System.Web;
using System.Web.Services;

namespace ${Namespace}
{
	public class ${EscapedIdentifier} : System.Web.Services.WebService
	{
		
	}
}

		",
			},
		},
	};
	static readonly NewFileTemplate AspMasterPage_Empty = new () {
		Id = "MasterPage-Empty",
		Name = @"Master Page without CodeBehind",
		Description = @"Creates an ASP.NET Master Page.",
		Language = @"C#",
		IconId = @"md-html-file-icon",
		DefaultFilename = @"",
		Root = "C#",
		ProjectType = "AspNetApp",
		Files = {
			new () {
				Name = @"${Name}.master",
				Body = @"
<%@ Master Language=""C#""%>
<!DOCTYPE html>
<html>
<head runat=""server"">
	<title>${Name}</title>
</head>
<body>
<div id=""container"">
<form runat=""server"">
    <asp:ContentPlaceHolder id=""contentPlaceHolder"" runat=""server"" /> 
</form>
</div>
</body>
</html>

		",
			},
		},
	};
	static readonly NewFileTemplate AspMasterPage_CodeBehind = new () {
		Id = "MasterPage-CodeBehind",
		Name = @"Master Page with CodeBehind",
		Description = @"Creates an ASP.NET Master Page with a CodeBehind class.",
		Language = @"C#",
		IconId = @"md-html-file-icon",
		DefaultFilename = @"",
		Root = "C#",
		ProjectType = "AspNetApp",
		Files = {
			new () {
				Name = @"${Name}.master",
				Body = @"
<%@ Master Language=""C#"" Inherits=""${Namespace}.${EscapedIdentifier}"" %>
<!DOCTYPE html>
<html>
<head runat=""server"">
	<title>${Name}</title>
</head>
<body>
<div id=""container"">
<form runat=""server"">
    <asp:ContentPlaceHolder id=""contentPlaceHolder"" runat=""server"" /> 
</form>
</div>
</body>
</html>

		",
			},
			new () {
				Name = @"${Name}.master.cs",
				Body = @"
using System;
using System.Web;
using System.Web.UI;
namespace ${Namespace} {
	public partial class ${EscapedIdentifier} : System.Web.UI.MasterPage 
	{
	}
}

		",
			},
			new () {
				Name = @"${Name}.master.designer.cs",
				AddStandardHeader = false,
				Body = @"
using System;
using System.Web;
using System.Web.UI;
namespace ${Namespace} {
	public partial class ${EscapedIdentifier} 
	{
	}
}

		",
			},
		},
	};
	static readonly NewFileTemplate AspNestedMasterPage = new () {
		Id = "NestedMasterPage",
		Name = @"Nested Master Page",
		Description = @"Creates an ASP.NET Nested Master Page.",
		Language = @"C#",
		IconId = @"md-html-file-icon",
		DefaultFilename = @"",
		Root = "C#",
		ProjectType = "AspNetApp",
		Files = {
		},
	};
	static readonly NewFileTemplate AspNestedMasterPage_CodeBehind = new () {
		Id = "NestedMasterPage-CodeBehind",
		Name = @"Nested Master Page with CodeBehind",
		Description = @"Creates an ASP.NET Nested Master Page with a CodeBehind class.",
		Language = @"C#",
		IconId = @"md-html-file-icon",
		DefaultFilename = @"",
		Root = "C#",
		ProjectType = "AspNetApp",
		Files = {
			new () {
				Name = @"${Name}.master.cs",
				Body = @"
using System;
using System.Web;
using System.Web.UI;
namespace ${Namespace} {
	public partial class ${EscapedIdentifier} : System.Web.UI.MasterPage 
	{
	}
}

		",
			},
			new () {
				Name = @"${Name}.master.designer.cs",
				AddStandardHeader = false,
				Body = @"
using System;
using System.Web;
using System.Web.UI;
namespace ${Namespace} {
	public partial class ${EscapedIdentifier} 
	{
	}
}

		",
			},
		},
	};
	static readonly NewFileTemplate AspGlobalAsax = new () {
		Id = "GlobalAsax",
		Name = @"ASP.NET Global.asax",
		Description = @"Creates an ASP.NET Global.asax file.",
		Language = @"C#",
		IconId = @"md-html-file-icon",
		DefaultFilename = @"Global.asax",
		FixedFilename = true,
		Root = "C#",
		ProjectType = "AspNetApp",
		Files = {
			new () {
				Name = @"Global.asax",
				Body = @"
<%@ Application Inherits=""${Namespace}.Global"" %>

		",
			},
			new () {
				Name = @"WebApiConfig.cs",
				SubPath = @"App_Start/WebApiConfig.cs",
				When = "IsAspWebApi",
				Body = @"﻿using System.Web.Http;

namespace ${RootNamespace}
{
	public static class WebApiConfig
	{
		public static void Register(HttpConfiguration config)
		{
			// Web API configuration and services

			// Web API routes
			config.MapHttpAttributeRoutes();

			config.Routes.MapHttpRoute(
				name: ""DefaultApi"",
				routeTemplate: ""api/{controller}/{id}"",
				defaults: new { id = RouteParameter.Optional }
			);
		}
	}
}
",
			},
			new () {
				Name = @"RouteConfig.cs",
				SubPath = @"App_Start/RouteConfig.cs",
				When = "IsAspMvc",
				Body = @"﻿using System.Web.Mvc;
using System.Web.Routing;

namespace ${RootNamespace}
{
	public class RouteConfig
	{
		public static void RegisterRoutes(RouteCollection routes)
		{
			routes.IgnoreRoute(""{resource}.axd/{*pathInfo}"");

			routes.MapRoute(
				name: ""Default"",
				url: ""{controller}/{action}/{id}"",
				defaults: new { controller = ""Home"", action = ""Index"", id = UrlParameter.Optional }
			);
		}
	}
}
",
			},
		},
	};
	static readonly NewFileTemplate AspWebConfig_Application = new () {
		Id = "WebConfig-Application",
		Name = @"Web Application Configuration",
		Description = @"Creates an ASP.NET Web.config file for an application.",
		Language = @"C#",
		IconId = @"md-html-file-icon",
		DefaultFilename = @"Web.config",
		FixedFilename = true,
		Root = "C#",
		ProjectType = "AspNetApp",
		Files = {
			new () {
				Name = @"Web.config",
				Body = @"
<?xml version=""1.0"" ?>
<!--
Web.config file for ${ProjectName}.

The settings that can be used in this file are documented at 
http://www.mono-project.com/Config_system.web and 
http://msdn2.microsoft.com/en-us/library/b5ysx397.aspx
-->
<configuration>
		<system.web>
			<compilation debug=""true"" targetFramework=""4.5"" />
			<httpRuntime targetFramework=""4.5"" />
	</system.web>
</configuration>

		",
			},
		},
	};
	static readonly NewFileTemplate AspWebConfig_SubDir = new () {
		Id = "WebConfig-SubDir",
		Name = @"Subdirectory Web Configuration",
		Description = @"Creates an ASP.NET web.config file for a subdirectory.",
		Language = @"C#",
		IconId = @"md-html-file-icon",
		DefaultFilename = @"web.config",
		FixedFilename = true,
		Root = "C#",
		ProjectType = "AspNetApp",
		Files = {
			new () {
				Name = @"web.config",
				Body = @"
<?xml version=""1.0"" encoding=""utf-8""?>
<!--
Web.config file for ${ProjectRelativeDirectory}.

The settings that can be used in this file are documented at 
http://www.mono-project.com/Config_system.web and 
http://msdn2.microsoft.com/en-us/library/b5ysx397.aspx
-->
<configuration>
	<system.web>
	
	</system.web>
</configuration>

		",
			},
		},
	};
	static readonly NewFileTemplate AspWebContentForm_CodeBehind = new () {
		Id = "WebContentForm-CodeBehind",
		Name = @"Web Content Form with CodeBehind",
		Description = @"Creates an ASP.NET Web Form that is the child of a Master Page, with a CodeBehind class.",
		Language = @"C#",
		IconId = @"md-html-file-icon",
		DefaultFilename = @"",
		Root = "C#",
		ProjectType = "AspNetApp",
		Files = {
			new () {
				Name = @"${Name}.aspx.cs",
				Body = @"
using System;
using System.Web;
using System.Web.UI;
namespace ${Namespace} {
	public partial class ${EscapedIdentifier} : System.Web.UI.Page 
	{
	}
}

		",
			},
			new () {
				Name = @"${Name}.aspx.designer.cs",
				AddStandardHeader = false,
				Body = @"
using System;
using System.Web;
using System.Web.UI;
namespace ${Namespace} {
	public partial class ${EscapedIdentifier} 
	{
	}
}

		",
			},
		},
	};
	static readonly NewFileTemplate AspWebContentForm = new () {
		Id = "WebContentForm",
		Name = @"Web Content Form",
		Description = @"Creates an ASP.NET Web Form that is the child of a Master Page.",
		Language = @"C#",
		IconId = @"md-html-file-icon",
		DefaultFilename = @"",
		Root = "C#",
		ProjectType = "AspNetApp",
		Files = {
		},
	};
	static readonly NewFileTemplate AspAspMvcController = new () {
		Id = "AspMvcController",
		Name = @"ASP.NET MVC Controller",
		Description = @"Creates an ASP.NET MVC Controller.",
		Language = @"C#",
		IconId = @"md-html-file-icon",
		DefaultFilename = @"",
		Root = "C#",
		ProjectType = "AspNetApp",
		Files = {
			new () {
				Name = @"${Name}.cs",
				Body = @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Ajax;

namespace ${RootNamespace}.Controllers
{
	public class ${EscapedIdentifier} : Controller
	{
	}
}

		",
			},
		},
	};
	static readonly NewFileTemplate AspAspMvcViewPage = new () {
		Id = "AspMvcViewPage",
		Name = @"ASP.NET MVC View Page",
		Description = @"Creates an MVC View page.",
		Language = @"C#",
		IconId = @"md-html-file-icon",
		DefaultFilename = @"",
		Root = "C#",
		ProjectType = "AspNetApp",
		Files = {
			new () {
				Name = @"${Name}.aspx",
				Body = @"
				<%@ Page Language=""C#"" Inherits=""System.Web.Mvc.ViewPage"" %>
<!DOCTYPE html>
<html>
<head runat=""server"">
	<title></title>
</head>
<body>
	<div>
		
	</div>
</body>
</html>

		",
			},
		},
	};
	static readonly NewFileTemplate AspAspMvcViewContentPage = new () {
		Id = "AspMvcViewContentPage",
		Name = @"ASP.NET MVC View Content",
		Description = @"Creates an ASP.NET MVC View Content page.",
		Language = @"C#",
		IconId = @"md-html-file-icon",
		DefaultFilename = @"",
		Root = "C#",
		ProjectType = "AspNetApp",
		Files = {
		},
	};
	static readonly NewFileTemplate AspAspMvcUserControl = new () {
		Id = "AspMvcUserControl",
		Name = @"ASP.NET MVC View User Control",
		Description = @"Creates an ASP.NET MVC User Control.",
		Language = @"C#",
		IconId = @"md-html-file-icon",
		DefaultFilename = @"",
		Root = "C#",
		ProjectType = "AspNetApp",
		Files = {
			new () {
				Name = @"${Name}.ascx",
				Body = @"
﻿<%@ Control Language=""C#"" Inherits=""System.Web.Mvc.ViewUserControl"" %>


		",
			},
		},
	};
	static readonly NewFileTemplate AspAspMvcViewMasterPage = new () {
		Id = "AspMvcViewMasterPage",
		Name = @"ASP.NET MVC Master Page",
		Description = @"Creates an ASP.NET MVC Master Page.",
		Language = @"C#",
		IconId = @"md-html-file-icon",
		DefaultFilename = @"",
		Root = "C#",
		ProjectType = "AspNetApp",
		Files = {
			new () {
				Name = @"${Name}.master",
				Body = @"
				<%@ Master Language=""C#"" Inherits=""System.Web.Mvc.ViewMasterPage"" %>
<!DOCTYPE html>
<html>
<head runat=""server"">
	<asp:ContentPlaceHolder ID=""head"" runat=""server"">
		<title></title>
	</asp:ContentPlaceHolder>
</head>
<body>
	<asp:ContentPlaceHolder ID=""MainContent"" runat=""server"">
		
	</asp:ContentPlaceHolder>
</body>
</html>

		",
			},
		},
	};
	static readonly NewFileTemplate AspAspMvcWebConfigViews = new () {
		Id = "AspMvcWebConfigViews",
		Name = @"ASP.NET MVC Views Configuration",
		Description = @"Creates an ASP.NET MVC Web.config file for the Views directory.",
		Language = @"C#",
		IconId = @"md-html-file-icon",
		DefaultFilename = @"Web.config",
		FixedFilename = true,
		Root = "C#",
		ProjectType = "AspNetApp",
		Files = {
			new () {
				Name = @"Web.config",
				Body = @"
<?xml version=""1.0""?>

<configuration>
	<configSections>
		<sectionGroup name=""system.web.webPages.razor"" type=""System.Web.WebPages.Razor.Configuration.RazorWebSectionGroup, System.Web.WebPages.Razor, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31BF3856AD364E35"">
			<section name=""host"" type=""System.Web.WebPages.Razor.Configuration.HostSection, System.Web.WebPages.Razor, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31BF3856AD364E35"" requirePermission=""false"" />
			<section name=""pages"" type=""System.Web.WebPages.Razor.Configuration.RazorPagesSection, System.Web.WebPages.Razor, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31BF3856AD364E35"" requirePermission=""false"" />
		</sectionGroup>
	</configSections>

	<system.web.webPages.razor>
		<host factoryType=""System.Web.Mvc.MvcWebRazorHostFactory, System.Web.Mvc, Version=5.2.3.0, Culture=neutral, PublicKeyToken=31BF3856AD364E35"" />
		<pages pageBaseType=""System.Web.Mvc.WebViewPage"">
			<namespaces>
				<add namespace=""System.Web.Mvc"" />
				<add namespace=""System.Web.Mvc.Ajax"" />
				<add namespace=""System.Web.Mvc.Html"" />
				<add namespace=""System.Web.Routing"" />
				<add namespace=""${RootNamespace}"" />
			</namespaces>
		</pages>
	</system.web.webPages.razor>

	<appSettings>
		<add key=""webpages:Enabled"" value=""false"" />
	</appSettings>

	<system.webServer>
		<handlers>
			<remove name=""BlockViewHandler""/>
			<add name=""BlockViewHandler"" path=""*"" verb=""*"" preCondition=""integratedMode"" type=""System.Web.HttpNotFoundHandler"" />
		</handlers>
	</system.webServer>
</configuration>
		",
			},
		},
	};
	static readonly NewFileTemplate AspAspMvcViewPageRazor = new () {
		Id = "AspMvcViewPageRazor",
		Name = @"ASP.NET MVC View Page (Razor)",
		Description = @"Creates an MVC Razor View page.",
		Language = @"C#",
		IconId = @"md-html-file-icon",
		DefaultFilename = @"",
		Root = "C#",
		ProjectType = "AspNetApp",
		Files = {
			new () {
				Name = @"${Name}.cshtml",
				Body = @"﻿@{
    Layout = null;
}

<!DOCTYPE html>

<html>
<head>
    <title></title>
</head>
<body>
    <div>
    
    </div>
</body>
</html>
",
			},
		},
	};
	static readonly NewFileTemplate AspAspMvcLayoutPage = new () {
		Id = "AspMvcLayoutPage",
		Name = @"ASP.NET MVC Layout Page (Razor)",
		Description = @"Creates an MVC Razor Layout page.",
		Language = @"C#",
		IconId = @"md-html-file-icon",
		DefaultFilename = @"",
		Root = "C#",
		ProjectType = "AspNetApp",
		Files = {
			new () {
				Name = @"${Name}.cshtml",
				Body = @"﻿<!DOCTYPE html>
<html>
<head>
    <title>@ViewBag.Title</title>
    <meta charset=""utf-8"" />
</head>
<body>
	@RenderBody()
</body>
</html>
",
			},
		},
	};
	static readonly NewFileTemplate AspAspMvcPartialViewPageRazor = new () {
		Id = "AspMvcPartialViewPageRazor",
		Name = @"ASP.NET MVC Partial View Page (Razor)",
		Description = @"Creates an MVC Razor Partial View page.",
		Language = @"C#",
		IconId = @"md-html-file-icon",
		DefaultFilename = @"",
		Root = "C#",
		ProjectType = "AspNetApp",
		Files = {
			new () {
				Name = @"${Name}.cshtml",
				Body = @"",
			},
		},
	};
	static readonly NewFileTemplate AspPreprocessedRazorTemplate = new () {
		Id = "PreprocessedRazorTemplate",
		Name = @"Preprocessed Razor Template",
		Description = @"A Razor template that will be preprocessed into a C# class.",
		Language = @"",
		IconId = @"md-file-source",
		DefaultFilename = @"Template",
		Root = "C#",
		Files = {
			new () {
				Name = @"${Name}.cshtml",
				Body = @"",
			},
		},
	};
	static readonly NewFileTemplate AspEmptyCSSFile = new () {
		Id = "EmptyCSSFile",
		Name = @"Empty CSS File",
		Description = @"Creates an empty CSS file.",
		Language = @"",
		IconId = @"md-file-css",
		DefaultFilename = @"EmptyCSSFile",
		Root = "C#",
		Files = {
			new () {
				DefaultExtension = @".css",
				Body = @"﻿body {
}
",
			},
		},
	};
	static readonly NewFileTemplate AspEmptyLESSFile = new () {
		Id = "EmptyLESSFile",
		Name = @"LESS Style Sheet",
		Description = @"LESS is a language that compiles into CSS.",
		Language = @"",
		IconId = @"md-file-less",
		DefaultFilename = @"EmptyLESSFile",
		Root = "C#",
		Files = {
			new () {
				DefaultExtension = @".less",
				Body = @"body {
}
",
			},
		},
	};
	static readonly NewFileTemplate AspEmptySCSSFile = new () {
		Id = "EmptySCSSFile",
		Name = @"SCSS Style Sheet (SASS)",
		Description = @"SCSS is a language that compiles into CSS.",
		Language = @"",
		IconId = @"md-file-scss",
		DefaultFilename = @"EmptySCSSFile",
		Root = "C#",
		Files = {
			new () {
				DefaultExtension = @".scss",
				Body = @"body {
}
",
			},
		},
	};
	static readonly NewFileTemplate AspEmptyJSONFile = new () {
		Id = "EmptyJSONFile",
		Name = @"Empty JSON File",
		Description = @"Creates an empty JSON file.",
		Language = @"",
		IconId = @"md-file-source",
		DefaultFilename = @"EmptyJSONFile",
		Root = "C#",
		Files = {
			new () {
				DefaultExtension = @".json",
				Body = @"{

}
",
			},
		},
	};
	static readonly NewFileTemplate AspEmptyJavaScriptFile = new () {
		Id = "EmptyJavaScriptFile",
		Name = @"JavaScript file",
		Description = @"A blank JavaScript source file.",
		Language = @"",
		IconId = @"md-file-js",
		DefaultFilename = @"EmptyJSFile",
		Root = "C#",
		Files = {
			new () {
				DefaultExtension = @".js",
				Body = @"",
			},
		},
	};
	static readonly NewFileTemplate AspEmptyTypeScriptFile = new () {
		Id = "EmptyTypeScriptFile",
		Name = @"TypeScript file",
		Description = @"A blank TypeScript source file.",
		Language = @"",
		IconId = @"md-file-ts",
		DefaultFilename = @"EmptyTSFile",
		Root = "C#",
		Files = {
			new () {
				DefaultExtension = @".ts",
				Body = @"",
			},
		},
	};
	static readonly NewFileTemplate AspnpmConfigurationFile = new () {
		Id = "npmConfigurationFile",
		Name = @"npm Configuration File",
		Description = @"Creates an npm Configuration File for package.json.",
		Language = @"",
		IconId = @"md-file-source",
		DefaultFilename = @"package",
		Root = "C#",
		Files = {
			new () {
				DefaultExtension = @".json",
				Body = @"{
  ""version"": ""1.0.0"",
  ""name"": ""asp.net"",
  ""private"": true,
  ""devDependencies"": {
  }
}
",
			},
		},
	};

	// ===== ASP.NET Core (13 templates, condición AspNetCore + minVersion) =====
	static readonly NewFileTemplate CoreAspNetCoreMiddlewareClass = new () {
		Id = "AspNetCoreMiddlewareClass",
		Name = @"Middleware Class",
		Description = @"Creates a class to write your middleware.",
		Language = @"C#",
		IconId = @"md-html-file-icon",
		DefaultFilename = @"Middleware",
		Root = "C#",
		Core = true,
		Files = {
			new () {
				Name = @"${Name}.cs",
				Body = @"﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace ${Namespace}
{
    // You may need to install the Microsoft.AspNetCore.Http.Abstractions package into your project
    public class ${EscapedIdentifier}
    {
        private readonly RequestDelegate _next;

        public ${EscapedIdentifier}(RequestDelegate next)
        {
            _next = next;
        }

        public Task Invoke(HttpContext httpContext)
        {

            return _next(httpContext);
        }
    }

    // Extension method used to add the middleware to the HTTP request pipeline.
    public static class ${EscapedIdentifier}Extensions
    {
        public static IApplicationBuilder UseMiddlewareClassTemplate(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<${EscapedIdentifier}>();
        }
    }
}
",
			},
		},
	};
	static readonly NewFileTemplate CoreAspNetCoreMvcController = new () {
		Id = "AspNetCoreMvcController",
		Name = @"Controller Class",
		Description = @"Creates an MVC Application Controller class.

The ASP.NET MVC Framework requires Controller names to be suffixed with ""Controller"".",
		Language = @"C#",
		IconId = @"md-html-file-icon",
		DefaultFilename = @"HomeController",
		Root = "C#",
		Core = true,
		Files = {
			new () {
				Name = @"${Name}.cs",
				Body = @"﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace ${Namespace}
{
    public class ${EscapedIdentifier} : Controller
    {
        // GET: /<controller>/
        public IActionResult Index()
        {
            return View();
        }
    }
}
",
			},
		},
	};
	static readonly NewFileTemplate CoreAspNetCoreRazorTagHelper = new () {
		Id = "AspNetCoreRazorTagHelper",
		Name = @"Razor Tag Helper",
		Description = @"Creates a class to write a Tag Helper.",
		Language = @"C#",
		IconId = @"md-html-file-icon",
		DefaultFilename = @"RazorTagHelper",
		Root = "C#",
		Core = true,
		Files = {
			new () {
				Name = @"${Name}.cs",
				Body = @"﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Runtime.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace ${Namespace}
{
    // You may need to install the Microsoft.AspNetCore.Razor.Runtime package into your project
    [HtmlTargetElement(""tag-name"")]
    public class ${EscapedIdentifier} : TagHelper
    {
        public override void Process(TagHelperContext context, TagHelperOutput output)
        {

        }
    }
}
",
			},
		},
	};
	static readonly NewFileTemplate CoreAspNetCoreStartupClass = new () {
		Id = "AspNetCoreStartupClass",
		Name = @"Startup Class",
		Description = @"Creates a class to configure your application.",
		Language = @"C#",
		IconId = @"md-html-file-icon",
		DefaultFilename = @"Startup",
		Root = "C#",
		Core = true,
		Files = {
			new () {
				Name = @"${Name}.cs",
				Body = @"﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;

namespace ${Namespace}
{
    public class ${EscapedIdentifier}
    {
        public void Configure(IApplicationBuilder app)
        {
            // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        }
    }
}
",
			},
		},
	};
	static readonly NewFileTemplate CoreAspNetCoreWebApiController = new () {
		Id = "AspNetCoreWebApiController",
		Name = @"Web API Controller Class",
		Description = @"Creates a Web API Controller.",
		Language = @"C#",
		IconId = @"md-html-file-icon",
		DefaultFilename = @"ValuesController",
		Root = "C#",
		Core = true,
		Files = {
			new () {
				Name = @"${Name}.cs",
				Body = @"﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace ${Namespace}
{
    [Route(""api/[controller]"")]
    public class ${EscapedIdentifier} : Controller
    {
        // GET: api/values
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { ""value1"", ""value2"" };
        }

        // GET api/values/5
        [HttpGet(""{id}"")]
        public string Get(int id)
        {
            return ""value"";
        }

        // POST api/values
        [HttpPost]
        public void Post([FromBody]string value)
        {
        }

        // PUT api/values/5
        [HttpPut(""{id}"")]
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/values/5
        [HttpDelete(""{id}"")]
        public void Delete(int id)
        {
        }
    }
}
",
			},
		},
	};
	static readonly NewFileTemplate CoreAspNetCoreRazorComponent = new () {
		Id = "AspNetCoreRazorComponent",
		Name = @"Razor Component",
		Description = @"Creates a reusable UI component implemented with Razor.",
		Language = @"C#",
		IconId = @"md-html-file-icon",
		DefaultFilename = @"Index",
		Root = "C#",
		Core = true,
		CoreMinVersion = new System.Version(3, 0),
		Files = {
			new () {
				Name = @"${Name}.razor",
				Body = @"﻿<h3>Component1</h3>

@code {

}
",
			},
		},
	};
	static readonly NewFileTemplate CoreAspNetCoreMVCViewImportsPage = new () {
		Id = "AspNetCoreMVCViewImportsPage",
		Name = @"Razor View Imports",
		Description = @"Creates a Razor View Import Page.",
		Language = @"C#",
		IconId = @"md-html-file-icon",
		DefaultFilename = @"_ViewImports",
		Root = "C#",
		Core = true,
		Files = {
			new () {
				Name = @"${Name}.cshtml",
				Body = @"﻿@namespace ${Namespace}
@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers",
			},
		},
	};
	static readonly NewFileTemplate CoreAspNetCoreMVCViewLayoutPage = new () {
		Id = "AspNetCoreMVCViewLayoutPage",
		Name = @"Razor Layout",
		Description = @"Creates a Razor View Layout page.",
		Language = @"C#",
		IconId = @"md-html-file-icon",
		DefaultFilename = @"_Layout",
		Root = "C#",
		Core = true,
		Files = {
			new () {
				Name = @"${Name}.cshtml",
				Body = @"﻿<!DOCTYPE html>

<html>
<head>
    <meta name=""viewport"" content=""width=device-width"" />
    <title>@ViewBag.Title</title>
</head>
<body>
    <div>
        @RenderBody()
    </div>
</body>
</html>
",
			},
		},
	};
	static readonly NewFileTemplate CoreAspNetCoreMVCViewPage = new () {
		Id = "AspNetCoreMVCViewPage",
		Name = @"Razor View",
		Description = @"Creates a Razor View Page.",
		Language = @"C#",
		IconId = @"md-html-file-icon",
		DefaultFilename = @"Index",
		Root = "C#",
		Core = true,
		Files = {
			new () {
				Name = @"${Name}.cshtml",
				Body = @"﻿@*
    For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860
*@
@{
}
",
			},
		},
	};
	static readonly NewFileTemplate CoreAspNetCoreMVCViewStartPage = new () {
		Id = "AspNetCoreMVCViewStartPage",
		Name = @"Razor View Start",
		Description = @"Creates a Razor View Start Page.",
		Language = @"C#",
		IconId = @"md-html-file-icon",
		DefaultFilename = @"_ViewStart",
		Root = "C#",
		Core = true,
		Files = {
			new () {
				Name = @"${Name}.cshtml",
				Body = @"﻿@{
    Layout = ""_Layout"";
}
",
			},
		},
	};
	static readonly NewFileTemplate CoreAspNetCoreRazorPage = new () {
		Id = "AspNetCoreRazorPage",
		Name = @"Razor Page",
		Description = @"Creates a Razor page without a page model.",
		Language = @"C#",
		IconId = @"md-html-file-icon",
		DefaultFilename = @"Index",
		Root = "C#",
		Core = true,
		CoreMinVersion = new System.Version(2, 0),
		Files = {
			new () {
				Name = @"${Name}.cshtml",
				Body = @"﻿@page
@{
}
",
			},
		},
	};
	static readonly NewFileTemplate CoreAspNetCoreRazorPageWithPageModel = new () {
		Id = "AspNetCoreRazorPageWithPageModel",
		Name = @"Razor Page (with page model)",
		Description = @"Creates a Razor page with a page model.",
		Language = @"C#",
		IconId = @"md-html-file-icon",
		DefaultFilename = @"Index",
		Root = "C#",
		Core = true,
		CoreMinVersion = new System.Version(2, 0),
		Files = {
			new () {
				Name = @"${Name}.cshtml",
				Body = @"﻿@page
@model ${Namespace}.${EscapedIdentifier}Model
@{
}
",
			},
			new () {
				Name = @"${Name}.cshtml.cs",
				Body = @"using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ${Namespace}
{
	public class ${EscapedIdentifier}Model : PageModel
    {
        public void OnGet()
        {
        }
    }
}",
			},
		},
	};
	static readonly NewFileTemplate CoreAspNetCoreAppSettingsFile = new () {
		Id = "AspNetCoreAppSettingsFile",
		Name = @"App Settings File",
		Description = @"An appsettings.json file used to configure Web Applications.",
		Language = @"",
		IconId = @"md-file-source",
		DefaultFilename = @"appsettings",
		Root = "C#",
		Files = {
			new () {
				DefaultExtension = @".json",
				Body = @"{
  ""ConnectionStrings"": {
    ""DefaultConnection"": ""DataSource=app.db""
  },
  ""Logging"": {
    ""LogLevel"": {
      ""Default"": ""Warning""
    }
  },
  ""AllowedHosts"": ""*""
}",
			},
		},
	};

	// ===== VB (2 templates, gate LanguageName=VBNet) =====
	static readonly NewFileTemplate VbEmptyVBFile = new () {
		Id = "EmptyVBFile",
		Name = @"Empty File",
		Description = @"Creates an empty VB.Net file.",
		Language = @"VBNet",
		IconId = @"md-file-source",
		DefaultFilename = @"EmptyVBFile",
		Root = "VB",
		Files = {
			new () {
				DefaultExtension = @".vb",
				Body = @"
        ",
			},
		},
	};
	static readonly NewFileTemplate VbVBAssemblyInfo = new () {
		Id = "VBAssemblyInfo",
		Name = @"Assembly Information",
		Description = @"A file defining assembly information attributes.",
		Language = @"VBNet",
		IconId = @"md-file-information",
		DefaultFilename = @"AssemblyInfo.vb",
		FixedFilename = true,
		Root = "VB",
		Files = {
			new () {
				Name = @"AssemblyInfo.vb",
				Body = @"
			Imports System.Reflection
Imports System.Runtime.CompilerServices

' Information about this assembly is defined by the following attributes. 
' Change them to the values specific to your project.

<assembly: AssemblyTitle(""${ProjectName}"")>
<assembly: AssemblyDescription("""")>
<assembly: AssemblyConfiguration("""")>
<assembly: AssemblyCompany("""")>
<assembly: AssemblyProduct("""")>
<assembly: AssemblyCopyright("""")>
<assembly: AssemblyTrademark("""")>
<assembly: AssemblyCulture("""")>

' The assembly version has the format ""{Major}.{Minor}.{Build}.{Revision}"".
' If the build and revision are set to '*' they will be updated automatically.

<assembly: AssemblyVersion(""1.0.0.0"")>

' The following attributes are used to specify the signing key for the assembly, 
' if desired. See the Mono documentation for more information about signing.

<assembly: AssemblyDelaySign(false)>
<assembly: AssemblyKeyFile("""")>
",
			},
		},
	};

	// ===== arrays de templates por categoría (RebuildCatalog compone las raíces) =====

	// AspNet: cat "ASP.NET Web Forms" (14 templates)
	static readonly NewFileTemplate[] AspAspNetCatWebForms =
	{
		AspWebForm_Empty,
		AspWebForm_CodeBehind,
		AspWebHandler_Empty,
		AspWebHandler_CodeBehind,
		AspWebControl_Empty,
		AspWebControl_CodeBehind,
		AspWebService_Empty,
		AspWebService_CodeBehind,
		AspMasterPage_Empty,
		AspMasterPage_CodeBehind,
		AspNestedMasterPage,
		AspNestedMasterPage_CodeBehind,
		AspWebContentForm_CodeBehind,
		AspWebContentForm,
	};

	// AspNet: cat "ASP.NET" (3 templates)
	static readonly NewFileTemplate[] AspAspNetCat =
	{
		AspGlobalAsax,
		AspWebConfig_Application,
		AspWebConfig_SubDir,
	};

	// AspNet: cat "ASP.NET MVC" (9 templates)
	static readonly NewFileTemplate[] AspAspNetCatMVC =
	{
		AspAspMvcController,
		AspAspMvcViewPage,
		AspAspMvcViewContentPage,
		AspAspMvcUserControl,
		AspAspMvcViewMasterPage,
		AspAspMvcWebConfigViews,
		AspAspMvcViewPageRazor,
		AspAspMvcLayoutPage,
		AspAspMvcPartialViewPageRazor,
	};

	// AspNet: cat "Text Templating" (1 templates)
	static readonly NewFileTemplate[] AspTextTemplating =
	{
		AspPreprocessedRazorTemplate,
	};

	// AspNet: cat "Web" (7 templates)
	static readonly NewFileTemplate[] AspWeb =
	{
		AspEmptyCSSFile,
		AspEmptyLESSFile,
		AspEmptySCSSFile,
		AspEmptyJSONFile,
		AspEmptyJavaScriptFile,
		AspEmptyTypeScriptFile,
		AspnpmConfigurationFile,
	};

	// AspNetCore: cat "ASP.NET Core" (13 templates)
	static readonly NewFileTemplate[] CoreAspNetCoreCat =
	{
		CoreAspNetCoreMiddlewareClass,
		CoreAspNetCoreMvcController,
		CoreAspNetCoreRazorTagHelper,
		CoreAspNetCoreStartupClass,
		CoreAspNetCoreWebApiController,
		CoreAspNetCoreRazorComponent,
		CoreAspNetCoreMVCViewImportsPage,
		CoreAspNetCoreMVCViewLayoutPage,
		CoreAspNetCoreMVCViewPage,
		CoreAspNetCoreMVCViewStartPage,
		CoreAspNetCoreRazorPage,
		CoreAspNetCoreRazorPageWithPageModel,
		CoreAspNetCoreAppSettingsFile,
	};

	// VB: cat "General"
	static readonly NewFileTemplate[] VbGeneral =
	{
		VbEmptyVBFile,
	};

	// VB: cat "Misc"
	static readonly NewFileTemplate[] VbMisc =
	{
		VbVBAssemblyInfo,
	};
}
