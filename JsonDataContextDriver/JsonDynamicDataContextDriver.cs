using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web;
using System.Windows.Input;
using System.Xml.Linq;
using JsonDataContext;
using JsonDataContextDriver.Notepad;
using LINQPad.Extensibility.DataContext;
using Microsoft.CSharp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xamasoft.JsonClassGenerator;
using MessageBox = System.Windows.MessageBox;

namespace JsonDataContextDriver
{
    //public class StaticDriver : StaticDataContextDriver
    //{
    //    static StaticDriver()
    //    {
    //        // Uncomment the following code to attach to Visual Studio's debugger when an exception is thrown:
    //        //AppDomain.CurrentDomain.FirstChanceException += (sender, args) =>
    //        //{
    //        //	if (args.Exception.StackTrace.Contains ("JsonDataContextDriver"))
    //        //		Debugger.Launch ();
    //        //};
    //    }

    //    public override string Name => "(Name for your driver)";

    //    public override string Author => "(Your name)";

    //    public override string GetConnectionDescription(IConnectionInfo cxInfo)
    //        => "(Description for this connection)";

    //    public override bool ShowConnectionDialog(IConnectionInfo cxInfo, ConnectionDialogOptions dialogOptions)
    //        => new ConnectionDialog(cxInfo).ShowDialog() == true;

    //    public override List<ExplorerItem> GetSchema(IConnectionInfo cxInfo, Type customType)
    //    {
    //        // TODO - implement
    //        return new ExplorerItem[0].ToList();
    //    }

    public class JsonDynamicDataContextDriver : DynamicDataContextDriver
    {
        static JsonDynamicDataContextDriver()
        {
            // Uncomment the following code to attach to Visual Studio's debugger when an exception is thrown:
            AppDomain.CurrentDomain.FirstChanceException += (sender, args) =>
            {
        	    if (args.Exception.StackTrace.Contains ("JsonDataContextDriver"))
                    System.Diagnostics.Debugger.Launch ();
            };
        }


        public override string Name
        {
            get { return "JSON DataContext Provider!"; }
        }

        public override string Author
        {
            get { return "Ryan Davis"; }
        }

        public override string GetConnectionDescription(IConnectionInfo cxInfo)
        {
            return String.IsNullOrWhiteSpace(cxInfo.DisplayName) ? "Unnamed JSON Data Context" : cxInfo.DisplayName;
        }

        public override bool ShowConnectionDialog(IConnectionInfo cxInfo, ConnectionDialogOptions dialogOptions)
        {
            var dbg = typeof(Xceed.Wpf.Toolkit.DropDownButton).FullName;
       
            try
            {
                return internalShowConnectionDialog(cxInfo, dialogOptions);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.ToString());
                System.Diagnostics.Debugger.Launch();
            }
            return false;
        }
        private bool internalShowConnectionDialog(IConnectionInfo cxInfo, ConnectionDialogOptions dialogOptions)
        {

            var dialog = new ConnectionDialog();
            dialog.SetContext(cxInfo, dialogOptions.IsNewConnection);

            var result = dialog.ShowDialog();
            return result == true;
        }


        //[Obsolete]
        //public override bool ShowConnectionDialog(IConnectionInfo cxInfo, bool isNewConnection)
        //{
        //    System.Diagnostics.Debugger.Launch();

        //    //var dialog = new ConnectionDialog();
        //    //dialog.SetContext(cxInfo, isNewConnection);

        //    //var result = dialog.ShowDialog();
        //    //return result == true;
        //    return false;
        //}

        public override IEnumerable<string> GetAssembliesToAdd(IConnectionInfo cxInfo)
        {
            return base.GetAssembliesToAdd(cxInfo)
                .Concat(new[] {typeof (JsonDataContextBase).Assembly.Location, typeof(HttpUtility).Assembly.Location});
        }

        public override IEnumerable<string> GetNamespacesToAdd(IConnectionInfo cxInfo)
        {
            return base.GetNamespacesToAdd(cxInfo)
                .Concat(_nameSpacesToAdd)
                .Distinct();
        }

        private List<string> _nameSpacesToAdd = new List<string>();


        public override List<ExplorerItem> GetSchemaAndBuildAssembly(IConnectionInfo cxInfo,
            AssemblyName assemblyToBuild, ref string nameSpace,
            ref string typeName)
        {
            try
            {
                return internalGetSchemaAndBuildAssembly(cxInfo, assemblyToBuild, ref nameSpace, ref typeName);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.ToString());
                System.Diagnostics.Debugger.Launch();
           
                return null;
            }
        }

        internal List<ExplorerItem> internalGetSchemaAndBuildAssembly(IConnectionInfo cxInfo,
            AssemblyName assemblyToBuild, ref string nameSpace,
            ref string typeName)
        {
            _nameSpacesToAdd = new List<string>();
            
            var xInputs = cxInfo.DriverData.Element("inputDefs");
            if (xInputs == null)
                return new List<ExplorerItem>();

            var jss = new JsonSerializerSettings {TypeNameHandling = TypeNameHandling.All};
            var inputDefs = JsonConvert.DeserializeObject<List<IJsonInput>>(xInputs.Value, jss).ToList();

            var ns = nameSpace;

            // generate class definitions
            var classDefinitions =
                inputDefs
                    .AsParallel()
                    .SelectMany(i =>
                    {
                        i.GenerateClasses(ns);
                        return i.GeneratedClasses;
                    })
                    .ToList();

            // add namespaces
            _nameSpacesToAdd.AddRange(inputDefs.SelectMany(i=>i.NamespacesToAdd));
            _nameSpacesToAdd.AddRange(classDefinitions.Select(c=> c.Namespace));

            // remove the error'd inputs
            var classGenErrors = inputDefs.SelectMany(i => i.Errors).ToList();

            classDefinitions =
                classDefinitions
                    .Where(c => c.Success)
                    .ToList();

            // resolve duplicates
            classDefinitions
                .GroupBy(c => c.ClassName)
                .Where(c => c.Count() > 1)
                .SelectMany(cs => cs.Select((c, i) => new {Class = c, Index = i + 1}).Skip(1))
                .ToList()
                .ForEach(c => c.Class.ClassName += "_" + c.Index);

            if (String.IsNullOrEmpty(cxInfo.DisplayName) && classDefinitions.Count == 1)
                cxInfo.DisplayName = classDefinitions.Single().ClassName.Replace("_", " ");

            // create code to compile
            var usings = "using System;\r\n" +
                         "using System.Collections.Generic;\r\n" +
                         "using System.IO;\r\n" +
                         "using Newtonsoft.Json;\r\n" +
                         "using System.Web;\r\n" +
                         "using JsonDataContext;\r\n";

            usings += String.Join("\r\n", classDefinitions.Select(c => String.Format("using {0};", c.Namespace)));

            var contextProperties =
                inputDefs.SelectMany(i => i.ContextProperties);

            var context =
                String.Format("namespace {1} {{\r\n\r\n public class {2} : JsonDataContextBase {{\r\n\r\n\t\t{0}\r\n\r\n}}\r\n\r\n}}",
                    String.Join("\r\n\r\n\t\t", contextProperties), nameSpace, typeName);
            var code = String.Join("\r\n", classDefinitions.Select(c => c.ClassDefinition));

            var contextWithCode = String.Join("\r\n\r\n", usings, context, code);

            var referencedAssemblies = GetCoreFxReferenceAssemblies(cxInfo)            
            .Concat(new[]
            {
                typeof(Newtonsoft.Json.JsonArrayAttribute).Assembly.Location,
                typeof(JsonDataContext.JsonDataContextBase).Assembly.Location
            }
            ).ToArray();
            var result = CompileSource(new CompilationInput()
            {
                FilePathsToReference = referencedAssemblies,
                OutputPath = assemblyToBuild.CodeBase,
                SourceCode = new string[] { contextWithCode }
            });

         

            if (!result.Errors.Any())
            {
                // Pray to the gods of UX for redemption..
                // We Can Do Better
                if (classGenErrors.Any())
                    MessageBox.Show(String.Format("Couldn't process {0} inputs:\r\n{1}", classGenErrors.Count,
                        String.Join(Environment.NewLine, classGenErrors)));

                var myType = DataContextDriver.LoadAssemblySafely(assemblyToBuild.CodeBase).GetType(String.Format("{0}.{1}", nameSpace, typeName));

                var res = LinqPadSampleCode.GetSchema(myType)
                    .Concat(inputDefs.SelectMany(i=>i.ExplorerItems??new List<ExplorerItem>()))
                    .ToList();



                return res;
               
            }
            else
            {
                // compile failed, this is Bad
                var sb = new StringBuilder();
                sb.AppendLine("Could not generate a typed context for the given inputs. The compiler returned the following errors:\r\n");

                foreach (var err in result.Errors)
                    sb.AppendFormat(" - {0}\r\n", err);

                if (classGenErrors.Any())
                {
                    sb.AppendLine("\r\nThis may have been caused by the following class generation errors:\r\n");
                    sb.AppendLine(String.Join(Environment.NewLine, String.Join(Environment.NewLine, classGenErrors)));
                }

                MessageBox.Show(sb.ToString());

                NotepadHelper.ShowMessage(contextWithCode, "Generated source code");

                throw new Exception("Could not generate a typed context for the given inputs");
            }
        }

        public override void InitializeContext(IConnectionInfo cxInfo, object context, QueryExecutionManager executionManager)
        {
            
            base.InitializeContext(cxInfo, context, executionManager);
            
            var ctx = (JsonDataContextBase) context;

            var xInputs = cxInfo.DriverData.Element("inputDefs");
            if (xInputs == null)
                return;

            var jss = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };
            var inputs = JsonConvert.DeserializeObject<List<IJsonInput>>(xInputs.Value, jss).ToList();

            inputs
                .OfType<JsonTextInput>()
                .ToList()
                .ForEach(c=> ctx._jsonTextInputs.Add(c.InputGuid, c.Json));
        }
    }
}