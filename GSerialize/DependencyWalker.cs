/*
 * Copyright 2020, Guan Xiaopeng
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */
 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace GSerialize
{
    public sealed class DependencyWalker
    {
        public delegate bool TypeFilter(Type type);

        public static List<Type> FindType(Assembly assembly, TypeFilter filter)
        {
            var set = new HashSet<Assembly>();
            GetDepencyAssemblies(assembly, set);
            var referencedAssemblies = new List<Assembly>(set.ToList());
            return CollectSatisfiedType(referencedAssemblies, filter);
        }

        public static List<Assembly> GetReferencedAssemblies(Assembly assembly)
        {
            var set = new HashSet<Assembly>();
            GetDepencyAssemblies(assembly, set);
            return new List<Assembly>(set.ToList());
        }

        public static List<Assembly> GetReferencedAssemblies(Assembly[] assemblies)
        {
            var set = new HashSet<Assembly>();
            foreach(var assembly in assemblies)
            {
                GetDepencyAssemblies(assembly, set);
            }
            return new List<Assembly>(set.ToList());
        }

        public static void GetDepencyAssemblies(Assembly assembly, HashSet<Assembly> assemblies)
        {
            if (!assemblies.Contains(assembly))
            {
                assemblies.Add(assembly);
                foreach(var aName in assembly.GetReferencedAssemblies())
                {
                    GetDepencyAssemblies(Assembly.Load(aName), assemblies);
                }
            }
        }

        public static List<Type> CollectSatisfiedType(List<Assembly> assemblies, TypeFilter filter)
        {
            var types = new List<Type>();
            foreach (var a in assemblies)
            {
                types.AddRange(SatisfiedTypeInAssembly(a, filter));
            }
            return types;
        }

        private static List<Type> SatisfiedTypeInAssembly(Assembly a, TypeFilter filter)
        {
            var filteredTypes = from t in a.DefinedTypes 
                where filter(t)
                select t;
            return new List<Type>(filteredTypes);
        }
    }
}