using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Hibzz.ReflectionToolkit
{
    internal class InspectWindow : EditorWindow
    {
        [SerializeField] VisualTreeAsset coreTreeAsset = default;
        [SerializeField] VisualTreeAsset majorBadgeAsset = default;
        [SerializeField] VisualTreeAsset minorBadgeAsset = default;
        [SerializeField] VisualTreeAsset resultItemAsset = default;

        TextField consoleField;       // a reference to the console field
        VisualElement badgeContainer; // stores the badges, like current assembly and type
        ListView resultListView;      // container storing the results

        VisualElement warningIcon;    // a container for the warning icon

        // a reference to the inspector
        Inspector inspector = new Inspector();

        [MenuItem("Hibzz/Launch Reflection Inspector")]
        static void OpenReflectionToolkitWindow()
        {
            var window = GetWindow<InspectWindow>();
            window.titleContent = new GUIContent("Reflection Inspector");
            window.minSize = new Vector2(400, 180);
        }

        void CreateGUI()
        {
            // Each editor window contains a root VisualElement object
            VisualElement root = rootVisualElement;

            // Instantiate UXML
            VisualElement labelFromUXML = coreTreeAsset.Instantiate();
            root.Add(labelFromUXML);

            // Get the search box from the root and hook it up to the new command event
            consoleField = root.Q<TextField>("ConsoleField");
            consoleField.RegisterCallback<KeyUpEvent>(e =>
            {
                if (e.keyCode != KeyCode.Return) { return; }

                var success = OnRecieveNewCommand();
                if (success)
                {
                    HideErrorMessage();
                }

                RefreshResultView(scrollToTop: success);
            });

            // get the container for current badges
            badgeContainer = root.Q<VisualElement>("BadgeContainer");

            // get the list element that stores the results
            resultListView = root.Q<ListView>("Results");
            resultListView.makeItem = () => resultItemAsset.Instantiate();
            resultListView.bindItem = PopulateItem;
            resultListView.itemsSource = inspector;

            // add callbacks for selecting things when double clicking listed elements
            resultListView.RegisterCallback<MouseDownEvent>(e =>
            {
                // not a double click, ignore
                if (e.clickCount != 2) { return; }
                bool success = SelectResultAtIndex(resultListView.selectedIndex);
                RefreshResultView(scrollToTop: success);
                HideErrorMessage();
            });

            // get the container for the warning icon
            warningIcon = root.Q<VisualElement>("WarningIcon");

            // setup the cyclic reference... uuuuggghhh
            inspector.inspectorWindow = this;

            // by default show the list of assemblies (if no assembly is selected)
            inspector.RefreshAssemblies();

            // refresh the result view
            RefreshResultView();
        }

        bool OnRecieveNewCommand()
        {
            // process the commands (empty field is a valid command)
            var command = new Command(consoleField.value);
            if(command.Primary == null) { return true; }

            // the primary command involves listing assemblies
            if (command.Primary == "assemblies")
            {
                inspector.RefreshAssemblies();
                return true;
            }

            // the primary command to list all types in selected assembly
            if(command.Primary == "types")
            {
                // check for any parameter with the key -a
                // this represents the user's intent to select an assembly with the given name
                if(command.Parameters.ContainsKey("-a"))
                {
                    var assembly = command.Parameters["-a"];
                    var success = inspector.SelectAssembly(assembly);

                    // the selection operation failed, report that to the user
                    if(!success) { return false; }
                }

                // refresh and return if the operation was successful
                return inspector.RefreshTypes();
            }

            // todo: members
            // the primary command to list all members in a selected type
            if(command.Primary == "members")
            {
                // check for any parameter with the key -a
                // this represents the user's intent to select an assembly with the given name
                if (command.Parameters.ContainsKey("-a"))
                {
                    var assembly = command.Parameters["-a"];
                    var success = inspector.SelectAssembly(assembly);

                    // the selection operation failed, report that to the user
                    if (!success) { return false; }
                }

                // check for any parameter with the key -t
                // this represents the user's intent to select a type with the given name
                if (command.Parameters.ContainsKey("-t"))
                {
                    var type = command.Parameters["-t"];
                    var success = inspector.SelectType(type);

                    // the selection operation failed, report that to the user
                    if(!success) { return false; }
                }

                // refresh and return if the process was successful
                return inspector.RefreshMembers();
            }

            // some error occurred
            DisplayErrorMessage($"Unknown Command '{command.Primary}'");
            return false;
        }

        void RefreshResultView(bool scrollToTop = true)
        {
            // no results are there in our queue... hide the result view
            if (inspector.Count <= 0)
            {
                resultListView.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);
                return;
            }

            // refresh major badges representing selected elements
            RefreshMajorBadges();

            // there's stuff to display, make sure it's visible
            resultListView.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.Flex);
            resultListView.RefreshItems();

            // set the position back to 0
            if (scrollToTop)
            {
                resultListView.ScrollToItem(0);
                resultListView.SetSelection(-1);
            }
        }

        void RefreshMajorBadges()
        {
            // clear any existing badges
            badgeContainer.Clear();

            // when an assembly is selected, show badge with assembly name
            if (inspector.SelectedAssembly == null) { return; }
            var assemblyBadge = GenerateMajorBadge(inspector.SelectedAssembly.GetName().Name, ColorScheme.Assembly);

            // when clicking the assembly badge, show a list of all badges
            assemblyBadge.RegisterCallback<MouseDownEvent>(e =>
            {
                // get the index of the selected assembly in a list of assemblies
                var assemblyIndex = inspector.Assemblies.IndexOf(inspector.SelectedAssembly);

                inspector.RefreshAssemblies();
                RefreshResultView(false);

                // focus on the selected assembly index
                EventCallback<GeometryChangedEvent> focusSelectionHandler = null;
                focusSelectionHandler = (e) =>
                {
                    resultListView.UnregisterCallback(focusSelectionHandler);
                    resultListView.ScrollToItem(assemblyIndex);
                    resultListView.SetSelection(assemblyIndex);
                };

                resultListView.RegisterCallback(focusSelectionHandler);
            });

            badgeContainer.Add(assemblyBadge);

            // when a type is selected, show badge with the type name (including namespace)
            if (inspector.SelectedType == null) { return; }
            var typeBadge = GenerateMajorBadge(inspector.SelectedType.FullName, ColorScheme.Type);

            // when clicking the type badge, show a list of all types in the assembly that the selected type is part of
            typeBadge.RegisterCallback<MouseDownEvent>(e =>
            {
                // get the index of the selected type in a list of types
                var typeIndex = inspector.Types.IndexOf(inspector.SelectedType);

                inspector.RefreshTypes();
                RefreshResultView();

                // focus on the selected type index
                EventCallback<GeometryChangedEvent> focusSelectionHandler = null;
                focusSelectionHandler = (e) =>
                {
                    resultListView.UnregisterCallback(focusSelectionHandler);
                    resultListView.ScrollToItem(typeIndex);
                    resultListView.SetSelection(typeIndex);
                };

                resultListView.RegisterCallback(focusSelectionHandler);
            });

            badgeContainer.Add(typeBadge);
        }

        VisualElement GenerateMajorBadge(string text, Color color)
        {
            // instantiate the badge
            var badge = majorBadgeAsset.Instantiate();

            // change text and color
            var label = badge.Q<Label>("Text");
            label.text = text;
            label.style.backgroundColor = new StyleColor(color);

            // export out
            return badge;
        }

        void PopulateItem(VisualElement element, int index)
        {
            var mainLabel = element.Q<Label>("MainLabel");
            var badgeContainer = element.Q<VisualElement>("Badges");

            if (inspector.Members.Count > 0)
            {
                // update the text on the label
                var member = inspector.Members[index];
                mainLabel.text = member.ToString();

                // start by clearning the badge container of any existing badges
                badgeContainer.Clear();

                // member is a field
                if(member.MemberType == MemberTypes.Field)
                {
                    // mark the member as field
                    var field = member as FieldInfo;
                    AddBadge("variable", ColorScheme.Field, badgeContainer);

                    // get the access modifiers (public, protected, private, etc.)
                    if (field.IsPublic)                 { AddBadge("public",             ColorScheme.Public,            badgeContainer); }
                    else if (field.IsPrivate)           { AddBadge("private",            ColorScheme.Private,           badgeContainer); }
                    else if (field.IsFamily)            { AddBadge("protected",          ColorScheme.Protected,         badgeContainer); }
                    else if (field.IsFamilyAndAssembly) { AddBadge("private protected",  ColorScheme.PrivateProtected,  badgeContainer); }
                    else if (field.IsAssembly)          { AddBadge("internal",           ColorScheme.Internal,          badgeContainer); }
                    else if (field.IsFamilyOrAssembly)  { AddBadge("protected internal", ColorScheme.ProtectedInternal, badgeContainer); }

                    // is it static?
                    if (field.IsStatic) { AddBadge("static", ColorScheme.Static, badgeContainer); }
                }

                // member is a property
                else if(member.MemberType == MemberTypes.Property)
                {
                    // mark the member as a property
                    var property = member as PropertyInfo;
                    AddBadge("property", ColorScheme.Property, badgeContainer);

                    // property "get" access modifier
                    var getter = property.GetMethod;
                    if (getter != null)
                    {
                        if (getter.IsPublic)                 { AddBadge("public get",             ColorScheme.Public,            badgeContainer); }
                        else if (getter.IsPrivate)           { AddBadge("private get",            ColorScheme.Private,           badgeContainer); }
                        else if (getter.IsFamily)            { AddBadge("protected get",          ColorScheme.Protected,         badgeContainer); }
                        else if (getter.IsFamilyAndAssembly) { AddBadge("private protected get",  ColorScheme.PrivateProtected,  badgeContainer); }
                        else if (getter.IsAssembly)          { AddBadge("internal get",           ColorScheme.Internal,          badgeContainer); }
                        else if (getter.IsFamilyOrAssembly)  { AddBadge("protected internal get", ColorScheme.ProtectedInternal, badgeContainer); }
                    }

                    // property "set" access modifier
                    var setter = property.SetMethod;
                    if(setter != null)
                    {
                        if (setter.IsPublic)                 { AddBadge("public set",             ColorScheme.Public,            badgeContainer); }
                        else if (setter.IsPrivate)           { AddBadge("private set",            ColorScheme.Private,           badgeContainer); }
                        else if (setter.IsFamily)            { AddBadge("protected set",          ColorScheme.Protected,         badgeContainer); }
                        else if (setter.IsFamilyAndAssembly) { AddBadge("private protected set",  ColorScheme.PrivateProtected,  badgeContainer); }
                        else if (setter.IsAssembly)          { AddBadge("internal set",           ColorScheme.Internal,          badgeContainer); }
                        else if (setter.IsFamilyOrAssembly)  { AddBadge("protected internal set", ColorScheme.ProtectedInternal, badgeContainer); }
                    }

                    // is it static?
                    if ((getter != null && getter.IsStatic) || (setter != null && setter.IsStatic))
                    {
                        AddBadge("static", ColorScheme.Static, badgeContainer);
                    }
                }

                // member is a method
                else if(member.MemberType == MemberTypes.Method)
                {
                    // mark the member as method
                    var method = member as MethodInfo;
                    AddBadge("method", ColorScheme.Method, badgeContainer);

                    // get the access modifier
                    if (method.IsPublic)                 { AddBadge("public",             ColorScheme.Public,            badgeContainer); }
                    else if (method.IsPrivate)           { AddBadge("private",            ColorScheme.Private,           badgeContainer); }
                    else if (method.IsFamily)            { AddBadge("protected",          ColorScheme.Protected,         badgeContainer); }
                    else if (method.IsFamilyAndAssembly) { AddBadge("private protected",  ColorScheme.PrivateProtected,  badgeContainer); }
                    else if (method.IsAssembly)          { AddBadge("internal",           ColorScheme.Internal,          badgeContainer); }
                    else if (method.IsFamilyOrAssembly)  { AddBadge("protected internal", ColorScheme.ProtectedInternal, badgeContainer); }

                    // is it static?
                    if (method.IsStatic) { AddBadge("static", ColorScheme.Static, badgeContainer); }
                }

                // member is a constructor
                else if(member.MemberType == MemberTypes.Constructor)
                {
                    // mark the member as a constructor
                    var constructor = member as ConstructorInfo;
                    AddBadge("constructor", ColorScheme.Constructor, badgeContainer);

                    // get the access modifier
                    if (constructor.IsPublic)                 { AddBadge("public",             ColorScheme.Public,            badgeContainer); }
                    else if (constructor.IsPrivate)           { AddBadge("private",            ColorScheme.Private,           badgeContainer); }
                    else if (constructor.IsFamily)            { AddBadge("protected",          ColorScheme.Protected,         badgeContainer); }
                    else if (constructor.IsFamilyAndAssembly) { AddBadge("private protected",  ColorScheme.PrivateProtected,  badgeContainer); }
                    else if (constructor.IsAssembly)          { AddBadge("internal",           ColorScheme.Internal,          badgeContainer); }
                    else if (constructor.IsFamilyOrAssembly)  { AddBadge("protected internal", ColorScheme.ProtectedInternal, badgeContainer); }
                }

                // member is an event
                else if(member.MemberType == MemberTypes.Event)
                {
                    // mark the member as an event
                    AddBadge("event", ColorScheme.Event, badgeContainer);
                }

                return;
            }

            if (inspector.Types.Count > 0)
            {
                // update the text on the label
                var type = inspector.Types[index];
                mainLabel.text = type.FullName;

                // clear the badge container and prepare to add the access modifier badges to it
                badgeContainer.Clear();

                // display a badge that it's public or not
                if (type.IsPublic) { AddBadge("public",   ColorScheme.Public, badgeContainer); }
                else               { AddBadge("internal", ColorScheme.Internal, badgeContainer); }

                // add badge for static, abstract, or sealed flags
                // c# represents a static class as being both abstract and sealed
                if (type.IsAbstract && type.IsSealed) { AddBadge("static",   ColorScheme.Static, badgeContainer); }
                else if (type.IsAbstract)             { AddBadge("abstract", ColorScheme.Abstract, badgeContainer); }
                else if (type.IsSealed)               { AddBadge("sealed",   ColorScheme.Sealed, badgeContainer); }

                return;
            }

            if (inspector.Assemblies.Count > 0)
            {
                mainLabel.text = inspector.Assemblies[index].GetName().Name;
                badgeContainer.Clear();
                return;
            }
        }

        bool SelectResultAtIndex(int index)
        {
            // the inspector can't further inspect a member, not a valid request
            if (inspector.Members.Count > 0) { return false; }

            // the user wants to inspect the selected type in detail and view the members
            if (inspector.Types.Count > 0)
            {
                var type = inspector[index] as System.Type;

                inspector.SelectType(type.FullName);
                inspector.RefreshMembers();
                return true;
            }

            // the user wants to inspect the selected assembly and view its types
            if (inspector.Assemblies.Count > 0)
            {
                var assembly = inspector[index] as Assembly;

                inspector.SelectAssembly(assembly.GetName().Name);
                inspector.RefreshTypes();

                return true;
            }

            // some random issue
            return false;
        }

        public void DisplayErrorMessage(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                HideErrorMessage();
                return;
            }

            warningIcon.style.display = DisplayStyle.Flex;
            warningIcon.tooltip = text;
        }

        public void HideErrorMessage()
        {
            warningIcon.style.display = DisplayStyle.None;
        }

        void AddBadge(string text, Color color, VisualElement container)
        {
            // instantiate and get the reference to the badge elements
            var badge = minorBadgeAsset.Instantiate();
            var label = badge.Q<Label>("Text");

            // update the text and color
            label.text = text;
            label.style.backgroundColor = color;

            // add it to the container
            container.Add(badge);
        }
    }
}

