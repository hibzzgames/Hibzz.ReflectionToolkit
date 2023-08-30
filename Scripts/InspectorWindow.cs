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
                if(e.keyCode != KeyCode.Return) { return; }
                
                var success = OnRecieveNewCommand();
                if(success)
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
                if(e.clickCount != 2) { return; }
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
            // split and cache the command
            var command = consoleField.value.Split(" ");

            // no command is provided (and that's a valid command)
            if (command.Length <= 0) { return true; }

            // check if the user wants to use the list command
            if (command[0] == "list")
            {
                // at least one parameter is required for "list"
                if(command.Length <= 1) 
                {
                    DisplayErrorMessage("At least one parameter is required for the list command");
                    return false; 
                }

                // user wants to list the assemblies
                if (command[1] == "-a" || command[1] == "assemblies")
                {
                    inspector.RefreshAssemblies();
                    return true;
                }

                // user wants to list the types
                if (command[1] == "-t" || command[1] == "types")
                {
                    return inspector.RefreshTypes();
                }

                // user wants to list the members
                if (command[1] == "-m" || command[1] == "members")
                {
                    return inspector.RefreshMembers();
                }

                // unknown usage of list command
                DisplayErrorMessage("Invalid usage of list command");
                return false; 
            }

            // check if the user wants to use the select command
            if (command[0] == "select")
            {
                // user needs to pass two arguments, a key and the value
                if(command.Length <= 2) 
                {
                    DisplayErrorMessage("At least two parameter is required for the select command");
                    return false; 
                }

                // user wants to select an assembly
                if (command[1] == "-a" || command[1] == "assembly")
                {
                    return inspector.SelectAssembly(command[2]);
                }

                // user wants to select a type
                if (command[1] == "-t" || command[1] == "type")
                {
                    inspector.SelectType(command[2]);
                    return true;
                }

                // unknown usage of select command
                DisplayErrorMessage("Unknown usage of Select command");
                return false;
            }

            // some error occurred
            DisplayErrorMessage("Unknown Command");
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
            if(scrollToTop)
            {
                resultListView.ScrollToItem(0);
                resultListView.SetSelection(-1);
            }
        }

        readonly Color __AssemblyBadgeColor = Color.HSVToRGB(0.59f, 0.7f, 0.7f);
        readonly Color __TypeBadgeColor = Color.HSVToRGB(0.73f, 0.7f, 0.7f);

        void RefreshMajorBadges()
        {
            // clear any existing badges
            badgeContainer.Clear();

            // when an assembly is selected, show badge with assembly name
            if(inspector.SelectedAssembly == null) { return; }
            var assemblyBadge = GenerateMajorBadge(inspector.SelectedAssembly.GetName().Name, __AssemblyBadgeColor);

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
            if(inspector.SelectedType == null) { return; }
            var typeBadge = GenerateMajorBadge(inspector.SelectedType.FullName, __TypeBadgeColor);

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
            
            if(inspector.Members.Count > 0)
            {
                var member = inspector.Members[index];
                mainLabel.text = member.ToString();

                // todo: add minor badges

                return;
            }

            if(inspector.Types.Count > 0)
            {
                var type = inspector.Types[index];
                mainLabel.text = type.FullName;

                // todo: add minor badges

                return;
            }

            if(inspector.Assemblies.Count > 0)
            {
                mainLabel.text = inspector.Assemblies[index].GetName().Name;
                return;
            }
        }

        bool SelectResultAtIndex(int index)
        {
            // the inspector can't further inspect a member, not a valid request
            if(inspector.Members.Count > 0) { return false; }

            // the user wants to inspect the selected type in detail and view the members
            if(inspector.Types.Count > 0)
            {
                var type = inspector[index] as System.Type;

                inspector.SelectType(type.FullName);
                inspector.RefreshMembers();
                return true;
            }

            // the user wants to inspect the selected assembly and view its types
            if(inspector.Assemblies.Count > 0)
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
            if(string.IsNullOrWhiteSpace(text))
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
    }
}

