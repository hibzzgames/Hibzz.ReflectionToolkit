using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Hibzz.ReflectionToolkit
{
    internal class ReflectionToolkitWindow : EditorWindow
    {
        [SerializeField] VisualTreeAsset coreTreeAsset = default;
        [SerializeField] VisualTreeAsset majorBadgeAsset = default;
        [SerializeField] VisualTreeAsset minorBadgeAsset = default;
        [SerializeField] VisualTreeAsset resultItemAsset = default;

        TextField consoleField; // a reference to the console field
        VisualElement badgeContainer; // stores the badges, like current assembly and type
        ListView resultListView; // container storing the results

        // a reference to the inspector
        Inspector inspector = new Inspector();

        [MenuItem("Hibzz/Launch Reflection Inspector")]
        static void OpenReflectionToolkitWindow()
        {
            var window = GetWindow<ReflectionToolkitWindow>();
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
                if(!success)
                {
                    // TODO: error ui
                    // inspector.Messages.Add("Given command is invalid");
                }

                RefreshResultView();
            });

            // get the container for current badges
            badgeContainer = root.Q<VisualElement>("BadgeContainer");

            // get the list element that stores the results
            resultListView = root.Q<ListView>("Results");
            resultListView.makeItem = () => resultItemAsset.Instantiate();
            resultListView.bindItem = PopulateItem;
            resultListView.itemsSource = inspector;

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
                if(command.Length <= 1) { return false; }

                // user wants to list the assemblies
                if (command[1] == "-a" || command[1] == "assemblies")
                {
                    inspector.RefreshAssemblies();
                    return true;
                }

                // user wants to list the types
                if (command[1] == "-t" || command[1] == "types")
                {
                    inspector.RefreshTypes();
                    return true;
                }

                // user wants to list the members
                if (command[1] == "-m" || command[1] == "members")
                {
                    inspector.RefreshMembers();
                    return true;
                }

                return false; // failed
            }

            // check if the user wants to use the select command
            if (command[0] == "select")
            {
                // user needs to pass two arguments, a key and the value
                if(command.Length <= 2) { return false; }

                // user wants to select an assembly
                if (command[1] == "-a" || command[1] == "assembly")
                {
                    inspector.SelectAssembly(command[2]);
                    return true;
                }

                // user wants to select a type
                if (command[1] == "-t" || command[1] == "type")
                {
                    inspector.SelectType(command[2]);
                    return true;
                }

                return false; // failed
            }

            // some error occurred
            return false;
        }

        void RefreshResultView()
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
        }

        readonly Color __AssemblyBadgeColor = Color.HSVToRGB(0.59f, 0.7f, 0.7f);
        readonly Color __TypeBadgeColor = Color.HSVToRGB(0.73f, 0.7f, 0.7f);

        void RefreshMajorBadges()
        {
            // clear any existing badges
            badgeContainer.Clear();

            // when an assembly is selected, show badge with assembly name
            if(inspector.SelectedAssembly == null) { return; }
            badgeContainer.Add(GenerateMajorBadge(inspector.SelectedAssembly.GetName().Name, __AssemblyBadgeColor));

            // when a type is selected, show badge with the type name (including namespace)
            if(inspector.SelectedType == null) { return; }
            badgeContainer.Add(GenerateMajorBadge(inspector.SelectedType.FullName, __TypeBadgeColor));
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
    }
}

