using System;
using System.Collections.Generic;
using System.Linq;
using LINQPad.Extensibility.DataContext;
// using static LINQPad.Extensibility.DataContext.DataContextDriver;

namespace JsonDataContextDriver
{
    public static class LinqPadSampleCode
    {
        // These methods taken from the Static/Universal LINQPad context driver sample 

        public static List<ExplorerItem> GetSchema(Type customType)
        {
            // Return the objects with which to populate the Schema Explorer by reflecting over customType.

            // We'll start by retrieving all the properties of the custom type that implement IEnumerable<T>:
            var topLevelProps =
            (
                from prop in customType.GetProperties()
                where prop.PropertyType != typeof(string)
                // Display all properties of type IEnumerable<T> (except for string!)
                
                let genericArgument = prop.PropertyType.GetGenericArguments().FirstOrDefault()

                where prop.PropertyType.GetInterface("IEnumerable") != null && genericArgument != null

                orderby prop.Name

                select new ExplorerItem(prop.Name, ExplorerItemKind.QueryableObject, ExplorerIcon.Table)
                {
                    IsEnumerable = true,
                    ToolTipText = prop.Name,
                    DragText = prop.Name,

                    // Store the entity type to the Tag property. We'll use it later.
                    Tag = genericArgument
                }

            ).ToList();

            // Create a lookup keying each element type to the properties of that type. This will allow
            // us to build hyperlink targets allowing the user to click between associations:
            var elementTypeLookup = topLevelProps.ToLookup(tp => (Type)tp.Tag);

            // Populate the columns (properties) of each entity:
            foreach (ExplorerItem table in topLevelProps)
            {
                Type parentType = (Type)table.Tag;
                var props = parentType.GetProperties().Select(p => GetChildItem(elementTypeLookup, p.Name, p.PropertyType));
                var fields = parentType.GetFields().Select(f => GetChildItem(elementTypeLookup, f.Name, f.FieldType));
                table.Children = props.Union(fields).OrderBy(childItem => childItem.Kind).ToList();
            }

            return topLevelProps;
        }

        private static ExplorerItem GetChildItem(ILookup<Type, ExplorerItem> elementTypeLookup, string childPropName, Type childPropType)
        {
            // If the property's type is in our list of entities, then it's a Many:1 (or 1:1) reference.
            // We'll assume it's a Many:1 (we can't reliably identify 1:1s purely from reflection).
            if (elementTypeLookup.Contains(childPropType))
                return new ExplorerItem(childPropName, ExplorerItemKind.ReferenceLink, ExplorerIcon.ManyToOne)
                {
                    HyperlinkTarget = elementTypeLookup[childPropType].First(),
                    // FormatTypeName is a helper method that returns a nicely formatted type name.
                    ToolTipText = FormatTypeName(childPropType, true),
                    DragText = childPropName
                };

            // Is the property's type a collection of entities?	 
            if (childPropType.GetInterface("IEnumerable") != null)
            {
                var elementType = childPropType.GetGenericArguments().FirstOrDefault();
                if (elementType != null && elementTypeLookup.Contains(elementType))
                {
                    return new ExplorerItem(childPropName, ExplorerItemKind.CollectionLink, ExplorerIcon.OneToMany)
                    {
                        HyperlinkTarget = elementTypeLookup[elementType].First(),
                        ToolTipText = FormatTypeName(elementType, true) ,
                        DragText = childPropName
                    };
                }
            }

            // Ordinary property:
            return new ExplorerItem(childPropName + " (" + FormatTypeName(childPropType, false) + ")",
                ExplorerItemKind.Property, ExplorerIcon.Column ) { DragText = childPropName};
        }


    }
}