using Music.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace GuitarHub
{
    public static class ScaleEnumerator
    {
        private static Type[] scaleTypes;

        public static Type[] ScaleTypes => scaleTypes ?? (scaleTypes = GetScaleTypes());

        private static Type[] GetScaleTypes()
        {
            var types = new List<Type>();
            foreach (Type type in
                Assembly.GetAssembly(typeof(ScaleBase))
                        .GetTypes()
                        .Where(myType => myType.IsClass && !myType.IsAbstract && myType.IsSubclassOf(typeof(ScaleBase))))
            {
                types.Add(type);
            }
            return types.ToArray();
        }
    }
}
