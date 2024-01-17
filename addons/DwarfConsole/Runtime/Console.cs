using Godot;
using System.Collections.Generic;

// TODO: changing font size

namespace DwarfConsole
{
    public partial class Console : Node
    {
        public struct Settings
        {
            private Color normalColor = new Color("white");
            private Color warningColor = new Color("#ebcb2f");
            private Color errorColor = new Color("#eb4b2f");

            public Color NormalColor
            {
                get
                {
                    return (Color)ProjectSettings.GetSetting("DwarfConsole/Defaults/NormalColor", normalColor);
                }
            }

            public Color WarningColor
            {
                get
                {
                    return (Color)ProjectSettings.GetSetting("DwarfConsole/Defaults/WarningColor", warningColor);
                }
            }

            public Color ErrorColor
            {
                get
                {
                    return (Color)ProjectSettings.GetSetting("DwarfConsole/Defaults/ErrorColor", errorColor);
                }
            }

            public Settings() {}
        }


        public enum LogType { Normal, Warning, Error }

        public static readonly Settings settings = new Settings();
        public static readonly CommandExecutor CommandExecutor = new CommandExecutor();
        
        private static Node messageContainer;
        private static LineEdit inputField;
        private static ScrollContainer scrollContainer;

        private static List<string> commandHistory = new List<string>();
        private static int historyPreviewIndex = 0;

        private static int maxMessages = 1000;
        private static bool isActive = false;

        public static int MaxMessages
        {
            get => maxMessages;
            set => maxMessages = Mathf.Max(maxMessages, 100);
        }

        public static bool IsActive
        {
            get => isActive;
        }

        private Console()
        {
            CommandExecutor.RegisterCommand("clear", new Command(this, typeof(Console).GetMethod(nameof(ClearConsole)), "Clears the console."));
        }

        private void Init(Node root)
        {
            TreeEntered += () => CallDeferred(nameof(OnEnterTreeAndReady));
            TreeExiting += () => CallDeferred(nameof(OnExitTreeAndReady));

            messageContainer = FindChild("MessageContainer");
            scrollContainer = FindChild("ScrollContainer") as ScrollContainer;
            scrollContainer.GetVScrollBar().Changed += ScrollToBottom;
            inputField = FindChild("LineEdit") as LineEdit;
            inputField.TextSubmitted += SubmitInput;

            foreach (var child in root.GetChildren())
            {
                CommandExecutor.TryRegisterNode(child);
            }
        }
        
        private void OnEnterTreeAndReady()
        {
            InitializeExecutor();
            inputField?.GrabFocus();
            isActive = true;
        }

        private void OnExitTreeAndReady()
        {
            inputField.Clear();
            isActive = false;
        }

        Node currentScene;
        private void InitializeExecutor()
        {
            if (GetTree().CurrentScene != currentScene)
            {
                CommandExecutor.TryUnregisterNode(currentScene);
                currentScene = GetTree().CurrentScene;
            }
            else return;

            AutoRegisterConsoleCommands(currentScene);

            currentScene.ChildEnteredTree += (node) =>
            {
                if (node != this)
                {
                    CommandExecutor.TryRegisterNode(node);
                }
            };

            currentScene.ChildExitingTree += (node) =>
            {
                if (node != this)
                {
                    CommandExecutor.TryUnregisterNode(node);
                }
            };
        }

        private void AutoRegisterConsoleCommands(Node root)
        {
            if (!IsInstanceValid(root))
                return;

            Queue<Node> nodesQueue = new Queue<Node>();
            nodesQueue.Enqueue(root);

            while (nodesQueue.Count > 0)
            {
                Node currentNode = nodesQueue.Dequeue();

                CommandExecutor.TryRegisterNode(currentNode);

                foreach (Node child in currentNode.GetChildren())
                {
                    if (child == this) continue;
                    nodesQueue.Enqueue(child);
                }
            }
        }

        public override void _Process(double delta)
        {
            if (Input.IsActionJustPressed("ui_up") && isActive)
            {
                historyPreviewIndex = Mathf.Max(0, historyPreviewIndex - 1);
                InputCommandFromHistory();
            }

            if (Input.IsActionJustPressed("ui_down") && isActive)
            {
                historyPreviewIndex = Mathf.Min(commandHistory.Count, historyPreviewIndex + 1);
                InputCommandFromHistory();
            }
        }

        private static string lastMsg = "";
        public static void Log(object value, LogType logType = LogType.Normal)
        {
            string msg = value.ToString();
            Node message;
            if (!string.IsNullOrEmpty(lastMsg.Replace(">> ", "")) && lastMsg == msg)
            {
                if (messageContainer.GetChildCount() == 0) return;
                message = messageContainer.GetChild(Mathf.Max(messageContainer.GetChildCount() - 1, 0));
                Label counter = message.FindChild("Counter") as Label;
                counter.Text = $"{counter.Text[0]} {int.Parse(counter.Text.Substring(1, counter.Text.Length - 1)) + 1}";
                counter.Visible = true;
                return;
            }

            message = ResourceLoader.Load<PackedScene>("res://addons/DwarfConsole/Prefabs/message.tscn").Instantiate();
            LineEdit content = message.FindChild("Content") as LineEdit;
            content.Text = msg;
            lastMsg = msg;
            messageContainer.AddChild(message);
            

            if (messageContainer.GetChildCount() > maxMessages)
                messageContainer.GetChild(0).QueueFree();

            switch (logType)
            {
                case LogType.Normal:
                    content.Modulate = settings.NormalColor;
                    break;
                case LogType.Warning:
                    content.Modulate = settings.WarningColor;
                    break;
                case LogType.Error:
                    content.Modulate = settings.ErrorColor;
                    break;
                default:
                    content.Modulate = settings.NormalColor;
                    break;
            }
        }

        private void SubmitInput(string content)
        {
            inputField.Clear();

            string command = content.Trim();
            if (!string.IsNullOrEmpty(command) && (commandHistory.Count == 0 || commandHistory[commandHistory.Count - 1] != command))
                commandHistory.Add(command);
            historyPreviewIndex = commandHistory.Count;
            Log($">> {command}");
            CommandExecutor.ExecuteCommand(command);
        }

        private void InputCommandFromHistory()
        {
            if (historyPreviewIndex < 0 || historyPreviewIndex >= commandHistory.Count)
                return;
            inputField.Text = commandHistory[historyPreviewIndex];
        }

        private double prevMaxValue;
        private void ScrollToBottom()
        {
            var scrollBar = scrollContainer.GetVScrollBar();
            bool shouldScroll = (prevMaxValue - scrollBar.GetRect().Size.Y - scrollBar.Value) <= 0;
            prevMaxValue = scrollBar.MaxValue;
            if (shouldScroll)
                scrollBar.Value = scrollBar.MaxValue;
        }

        public void ClearConsole()
        {
            foreach (var child in messageContainer.GetChildren())
            {
                child.QueueFree();
            }
        }
    }
}
