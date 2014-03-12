using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Web;
using System.Web.Optimization;
using System.Linq;

namespace ClrModelBundling.Extensions
{
    /// <summary>
    /// Serializes CLR modles into default JSON objects that can be referenced from a <script> tag.
    /// </summary>
    public class ModelBundle: ScriptBundle //Extending from ScriptBundle gives us the minification transform.
    {
        private List<Type> _modelTypes;
        private string _virtualPath;

        /// <summary>
        /// Initializes a new instance of a ModelBunle.
        /// </summary>
        /// <param name="virtualPath">The path that will be used for resolving/referencing sripts from a client.</param>
        public ModelBundle(string virtualPath)
            : base(virtualPath)
        {
            _virtualPath = virtualPath;
            _modelTypes = new List<Type>();
        }

        /// <summary>
        /// Initializes a new instance of a ModelBunle.
        /// </summary>
        /// <param name="virtualPath">The path that will be used for resolving/referencing sripts from a client.</param>
        /// <param name="modelTypes">A list of CLR types to be converted to JSON and made accessible to clients as a JavaScript resource.</param>
        public ModelBundle(string virtualPath, List<Type> modelTypes):base (virtualPath)
        {
            _modelTypes = modelTypes;
            _virtualPath = virtualPath;
        }

        /// <summary>
        /// Specifies the default namespace under which CLR models will be scoped when turned into JavaScript.
        /// </summary>
        public string JsNamespace { get; set; }

        /// <summary>
        /// Using the Newtonsoft.Json library for serialization, additional settings can be used to affect JSON output.
        /// </summary>
        public JsonSerializerSettings SerializationSettings { get; set; }

        /// <summary>
        /// Get all classes in the designated namespace from the designated assembly. 
        /// </summary>
        /// <param name="assembly">The Assembly to search in.</param>
        /// <param name="nameSpace">The Namespace of the classes to include.</param>
        public void Include(Assembly assembly, string nameSpace)
        {
            var typesInNameSpace = assembly.GetTypes().Where(x => string.Equals(x.Namespace, nameSpace, StringComparison.Ordinal));

            if (typesInNameSpace.Any())
            {
                _modelTypes.AddRange(typesInNameSpace);
            }
        }

        public override BundleResponse GenerateBundleResponse(BundleContext context)
        {
            BundleResponse response = new BundleResponse();
            StringBuilder contentResponse = new StringBuilder();

            //Initialize JS namespace if configured
            if(!string.IsNullOrWhiteSpace(JsNamespace))
            {
                var nsInitFormat = "var {0} = {0} || {{}};";
                contentResponse.AppendLine(string.Format(nsInitFormat, JsNamespace));
            }

            foreach(Type model in _modelTypes)
            {
                var modelName = model.Name;
                string modelJsonObj = null;

                try
                {
                    if (SerializationSettings == null)
                    {
                        modelJsonObj = JsonConvert.SerializeObject(Activator.CreateInstance(model));
                    }
                    else
                    {
                        modelJsonObj = JsonConvert.SerializeObject(Activator.CreateInstance(model), SerializationSettings);
                    }
                }
                catch (Exception ex)
                {
                    //TODO: handle serialization exceptions.
                }

                if (modelJsonObj == null) continue;

                string modelDef;
                if(!string.IsNullOrWhiteSpace(JsNamespace))
                {
                    modelDef = string.Format("{0}.{1} = {2};", JsNamespace, modelName, modelJsonObj);
                }
                else
                {
                    modelDef = string.Format("var {0} = {1};", modelName, modelJsonObj);
                }

                contentResponse.AppendLine(modelDef);
            }

            response.ContentType = "text/javascript";
            response.Content = contentResponse.ToString();
            response.Files = new List<BundleFile>();
            response.Cacheability = HttpCacheability.Private;

            return response;
        }
    }
}