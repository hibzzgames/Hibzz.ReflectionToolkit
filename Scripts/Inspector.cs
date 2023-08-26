using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Hibzz.ReflectionToolkit
{
    internal class Inspector : IList
    {
        /// <summary>
        /// A list of assemblies to display
        /// </summary>
        public List<Assembly> Assemblies = new List<Assembly>();

        /// <summary>
        /// A list of all types in the selected assembly
        /// </summary>
        public List<Type> Types = new List<Type>();

        /// <summary>
        /// A list of all member info in the selected type
        /// </summary>
        public List<MemberInfo> Members = new List<MemberInfo>();

        /// <summary>
        /// The selected assembly
        /// </summary>
        public Assembly SelectedAssembly = null;

        /// <summary>
        /// The selected type
        /// </summary>
        public Type SelectedType = null;

        // Not fixed size
        public bool IsFixedSize => false;

        // not read only
        public bool IsReadOnly => false;

        // count is on an order of priority (members -> types -> assemblies)
        public int Count 
        { 
            get 
            {
                if(Members.Count > 0)    { return Members.Count;    }
                if(Types.Count > 0)      { return Types.Count;      }
                if(Assemblies.Count > 0) { return Assemblies.Count; }
                return 0;
            } 
        }

        // no idea if the datastructure is threadsafe... so, being safe and marking it false
        public bool IsSynchronized => false;

        // same reason as above, marking it as null
        public object SyncRoot => null;

        // index operation
        public object this[int index] 
        { 
            get
            {
                if (Members.Count > 0)    { return Members[index];    }
                if (Types.Count > 0)      { return Types[index];      }
                if (Assemblies.Count > 0) { return Assemblies[index]; }

                return null; // index unavailable
            }

            // does nothing
            set { }
        }

        /// <summary>
        /// Refresh the assemblies
        /// </summary>
        public void RefreshAssemblies()
        {
            // due to the convoluted mess that I've written here with food poisoning, this must be done in order to
            // get the priority system to work (have a look at count for an example)
            Members.Clear();
            Types.Clear();

            if(Assemblies.Count > 0) { return; }
            Assemblies = System.AppDomain.CurrentDomain.GetAssemblies().ToList();
        }

        /// <summary>
        /// Select an assembly with the given name
        /// </summary>
        /// <param name="assemblyName">The name of the assembly</param>
        public void SelectAssembly(string assemblyName)
        {
            // make sure that the assembly is available
            RefreshAssemblies();
            if(!Assemblies.Exists((assembly) => assembly.GetName().Name == assemblyName))
            {
                // assembly with the given name not found
                SelectedAssembly = null;

                // TODO: error ui
                // Messages.Add($"Assembly with the given name '{assemblyName}' not found");
                return;
            }

            // load and mark it as selected assembly
            SelectedAssembly = Assembly.Load(assemblyName);
            SelectedType = null;
        }

        /// <summary>
        /// Refresh the types in the selected assembly
        /// </summary>
        public void RefreshTypes()
        {
            // same reasoning as the RefreshAssemblies
            Members.Clear();

            // make sure an assembly is selected
            if(SelectedAssembly == null)
            {
                Types.Clear();

                // TODO: error ui
                // Messages.Add($"No assembly is currently selected. Please select an assembly to explore its types.");
                return;
            }

            // put all the types in the selected assembly into a list
            Types = SelectedAssembly.GetTypes().ToList();
        }

        /// <summary>
        /// Select the type with the given name in the selected assembly
        /// </summary>
        /// <param name="typeName">The name of the type to select</param>
        public void SelectType(string typeName)
        {
            // make sure the type, the user wants to select is valid
            RefreshTypes();
            var foundType = Types.FirstOrDefault(type => type.FullName == typeName);
            if (foundType == null)
            {
                // TODO: error ui
                // Messages.Add($"Given type with the name '{typeName}' not found in '{SelectedAssembly.GetName().Name}'");
                return;
            }

            SelectedType = foundType;
        }

        /// <summary>
        /// Refresh the members on the selected type
        /// </summary>
        public void RefreshMembers()
        {
            // make sure a type is selected
            if(SelectedType == null)
            {
                Members.Clear();

                // TODO: error ui
                // Messages.Add("No type is currently selected. Please select a type to explore its members");
                return;
            }

            // add all members in the selected type into the members
            Members = SelectedType.GetMembers().ToList();
        }

        public void Insert(int index, object value) { }
        public void Remove(object value) { }
        public void RemoveAt(int index) { }
        public void CopyTo(Array array, int index) { }

        public int Add(object value) { return -1; }

        public IEnumerator GetEnumerator() { return null; }
        public int IndexOf(object value)   { return -1; }
        public bool Contains(object value) { return false; }

        public void Clear() 
        {
            Assemblies.Clear();
            Types.Clear();
            Members.Clear();
        }
    }
}
